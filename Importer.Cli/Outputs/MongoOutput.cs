using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using Importer.Cli.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Importer.Cli.Outputs
{
    [Output(OutputMode.Mongo)]
    public class MongoOutput : IOutput
    {
        private IMongoClient? _mongoClient;

        private readonly BatchBlock<BsonDocument> _batchBlock = new BatchBlock<BsonDocument>(512);
        private ActionBlock<BsonDocument[]>? _processBatch;

        public void Configure(OutputConfig config)
        {
            _mongoClient = MakeMongoClient(config);

            _processBatch = new ActionBlock<BsonDocument[]>(WriteBatch, new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            });
            _batchBlock.LinkTo(_processBatch);
        }

        public void Present(BsonDocument document)
        {
            _batchBlock.Post(document);
        }

        private void WriteBatch(BsonDocument[] batch)
        {
            var database = _mongoClient.GetDatabase("Motor");
            var collection = database.GetCollection<BsonDocument>("Statistik");

            List<ReplaceOneModel<BsonDocument>> models = new List<ReplaceOneModel<BsonDocument>>();
            foreach (BsonDocument document in batch)
            {
                BsonDocument filter = new BsonDocument()
                {
                    ["_id"] = document["_id"]
                };
                
                models.Add(new ReplaceOneModel<BsonDocument>(filter, document)
                {
                    IsUpsert = true
                });
            }
            collection.BulkWrite(models);
        }
        
        private IMongoClient MakeMongoClient(OutputConfig importOptions)
        {
            MongoClientSettings settings = MongoClientSettingsFactory.CreateMongoClientSettings(importOptions);
            return new MongoClient(settings);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}