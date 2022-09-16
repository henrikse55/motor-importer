using Importer.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Hosting;
using Storage.Net;
using Storage.Net.Blobs;

var host = Host
    .CreateDefaultBuilder()
    .ConfigureServices(_ =>
    {
        _.AddSingleton<IBlobStorage>(_ => StorageFactory.Blobs.DirectoryFiles("/tmp/motor"));
    })
    .UseOrleans(_ =>
    {
        _.UseLocalhostClustering();
        _.AddStartupTask<ExtractService>();
        _.UseDashboard(options =>
        {
            options.Port = 8080;
        });
    });

host
    .Start()
    .WaitForShutdown();

// Channel<ReaderBatchResult> bounded = Channel.CreateBounded<ReaderBatchResult>(64);
// Task readTask = MotorReader.ReadXmlFromStream(bounded, File.OpenRead("/storage/motor/data.xml"));
//
// await Parallel.ForEachAsync(bounded.Reader.ReadAllAsync(), (result, token) =>
// {
//     foreach (MemoryOwner<byte> memoryOwner in result.Batch)
//     {
//         using var content = XmlConverter.ConvertToU8(memoryOwner);
//     }
//
//     return ValueTask.CompletedTask;
// });
//
// await readTask;