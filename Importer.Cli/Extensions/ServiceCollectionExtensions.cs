using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Importer.Cli.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCliCommands(this IServiceCollection services)
        {
            IEnumerable<Type> commandsInAssembly = Assembly.GetCallingAssembly().GetTypesWithBaseOf<Command>();
            foreach (Type type in commandsInAssembly) 
                services.AddSingleton(typeof(Command), type);
            
            return services;
        }
    }
}