using CampaignWatchWorker.Application.Services.Interfaces;
using CampaignWatchWorker.Domain.Models.Configuration;
using CampaignWatchWorker.Domain.Models.Entities.Alerts;
using CampaignWatchWorker.Domain.Models.Entities.Campaigns;
using CampaignWatchWorker.Domain.Models.Entities.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace CampaignWatchWorker.Infra.Services.Email
{
    public class EmailDispatcherService : IEmailDispatcherService
    {
        private readonly SmtpSettings _settings;
        private readonly ILogger<EmailDispatcherService> _logger;

        public EmailDispatcherService(SmtpSettings settings, ILogger<EmailDispatcherService> logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public async Task SendAsync(AlertConfigurationModel rule, CampaignModel campaign, StepDiagnosticModel issue)
        {
            try
            {
                using var message = new MailMessage();

                message.From = new MailAddress(_settings.FromAddress, _settings.FromName);
                
                if (!string.IsNullOrEmpty(rule.Recipient))
                {
                    foreach (var email in rule.Recipient.Split(';'))
                    {
                        if (!string.IsNullOrWhiteSpace(email))
                            message.To.Add(new MailAddress(email.Trim()));
                    }
                }
                else
                {
                    _logger.LogWarning("Regra de alerta {RuleId} sem destinatário configurado.", rule.Id);
                    return;
                }

                message.Subject = $"[ALERTA] {issue.Severity.ToString().ToUpper()}: {campaign.Name}";
                message.Body = GenerateHtmlBody(campaign, issue);
                message.IsBodyHtml = true;

                using var client = new SmtpClient(_settings.Host, _settings.Port);

                if (!string.IsNullOrEmpty(_settings.UserName))
                {
                    client.Credentials = new NetworkCredential(_settings.UserName, _settings.Password);
                    client.EnableSsl = true;
                }
                else
                {
                    client.UseDefaultCredentials = false;
                }

                await client.SendMailAsync(message);

                _logger.LogInformation("📧 Email enviado para {Recipient}. Regra: {RuleName}. Erro: {Error}",
                    rule.Recipient, rule.Name, issue.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao enviar email SMTP. Host: {Host}. Destinatário: {Recipient}", _settings.Host, rule.Recipient);
            }
        }

        private string GenerateHtmlBody(CampaignModel campaign, StepDiagnosticModel issue)
        {
            var color = issue.Severity == Domain.Models.Enums.HealthStatusEnum.Critical ? "#d9534f" : "#f0ad4e";
            return $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #ddd; max-width: 600px;'>
                    <h2 style='color: {color};'>Alerta: {issue.Severity}</h2>
                    <p>O monitoramento detectou um problema:</p>
                    <ul>
                        <li><strong>Cliente:</strong> {campaign.ClientName}</li>
                        <li><strong>Campanha:</strong> {campaign.Name} (ID: {campaign.IdCampaign})</li>
                        <li><strong>Step:</strong> {issue.StepName}</li>
                    </ul>
                    <div style='background-color: #fff3cd; padding: 10px; border-radius: 4px;'>
                        <strong>Detalhe do Erro:</strong><br/>{issue.Message}
                    </div>
                    <p style='font-size: 12px; color: #999; margin-top: 15px;'>Gerado em {issue.DetectedAt:G} UTC</p>
                </div>
            ";
        }
    }
}