using CampaignWatchWorker.Data.Factories.Common;
using MongoDB.Driver;

namespace CampaignWatchWorker.Infra.Effpush.Factories
{
    public class EffpushMongoFactory : IEffpushMongoFactory
    {
        private readonly IMongoDbFactory _factory;
        private readonly string _databaseName;

        public EffpushMongoFactory(IMongoDbFactory factory, string databaseName)
        {
            _factory = factory;
            _databaseName = databaseName;
        }

        public IMongoDatabase GetDatabase()
        {
            return _factory.GetDatabase("MongoDB.Effpush", _databaseName);
        }
    }
}
