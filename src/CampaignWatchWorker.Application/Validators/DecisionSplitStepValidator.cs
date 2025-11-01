using CampaignWatchWorker.Domain.Models;
using CampaignWatchWorker.Domain.Models.Diagnostics;
using CampaignWatchWorker.Domain.Models.Enums;

namespace CampaignWatchWorker.Application.Validators
{
    public class DecisionSplitStepValidator : IStepValidator
    {
        public WorkflowStepTypeEnum SupportedStepType => WorkflowStepTypeEnum.DecisionSplit;

        public async Task<StepDiagnostic> ValidateAsync(WorkflowStep step, ExecutionModel execution, CampaignModel campaign)
        {
            var diagnostic = new StepDiagnostic
            {
                StepId = step.OriginalStepId,
                StepName = step.Name,
                DetectedAt = DateTime.UtcNow
            };

            if (!string.IsNullOrEmpty(step.Error))
            {
                diagnostic.DiagnosticType = DiagnosticTypeEnum.StepFailed;
                diagnostic.Severity = HealthStatusEnum.Error;
                diagnostic.Message = $"Etapa de decisão falhou: {step.Error}";
                diagnostic.AdditionalData["OriginalError"] = step.Error;
            }
            else
            {
                diagnostic.Severity = HealthStatusEnum.Healthy;
                diagnostic.Message = "Etapa de decisão executada com sucesso.";
            }

            return diagnostic;
        }
    }
}
