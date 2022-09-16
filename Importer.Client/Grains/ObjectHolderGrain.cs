using Importer.Client.Grains.Interfaces;
using Orleans;
using Orleans.Concurrency;

namespace Importer.Client.Grains;

public class ObjectHolderGrain : Grain, IObjectHolder
{
    private Guid? _id = null;
    
    public Task<Guid?> GetObjectId() => Task.FromResult<Guid?>(_id);

    public Task<Guid> Parse(Immutable<string> content)
    {
        _id = Guid.NewGuid();
        return Task.FromResult(_id.Value);
    }
}