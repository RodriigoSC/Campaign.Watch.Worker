// CampaignWatchWorker.Application/Processor/ProcessorApplication.cs
using CampaignWatchWorker.Application.Mappers;
using CampaignWatchWorker.Application.Services;
using CampaignWatchWorker.Domain.Models;
using CampaignWatchWorker.Domain.Models.Diagnostics;
using CampaignWatchWorker.Domain.Models.Enums;
using CampaignWatchWorker.Domain.Models.Interfaces.Repositories;
using CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Campaign;
using Microsoft.Extensions.Logging;

namespace CampaignWatchWorker.Application.Processor
{
    public class ProcessorApplication : IProcessorApplication
    {
        private readonly ICampaignReadModelService _campaignReadModelService;
        private readonly ICampaignModelRepository _campaignModelRepository;
        private readonly IExecutionModelRepository _executionModelRepository;
        private readonly ICampaignMapper _campaignMapper;
        private readonly ICampaignHealthAnalyzer _healthAnalyzer;
        private readonly ILogger<ProcessorApplication> _logger;

        public ProcessorApplication(
            ICampaignReadModelService campaignReadModelService,
            ICampaignModelRepository campaignModelRepository,
            IExecutionModelRepository executionModelRepository,
            ICampaignMapper campaignMapper,
            ICampaignHealthAnalyzer healthAnalyzer,
            ILogger<ProcessorApplication> logger)
        {
            _campaignReadModelService = campaignReadModelService;
            _campaignModelRepository = campaignModelRepository;
            _executionModelRepository = executionModelRepository;
            _campaignMapper = campaignMapper;
            _healthAnalyzer = healthAnalyzer;
            _logger = logger;
        }

