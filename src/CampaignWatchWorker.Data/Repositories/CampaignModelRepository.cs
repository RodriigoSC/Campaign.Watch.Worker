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
                .Set(x => x.ModifiedAt, DateTime.UtcNow) // Sempre atualiza ModifiedAt na atualização
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

                // Campos definidos apenas na inserção (Upsert)
                .SetOnInsert(x => x.Id, campaignModel.Id == ObjectId.Empty ? ObjectId.GenerateNewId() : campaignModel.Id) // Garante um ID na inserção
                .SetOnInsert(x => x.ClientName, campaignModel.ClientName) // Necessário para o filtro funcionar no upsert
                .SetOnInsert(x => x.IdCampaign, campaignModel.IdCampaign) // Necessário para o filtro funcionar no upsert
                .SetOnInsert(x => x.CreatedAt, campaignModel.CreatedAt == DateTime.MinValue ? DateTime.UtcNow : campaignModel.CreatedAt) // Define CreatedAt na inserção
                .SetOnInsert(x => x.FirstMonitoringAt, campaignModel.FirstMonitoringAt ?? DateTime.UtcNow); // Define FirstMonitoringAt na inserção


            var options = new UpdateOptions { IsUpsert = true };

            var result = await _collection.UpdateOneAsync(filter, updateDefinition, options);

            // Se foi um upsert (inserção), o ID pode ter sido gerado pelo MongoDB.
            // Atualizamos o ID no objeto campaignModel para refletir o ID real no banco.
            if (result.UpsertedId != null)
            {
                campaignModel.Id = result.UpsertedId.AsObjectId;
            }
            // Se foi apenas um update e o ID já existia, ele permanece o mesmo.

            return result.IsAcknowledged && (result.ModifiedCount > 0 || result.UpsertedId != null);
        }

        public async Task<CampaignModel?> ObterCampanhaPorIdAsync(string clientName, string idCampaign)
        {
            var filter = Builders<CampaignModel>.Filter.And(
                Builders<CampaignModel>.Filter.Eq(c => c.ClientName, clientName),
                Builders<CampaignModel>.Filter.Eq(c => c.IdCampaign, idCampaign)
            );
            return await _collection.FindAsync(filter).Result.FirstOrDefaultAsync();
        }
    }
}