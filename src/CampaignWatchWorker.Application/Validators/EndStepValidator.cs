using CampaignWatchWorker.Domain.Models;
using CampaignWatchWorker.Domain.Models.Diagnostics;
using CampaignWatchWorker.Domain.Models.Enums;

namespace CampaignWatchWorker.Application.Validators
{
    public class EndStepValidator : IStepValidator
    {
        public WorkflowStepTypeEnum SupportedStepType => WorkflowStepTypeEnum.End;

        public async Task<StepDiagnostic> ValidateAsync(WorkflowStep step,ExecutionModel execution,CampaignModel campaign)
        {
            var diagnostic = new StepDiagnostic
            {
                StepId = step.OriginalStepId,
                StepName = step.Name,
                DetectedAt = DateTime.UtcNow
            };

            // 1. Verificar erro explícito
            if (!string.IsNullOrEmpty(step.Error))
            {
                diagnostic.DiagnosticType = DiagnosticTypeEnum.StepFailed;
                diagnostic.Severity = HealthStatusEnum.Error;
                diagnostic.Message = $"Etapa final falhou: {step.Error}";
                diagnostic.AdditionalData["OriginalError"] = step.Error;
                return diagnostic;
            }

            // 2. Verificar consistência de status
            if (step.Status == "Completed" && execution.Status != "Completed")
            {
                diagnostic.DiagnosticType = DiagnosticTypeEnum.IncompleteExecution;
                diagnostic.Severity = HealthStatusEnum.Warning;
                diagnostic.Message = "ALERTA: Etapa final concluída, mas a execução geral ainda não foi marcada como 'Completed'.";
                diagnostic.AdditionalData["ExecutionStatus"] = execution.Status;
            }
            else if (step.Status != "Completed" && execution.Status == "Completed")
            {
                diagnostic.DiagnosticType = DiagnosticTypeEnum.IncompleteExecution;
                diagnostic.Severity = HealthStatusEnum.Error;
                diagnostic.Message = "ERRO: Execução marcada como 'Completed', mas a etapa final não foi concluída.";
                diagnostic.AdditionalData["StepStatus"] = step.Status;
            }
            else
            {
                diagnostic.Severity = HealthStatusEnum.Healthy;
                diagnostic.Message = "Etapa final concluída consistentemente com a execução.";
            }

            return diagnostic;
        }
    }
}
