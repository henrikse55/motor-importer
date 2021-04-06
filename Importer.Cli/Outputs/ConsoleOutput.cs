using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Importer.Cli.Attributes;
using Microsoft.Extensions.Primitives;
using MongoDB.Bson;

namespace Importer.Cli.Outputs
{
    [Output(OutputMode.Console)]
    public class ConsoleOutput : IOutput
    {
        public void Present(BsonDocument document)
        {
            Console.WriteLine(document.ToArray());
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}