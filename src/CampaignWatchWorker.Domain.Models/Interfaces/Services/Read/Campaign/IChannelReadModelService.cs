using CampaignWatchWorker.Domain.Models.Read.Campaign;

namespace CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Campaign
{
    public interface IChannelReadModelService
    {
        Task<Dictionary<string, ConsolidatedChannelReadModel>> GetConsolidatedChannelDataAsync(string executionId);
    }
}
