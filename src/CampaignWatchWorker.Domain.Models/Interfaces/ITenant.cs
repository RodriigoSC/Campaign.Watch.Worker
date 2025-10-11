namespace CampaignWatchWorker.Domain.Models.Interfaces
{
    public interface ITenant
    {
        public string Id { get; }
        public string Name { get; }
        public string DatabaseCampaign { get; }
        public string DatabaseEffmail { get; }
        public string DatabaseEffsms { get; }
        public string DatabaseEffpush { get; }
        public string DatabaseEffwhatsapp { get; }
        public string QueueNameMonitoring { get; }
    }
}
