using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Importer.Extensions;
using Importer.Metrics.Counters;
using Microsoft.Toolkit.HighPerformance.Buffers;

namespace Importer.Readers
{
    public abstract class ReaderBase
    {
        protected readonly CancellationToken _cancellationToken;

        public ReaderBase(CancellationToken cancellationToken)
        {
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
            while (true)
            {
                if (reader.TryReadTo(out ReadOnlySequence<byte> xmlEntry, Constants.EndingTagBytes))
                {
                    MemoryOwner<byte> memory = xmlEntry.CopyToMemoryOwner();
                    PresentEntry(memory);
                }
                else
                {
                    break;
                }
            }
            ScanComplete();
            return reader.Position;
        }

        /// <summary>
        /// Invoked on each xml entry found
        /// </summary>
        protected virtual void PresentEntry(MemoryOwner<byte> entry)
        {
        }

        /// <summary>
        /// Invoked when no xml entries can be found
        /// </summary>
        protected virtual void ScanComplete()
        {
        }
    }
}