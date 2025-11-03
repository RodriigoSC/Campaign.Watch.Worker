using CampaignWatchWorker.Application.Services.Interfaces;
using CampaignWatchWorker.Domain.Models.Entities.Alerts;
using CampaignWatchWorker.Domain.Models.Entities.Campaigns;
using CampaignWatchWorker.Domain.Models.Entities.Diagnostics;
using Microsoft.Extensions.Logging;

namespace CampaignWatchWorker.Application.Services
{
    public class LogEmailDispatcher : IEmailDispatcher
    {
        private readonly ILogger<LogEmailDispatcher> _logger;
        public LogEmailDispatcher(ILogger<LogEmailDispatcher> logger) { _logger = logger; }

        public Task SendAsync(AlertConfigurationModel rule, CampaignModel campaign, StepDiagnosticModel issue)
        {
            _logger.LogWarning(
                "[ALERTA EMAIL] Disparado para: {Recipient}. Regra: {RuleName}. Campanha: {CampaignName}. Mensagem: {Message}",
                rule.Recipient, rule.Name, campaign.Name, issue.Message
            );
            return Task.CompletedTask;
        }
    }

    public class LogWebhookDispatcher : IWebhookDispatcher
    {
        private readonly ILogger<LogWebhookDispatcher> _logger;
        public LogWebhookDispatcher(ILogger<LogWebhookDispatcher> logger) { _logger = logger; }

        public Task SendAsync(AlertConfigurationModel rule, CampaignModel campaign, StepDiagnosticModel issue)
        {
            _logger.LogWarning(
                "[ALERTA WEBHOOK] Disparado para: {Recipient}. Regra: {RuleName}. Campanha: {CampaignName}. Mensagem: {Message}",
                rule.Recipient, rule.Name, campaign.Name, issue.Message
            );
            // Aqui iria a lógica de `HttpClient.PostAsJsonAsync(rule.Recipient, payload)`
            return Task.CompletedTask;
        }
    }
}
