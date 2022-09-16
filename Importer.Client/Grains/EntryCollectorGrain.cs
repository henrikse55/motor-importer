using System.Threading.Channels;
using Importer.Client.Grains.Interfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using Storage.Net.Blobs;

namespace Importer.Client.Grains;

[StatelessWorker]
public class EntryCollectorGrain : Grain, IEntryCollector
{
    private readonly Channel<Guid> _pendingItems = Channel.CreateUnbounded<Guid>();
    private readonly ILogger<EntryCollectorGrain> _logger;
    private readonly IBlobStorage _storage;
    
    public EntryCollectorGrain(
        ILogger<EntryCollectorGrain> logger,
        IBlobStorage storage)
    {
        _logger = logger;
        _storage = storage;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        RegisterTimer(Process, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        return Task.CompletedTask;
    }

    private async Task Process(object arg)
    {
        _logger.LogInformation("Available {Items} - {Time}", _pendingItems.Reader.Count, DateTime.UtcNow);
        while (_pendingItems.Reader.TryRead(out Guid guid))
        {
            var parser = GrainFactory.GetGrain<IEntryParser>(guid);
            await parser.Initialize();
        }
        _logger.LogInformation("Process Kickoff done");
    }

    public async Task Available(Guid id)
    {
        await _pendingItems.Writer.WriteAsync(id);
    }
}