using Importer.Reader;
using Importer.Reader.Memory;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Statistics;


IHost host = Host.CreateDefaultBuilder(args)
    // .UseOrleans(_ =>
    // {
    //     _.UseLocalhostClustering();
    //
    //     _.Configure<ClusterOptions>(options =>
    //     {
    //         options.ClusterId = "dev";
    //         options.ServiceId = "dev";
    //     });
    //     _.ConfigureEndpoints(siloPort: 11112, gatewayPort: 11112);
    //     
    //     _.UseLinuxEnvironmentStatistics();
    //
    //     _.AddMemoryStreams<DefaultMemoryMessageBodySerializer>("memory")
    //         .AddMemoryGrainStorage("PubSubStore");
    //
    //     _.UseDashboard();
    // })
    .ConfigureServices(services =>
    {
        services.AddSingleton<IMemoryStore, PinnedMemoryStore>();
        services.AddHostedService<ReaderService>();
    })
    .Build();

await host.RunAsync();