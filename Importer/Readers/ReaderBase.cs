using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.Extensions.ObjectPool;

namespace Importer.Readers;

public abstract class ReaderBase
{
    protected readonly CancellationToken _cancellationToken;
    private readonly ObjectPool<XmlBatchItem> _batchItemPool =
        new DefaultObjectPool<XmlBatchItem>(new DefaultPooledObjectPolicy<XmlBatchItem>());

    private readonly Channel<XmlBatchItem> _channel = Channel.CreateUnbounded<XmlBatchItem>();

    protected ReaderBase(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
    }

    public async Task Read(Stream xmlStream)
    {
        Pipe pipe = new();
        Task write = FillPipe(pipe.Writer, xmlStream);
        Task read = ReadPipe(pipe.Reader);
        Task present = PresentFromChannel();
            
        await Task.WhenAll(write, read, present).ConfigureAwait(false);
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

            SequencePosition position = ScanForDelimiterBatch(buffer, out var batchItem);
            reader.AdvanceTo(position, buffer.End);
            
            await _channel.Writer.WriteAsync(batchItem, _cancellationToken).ConfigureAwait(false);
            
            CommitScan();

            if (result.IsCompleted)
            {
                break;
            }
        }
        
        await reader.CompleteAsync().ConfigureAwait(false);
    }

    private SequencePosition ScanForDelimiterBatch(ReadOnlySequence<byte> sequence, out XmlBatchItem batchItem)
    {
        batchItem = _batchItemPool.Get();
        
        SequenceReader<byte> reader = new(sequence);
        while (reader.TryReadTo(out ReadOnlySequence<byte> xmlEntry, "</ns:Statistik>"u8))
        {
            batchItem.Write(xmlEntry);
        }
        return reader.Position;
    }

    private async Task PresentFromChannel()
    {
        await foreach(var item in _channel.Reader.ReadAllAsync(_cancellationToken).ConfigureAwait(false))
        {
            await PresentEntry(item).ConfigureAwait(false);
            _batchItemPool.Return(item);
        }
    }

    /// <summary>
    /// Invoked on each xml entry found
    /// </summary>
    // protected abstract void PresentEntry(ref ReadOnlySequence<byte> entry);
    protected abstract Task PresentEntry(XmlBatchItem entry);

    protected abstract void CommitScan();
}