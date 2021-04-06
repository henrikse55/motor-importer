using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

using static Importer.Constants;

namespace Importer
{
    public class MotorReader
    {
        private readonly ChannelWriter<ReaderResult> _resultChannel;
        
        public static Task ReadXmlFromStream(ChannelWriter<ReaderResult> resultChannel, Stream xmlStream) 
            => new MotorReader(resultChannel).Read(xmlStream);

        public MotorReader(ChannelWriter<ReaderResult> resultChannel)
        {
            _resultChannel = resultChannel;
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
            while (true)
            {
                Memory<byte> buffer = writer.GetMemory(minimalSize);

                int bytesRead = await stream.ReadAsync(buffer);

                if (bytesRead == 0)
                {
                    break;
                }

                writer.Advance(bytesRead);

                FlushResult flushResult = await writer.FlushAsync();
                if (flushResult.IsCompleted)
                {
                    break;
                }
            }
            await writer.CompleteAsync();
        }
        
        private async Task ReadPipe(PipeReader reader)
        {
            while (true)
            {
                ReadResult result = await reader.ReadAsync();
                var buffer = result.Buffer;

                SequencePosition position = ScanForDelimiter(buffer);
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
            SequenceReader<byte> reader = new SequenceReader<byte>(sequence);
            while (true)
            {
                if (reader.TryReadTo(out ReadOnlySequence<byte> xmlEntry, EndingTagBytes))
                {
                    PresentReaderResult(xmlEntry);
                }
                else
                {
                    break;
                }
            }
            return reader.Position;
        }

        private void PresentReaderResult(ReadOnlySequence<byte> xmlEntry)
        {
            byte[] entryBuffer = xmlEntry.ToArray();
            while (!_resultChannel.TryWrite(new ReaderResult(entryBuffer)))
            {
                //spin...
            }
        }
    }
}