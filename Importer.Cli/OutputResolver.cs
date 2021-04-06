using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Importer.Cli.Attributes;
using Importer.Cli.Outputs;

namespace Importer.Cli
{
    public class OutputResolver
    {
        private readonly Dictionary<OutputMode, IOutput> _outputs;

        public OutputResolver(IEnumerable<IOutput> outputs)
        {
            _outputs = outputs.ToDictionary(KeySelector);
        }

        public IOutput Resolve(OutputMode mode) 
            => _outputs[mode];

        private OutputMode KeySelector(IOutput output)
        {
            Type type = output.GetType();
            OutputAttribute outputAttribute = type.GetCustomAttribute<OutputAttribute>() 
                                              ?? throw new NullReferenceException();
            return outputAttribute.Mode;
        }
    }
}