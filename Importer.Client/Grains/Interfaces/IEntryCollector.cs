using Orleans;

namespace Importer.Client.Grains.Interfaces;

public interface IEntryCollector : IGrainWithStringKey
{
    public Task Available(Guid id);
}