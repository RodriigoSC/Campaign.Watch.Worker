using CampaignWatchWorker.Domain.Models;
using CampaignWatchWorker.Domain.Models.Diagnostics;
using CampaignWatchWorker.Domain.Models.Enums;

namespace CampaignWatchWorker.Application.Validators
{
    public class FilterStepValidator : IStepValidator
    {
        public WorkflowStepTypeEnum SupportedStepType => WorkflowStepTypeEnum.Filter;

        private static readonly TimeSpan CriticalTimeout = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan WarningTimeout = TimeSpan.FromMinutes(10);

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
                diagnostic.Message = $"Etapa de filtro falhou: {step.Error}";
                diagnostic.AdditionalData["OriginalError"] = step.Error;
                return diagnostic;
            }

            if (step.Status == "Completed")
            {
                diagnostic.Severity = HealthStatusEnum.Healthy;
                diagnostic.Message = "Etapa de filtro concluída com sucesso.";
                return diagnostic;
            }
            
            if (step.StartDate.HasValue)
            {
                var runningTime = DateTime.UtcNow - step.StartDate.Value;

                if (runningTime > CriticalTimeout)
                {
                    diagnostic.DiagnosticType = DiagnosticTypeEnum.FilterStuck;
                    diagnostic.Severity = HealthStatusEnum.Critical;
                    diagnostic.Message = $"CRÍTICO: Etapa de filtro em execução há mais de {CriticalTimeout.TotalHours} minutos. Pode estar travada.";
                    diagnostic.AdditionalData["RunningTimeHours"] = runningTime.TotalHours;
                }
                else if (runningTime > WarningTimeout)
                {
                    diagnostic.DiagnosticType = DiagnosticTypeEnum.FilterStuck;
                    diagnostic.Severity = HealthStatusEnum.Warning;
                    diagnostic.Message = $"ALERTA: Etapa de filtro em execução há mais de {WarningTimeout.TotalHours} minutos.";
                    diagnostic.AdditionalData["RunningTimeHours"] = runningTime.TotalHours;
                }
                else
                {
                    diagnostic.Severity = HealthStatusEnum.Healthy;
                    diagnostic.Message = "Etapa de filtro em execução.";
                }
            }
            else
            {
                diagnostic.Severity = HealthStatusEnum.Healthy;
                diagnostic.Message = "Etapa de filtro aguardando execução (execução sem data de início).";
            }

            return diagnostic;
        }
    }
}
