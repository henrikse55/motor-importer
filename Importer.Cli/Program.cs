using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using Importer.Cli.Commands;
using Importer.Cli.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Importer.Cli
{
    class Program
    {
        static Task<int> Main(string[] args)
        {
            ServiceProvider provider = BuildServiceProvider();
            return BuildParser(provider).InvokeAsync(args);
        }

        private static ServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddCliCommands();

            return services.BuildServiceProvider();
        }

        private static Parser BuildParser(ServiceProvider provider)
        {
            CommandLineBuilder builder = new CommandLineBuilder();
            var commands = provider.GetServices<Command>();
            foreach (Command command in commands)
            {
                builder.AddCommand(command);
            }

            return builder.UseDefaults().Build();
        }
    }
}