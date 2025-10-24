using CampaignWatchWorker.Data.Factories;
using CampaignWatchWorker.Data.Repositories.Common;
using CampaignWatchWorker.Domain.Models;
using CampaignWatchWorker.Domain.Models.Interfaces.Repositories;
using MongoDB.Bson;
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

        
        /*public async Task<bool> AtualizarCampanhaAsync(CampaignModel campaignModel)
        {            
            var filter = Builders<CampaignModel>.Filter.And(
                Builders<CampaignModel>.Filter.Eq(c => c.ClientName, campaignModel.ClientName),
                Builders<CampaignModel>.Filter.Eq(c => c.IdCampaign, campaignModel.IdCampaign)
            );
            
            var result = await _collection.ReplaceOneAsync(filter, campaignModel, new ReplaceOptions { IsUpsert = true });

            return result.IsAcknowledged && (result.ModifiedCount > 0 || result.UpsertedId != null);
        }*/

        public async Task<bool> AtualizarCampanhaAsync(CampaignModel campaignModel)
        {
            var filter = Builders<CampaignModel>.Filter.And(
                Builders<CampaignModel>.Filter.Eq(c => c.ClientName, campaignModel.ClientName),
                Builders<CampaignModel>.Filter.Eq(c => c.IdCampaign, campaignModel.IdCampaign)
            );

            // Define quais campos serão atualizados
            var updateDefinition = Builders<CampaignModel>.Update
                .Set(x => x.NumberId, campaignModel.NumberId)
                .Set(x => x.Name, campaignModel.Name)
                .Set(x => x.CampaignType, campaignModel.CampaignType)
                .Set(x => x.Description, campaignModel.Description)
                .Set(x => x.ProjectId, campaignModel.ProjectId)
                .Set(x => x.IsActive, campaignModel.IsActive)
                .Set(x => x.ModifiedAt, campaignModel.ModifiedAt)
                .Set(x => x.StatusCampaign, campaignModel.StatusCampaign)
                .Set(x => x.MonitoringStatus, campaignModel.MonitoringStatus)
                .Set(x => x.NextExecutionMonitoring, campaignModel.NextExecutionMonitoring)
                .Set(x => x.LastCheckMonitoring, campaignModel.LastCheckMonitoring)
                .Set(x => x.HealthStatus, campaignModel.HealthStatus)
                .Set(x => x.IsDeleted, campaignModel.IsDeleted)
                .Set(x => x.IsRestored, campaignModel.IsRestored)
                .Set(x => x.Scheduler, campaignModel.Scheduler)
                .Set(x => x.TotalExecutionsProcessed, campaignModel.TotalExecutionsProcessed)
                .Set(x => x.ExecutionsWithErrors, campaignModel.ExecutionsWithErrors)
                .Set(x => x.MonitoringNotes, campaignModel.MonitoringNotes)

                .SetOnInsert(x => x.Id, ObjectId.GenerateNewId())
                .SetOnInsert(x => x.ClientName, campaignModel.ClientName)
                .SetOnInsert(x => x.IdCampaign, campaignModel.IdCampaign)
                .SetOnInsert(x => x.CreatedAt, campaignModel.CreatedAt)
                .SetOnInsert(x => x.FirstMonitoringAt, campaignModel.FirstMonitoringAt);


            var options = new UpdateOptions { IsUpsert = true };

            var result = await _collection.UpdateOneAsync(filter, updateDefinition, options);

            return result.IsAcknowledged && (result.ModifiedCount > 0 || result.UpsertedId != null);
        }
    }
}