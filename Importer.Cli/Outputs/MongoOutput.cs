using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Importer.Cli.Commands;
using Importer.Cli.Extensions;
using Importer.Cli.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Importer.Cli.Outputs
{
    public class MongoOutput : IOutput
    {
        private readonly IMongoClient _mongoClient;
        private List<BsonDocument> _batchList = new List<BsonDocument>(512);

        public MongoOutput(ImportOptions options)
        {
            _mongoClient = MakeMongoClient(options);
        }

        public async Task Start(ChannelReader<ReaderResult> reader)
        {
            await foreach (ReaderResult result in reader.ReadAllAsync())
            {
                string jsonData = XmlConverter.ConvertToJson(result);
                AddToBatch(jsonData);
            }
            
            WriteBatch();
        }

        private void AddToBatch(string jsonData)
        {
            BsonDocument document = BsonDocument.Parse(jsonData);
            _batchList.Add(document);

            if (_batchList.Count >= 128)
            {
                WriteBatch();
            }
        }

        private void WriteBatch()
        {
            var database = _mongoClient.GetDatabase("Motor");
            var collection = database.GetCollection<BsonDocument>("Statistik");
            collection.InsertMany(_batchList);
            _batchList.Clear();
        }

        private IMongoClient MakeMongoClient(ImportOptions importOptions)
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