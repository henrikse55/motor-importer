using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Importer.Cli.Extensions
{
    public static class AssemblyExtensions
    {
        public static IEnumerable<Type> GetTypesOfInterface<TType>(this Assembly assembly) => 
            assembly.GetTypes().Where(x => x.GetInterfaces().Any(t => t == typeof(TType)));
    }
}