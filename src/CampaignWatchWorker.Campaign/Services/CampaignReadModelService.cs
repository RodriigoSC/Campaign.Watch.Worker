using CampaignWatchWorker.Domain.Models.Enums;
using CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Campaign;
using CampaignWatchWorker.Domain.Models.Read.Campaign;
using CampaignWatchWorker.Infra.Campaign.Factories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CampaignWatchWorker.Infra.Campaign.Services
{
    public class CampaignReadModelService : ICampaignReadModelService
    {
        private readonly ICampaignMongoFactory _factory;

        public CampaignReadModelService(ICampaignMongoFactory factory)
        {
            _factory = factory;
        }

        public async Task<IEnumerable<CampaignReadModel>> GetCampaigns()
        {
            var db = _factory.GetDatabase();
            var collection = db.GetCollection<CampaignReadModel>("Campaign");
            return await collection.Find(_ => true).ToListAsync();
        }

        public async Task<IEnumerable<ExecutionReadModel>> GetExecutionsByCampaign(string campaignId)
        {
            var db = _factory.GetDatabase();
            var collection = db.GetCollection<ExecutionReadModel>("ExecutionPlan");

            return await collection
                .Find(x => x.CampaignId.ToString() == campaignId && x.FlagCount == false)
                .ToListAsync();
        }

        public async Task<CampaignReadModel> GetCampaignById(string campaignId)
        {
            var db = _factory.GetDatabase();
            var collection = db.GetCollection<CampaignReadModel>("Campaign");

            if (!ObjectId.TryParse(campaignId, out var campaignObjectId))
            {
                return null;
            }

            return await collection.Find(x => x.Id == campaignObjectId.ToString()).FirstOrDefaultAsync();
        }
        
        public async Task<IEnumerable<CampaignReadModel>> GetDiscoverableCampaignsAsync()
        {
            var db = _factory.GetDatabase();
            var collection = db.GetCollection<CampaignReadModel>("Campaign");

            var filterBuilder = Builders<CampaignReadModel>.Filter;

            // Filtro base: Apenas campanhas ativas e não deletadas
            var activeFilter = filterBuilder.Eq(x => x.IsActive, true) &
                               filterBuilder.Eq(x => x.IsDeleted, false);

            // Filtro 1: Campanhas que exigem monitoramento ATIVO
            // Inclui apenas campanhas que estão de fato rodando ou agendadas
            var statusFilter = filterBuilder.In(x => x.Status,
                new[]
                {
                    (int)CampaignStatusEnum.Executing,
                    (int)CampaignStatusEnum.Scheduled
                });

            // Filtro 2: Campanhas que exigem uma ÚLTIMA VERIFICAÇÃO
            // Inclui campanhas que mudaram para um estado final nos últimos 3 dias
            var recentFinishedFilter = filterBuilder.In(x => x.Status,
                new[]
                {
                    (int)CampaignStatusEnum.Completed,
                    (int)CampaignStatusEnum.Error,
                    (int)CampaignStatusEnum.Canceled,   
                    (int)CampaignStatusEnum.Canceling
                }) & filterBuilder.Gte(x => x.ModifiedAt, DateTime.UtcNow.AddDays(-30));


            var finalFilter = activeFilter & (statusFilter | recentFinishedFilter);

            return await collection.Find(finalFilter).ToListAsync();
        }
    }
}
