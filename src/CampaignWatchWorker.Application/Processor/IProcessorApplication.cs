namespace CampaignWatchWorker.Application.Processor
{
    public interface IProcessorApplication
    {
        Task ProcessDueCampaignsForClientAsync();
    }
}
