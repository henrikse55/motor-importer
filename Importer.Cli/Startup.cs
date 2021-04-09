using System;
using System.CommandLine;
using System.Diagnostics;
using Importer.Cli.Commands;
using Importer.Cli.Extensions;
using Importer.Metrics.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Importer.Cli
{
    public static class Startup
    {
        public static RootCommand BuildRootCommand()
        {
            var root = new RootCommand();
            root.AddCommand(new ImportCommand());

            return root;
        }
        
        public static void ConfigureServices(HostBuilderContext host, IServiceCollection services)
        {
            Console.WriteLine(Process.GetCurrentProcess().Id);
            services.AddLogging(builder =>
            {
                builder.AddConsole();
            });

            services.AddCommandHandlers();
            services.AddOutputs();
            
            services.AddMetricServer();
        }
    }
}