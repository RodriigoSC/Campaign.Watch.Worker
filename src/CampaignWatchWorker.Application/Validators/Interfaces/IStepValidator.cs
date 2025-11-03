using CampaignWatchWorker.Domain.Models.Entities.Campaigns;
using CampaignWatchWorker.Domain.Models.Entities.Diagnostics;
using CampaignWatchWorker.Domain.Models.Enums;

namespace CampaignWatchWorker.Application.Validators
{
    public interface IStepValidator
    {
        WorkflowStepTypeEnum SupportedStepType { get; }

        Task<StepDiagnosticModel> ValidateAsync(WorkflowStep step, ExecutionModel execution, CampaignModel campaign);
    }
}
