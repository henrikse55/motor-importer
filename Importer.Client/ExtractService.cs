using System.Threading.Channels;
using Importer.Client.Grains.Interfaces;
using Importer.Converters;
using Importer.Readers;
using Microsoft.Toolkit.HighPerformance.Buffers;
using Orleans;
using Orleans.Runtime;
using Storage.Net.Blobs;

namespace Importer.Client;

public class ExtractService : IStartupTask
{
    private readonly IClusterClient _client;
    private readonly IBlobStorage _blobStorage;
    
    public ExtractService(
        IClusterClient client,
        IBlobStorage blobStorage)
    {
        _client = client;
        _blobStorage = blobStorage;
    }

    public async Task Execute(CancellationToken cancellationToken)
    {
        Channel<ReaderBatchResult> bounded = Channel.CreateBounded<ReaderBatchResult>(64);
        Task readTask = MotorReader.ReadXmlFromStream(bounded, File.OpenRead("/storage/motor/data.xml"));

        Task parseTask = Task.Run(async () =>
        {
            IEntryCollector collector = _client.GetGrain<IEntryCollector>("");
            await foreach (var result in bounded.Reader.ReadAllAsync())
            {
                foreach (MemoryOwner<byte> memoryOwner in result.Batch)
                {
                    // string content = XmlConverter.PatchXmlData(memoryOwner.Span);
                    // Guid guid = Guid.NewGuid();
                    
                    // await _blobStorage.WriteTextAsync($"raw/{guid}", content);
                    // await collector.Available(guid);
                }
            }
        });

        await Task.WhenAll(readTask, parseTask);
    }
}