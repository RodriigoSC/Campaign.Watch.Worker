using CampaignWatchWorker.Domain.Models.Entities.Alerts;
using CampaignWatchWorker.Domain.Models.Entities.Campaigns;
using CampaignWatchWorker.Domain.Models.Entities.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CampaignWatchWorker.Domain.Models.Interfaces.Services.Email
{
    public interface IEmailDispatcherService
    {
        Task SendAsync(AlertConfigurationModel rule, CampaignModel campaign, StepDiagnosticModel issue);
    }
}
