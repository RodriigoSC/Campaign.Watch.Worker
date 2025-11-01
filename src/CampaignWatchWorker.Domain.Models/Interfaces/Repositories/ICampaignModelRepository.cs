namespace CampaignWatchWorker.Domain.Models.Interfaces.Repositories
{
    public interface ICampaignModelRepository
    {
        Task<CampaignModel> CreateCampaignAsync(CampaignModel campaignModel);
        Task<bool> UpdateCampaignAsync(CampaignModel campaignModel);
        Task<CampaignModel?> GetCampaignByIdAsync(string clientName, string idCampaign);
        Task<IEnumerable<CampaignModel>> GetDueCampaignsForClientAsync(string clientName);
    }
}
