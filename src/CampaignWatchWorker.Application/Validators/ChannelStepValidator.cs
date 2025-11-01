using CampaignWatchWorker.Domain.Models;
using CampaignWatchWorker.Domain.Models.Diagnostics;
using CampaignWatchWorker.Domain.Models.Enums;

namespace CampaignWatchWorker.Application.Validators
{
    public class ChannelStepValidator : IStepValidator
    {
        public WorkflowStepTypeEnum SupportedStepType => WorkflowStepTypeEnum.Channel;

        private const double CriticalErrorRate = 0.5; // 50%
        private const double WarningErrorRate = 0.2;  // 20%
        private static readonly TimeSpan FileProcessingTimeout = TimeSpan.FromHours(1);

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
                diagnostic.Message = $"Etapa de canal falhou: {step.Error}";
                diagnostic.AdditionalData["OriginalError"] = step.Error;
                return diagnostic;
            }

            if (step.IntegrationData == null)
            {
                if (step.MonitoringNotes?.Contains("ERRO") == true)
                {
                    diagnostic.DiagnosticType = DiagnosticTypeEnum.IntegrationError;
                    diagnostic.Severity = HealthStatusEnum.Error;
                    diagnostic.Message = "Falha ao buscar dados de integração do canal. Canal desconhecido ou falha na conexão.";
                    diagnostic.AdditionalData["ChannelName"] = step.ChannelName ?? "Desconhecido";
                    return diagnostic;
                }

                diagnostic.Severity = HealthStatusEnum.Warning;
                diagnostic.Message = "Dados de integração do canal não encontrados (IntegrationData is null).";
                return diagnostic;
            }

            var integration = step.IntegrationData;
            diagnostic.AdditionalData["ChannelName"] = integration.ChannelName;
            diagnostic.AdditionalData["IntegrationStatus"] = integration.IntegrationStatus;

            // 3. Verificar status de erro na integração (ex: "Error" no Effmail)
            if (integration.IntegrationStatus == "Error")
            {
                diagnostic.DiagnosticType = DiagnosticTypeEnum.IntegrationError;
                diagnostic.Severity = HealthStatusEnum.Error;
                diagnostic.Message = "O sistema de canal reportou um status de 'Erro' para esta trigger.";
                return diagnostic;
            }

            // 4. Analisar taxas de erro de Leads
            if (integration.Leads != null)
            {
                var leads = integration.Leads;
                var totalProcessed = (leads.Success ?? 0) + (leads.Error ?? 0) + (leads.Blocked ?? 0) + (leads.Optout ?? 0) + (leads.Deduplication ?? 0);

                if (totalProcessed > 0 && (leads.Error ?? 0) > 0)
                {
                    double errorRate = (double)leads.Error.Value / totalProcessed;

                    if (errorRate > CriticalErrorRate)
                    {
                        diagnostic.DiagnosticType = DiagnosticTypeEnum.IntegrationError;
                        diagnostic.Severity = HealthStatusEnum.Critical;
                        diagnostic.Message = $"CRÍTICO: Alta taxa de erro no envio ({errorRate:P0}). {leads.Error} de {totalProcessed} leads falharam.";
                    }
                    else if (errorRate > WarningErrorRate)
                    {
                        diagnostic.DiagnosticType = DiagnosticTypeEnum.IntegrationError;
                        diagnostic.Severity = HealthStatusEnum.Warning;
                        diagnostic.Message = $"ALERTA: Taxa de erro no envio elevada ({errorRate:P0}). {leads.Error} de {totalProcessed} leads falharam.";
                    }

                    diagnostic.AdditionalData["ErrorRate"] = errorRate;
                    diagnostic.AdditionalData["LeadsSuccess"] = leads.Success;
                    diagnostic.AdditionalData["LeadsError"] = leads.Error;
                    diagnostic.AdditionalData["TotalProcessed"] = totalProcessed;
                }
            }           

            if (diagnostic.Severity == default(HealthStatusEnum))
            {
                diagnostic.Severity = HealthStatusEnum.Healthy;
                diagnostic.Message = $"Etapa de canal operando. Status: {step.Status}";
            }

            return diagnostic;
        }
    }
}
