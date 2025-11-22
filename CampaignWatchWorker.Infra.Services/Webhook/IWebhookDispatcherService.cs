using CampaignWatchWorker.Domain.Models.Entities.Alerts;
using CampaignWatchWorker.Domain.Models.Entities.Campaigns;
using CampaignWatchWorker.Domain.Models.Entities.Diagnostics;

namespace CampaignWatchWorker.Infra.Services.Webhook
{
    public interface IWebhookDispatcherService
    {
        Task SendAsync(AlertConfigurationModel rule, CampaignModel campaign, StepDiagnosticModel issue);
    }
}
