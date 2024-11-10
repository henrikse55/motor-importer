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

    public async Task<long> Read(Stream xmlStream)
    {
        var pipeReader = PipeReader.Create(xmlStream);
        Task read = ReadPipe(pipeReader);
            
        await read.ConfigureAwait(false);
        return 0;
    }

    private async Task ReadPipe(PipeReader reader)
    {
        while (!_cancellationToken.IsCancellationRequested)
        {
            ReadResult result = await reader.ReadAsync(_cancellationToken).ConfigureAwait(false);
            var buffer = result.Buffer;

            SequencePosition position = ScanForDelimiter(buffer);
            reader.AdvanceTo(position, buffer.End);
            
            CommitScan();

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
            PresentEntry(ref xmlEntry);
        }
        return reader.Position;
    }

    /// <summary>
    /// Invoked on each xml entry found
    /// </summary>
    protected abstract void PresentEntry(ref ReadOnlySequence<byte> entry);

    protected abstract void CommitScan();
}