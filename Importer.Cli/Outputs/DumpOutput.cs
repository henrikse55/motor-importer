using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using Importer.Cli.Attributes;
using MongoDB.Bson;

namespace Importer.Cli.Outputs
{
    [Output(OutputMode.Dump)]
    public class DumpOutput : IOutput
    {
        public void Present(BsonDocument document)
        {
            //NOOP
        }
        
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }


    }
}