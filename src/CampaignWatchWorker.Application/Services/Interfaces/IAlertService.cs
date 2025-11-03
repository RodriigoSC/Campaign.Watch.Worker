using CampaignWatchWorker.Domain.Models.Entities.Campaigns;
using CampaignWatchWorker.Domain.Models.Entities.Diagnostics;

namespace CampaignWatchWorker.Application.Services.Interfaces
{
    public interface IAlertService
    {
        Task ProcessAlertsAsync(CampaignModel campaign, ExecutionDiagnosticModel executionDiagnosticModel);
    }
}
