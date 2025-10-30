using CampaignWatchWorker.Domain.Models;
using CampaignWatchWorker.Domain.Models.Interfaces;

namespace CampaignWatchWorker.Infra.MultiTenant
{
    public class TenantContext : ITenantContext
    {
        public ClientConfig Client { get; private set; }

        public void SetClient(ClientConfig client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }
    }
}
