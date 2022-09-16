using Orleans;
using Orleans.Concurrency;

namespace Importer.Client.Grains.Interfaces;

public interface IEntryParser : IGrainWithGuidKey
{
    // [OneWay]
    public Task Initialize();
}