using CampaignWatchWorker.Data.Factories.Common;
using CampaignWatchWorker.Domain.Models.Interfaces;
using MongoDB.Driver;

namespace CampaignWatchWorker.Infra.Campaign.Factories
{
    public class CampaignMongoFactory : ICampaignMongoFactory
    {
        private readonly IMongoDbFactory _factory;
        private readonly string _databaseName;

        public CampaignMongoFactory(IMongoDbFactory factory, ITenantContext tenantContext)
        {
            _factory = factory;

            if (tenantContext.Client == null)
            {
                throw new InvalidOperationException("TenantContext não foi inicializado. Processamento fora de escopo.");
            }

            _databaseName = tenantContext.Client.CampaignConfig.Database;
        }

        public IMongoDatabase GetDatabase()
        {
            return _factory.GetDatabase("MongoDB.Campaign", _databaseName);
        }
    }
}
