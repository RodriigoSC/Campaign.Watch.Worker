using CampaignWatchWorker.Data.Factories.Common;
using MongoDB.Driver;

namespace CampaignWatchWorker.Infra.Campaign.Factories
{
    public class CampaignMongoFactory : ICampaignMongoFactory
    {
        private readonly IMongoDbFactory _factory;

        private readonly string _databaseName;

        public CampaignMongoFactory(IMongoDbFactory factory, string databaseName)
        {
            _factory = factory;
            _databaseName = databaseName;
        }

        public IMongoDatabase GetDatabase()
        {
            return _factory.GetDatabase("MongoDB.Campaign", _databaseName);
        }
    }
}