        public void Process(object obj)
        {
            string campaignId = null;
            try
            {
                campaignId = obj?.ToString();
                if (string.IsNullOrEmpty(campaignId))
                {
                    _logger.LogWarning("ID da Campanha nulo ou vazio. Mensagem ignorada.");
                    return;
                }

                _logger.LogInformation($"Iniciando processamento para a Campanha ID: {campaignId}");

                // 1. Buscar dados da campanha
                var campaignReadModel = _campaignReadModelService.GetCampaignById(campaignId).GetAwaiter().GetResult();
                if (campaignReadModel == null)
                {
                    _logger.LogWarning($"Campanha com ID {campaignId} não encontrada no sistema de origem.");
                    return;
                }

                // 2. Mapear e persistir campanha
                var campaignModel = _campaignMapper.MapToCampaignModel(campaignReadModel);

                // 3. Buscar e processar execuções
                var executions = _campaignReadModelService.GetExecutionsByCampaign(campaignId).GetAwaiter().GetResult();
                var executionModels = new List<ExecutionModel>();

                if (executions != null && executions.Any())
                {
                    foreach (var executionRead in executions)
                    {
                        try
                        {
                            // Mapear execução
                            var executionModel = _campaignMapper.MapToExecutionModel(executionRead, campaignModel.Id);

                            // Analisar saúde da execução
                            var diagnostic = _healthAnalyzer.AnalyzeExecutionAsync(executionModel, campaignModel).GetAwaiter().GetResult();

                            // Log do diagnóstico
                            LogExecutionDiagnostic(diagnostic, executionModel);

                            // Persistir execução
                            _executionModelRepository.AtualizarExecucaoAsync(executionModel).GetAwaiter().GetResult();
                            executionModels.Add(executionModel);

                            _logger.LogInformation($"Execução {executionModel.OriginalExecutionId} processada. Status: {diagnostic.Summary}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Erro ao processar a execução ID: {executionRead.ExecutionId}");
                            continue;
                        }
                    }
                }

                // 4. Analisar saúde geral da campanha
                var campaignHealth = _healthAnalyzer.AnalyzeCampaignHealthAsync(campaignModel, executionModels).GetAwaiter().GetResult();
                campaignModel.HealthStatus = campaignHealth;
                campaignModel.LastCheckMonitoring = DateTime.UtcNow;

                // Definir próxima checagem baseado no tipo e status
                campaignModel.NextExecutionMonitoring = CalculateNextCheck(campaignModel);

                // 5. Persistir campanha atualizada
                _campaignModelRepository.AtualizarCampanhaAsync(campaignModel).GetAwaiter().GetResult();

                // 6. Log final
                LogCampaignSummary(campaignModel, campaignHealth, executionModels.Count);

                _logger.LogInformation($"Campanha '{campaignModel.Name}' processada com sucesso.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"ERRO FATAL ao processar a mensagem para a Campanha ID: {campaignId}");
            }
        }

        private void LogExecutionDiagnostic(ExecutionDiagnostic diagnostic, ExecutionModel execution)
        {
            var logLevel = diagnostic.OverallHealth switch
            {
                HealthStatusEnum.Critical => LogLevel.Critical,
                HealthStatusEnum.Error => LogLevel.Error,
                HealthStatusEnum.Warning => LogLevel.Warning,
                _ => LogLevel.Information
            };

            _logger.Log(logLevel, $"Diagnóstico da Execução {execution.OriginalExecutionId}: {diagnostic.Summary}");

            // Detalhar problemas encontrados
            foreach (var stepDiag in diagnostic.StepDiagnostics.Where(d => d.Severity != HealthStatusEnum.Healthy))
            {
                _logger.Log(
                    ConvertHealthToLogLevel(stepDiag.Severity),
                    $"  Step '{stepDiag.StepName}' ({stepDiag.StepId}): {stepDiag.Message}"
                );
            }
        }

        private void LogCampaignSummary(CampaignModel campaign, MonitoringHealthStatus health, int executionCount)
        {
            var campaignType = campaign.CampaignType == CampaignTypeEnum.Pontual ? "Pontual" : "Recorrente";

            _logger.LogInformation($"=== RESUMO DA CAMPANHA ===");
            _logger.LogInformation($"Nome: {campaign.Name}");
            _logger.LogInformation($"Tipo: {campaignType}");
            _logger.LogInformation($"Status: {campaign.StatusCampaign}");
            _logger.LogInformation($"Execuções processadas: {executionCount}");
            _logger.LogInformation($"Saúde: {health.LastMessage}");

            if (health.HasIntegrationErrors)
            {
                _logger.LogWarning($"⚠️ Campanha com problemas de integração detectados");
            }

            if (health.HasPendingExecution)
            {
                _logger.LogInformation($"⏳ Há execução pendente ou em andamento");
            }

            _logger.LogInformation($"Próxima verificação agendada para: {campaign.NextExecutionMonitoring:dd/MM/yyyy HH:mm}");
            _logger.LogInformation($"========================");
        }

        private DateTime? CalculateNextCheck(CampaignModel campaign)
        {
            var now = DateTime.UtcNow;

            // Se a campanha está inativa ou deletada, não precisa checar
            if (!campaign.IsActive || campaign.IsDeleted)
            {
                return null;
            }

            // Se há problemas críticos, verificar em 5 minutos
            if (campaign.HealthStatus.HasIntegrationErrors)
            {
                return now.AddMinutes(5);
            }

            // Campanha pontual
            if (campaign.CampaignType == CampaignTypeEnum.Pontual)
            {
                // Se já executou e está completa, não precisa mais checar
                if (campaign.StatusCampaign == CampaignStatusEnum.Completed)
                {
                    return null;
                }

                // Se está agendada para o futuro, verificar 5 minutos antes
                if (campaign.Scheduler != null && campaign.Scheduler.StartDateTime > now)
                {
                    return campaign.Scheduler.StartDateTime.AddMinutes(-5);
                }

                // Se está em execução, verificar a cada 10 minutos
                if (campaign.StatusCampaign == CampaignStatusEnum.Executing)
                {
                    return now.AddMinutes(10);
                }

                // Padrão: verificar em 30 minutos
                return now.AddMinutes(30);
            }

            // Campanha recorrente
            if (campaign.CampaignType == CampaignTypeEnum.Recorrente)
            {
                // Se há execução pendente, verificar mais frequentemente
                if (campaign.HealthStatus.HasPendingExecution)
                {
                    return now.AddMinutes(10);
                }

                // Se está fora do período de recorrência, não verificar
                if (campaign.Scheduler != null)
                {
                    if (now < campaign.Scheduler.StartDateTime)
                    {
                        return campaign.Scheduler.StartDateTime.AddMinutes(-5);
                    }

                    if (campaign.Scheduler.EndDateTime.HasValue && now > campaign.Scheduler.EndDateTime.Value)
                    {
                        return null;
                    }
                }

                // Verificação padrão a cada hora para campanhas recorrentes
                return now.AddHours(1);
            }

            // Padrão genérico
            return now.AddMinutes(30);
        }

        private LogLevel ConvertHealthToLogLevel(HealthStatusEnum health)
        {
            return health switch
            {
                HealthStatusEnum.Critical => LogLevel.Critical,
                HealthStatusEnum.Error => LogLevel.Error,
                HealthStatusEnum.Warning => LogLevel.Warning,
                _ => LogLevel.Information
            };
        }
    }
}