namespace CampaignWatchWorker.Application.Processor
{
    public interface IProcessorApplication
    {
        Task DiscoverNewCampaignsAsync();

        Task ProcessCampaignByEventAsync(string campaignId);
    }
}
