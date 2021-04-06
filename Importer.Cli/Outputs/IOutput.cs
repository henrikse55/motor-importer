using System;
using MongoDB.Bson;

namespace Importer.Cli.Outputs
{
    public interface IOutput : IDisposable
    {
        void Configure(OutputConfig config)
        {
        }
        void Present(BsonDocument document);
    }
}