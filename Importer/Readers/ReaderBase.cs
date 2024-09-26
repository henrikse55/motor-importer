using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Importer.Readers;

public abstract class ReaderBase
{
    protected readonly CancellationToken _cancellationToken;

    protected ReaderBase(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
    }

    public async Task Read(Stream xmlStream)
    {
        Pipe pipe = new();
        Task write = FillPipe(pipe.Writer, xmlStream);
        Task read = ReadPipe(pipe.Reader);
            
        await Task.WhenAll(write, read).ConfigureAwait(false);
    }

    private async Task FillPipe(PipeWriter writer, Stream stream)
    {
        const int minimalSize = 4096 * 4096;
        while (!_cancellationToken.IsCancellationRequested)
        {
            Memory<byte> buffer = writer.GetMemory(minimalSize);

            int bytesRead = await stream.ReadAsync(buffer, _cancellationToken).ConfigureAwait(false);

            if (bytesRead == 0)
            {
                break;
            }

            writer.Advance(bytesRead);

            FlushResult flushResult = await writer.FlushAsync(_cancellationToken).ConfigureAwait(false);
            if (flushResult.IsCompleted)
            {
                break;
            }
        }
        await writer.CompleteAsync().ConfigureAwait(false);
    }

    private async Task ReadPipe(PipeReader reader)
    {
        while (!_cancellationToken.IsCancellationRequested)
        {
            ReadResult result = await reader.ReadAsync(_cancellationToken).ConfigureAwait(false);
            var buffer = result.Buffer;

            SequencePosition position = ScanForDelimiter(buffer);
            reader.AdvanceTo(position, buffer.End);

            if (result.IsCompleted)
            {
                break;
            }
        }
        
        await reader.CompleteAsync().ConfigureAwait(false);
    }

    private SequencePosition ScanForDelimiter(ReadOnlySequence<byte> sequence)
    {
        SequenceReader<byte> reader = new(sequence);
        while (reader.TryReadTo(out ReadOnlySequence<byte> xmlEntry, "</ns:Statistik>"u8))
        {
            // MemoryOwner<byte> memory = xmlEntry.CopyToMemoryOwner();
            PresentEntry(xmlEntry);
        }
        return reader.Position;
    }

    /// <summary>
    /// Invoked on each xml entry found
    /// </summary>
    protected abstract void PresentEntry(ReadOnlySequence<byte> entry);
}