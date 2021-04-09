using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Importer.Metrics.Counters;
using Microsoft.Toolkit.HighPerformance.Buffers;
using static Importer.Constants;

namespace Importer
{
    public class MotorReader
    {
        private readonly ChannelWriter<ReaderBatchResult> _resultChannel;
        private readonly CancellationToken _cancellationToken;

        public static Task ReadXmlFromStream(ChannelWriter<ReaderBatchResult> resultChannel, Stream xmlStream, CancellationToken token = default) 
            => new MotorReader(resultChannel, token).Read(xmlStream);

        public MotorReader(ChannelWriter<ReaderBatchResult> resultChannel, CancellationToken cancellationToken)
        {
            _resultChannel = resultChannel;
            _cancellationToken = cancellationToken;
        }

        public async Task Read(Stream xmlStream)
        {
            Pipe pipe = new Pipe();
            Task write = FillPipe(pipe.Writer, xmlStream);
            Task read = ReadPipe(pipe.Reader);
            
            await Task.WhenAll(write, read);
        }

        private async Task FillPipe(PipeWriter writer, Stream stream)
        {
            const int minimalSize = 1024 * 1024;
            while (!_cancellationToken.IsCancellationRequested)
            {
                Memory<byte> buffer = writer.GetMemory(minimalSize);

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
            while (!_cancellationToken.IsCancellationRequested)
            {
                ReadResult result = await reader.ReadAsync(_cancellationToken);
                var buffer = result.Buffer;

                var stopwatch = MotorPipeEventSource.Log.ScanForXmlStart();
                
                SequencePosition position = ScanForDelimiter(buffer);
                reader.AdvanceTo(position, buffer.End);
                
                MotorPipeEventSource.Log.ScanForXmlStop(stopwatch.Value);

                if (result.IsCompleted)
                {
                    break;
                }
            }
            await reader.CompleteAsync();
        }
        
        private SequencePosition ScanForDelimiter(ReadOnlySequence<byte> sequence)
        {
            SequenceReader<byte> reader = new SequenceReader<byte>(sequence);
            List<MemoryOwner<byte>> batchResults = new List<MemoryOwner<byte>>(250);
            while (true)
            {
                if (reader.TryReadTo(out ReadOnlySequence<byte> xmlEntry, EndingTagBytes))
                {
                    MemoryOwner<byte> memory = GetMemoryCopy(xmlEntry);
                    batchResults.Add(memory);
                }
                else
                {
                    break;
                }
            }
            PresentReaderBatch(new ReaderBatchResult(batchResults));
            return reader.Position;
        }

        private static MemoryOwner<byte> GetMemoryCopy(ReadOnlySequence<byte> xmlEntry)
        {
            MemoryOwner<byte> memory = MemoryOwner<byte>.Allocate((int) xmlEntry.Length);
            xmlEntry.CopyTo(memory.Span);
            return memory;
        }

        private void PresentReaderBatch(ReaderBatchResult batchResult)
        {
            MotorPipeEventSource.Log.FoundEntries(batchResult.Batch.Count);
            var task = _resultChannel.WriteAsync(batchResult, _cancellationToken);
            if (!task.IsCompleted)
            {
                task.AsTask().Wait(_cancellationToken);
            }
        }
    }
}