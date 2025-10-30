namespace CampaignWatchWorker.Domain.Models.Interfaces.Services
{
    public interface IClientConfigService
    {
        Task<List<ClientConfig>> GetAllActiveClientsAsync();
    }
}
