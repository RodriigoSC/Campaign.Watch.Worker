using CampaignWatchWorker.Domain.Models;
using CampaignWatchWorker.Domain.Models.Diagnostics;
using CampaignWatchWorker.Domain.Models.Enums;

namespace CampaignWatchWorker.Application.Validators
{
    public class WaitStepValidator : IStepValidator
    {
        public WorkflowStepTypeEnum SupportedStepType => WorkflowStepTypeEnum.Wait;

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
                diagnostic.Message = $"Step de espera falhou: {step.Error}";
                diagnostic.AdditionalData["OriginalError"] = step.Error;
                return diagnostic;
            }

            // Tentar extrair informações de tempo de espera dos dados da execução
            // Assumindo que pode haver um campo indicando o horário planejado
            DateTime? scheduledWaitTime = null;

            // Exemplo: Se o step tiver metadados com horário programado
            // scheduledWaitTime = ExtractScheduledTime(step);

            if (step.Status == "Completed")
            {
                diagnostic.Severity = HealthStatusEnum.Healthy;
                diagnostic.Message = "Step de espera completado com sucesso";
                return diagnostic;
            }

            // Se o step está em progresso
            if (step.Status == "InProgress" || step.Status == "Running")
            {
                // Verificar se passou do tempo esperado
                if (scheduledWaitTime.HasValue && DateTime.UtcNow > scheduledWaitTime.Value.AddMinutes(5))
                {
                    diagnostic.DiagnosticType = DiagnosticTypeEnum.WaitStepMissed;
                    diagnostic.Severity = HealthStatusEnum.Warning;
                    diagnostic.Message = $"ALERTA: Step de espera deveria ter sido executado em {scheduledWaitTime.Value:dd/MM/yyyy HH:mm}, mas ainda está pendente.";
                    diagnostic.AdditionalData["ScheduledTime"] = scheduledWaitTime.Value;
                    diagnostic.AdditionalData["DelayMinutes"] = (DateTime.UtcNow - scheduledWaitTime.Value).TotalMinutes;
                    return diagnostic;
                }

                diagnostic.Severity = HealthStatusEnum.Healthy;
                diagnostic.Message = "Aguardando período de espera configurado";

                if (scheduledWaitTime.HasValue)
                {
                    diagnostic.AdditionalData["ScheduledTime"] = scheduledWaitTime.Value;
                }

                return diagnostic;
            }

            // Status desconhecido ou pendente
            if (scheduledWaitTime.HasValue && DateTime.UtcNow > scheduledWaitTime.Value.AddMinutes(15))
            {
                diagnostic.DiagnosticType = DiagnosticTypeEnum.WaitStepMissed;
                diagnostic.Severity = HealthStatusEnum.Error;
                diagnostic.Message = $"ERRO: Step de espera não foi executado no horário programado ({scheduledWaitTime.Value:dd/MM/yyyy HH:mm}). Atraso de {(DateTime.UtcNow - scheduledWaitTime.Value).TotalMinutes:F0} minutos.";
                diagnostic.AdditionalData["ScheduledTime"] = scheduledWaitTime.Value;
                diagnostic.AdditionalData["DelayMinutes"] = (DateTime.UtcNow - scheduledWaitTime.Value).TotalMinutes;
                return diagnostic;
            }

            diagnostic.Severity = HealthStatusEnum.Healthy;
            diagnostic.Message = "Step de espera aguardando execução";
            return diagnostic;
        }        
    }
}
