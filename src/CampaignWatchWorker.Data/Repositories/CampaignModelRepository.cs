using CampaignWatchWorker.Data.Factories;
using CampaignWatchWorker.Data.Repositories.Common;
using CampaignWatchWorker.Domain.Models;
using CampaignWatchWorker.Domain.Models.Interfaces.Repositories;
using MongoDB.Driver;

namespace CampaignWatchWorker.Data.Repositories
{
    public class CampaignModelRepository : CommonRepository<CampaignModel>, ICampaignModelRepository
    {
        public CampaignModelRepository(IPersistenceMongoFactory persistenceFactory) : base(persistenceFactory, "CampaignMonitoring")
        {
            var uniqueIndexKeys = Builders<CampaignModel>.IndexKeys
                .Ascending(x => x.ClientName)
                .Ascending(x => x.IdCampaign);
            var uniqueIndexModel = new CreateIndexModel<CampaignModel>(
                uniqueIndexKeys,
                new CreateIndexOptions { Unique = true, Name = "Client_IdCampaign_Unique" });

            var workerIndexKeys = Builders<CampaignModel>.IndexKeys
                .Ascending(x => x.IsActive)
                .Ascending(x => x.NextExecutionMonitoring);
            var workerIndexModel = new CreateIndexModel<CampaignModel>(
                workerIndexKeys,
                new CreateIndexOptions { Name = "Worker_Monitoring_Query" });

            CreateIndexesAsync(new List<CreateIndexModel<CampaignModel>> { uniqueIndexModel, workerIndexModel }).GetAwaiter().GetResult();
        }

        public async Task<CampaignModel> CriarCampanhaAsync(CampaignModel campaignModel)
        {
            await _collection.InsertOneAsync(campaignModel);
            return campaignModel;
        }

        
        public async Task<bool> AtualizarCampanhaAsync(CampaignModel campaignModel)
        {            
            var filter = Builders<CampaignModel>.Filter.And(
                Builders<CampaignModel>.Filter.Eq(c => c.ClientName, campaignModel.ClientName),
                Builders<CampaignModel>.Filter.Eq(c => c.IdCampaign, campaignModel.IdCampaign)
            );
            
            var result = await _collection.ReplaceOneAsync(filter, campaignModel, new ReplaceOptions { IsUpsert = true });

            return result.IsAcknowledged && (result.ModifiedCount > 0 || result.UpsertedId != null);
        }
    }
}