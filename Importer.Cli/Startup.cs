using System;
using System.CommandLine;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Importer.Cli.Commands;
using Importer.Cli.Extensions;
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
        }
    }
}