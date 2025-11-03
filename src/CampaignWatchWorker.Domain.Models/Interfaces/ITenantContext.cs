using CampaignWatchWorker.Domain.Models.Entities.Clients;

namespace CampaignWatchWorker.Domain.Models.Interfaces
{
    public interface ITenantContext
    {
        ClientMoldel Client { get; }

        void SetClient(ClientMoldel client);
    }
}
