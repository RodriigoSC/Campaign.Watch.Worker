using CampaignWatchWorker.Data.Factories.Common;
using MongoDB.Driver;

namespace CampaignWatchWorker.Infra.Effpush.Factories
{
    public class EffpushMongoFactory : IEffpushMongoFactory
    {
        private readonly IMongoDbFactory _factory;

        public EffpushMongoFactory(IMongoDbFactory factory)
        {
            _factory = factory;
        }

        public IMongoDatabase GetDatabase(string dbName)
        {
            return _factory.GetDatabase("MongoDB.Effpush", dbName);
        }
    }
}
