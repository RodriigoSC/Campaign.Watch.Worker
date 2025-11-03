using CampaignWatchWorker.Domain.Models.Entities.Clients;

namespace CampaignWatchWorker.Domain.Models.Interfaces.Services
{
    public interface IClientConfigService
    {
        Task<List<ClientMoldel>> GetAllActiveClientsAsync();
    }
}
