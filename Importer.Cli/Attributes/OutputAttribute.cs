using System;
using Importer.Cli.Outputs;

namespace Importer.Cli.Attributes
{
    public class OutputAttribute : Attribute
    {
        public OutputMode Mode { get; }

        public OutputAttribute(OutputMode mode)
        {
            Mode = mode;
        }
    }
}