using System.Buffers;
using System.Diagnostics.Metrics;
using System.IO.Pipelines;
using System.Threading.Channels;
using Google.Protobuf;
using Grpc.Core;
using Importer.Reader.Memory;
using Importer.Zip;
using Microsoft.Toolkit.HighPerformance.Buffers;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;
using Channel = System.Threading.Channels.Channel;

namespace Importer.Reader
{
    public class ReaderService : BackgroundService
    {
        private readonly Meter _meter = new("Motor");
        private readonly CancellationTokenSource _tokenSource = new();
        private readonly CancellationToken _cancellationToken;
        private readonly IClusterClient _clusterClient;
        private readonly IMemoryStore _memoryStore;

        private readonly ObservableCounter<long> _entries;
        private readonly Counter<long> _readSize;
        private int _entriesCount = 0;
        private readonly ILogger<ReaderService> _logger;


        public ReaderService(
            // IClusterClient clusterClient,
            ILogger<ReaderService> logger, 
            IMemoryStore memoryStore)
        {
            // _clusterClient = clusterClient;
            _logger = logger;
            _memoryStore = memoryStore;
            _entries = _meter.CreateObservableCounter<long>("Xml Entries", () => _entriesCount, "document");
            _readSize = _meter.CreateCounter<long>("Xml Size", "bytes");
            _cancellationToken = _tokenSource.Token;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await using Stream file = File.OpenRead("/storage/motor/data.xml");
            await Read(new BufferedStream(file, 4096));
        }
        
        public async Task Read(Stream xmlStream)
        {
            
            Pipe pipe = new();
            Task write = FillPipe(pipe.Writer, xmlStream);
            Task read = ReadPipe(pipe.Reader);

            await Task.WhenAll(write, read);
        }

        private async Task FillPipe(PipeWriter writer, Stream stream)
        {
            while (!_cancellationToken.IsCancellationRequested)
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
            // IStreamProvider? provider = _clusterClient.GetStreamProvider("memory");
            
            while (!_cancellationToken.IsCancellationRequested)
            {
                ReadResult result = await reader.ReadAsync(_cancellationToken);
                ReadOnlySequence<byte> buffer = result.Buffer;

                // using XmlBucket bucket = new XmlBucket(2048*1024);
                SequencePosition position = ScanForDelimiter2(buffer);
                
                // Guid[] items = bucket.WriteToStore(_memoryStore).ToArray();
                //
                // IAsyncStream<Guid[]>? stream = provider.GetStream<Guid[]>(StreamId.Create("xml", "memory-xml" + items.Length));
                // await stream.OnNextAsync(items).ConfigureAwait(false);

                reader.AdvanceTo(position, buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }

            await reader.CompleteAsync();
        }
        
        private SequencePosition ScanForDelimiter2(ReadOnlySequence<byte> sequence)
        {
            SequenceReader<byte> reader = new(sequence);
            
            while (reader.TryReadTo(out ReadOnlySequence<byte> xmlEntry, Constants.EndingTagBytes))
            {
                _entriesCount++;
            }
            return reader.Position;
        }

        private SequencePosition ScanForDelimiter(ReadOnlySequence<byte> sequence, XmlBucket bucket)
        {
            SequenceReader<byte> reader = new(sequence);
            
            while (reader.TryReadTo(out ReadOnlySequence<byte> xmlEntry, Constants.EndingTagBytes))
            {
                _entriesCount++;
                bucket.WriteEntry(xmlEntry);
            }
            return reader.Position;
        }
    }
}