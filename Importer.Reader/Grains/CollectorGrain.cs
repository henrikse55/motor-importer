using System.Diagnostics;
using Importer.Reader.Memory;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;
using Orleans.Streams.Core;

namespace Importer.Reader.Grains;

public interface ICollectorGrain : IGrainWithStringKey
{
    
}

[ImplicitStreamSubscription("xml")]
public class CollectorGrain : Grain, ICollectorGrain, IAsyncObserver<Guid[]>, IStreamSubscriptionObserver
{
    private readonly ILogger<CollectorGrain> _logger;
    private readonly IMemoryStore _memoryStore;

    public CollectorGrain(
        ILogger<CollectorGrain> logger,
        IMemoryStore memoryStore)
    {
        _logger = logger;
        _memoryStore = memoryStore;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting {Key}", this.GetPrimaryKeyString());
        return Task.CompletedTask;
    }

    public Task OnNextAsync(Guid[] item, StreamSequenceToken token = null)
    {
        for (int i = 0; i < item.Length; i++)
        {
            ref Guid id = ref item[i];
            _ = _memoryStore.Get(id);
        }
        return Task.CompletedTask;
    }

    public Task OnCompletedAsync()
    {
        throw new NotImplementedException();
    }

    public Task OnErrorAsync(Exception ex)
    {
        throw new NotImplementedException();
    }
    
    public async Task OnSubscribed(IStreamSubscriptionHandleFactory handleFactory)
    {
        var handle= handleFactory.Create<Guid[]>();
        await handle.ResumeAsync(this);
    }
}