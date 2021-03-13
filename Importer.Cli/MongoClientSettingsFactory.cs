using System;
using System.Collections.Generic;
using System.Linq;
using Importer.Cli.Extensions;
using Importer.Cli.Options;
using MongoDB.Driver;

namespace Importer.Cli
{
    public class MongoClientSettingsFactory
    {
        private readonly string _mongoClients;
        private readonly string? _authString;

        private readonly MongoClientSettings _settings = new MongoClientSettings();
        
        public static MongoClientSettings CreateMongoClientSettings(string mongoClient, string? authString)
            => new MongoClientSettingsFactory(mongoClient, authString).CreateMongoClientSettings();
        
        public static MongoClientSettings CreateMongoClientSettings(ImportOptions options) 
            => CreateMongoClientSettings(
                options.Mongo ?? throw new ArgumentNullException(nameof(options), "Address to mongo need to be present"),
                options.Auth);
        
        public MongoClientSettingsFactory(string mongoClients, string? authString)
        {
            if (string.IsNullOrEmpty(mongoClients))
                throw new ArgumentNullException(nameof(mongoClients));
            
            _mongoClients = mongoClients;
            _authString = authString;
        }

        public MongoClientSettings CreateMongoClientSettings()
        {
            ConfigureMongoServers();
            ConfigureAuthentication();
            return _settings;
        }
        
        private void ConfigureMongoServers()
        {
            if (ContainsMultipleMongoAddresses(_mongoClients))
            {
                _settings.Servers = GetMongoServers(_mongoClients);
                return;
            }
            _settings.Server = MongoServerAddress.Parse(_mongoClients);
        }
        
        private void ConfigureAuthentication()
        {
            if (string.IsNullOrEmpty(_authString))
                return;
                
            (string username, string password) credTuple = _authString.GetMongoGetCredentials();
            _settings.Credential = MongoCredential.CreateCredential("Motor", credTuple.username, credTuple.password);
        }
        
        private bool ContainsMultipleMongoAddresses(string mongoAddress) 
            => mongoAddress.Contains(',');
        
        private IEnumerable<MongoServerAddress> GetMongoServers(string mongoAddress) 
            => mongoAddress
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(MongoServerAddress.Parse);
    }
}