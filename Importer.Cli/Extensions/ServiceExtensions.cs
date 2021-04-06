using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Importer.Cli.Outputs;
using Microsoft.Extensions.DependencyInjection;

namespace Importer.Cli.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddOutputs(this IServiceCollection services)
        {
            var outputs = Assembly
                .GetCallingAssembly()
                .GetTypesOfInterface<IOutput>();
            
            foreach (Type output in outputs)
            {
                services.AddSingleton(typeof(IOutput), output);
            }

            services.AddSingleton<OutputResolver>();

            return services;
        }
        
        public static IServiceCollection AddCommandHandlers(this IServiceCollection services)
        {
            var handlerTypes = Assembly
                .GetCallingAssembly()
                .GetTypes()
                .Where(x => x.GetInterfaces().Any())
                .Where(GenericTypeDefinitionMatches);

            foreach (Type type in handlerTypes)
            {
                var interfaceType = type.GetInterfaces().First();
                services.AddSingleton(interfaceType, type);
            }
            
            return services;
        }
        
        private static bool GenericTypeDefinitionMatches(Type interfaceType)
        {
            return GetGenericOnly(interfaceType).Any(x => x.GetGenericTypeDefinition() == typeof(IResolvableCommandHandler<>));
        }

        private static IEnumerable<Type> GetGenericOnly(Type type)
        {
            return type.GetInterfaces().Where(x => x.IsGenericType);
        }
    }
}