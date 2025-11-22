using CampaignWatchWorker.Application.Services.Interfaces;
using CampaignWatchWorker.Domain.Models.Entities.Alerts;
using CampaignWatchWorker.Domain.Models.Entities.Campaigns;
using CampaignWatchWorker.Domain.Models.Entities.Diagnostics;
using CampaignWatchWorker.Domain.Models.Enums;
using CampaignWatchWorker.Domain.Models.Enums.Alerts;
using CampaignWatchWorker.Domain.Models.Interfaces;
using CampaignWatchWorker.Domain.Models.Interfaces.Repositories.Alerts;
using CampaignWatchWorker.Domain.Models.Interfaces.Services.Email;
using CampaignWatchWorker.Domain.Models.Interfaces.Services.Webhook;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace CampaignWatchWorker.Application.Services
{
    public class AlertService : IAlertService
    {
        private readonly IAlertConfigurationRepository _configRepository;
        private readonly IAlertHistoryRepository _historyRepository;
        private readonly ITenantContext _tenantContext;
        private readonly IEmailDispatcherService _emailDispatcher;
        private readonly IWebhookDispatcherService _webhookDispatcher;
        private readonly ILogger<AlertService> _logger;

        public AlertService(
            IAlertConfigurationRepository configRepository,
            IAlertHistoryRepository historyRepository,
            ITenantContext tenantContext,
            IEmailDispatcherService emailDispatcher,
            IWebhookDispatcherService webhookDispatcher,
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
            // Se não houver diagnósticos ou a lista estiver vazia, não faz nada
            if (executionDiagnostic?.StepDiagnostics == null || !executionDiagnostic.StepDiagnostics.Any())
            {
                return;
            }

            // 1. Buscar regras de alerta ativas (Do Cliente e Globais)
            var clientRulesTask = _configRepository.GetByScopeAsync(_tenantContext.Client.Id);
            var globalRulesTask = _configRepository.GetByScopeAsync(null); // null = Regras Globais

            await Task.WhenAll(clientRulesTask, globalRulesTask);

            var allRules = (await clientRulesTask)
                .Concat(await globalRulesTask)
                .Where(r => r.IsActive)
                .ToList();

            if (!allRules.Any()) return; // Sem regras, sem alertas.

            // 2. Analisar cada problema encontrado na execução
            foreach (var issue in executionDiagnostic.StepDiagnostics)
            {
                // Ignora diagnósticos "Healthy" (Sucesso não gera alerta de erro)
                if (issue.Severity == HealthStatusEnum.Healthy) continue;

                // 3. Encontrar regras que dão "Match" com este problema
                var matchingRules = FindMatchingRules(issue, allRules);

                // 4. Disparar alertas para as regras encontradas
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
                // Conversão de Enums (Domínio do Worker -> Domínio de Alerta)
                if (!TryMapSeverity(issue.Severity, out AlertSeverity issueSeverity))
                {
                    continue;
                }

                // A. Filtro de Severidade Mínima
                // Ex: Se a regra pede "Error" e o problema é "Warning", ignoramos.
                if (rule.MinSeverity.HasValue && issueSeverity < rule.MinSeverity.Value)
                {
                    continue;
                }

                // B. Filtro de Tipo de Condição (Opcional na regra)
                // Ex: Regra específica apenas para "Integração Falhou"
                if (rule.ConditionType.HasValue)
                {
                    if (TryMapCondition(issue.DiagnosticType, out AlertConditionType issueConditionType))
                    {
                        if (issueConditionType != rule.ConditionType.Value)
                        {
                            continue; // Tipo do problema não bate com a regra
                        }
                    }
                }

                matchingRules.Add(rule);
            }

            return matchingRules;
        }

        private async Task DispatchAlertAsync(AlertConfigurationModel rule, CampaignModel campaign, StepDiagnosticModel issue)
        {
            try
            {
                // 1. Envio via Canal Apropriado
                switch (rule.Type)
                {
                    case AlertChannelType.Email:
                        // Aqui chamamos o EmailDispatcherService que criamos anteriormente
                        await _emailDispatcher.SendAsync(rule, campaign, issue);
                        break;

                    case AlertChannelType.Webhook:
                        await _webhookDispatcher.SendAsync(rule, campaign, issue);
                        break;
                }

                // 2. Registro no Histórico (Auditoria)
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
                _logger.LogError(ex, "Falha ao despachar alerta '{RuleName}' (ID: {RuleId})", rule.Name, rule.Id);
            }
        }

        // --- Helpers de Mapeamento ---

        private bool TryMapSeverity(HealthStatusEnum workerSeverity, out AlertSeverity alertSeverity)
        {
            switch (workerSeverity)
            {
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
                case DiagnosticTypeEnum.WaitStepMissed:
                    alertCondition = AlertConditionType.ExecutionDelayed;
                    return true;
                case DiagnosticTypeEnum.FilterStuck:
                    alertCondition = AlertConditionType.FilterStuck;
                    return true;
                case DiagnosticTypeEnum.IntegrationError:
                    alertCondition = AlertConditionType.IntegrationError;
                    return true;
                case DiagnosticTypeEnum.CampaignNotFinalized:
                case DiagnosticTypeEnum.IncompleteExecution:
                    alertCondition = AlertConditionType.CampaignNotFinalized;
                    return true;
                default:
                    alertCondition = default;
                    return false;
            }
        }
    }
}