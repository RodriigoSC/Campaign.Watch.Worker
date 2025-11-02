using DTM_Vault.Data.KeyValue;
using MongoDB.Driver;
using System.Collections.Concurrent;

namespace CampaignWatchWorker.Data.Factories.Common
{
    public class MongoDbFactory : IMongoDbFactory
    {       
        private readonly IVaultService _vaultService;

        private readonly string _environment;

        private readonly ConcurrentDictionary<string, MongoClient> _clients = new();

        public MongoDbFactory(IVaultService vaultService, string environment)
        {
            _vaultService = vaultService;
            _environment = environment;
        }

        private MongoClient GetClient(string connectionKey)
        {
            return _clients.GetOrAdd(connectionKey, key =>
            {
                var host = _vaultService.GetKeyAsync($"monitoring/{_environment}/data/keys", $"{key}.host").GetAwaiter().GetResult();
                var user = _vaultService.GetKeyAsync($"monitoring/{_environment}/data/keys", $"{key}.user").GetAwaiter().GetResult();
                var pass = _vaultService.GetKeyAsync($"monitoring/{_environment}/data/keys", $"{key}.pass").GetAwaiter().GetResult();

                var connString = $"mongodb://{user}:{pass}@{host}";
                return new MongoClient(connString);
            });
        }

        public IMongoDatabase GetDatabase(string connectionKey, string databaseName)
        {
            var client = GetClient(connectionKey);
            return client.GetDatabase(databaseName);
        }
    }
}
