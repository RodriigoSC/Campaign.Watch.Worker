using CampaignWatchWorker.Domain.Models.Entities.Alerts;
using CampaignWatchWorker.Domain.Models.Entities.Campaigns;
using CampaignWatchWorker.Domain.Models.Entities.Diagnostics;
using CampaignWatchWorker.Domain.Models.Interfaces.Services.Webhook;
using Microsoft.Extensions.Logging;

namespace CampaignWatchWorker.Infra.Services.Webhook
{
    public class WebhookDispatcherService : IWebhookDispatcherService
    {
        private readonly ILogger<WebhookDispatcherService> _logger;

        public WebhookDispatcherService(ILogger<WebhookDispatcherService> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(AlertConfigurationModel rule, CampaignModel campaign, StepDiagnosticModel issue)
        {
            _logger.LogWarning(
                "[WEBHOOK SIMULADO] Disparado para: {Recipient}. Regra: {RuleName}. Campanha: {CampaignName}. Mensagem: {Message}",
                rule.Recipient, rule.Name, campaign.Name, issue.Message
            );

            return Task.CompletedTask;
        }
    }
}
