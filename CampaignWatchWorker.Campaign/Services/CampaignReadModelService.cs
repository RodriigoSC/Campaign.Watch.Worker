using CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Campaign;
using CampaignWatchWorker.Domain.Models.Read.Campaign;
using CampaignWatchWorker.Infra.Campaign.Factories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CampaignWatchWorker.Infra.Campaign.Services
{
    public class CampaignReadModelService : ICampaignReadModelService
    {
        /// <summary>
        /// A fábrica para obter conexões com os bancos de dados de campanha dos clientes.
        /// </summary>
        private readonly ICampaignMongoFactory _factory;

        /// <summary>
        /// Inicializa uma nova instância da classe CampaignReadService.
        /// </summary>
        /// <param name="factory">A fábrica de conexão com os bancos de dados de campanha a ser injetada.</param>
        public CampaignReadModelService(ICampaignMongoFactory factory)
        {
            _factory = factory;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<CampaignReadModel>> GetCampaigns(string dbName)
        {
            var db = _factory.GetDatabase(dbName);
            var collection = db.GetCollection<CampaignReadModel>("Campaign");
            return await collection.Find(_ => true).ToListAsync();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ExecutionReadModel>> GetExecutionsByCampaign(string dbName, string campaignId)
        {
            var db = _factory.GetDatabase(dbName);
            var collection = db.GetCollection<ExecutionReadModel>("ExecutionPlan");

            return await collection
                .Find(x => x.CampaignId.ToString() == campaignId && x.FlagCount == false)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<CampaignReadModel> GetCampaignById(string dbName, string campaignId)
        {
            var db = _factory.GetDatabase(dbName);
            var collection = db.GetCollection<CampaignReadModel>("Campaign");

            if (!ObjectId.TryParse(campaignId, out var campaignObjectId))
            {
                return null;
            }

            return await collection.Find(x => x.Id == campaignObjectId.ToString()).FirstOrDefaultAsync();
        }
    }
}
