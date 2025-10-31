using CampaignWatchWorker.Domain.Models;
using CampaignWatchWorker.Domain.Models.Diagnostics;
using CampaignWatchWorker.Domain.Models.Enums;

namespace CampaignWatchWorker.Application.Validators
{
    public class FilterStepValidator : IStepValidator
    {
        public WorkflowStepTypeEnum SupportedStepType => WorkflowStepTypeEnum.Filter;

        private static readonly TimeSpan CriticalTimeout = TimeSpan.FromHours(2);
        private static readonly TimeSpan WarningTimeout = TimeSpan.FromHours(1);

        public async Task<StepDiagnostic> ValidateAsync(WorkflowStep step, ExecutionModel execution, CampaignModel campaign)
        {
            var diagnostic = new StepDiagnostic
            {
                StepId = step.OriginalStepId,
                StepName = step.Name,
                DetectedAt = DateTime.UtcNow
            };

            // 1. Verificar se há um erro explícito no step
            if (!string.IsNullOrEmpty(step.Error))
            {
                diagnostic.DiagnosticType = DiagnosticTypeEnum.StepFailed;
                diagnostic.Severity = HealthStatusEnum.Error;
                diagnostic.Message = $"Etapa de filtro falhou: {step.Error}";
                diagnostic.AdditionalData["OriginalError"] = step.Error;
                return diagnostic;
            }

            // 2. Se o step foi concluído, está saudável
            if (step.Status == "Completed")
            {
                diagnostic.Severity = HealthStatusEnum.Healthy;
                diagnostic.Message = "Etapa de filtro concluída com sucesso.";
                return diagnostic;
            }

            // 3. Se não foi concluído, verificar timeout
            // Nota: O modelo WorkflowStep não possui StartDate.
            // Usamos o StartDate da Execução como a melhor aproximação.
            if (execution.StartDate.HasValue)
            {
                var runningTime = DateTime.UtcNow - execution.StartDate.Value;

                if (runningTime > CriticalTimeout)
                {
                    diagnostic.DiagnosticType = DiagnosticTypeEnum.FilterStuck;
                    diagnostic.Severity = HealthStatusEnum.Critical;
                    diagnostic.Message = $"CRÍTICO: Etapa de filtro em execução há mais de {CriticalTimeout.TotalHours} horas. Pode estar travada.";
                    diagnostic.AdditionalData["RunningTimeHours"] = runningTime.TotalHours;
                }
                else if (runningTime > WarningTimeout)
                {
                    diagnostic.DiagnosticType = DiagnosticTypeEnum.FilterStuck;
                    diagnostic.Severity = HealthStatusEnum.Warning;
                    diagnostic.Message = $"ALERTA: Etapa de filtro em execução há mais de {WarningTimeout.TotalHours} hora(s).";
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
