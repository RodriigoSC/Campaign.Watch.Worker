namespace CampaignWatchWorker.Domain.Models.Interfaces
{
    public interface ITenant
    {
        public string Id { get; }
        public string Name { get; }
        public string Database { get; }
        public string GenerateLeadReport { get; }
        public string QueueNameProcessorLeads { get; }
        public string QueueNameProcessorLeadsUnitary { get; }
        public string QueueNameProcessorLeadsShort { get; }
        public string SftpHost { get; }
        public string SftpPassword { get; }
        public string SftpUsername { get; }
        public string SftpDirectory { get; }
    }
}
