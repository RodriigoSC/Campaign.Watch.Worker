using CampaignWatchWorker.Application.DTOs;

namespace CampaignWatchWorker.Application.Processor
{
    public interface IProcessorApplication
    {
        Task ProcessProjectScopeAsync(ProjectQueueMessage message);
    }
}
