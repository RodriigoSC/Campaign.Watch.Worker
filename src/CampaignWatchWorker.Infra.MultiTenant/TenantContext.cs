using CampaignWatchWorker.Domain.Models.Entities.Clients;
using CampaignWatchWorker.Domain.Models.Interfaces;

namespace CampaignWatchWorker.Infra.MultiTenant
{
    public class TenantContext : ITenantContext
    {
        public ClientMoldel Client { get; private set; }

        public void SetClient(ClientMoldel client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }
    }
}
