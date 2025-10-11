using CampaignWatchWorker.Data.Factories.Common;
using MongoDB.Driver;

namespace CampaignWatchWorker.Infra.Effmail.Factories
{
    public class EffmailMongoFactory : IEffmailMongoFactory
    {
        private readonly IMongoDbFactory _factory;
        private readonly string _databaseName;

        public EffmailMongoFactory(IMongoDbFactory factory, string databaseName)
        {
            _factory = factory;
            _databaseName = databaseName;
        }

        public IMongoDatabase GetDatabase()
        {
            return _factory.GetDatabase("MongoDB.Effmail", _databaseName);
        }
    }
}
