using CampaignWatchWorker.Domain.Models;
using CampaignWatchWorker.Domain.Models.Diagnostics;
using CampaignWatchWorker.Domain.Models.Enums;

namespace CampaignWatchWorker.Application.Validators
{
    public class DatedStepValidator : IStepValidator
    {
        public WorkflowStepTypeEnum SupportedStepType => WorkflowStepTypeEnum.Dated;

        private static readonly TimeSpan WarningDelay = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan CriticalDelay = TimeSpan.FromMinutes(10);

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
                diagnostic.Message = $"Etapa 'Espera por uma data' falhou: {step.Error}";
                diagnostic.AdditionalData["OriginalError"] = step.Error;
                return diagnostic;
            }

            if (step.Status == "Completed")
            {
                diagnostic.Severity = HealthStatusEnum.Healthy;
                diagnostic.Message = "Etapa 'Espera por uma data' concluída com sucesso.";
                return diagnostic;
            }
            
            DateTime? scheduledDate = null;
            if (campaign.WorkflowConfiguration.TryGetValue(step.OriginalStepId, out var stepConfig))
            {
                scheduledDate = stepConfig.ScheduledExecutionDate;
            }

            if (step.Status == "Running" || step.Status == "InProgress")
            {
                if (!scheduledDate.HasValue)
                {
                    diagnostic.Severity = HealthStatusEnum.Warning;
                    diagnostic.Message = "Etapa 'Espera por uma data' está em 'Running', mas não foi possível encontrar sua data de agendamento na configuração da campanha.";
                    return diagnostic;
                }

                var now = DateTime.UtcNow;

                if (now > scheduledDate.Value.Add(CriticalDelay))
                {
                    diagnostic.DiagnosticType = DiagnosticTypeEnum.ExecutionDelayed;
                    diagnostic.Severity = HealthStatusEnum.Critical;
                    diagnostic.Message = $"CRÍTICO: Etapa 'Espera por uma data' agendada para {scheduledDate.Value:G} (UTC) ainda está 'Running'. Atraso de mais de {CriticalDelay.TotalHours} hora(s).";
                }
                else if (now > scheduledDate.Value.Add(WarningDelay))
                {
                    diagnostic.DiagnosticType = DiagnosticTypeEnum.ExecutionDelayed;
                    diagnostic.Severity = HealthStatusEnum.Warning;
                    diagnostic.Message = $"ALERTA: Etapa 'Espera por uma data' agendada para {scheduledDate.Value:G} (UTC) ainda está 'Running'. Atraso de mais de {WarningDelay.TotalMinutes} minutos(s).";
                }
                else
                {
                    diagnostic.Severity = HealthStatusEnum.Healthy;
                    diagnostic.Message = $"Etapa aguardando a data/hora agendada: {scheduledDate.Value:G} (UTC).";
                }

                diagnostic.AdditionalData["ScheduledExecutionDate"] = scheduledDate.Value;
                return diagnostic;
            }

            if (step.Status == "Waiting")
            {
                diagnostic.Severity = HealthStatusEnum.Healthy;
                diagnostic.Message = "Etapa 'Espera por uma data' aguardando etapa anterior.";
                if (scheduledDate.HasValue)
                {
                    diagnostic.AdditionalData["ScheduledExecutionDate"] = scheduledDate.Value;
                }
                return diagnostic;
            }

            diagnostic.Severity = HealthStatusEnum.Healthy;
            diagnostic.Message = $"Etapa 'Espera por uma data' com status: {step.Status}";
            return diagnostic;
        }
    }
}