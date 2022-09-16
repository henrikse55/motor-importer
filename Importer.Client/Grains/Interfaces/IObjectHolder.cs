using Orleans;
using Orleans.Concurrency;

namespace Importer.Client.Grains.Interfaces;

public interface IObjectHolder : IGrainWithStringKey
{
    public Task<Guid?> GetObjectId();

    public Task<Guid> Parse(Immutable<string> content);
}