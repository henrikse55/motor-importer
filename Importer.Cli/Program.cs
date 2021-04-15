using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Importer.Cli
{
    class Program
    {
        static Task<int> Main(string[] args) 
            => BuildParser()
                .InvokeAsync(args);

        private static Parser BuildParser()
        {
            CommandLineBuilder builder = new CommandLineBuilder(Startup.BuildRootCommand());
            return builder
                .UseHost(_ => Host.CreateDefaultBuilder(), ConfigureHost)
                .UseDefaults()
                .Build();
        }

        private static void ConfigureHost(IHostBuilder builder) 
            => builder.ConfigureServices(Startup.ConfigureServices);
    }
}