using CampaignWatchWorker.Data.Factories;
using CampaignWatchWorker.Data.Repositories.Common;
using CampaignWatchWorker.Domain.Models.Entities.Alerts;
using CampaignWatchWorker.Domain.Models.Interfaces.Repositories.Alerts;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CampaignWatchWorker.Data.Repositories
{
    public class AlertConfigurationRepository : CommonRepository<AlertConfigurationModel>, IAlertConfigurationRepository
    {
        // Note que usamos IPersistenceMongoFactory (o mesmo DB da API)
        public AlertConfigurationRepository(IPersistenceMongoFactory persistenceFactory)
            : base(persistenceFactory, "AlertConfiguration")
        {
            // Índices (opcional no worker, mas bom para garantir)
            var indexKeys = Builders<AlertConfigurationModel>.IndexKeys
                .Ascending(x => x.ClientId);
            var indexModel = new CreateIndexModel<AlertConfigurationModel>(
                indexKeys,
                new CreateIndexOptions { Name = "Alerts_ClientId_Scope_Query" });

            CreateIndexesAsync(new List<CreateIndexModel<AlertConfigurationModel>> { indexModel }).GetAwaiter().GetResult();
        }

        public async Task<IEnumerable<AlertConfigurationModel>> GetByScopeAsync(ObjectId? clientId)
        {
            var filter = Builders<AlertConfigurationModel>.Filter.Eq(e => e.ClientId, clientId);
            return await _collection.Find(filter).ToListAsync();
        }
    }
}
