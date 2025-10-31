using CampaignWatchWorker.Domain.Models.Read.Campaign;

namespace CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Campaign
{
    public interface ICampaignReadModelService
    {
        Task<IEnumerable<CampaignReadModel>> GetCampaigns();
        Task<IEnumerable<ExecutionReadModel>> GetExecutionsByCampaign(string campaignId);
        Task<CampaignReadModel> GetCampaignById(string campaignId);
        Task<IEnumerable<CampaignReadModel>> GetDiscoverableCampaignsAsync();
    }
}
