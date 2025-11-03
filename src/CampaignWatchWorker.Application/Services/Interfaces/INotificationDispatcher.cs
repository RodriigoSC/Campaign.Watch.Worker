using CampaignWatchWorker.Domain.Models.Entities.Alerts;
using CampaignWatchWorker.Domain.Models.Entities.Campaigns;
using CampaignWatchWorker.Domain.Models.Entities.Diagnostics;

namespace CampaignWatchWorker.Application.Services.Interfaces
{
    public interface INotificationDispatcher
    {
        Task SendAsync(AlertConfigurationModel rule, CampaignModel campaign, StepDiagnosticModel issue);
    }
}
