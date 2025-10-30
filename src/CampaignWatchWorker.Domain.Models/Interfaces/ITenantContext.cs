namespace CampaignWatchWorker.Domain.Models.Interfaces
{
    public interface ITenantContext
    {        
        ClientConfig Client { get; }

        void SetClient(ClientConfig client);
    }
}
