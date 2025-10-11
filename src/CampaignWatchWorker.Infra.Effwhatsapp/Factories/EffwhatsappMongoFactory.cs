using CampaignWatchWorker.Data.Factories.Common;
using MongoDB.Driver;

namespace CampaignWatchWorker.Infra.Effwhatsapp.Factories
{
    public class EffwhatsappMongoFactory : IEffwhatsappMongoFactory
    {
        private readonly IMongoDbFactory _factory;
        private readonly string _databaseName;

        public EffwhatsappMongoFactory(IMongoDbFactory factory, string databaseName)
        {
            _factory = factory;
            _databaseName = databaseName;
        }

        public IMongoDatabase GetDatabase()
        {
            return _factory.GetDatabase("MongoDB.Effwhatsapp", _databaseName);
        }
    }
}
