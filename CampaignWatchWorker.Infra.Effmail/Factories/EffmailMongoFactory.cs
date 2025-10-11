using CampaignWatchWorker.Data.Factories.Common;
using MongoDB.Driver;

namespace CampaignWatchWorker.Infra.Effmail.Factories
{
    public class EffmailMongoFactory : IEffmailMongoFactory
    {
        private readonly IMongoDbFactory _factory;

        public EffmailMongoFactory(IMongoDbFactory factory)
        {
            _factory = factory;
        }

        public IMongoDatabase GetDatabase(string dbName)
        {
            return _factory.GetDatabase("MongoDB.Effmail", dbName);
        }
    }
}
