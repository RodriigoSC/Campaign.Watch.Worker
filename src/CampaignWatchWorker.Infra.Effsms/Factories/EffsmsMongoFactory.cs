using CampaignWatchWorker.Data.Factories.Common;
using MongoDB.Driver;

namespace CampaignWatchWorker.Infra.Effsms.Factories
{
    public class EffsmsMongoFactory : IEffsmsMongoFactory
    {
        private readonly IMongoDbFactory _factory;
        private readonly string _databaseName;


        public EffsmsMongoFactory(IMongoDbFactory factory, string databaseName)
        {
            _factory = factory;
            _databaseName = databaseName;
        }

        public IMongoDatabase GetDatabase()
        {
            return _factory.GetDatabase("MongoDB.Effsms", _databaseName);
        }
    }
}
