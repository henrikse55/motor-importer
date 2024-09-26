using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using FASTER.core;

namespace Perf.Utils;

public class ReaderTester
{
    private int _limit;
    private readonly CancellationTokenSource _tokenSource = new();
    private readonly CancellationToken _cancellationToken;

    private readonly FasterLogSettings _settings = new(null);
    private readonly FasterLog _log;

    public ReaderTester(int limit)
    {
        _limit = limit;
        _cancellationToken = _tokenSource.Token;
        _log = new FasterLog(_settings);
    }
        
    public async Task Run()
    {
        await using Stream file = File.OpenRead("/Storage/motor/data.xml");
        await ReadNoBatching(file);
    }
    
    public async Task RunSimpleBatching()
    {
        await using Stream file = File.OpenRead("/Storage/motor/data.xml");
        await ReadSimpleBatching(file);
    }
        
    public async Task ReadNoBatching(Stream xmlStream)
    {
        Pipe pipe = new();
        Task write = FillPipe(pipe.Writer, xmlStream);
        Task read = ReadPipe(pipe.Reader);

        await Task.WhenAll(write, read);
    }
    
    public async Task ReadSimpleBatching(Stream xmlStream)
    {
        Pipe pipe = new();
        Task write = FillPipe(pipe.Writer, xmlStream);
        Task read = ReadPipeSimpleBatching(pipe.Reader);

        await Task.WhenAll(write, read);
    }

    private async Task FillPipe(PipeWriter writer, Stream stream)
    {
        while (!_cancellationToken.IsCancellationRequested && _limit <= 0)
        {
            Memory<byte> buffer = writer.GetMemory(4096);

            int bytesRead = await stream.ReadAsync(buffer, _cancellationToken);

            if (bytesRead == 0)
            {
                break;
            }

            writer.Advance(bytesRead);

            FlushResult flushResult = await writer.FlushAsync(_cancellationToken);
            if (flushResult.IsCompleted)
            {
                break;
            }
        }
        await writer.CompleteAsync();
    }

    private async Task ReadPipe(PipeReader reader)
    {
        int commitInterval = 20_000_0;
        while (!_cancellationToken.IsCancellationRequested && _limit <= 0)
        {
            ReadResult result = await reader.ReadAsync(_cancellationToken);
            ReadOnlySequence<byte> buffer = result.Buffer;

            SequencePosition position = ScanForDelimiter(buffer);

            if (--commitInterval <= 0)
            {
                commitInterval = 20_000_0;
                await _log.CommitAsync(_cancellationToken);
            }
            
            reader.AdvanceTo(position, buffer.End);

            if (result.IsCompleted)
            {
                break;
            }
        }

        await reader.CompleteAsync();
    }
    
    private async Task ReadPipeSimpleBatching(PipeReader reader)
    {
        int commitInterval = 20_000_0;
        while (!_cancellationToken.IsCancellationRequested && _limit <= 0)
        {
            ReadResult result = await reader.ReadAsync(_cancellationToken);
            ReadOnlySequence<byte> buffer = result.Buffer;

            SequencePosition position = ScanForDelimiterWithSimpleBatching(buffer);

            if (--commitInterval <= 0)
            {
                commitInterval = 20_000_0;
                await _log.CommitAsync(_cancellationToken);
            }
            
            reader.AdvanceTo(position, buffer.End);

            if (result.IsCompleted)
            {
                break;
            }
        }

        await reader.CompleteAsync();
    }
        
    private SequencePosition ScanForDelimiter(ReadOnlySequence<byte> sequence)
    {
        throw new NotImplementedException();

        // SequenceReader<byte> reader = new(sequence);
        //
        // while (reader.TryReadTo(out ReadOnlySequence<byte> xmlEntry, Constants.EndingTagBytes) && !_cancellationToken.IsCancellationRequested)
        // {
        //     _limit--;
        //     Span<byte> buffer = GC.AllocateUninitializedArray<byte>((int)xmlEntry.Length);
        //     
        //     xmlEntry.CopyTo(buffer);
        //     _log.Enqueue(buffer);
        // }
        // return reader.Position;
    }
    
    private SequencePosition ScanForDelimiterWithSimpleBatching(ReadOnlySequence<byte> sequence)
    {
        throw new NotImplementedException();
        // SequenceReader<byte> reader = new(sequence);
        //
        // SimpleSpanBatch batch = new();
        //
        // while (reader.TryReadTo(out ReadOnlySequence<byte> xmlEntry, Constants.EndingTagBytes) && !_cancellationToken.IsCancellationRequested)
        // {
        //     _limit--;
        //     batch.Add(xmlEntry);
        // }
        //
        // _log.Enqueue(batch);
        // return reader.Position;
    }
}