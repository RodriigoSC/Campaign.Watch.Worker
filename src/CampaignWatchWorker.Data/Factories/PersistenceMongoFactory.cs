using CampaignWatchWorker.Data.Factories.Common;
using MongoDB.Driver;

namespace CampaignWatchWorker.Data.Factories
{
    public class PersistenceMongoFactory : IPersistenceMongoFactory
    {
        private readonly IMongoDatabase _database;

        public PersistenceMongoFactory(IMongoDbFactory factory, string databaseName)
        {
            _database = factory.GetDatabase("MongoDB.Persistence", databaseName);
        }

        public IMongoDatabase GetDatabase()
        {
            return _database;
        }
    }
}
