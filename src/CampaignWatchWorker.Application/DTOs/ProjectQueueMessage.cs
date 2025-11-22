namespace CampaignWatchWorker.Application.DTOs
{
    public class ProjectQueueMessage
    {
        public string ClientName { get; set; }

        public string ProjectId { get; set; }

        public string? CampaignId { get; set; }
    }
}
