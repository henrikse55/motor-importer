using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using Humanizer;
using Importer.Zip;

namespace Importer.FasterKv;

public class RandomAccessReader
{
    private readonly ILogger<RandomAccessReader> _logger;
    private readonly XmlLogStorage _logStorage;

    public RandomAccessReader(
        ILogger<RandomAccessReader> logger,
        XmlLogStorage logStorage)
    {
        _logger = logger;
        _logStorage = logStorage;
    }

    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        PipeOptions pipeOptions = new PipeOptions(minimumSegmentSize: 4096 * 4096);
        var file = File.OpenRead("/storage/motor/data.zip");
        var zipStream = new StreamableZipFile(file);
        
        Pipe pipe = new Pipe(pipeOptions);
        Task write = FillPipe(pipe.Writer, zipStream.GetStream(), stoppingToken);
        Task read = ReadPipe(pipe.Reader, stoppingToken);

        await Task.WhenAll(write, read).ConfigureAwait(false);
        _logStorage.Commit(true);
    }
    

    private async Task FillPipe(PipeWriter writer, Stream stream, CancellationToken token = default)
    {
        long totalRead = 0;
        const int minimalSize = 4096 * 2048;
        while (!token.IsCancellationRequested)
        {
            Memory<byte> buffer = writer.GetMemory(minimalSize);

            int bytesRead = await stream.ReadAsync(buffer, token).ConfigureAwait(false);

            if (bytesRead == 0)
            {
                break;
            }

            // totalRead += bytesRead;
            // _logger.LogTotalRead(totalRead.Bytes().Gigabytes);

            writer.Advance(bytesRead);

            FlushResult flushResult = await writer.FlushAsync(token).ConfigureAwait(false);
            if (flushResult.IsCompleted)
            {
                break;
            }
        }

        await writer.CompleteAsync().ConfigureAwait(false);
        _logger.LogInformation("Thread({ThreadId}) FillPipe has completed", Environment.CurrentManagedThreadId);
    }
    
    private async Task ReadPipe(PipeReader reader, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            ReadResult result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            ReadOnlySequence<byte> buffer = result.Buffer;

            SequencePosition position = ScanForDelimiter(buffer);

            reader.AdvanceTo(position, buffer.End);

            if (result.IsCompleted)
            {
                break;
            }
        }
        
        await reader.CompleteAsync().ConfigureAwait(false);
        _logger.LogInformation("Thread({ThreadId}) ReadPipe has completed", Environment.CurrentManagedThreadId);
    }
    
    [SkipLocalsInit]
    private SequencePosition ScanForDelimiter(ReadOnlySequence<byte> sequence)
    {
        long totalBytesConsumed = 0;
        int scanLoopCounter = 0;
        
        SequenceReader<byte> reader = new(sequence);
        // List<ReadOnlySequence<byte>> xmlEntries = new List<ReadOnlySequence<byte>>(1024);
        while (reader.TryReadTo(out ReadOnlySequence<byte> xmlEntry, "</ns:Statistik>"u8))
        {
            // xmlEntries.Add(xmlEntry);
            _logStorage.Enqueue(xmlEntry);
            totalBytesConsumed += xmlEntry.Length;
            scanLoopCounter++;
        }
        _logStorage.Commit();
        // _logStorage.Enqueue(xmlEntries);

        MotorMeter.Log.ReportScanLoop(scanLoopCounter);
        MotorMeter.Log.ReportBytesWritten(totalBytesConsumed);

        return reader.Position;
    }
}

public static partial class RandomAccessReaderLogging
{
    [LoggerMessage(
        level: LogLevel.Information,
        message: "A total of {TotalReadInGigabytes} have been read", eventId: 1000)]
    public static partial void LogTotalRead(this ILogger logger, double totalReadInGigabytes);
}