using CampaignWatchWorker.Data.Factories;
using CampaignWatchWorker.Domain.Models.Entities.Clients;
using CampaignWatchWorker.Domain.Models.Interfaces.Services;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace CampaignWatchWorker.Data.Services
{
    public class ClientConfigService : IClientConfigService
    {
        private readonly IPersistenceMongoFactory _persistenceFactory;
        private readonly ILogger<ClientConfigService> _logger;

        public ClientConfigService(IPersistenceMongoFactory persistenceFactory, ILogger<ClientConfigService> logger)
        {
            _persistenceFactory = persistenceFactory;
            _logger = logger;
        }

        public async Task<List<ClientMoldel>> GetAllActiveClientsAsync()
        {
            try
            {
                var db = _persistenceFactory.GetDatabase();
                var collection = db.GetCollection<ClientMoldel>("Clients");
                return await collection.Find(c => c.IsActive).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Não foi possível carregar a configuração dos clientes do banco de dados.");
                return new List<ClientMoldel>();
            }
        }
    }
}
