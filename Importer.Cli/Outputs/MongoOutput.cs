using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Importer.Cli.Attributes;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Importer.Cli.Outputs
{
    [Output(OutputMode.Mongo)]
    public class MongoOutput : IOutput
    {
        private IMongoClient? _mongoClient;
        private ConcurrentBag<BsonDocument> _batchList = new ConcurrentBag<BsonDocument>();

        public void Configure(OutputConfig config)
        {
            _mongoClient = MakeMongoClient(config);
        }

        public void Present(BsonDocument document)
        {
            lock (_batchList)
            {
                _batchList.Add(document);
                if (_batchList.Count >= 512)
                {
                    ThreadPool.QueueUserWorkItem(WriteBatch, _batchList.ToArray(), true);
                    _batchList.Clear();
                }
            }
        }

        private void WriteBatch(BsonDocument[] batch)
        {
            var database = _mongoClient.GetDatabase("Motor");
            var collection = database.GetCollection<BsonDocument>("Statistik");

            ReplaceOneModel<BsonDocument>[] batchRequests = MakeBatchRequests(batch);
            if (batchRequests.Length <= 0)
                return;
            
            collection.BulkWrite(batchRequests);
        }

        private static ReplaceOneModel<BsonDocument>[] MakeBatchRequests(BsonDocument[] batch)
        {
            return batch.Select(x =>
            {
                BsonDocument filter = new BsonDocument()
                {
                    ["_id"] = x["_id"]
                };

                return new ReplaceOneModel<BsonDocument>(filter, x)
                {
                    IsUpsert = true
                };
            }).ToArray();
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