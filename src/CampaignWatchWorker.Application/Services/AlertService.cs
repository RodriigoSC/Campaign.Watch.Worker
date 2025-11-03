using CampaignWatchWorker.Application.Services.Interfaces;
using CampaignWatchWorker.Domain.Models.Entities.Alerts;
using CampaignWatchWorker.Domain.Models.Entities.Campaigns;
using CampaignWatchWorker.Domain.Models.Entities.Diagnostics;
using CampaignWatchWorker.Domain.Models.Enums;
using CampaignWatchWorker.Domain.Models.Enums.Alerts;
using CampaignWatchWorker.Domain.Models.Interfaces;
using CampaignWatchWorker.Domain.Models.Interfaces.Repositories.Alerts;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace CampaignWatchWorker.Application.Services
{
    public class AlertService : IAlertService
    {
        private readonly IAlertConfigurationRepository _configRepository;
        private readonly IAlertHistoryRepository _historyRepository;
        private readonly ITenantContext _tenantContext;
        private readonly IEmailDispatcher _emailDispatcher;
        private readonly IWebhookDispatcher _webhookDispatcher;
        private readonly ILogger<AlertService> _logger;

        public AlertService(
            IAlertConfigurationRepository configRepository,
            IAlertHistoryRepository historyRepository,
            ITenantContext tenantContext,
            IEmailDispatcher emailDispatcher,
            IWebhookDispatcher webhookDispatcher,
            ILogger<AlertService> logger)
        {
            _configRepository = configRepository;
            _historyRepository = historyRepository;
            _tenantContext = tenantContext;
            _emailDispatcher = emailDispatcher;
            _webhookDispatcher = webhookDispatcher;
            _logger = logger;
        }

        public async Task ProcessAlertsAsync(CampaignModel campaign, ExecutionDiagnosticModel executionDiagnostic)
        {
            if (executionDiagnostic?.StepDiagnostics == null || !executionDiagnostic.StepDiagnostics.Any())
            {
                return; // Sem diagnósticos, sem alertas.
            }

            // 1. Buscar regras aplicáveis (Globais + Específicas do Cliente)
            var clientRulesTask = _configRepository.GetByScopeAsync(_tenantContext.Client.Id);
            var globalRulesTask = _configRepository.GetByScopeAsync(null); // null = Global

            await Task.WhenAll(clientRulesTask, globalRulesTask);

            var allRules = (await clientRulesTask)
                .Concat(await globalRulesTask)
                .Where(r => r.IsActive)
                .ToList();

            if (!allRules.Any())
            {
                return; // Nenhuma regra de alerta ativa
            }

            // 2. Iterar sobre os problemas (diagnósticos) encontrados
            foreach (var issue in executionDiagnostic.StepDiagnostics)
            {
                // Ignorar diagnósticos "Healthy"
                if (issue.Severity == HealthStatusEnum.Healthy) continue;

                // 3. Verificar quais regras o problema aciona
                var matchingRules = FindMatchingRules(issue, allRules);

                foreach (var rule in matchingRules)
                {
                    await DispatchAlertAsync(rule, campaign, issue);
                }
            }
        }

        private List<AlertConfigurationModel> FindMatchingRules(StepDiagnosticModel issue, List<AlertConfigurationModel> allRules)
        {
            var matchingRules = new List<AlertConfigurationModel>();

            foreach (var rule in allRules)
            {
                // Mapear a gravidade do diagnóstico (Worker) para a gravidade da regra (API)
                if (!TryMapSeverity(issue.Severity, out AlertSeverity issueSeverity))
                {
                    continue; // Gravidade desconhecida
                }

                // A. Verificar Gravidade Mínima
                // Se a regra não define gravidade (null), ela pega qualquer uma.
                // Se define (ex: Warning), a gravidade do problema (ex: Error) deve ser >=
                if (rule.MinSeverity.HasValue && issueSeverity < rule.MinSeverity.Value)
                {
                    continue; // Problema é menos grave que o mínimo da regra
                }

                // B. Verificar Condição
                // Se a regra não define condição (null), ela pega qualquer uma.
                if (rule.ConditionType.HasValue)
                {
                    // Mapear o tipo de diagnóstico (Worker) para o tipo de condição (API)
                    if (!TryMapCondition(issue.DiagnosticType, out AlertConditionType issueConditionType))
                    {
                        continue; // Diagnóstico não mapeável
                    }

                    if (issueConditionType != rule.ConditionType.Value)
                    {
                        continue; // O tipo de problema não bate com o da regra
                    }
                }

                // Se passou nas duas verificações, a regra é acionada
                matchingRules.Add(rule);
            }

            return matchingRules;
        }

        private async Task DispatchAlertAsync(AlertConfigurationModel rule, CampaignModel campaign, StepDiagnosticModel issue)
        {
            try
            {
                // 1. Disparar a notificação
                switch (rule.Type)
                {
                    case AlertChannelType.Email:
                        await _emailDispatcher.SendAsync(rule, campaign, issue);
                        break;
                    case AlertChannelType.Webhook:
                        await _webhookDispatcher.SendAsync(rule, campaign, issue);
                        break;
                }

                // 2. Salvar no histórico
                var historyEntry = new AlertHistoryModel
                {
                    Id = ObjectId.GenerateNewId(),
                    ClientId = rule.ClientId,
                    AlertConfigurationId = rule.Id,
                    Severity = issue.Severity.ToString(),
                    Message = issue.Message,
                    CampaignName = campaign.Name,
                    StepName = issue.StepName,
                    DetectedAt = issue.DetectedAt
                };
                await _historyRepository.CreateAsync(historyEntry);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao despachar alerta ID {AlertId} para regra '{RuleName}'", rule.Id, rule.Name);
            }
        }


        // --- Mapeamento entre Enums do Worker e Enums da API/Alerta ---

        private bool TryMapSeverity(HealthStatusEnum workerSeverity, out AlertSeverity alertSeverity)
        {
            switch (workerSeverity)
            {
                case HealthStatusEnum.Healthy:
                    alertSeverity = AlertSeverity.Healthy;
                    return true;
                case HealthStatusEnum.Warning:
                    alertSeverity = AlertSeverity.Warning;
                    return true;
                case HealthStatusEnum.Error:
                    alertSeverity = AlertSeverity.Error;
                    return true;
                case HealthStatusEnum.Critical:
                    alertSeverity = AlertSeverity.Critical;
                    return true;
                default:
                    alertSeverity = default;
                    return false;
            }
        }

        private bool TryMapCondition(DiagnosticTypeEnum workerDiagnostic, out AlertConditionType alertCondition)
        {
            switch (workerDiagnostic)
            {
                case DiagnosticTypeEnum.StepFailed:
                    alertCondition = AlertConditionType.StepFailed;
                    return true;
                case DiagnosticTypeEnum.ExecutionDelayed:
                    alertCondition = AlertConditionType.ExecutionDelayed;
                    return true;
                case DiagnosticTypeEnum.FilterStuck:
                    alertCondition = AlertConditionType.FilterStuck;
                    return true;
                case DiagnosticTypeEnum.IntegrationError:
                    alertCondition = AlertConditionType.IntegrationError;
                    return true;
                case DiagnosticTypeEnum.CampaignNotFinalized:
                    alertCondition = AlertConditionType.CampaignNotFinalized;
                    return true;
                // Outros tipos de diagnóstico do worker (WaitStepMissed, etc.) podem ser mapeados aqui
                default:
                    alertCondition = default;
                    return false; // Este tipo de diagnóstico não pode disparar um alerta
            }
        }
    }
}
