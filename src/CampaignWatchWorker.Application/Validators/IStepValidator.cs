using CampaignWatchWorker.Domain.Models;
using CampaignWatchWorker.Domain.Models.Diagnostics;
using CampaignWatchWorker.Domain.Models.Enums;

namespace CampaignWatchWorker.Application.Validators
{
    public interface IStepValidator
    {
        WorkflowStepTypeEnum SupportedStepType { get; }

        Task<StepDiagnostic> ValidateAsync(WorkflowStep step, ExecutionModel execution, CampaignModel campaign);
    }
}
