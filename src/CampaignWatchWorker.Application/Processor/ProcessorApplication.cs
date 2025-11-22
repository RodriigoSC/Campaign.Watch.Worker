using CampaignWatchWorker.Application.Analyzer;
using CampaignWatchWorker.Application.DTOs;
using CampaignWatchWorker.Application.Mappers;
using CampaignWatchWorker.Application.Services.Interfaces;
using CampaignWatchWorker.Domain.Models.Configuration;
using CampaignWatchWorker.Domain.Models.Entities.Campaigns;
using CampaignWatchWorker.Domain.Models.Enums;
using CampaignWatchWorker.Domain.Models.Interfaces;
using CampaignWatchWorker.Domain.Models.Interfaces.Repositories;
using CampaignWatchWorker.Domain.Models.Interfaces.Services; // Namespace do ISchedulerService
using CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Campaign;
using CampaignWatchWorker.Domain.Models.Read.Campaign;
using Microsoft.Extensions.Logging;

namespace CampaignWatchWorker.Application.Processor
{
    public class ProcessorApplication : IProcessorApplication
    {
        private readonly ICampaignReadModelService _readService;
        private readonly ICampaignModelRepository _campaignRepository;
        private readonly IExecutionModelRepository _executionRepository;
        private readonly ICampaignMapper _mapper;
        private readonly ICampaignHealthAnalyzer _analyzer;
        private readonly ITenantContext _tenantContext;
        private readonly IChannelReadModelService _channelService;
        private readonly IAlertService _alertService;
        private readonly ISchedulerService _schedulerService; // <--- Nova dependência
        private readonly ILogger<ProcessorApplication> _logger;

        public ProcessorApplication(
            ICampaignReadModelService readService,
            ICampaignModelRepository campaignRepository,
            IExecutionModelRepository executionRepository,
            ICampaignMapper mapper,
            ICampaignHealthAnalyzer analyzer,
            ITenantContext tenantContext,
            IChannelReadModelService channelService,
            IAlertService alertService,
            ISchedulerService schedulerService, // <--- Injeção
            ILogger<ProcessorApplication> logger)
        {
            _readService = readService;
            _campaignRepository = campaignRepository;
            _executionRepository = executionRepository;
            _mapper = mapper;
            _analyzer = analyzer;
            _tenantContext = tenantContext;
            _channelService = channelService;
            _alertService = alertService;
            _schedulerService = schedulerService; // <--- Atribuição
            _logger = logger;
        }

        public async Task ProcessProjectScopeAsync(ProjectQueueMessage message)
        {
            var logPrefix = $"[{DateTime.Now:HH:mm:ss}][{message.ClientName}]";
            _logger.LogInformation("[{Client}] 🔄 Iniciando processamento do Projeto: {ProjectId}", message.ClientName, message.ProjectId);

            // Tratamento para modo "Single Campaign" se o ID vier na mensagem
            if (!string.IsNullOrEmpty(message.CampaignId))
            {
                Console.WriteLine($"{logPrefix} 🎯 Processando campanha específica: {message.CampaignId}");
            }
            else
            {
                Console.WriteLine($"{logPrefix} 🔄 Iniciando varredura do Projeto: {message.ProjectId}");
            }

            try
            {
                // 1. Busca campanhas na origem
                // Se for Single Campaign, a lógica ideal seria buscar apenas ela, mas para manter a consistência
                // do método GetCampaignsByProjectAsync, buscamos o escopo e filtramos se necessário,
                // ou confiamos que a varredura rápida resolve.
                // *Nota: Se quiser otimizar Single Campaign, adicione um GetCampaignById aqui.*

                var sourceCampaigns = await _readService.GetCampaignsByProjectAsync(message.ProjectId);
                var sourceList = sourceCampaigns?.ToList() ?? new List<CampaignReadModel>();

                _logger.LogInformation("[{Client}] Encontradas {Count} campanhas na origem.", message.ClientName, sourceList.Count);
                Console.WriteLine($"{logPrefix} Encontradas {sourceList.Count} campanhas na origem.");

                // 2. Limpeza de Zumbis (Apenas se for varredura completa, para segurança)
                if (string.IsNullOrEmpty(message.CampaignId))
                {
                    await CleanUpZombieCampaignsAsync(message.ClientName, message.ProjectId, sourceList);
                }

                if (!sourceList.Any()) return;

                // Se a mensagem tiver CampaignId, filtramos a lista para processar apenas ela
                if (!string.IsNullOrEmpty(message.CampaignId))
                {
                    sourceList = sourceList.Where(x => x.Id == message.CampaignId).ToList();
                    if (!sourceList.Any())
                    {
                        Console.WriteLine($"{logPrefix} ⚠️ Campanha {message.CampaignId} não encontrada na lista do projeto.");
                        return;
                    }
                }

                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 10,
                    CancellationToken = CancellationToken.None
                };

                await Parallel.ForEachAsync(sourceList, parallelOptions, async (sourceCampaign, token) =>
                {
                    await SyncAndAnalyzeCampaignAsync(sourceCampaign);
                });

                Console.WriteLine($"{logPrefix} ✅ Processamento concluído.");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "[{Client}] Erro fatal ao processar projeto {ProjectId}", message.ClientName, message.ProjectId);
                throw;
            }
        }

        private async Task CleanUpZombieCampaignsAsync(string clientName, string projectId, List<CampaignReadModel> sourceCampaigns)
        {
            try
            {
                var sourceIds = sourceCampaigns.Select(c => c.Id).ToHashSet();
                var localIds = await _campaignRepository.GetIdsByProjectIdAsync(clientName, projectId);
                var zombiesToDelete = localIds.Where(id => !sourceIds.Contains(id)).ToList();

                if (zombiesToDelete.Any())
                {
                    _logger.LogInformation("[{Client}] Removendo {Count} campanhas obsoletas (Zumbis)...", clientName, zombiesToDelete.Count);
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}][{clientName}] 🧹 Removendo {zombiesToDelete.Count} campanhas obsoletas...");

                    await _campaignRepository.DeleteManyAsync(clientName, zombiesToDelete);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Client}] Falha ao limpar campanhas zumbis", clientName);
            }
        }

        private async Task SyncAndAnalyzeCampaignAsync(CampaignReadModel source)
        {
            var clientName = _tenantContext.Client.Name;
            var logPrefix = $"[{DateTime.Now:HH:mm:ss}][{clientName}][Campanha: {source.Id}]";

            try
            {
                var campaignModel = _mapper.MapToCampaignModel(source);
                var existingCampaign = await _campaignRepository.GetCampaignByIdAsync(clientName, source.Id);

                if (existingCampaign != null)
                {
                    campaignModel.Id = existingCampaign.Id;
                    campaignModel.FirstMonitoringAt = existingCampaign.FirstMonitoringAt;
                    campaignModel.CreatedAt = existingCampaign.CreatedAt;

                    if (campaignModel.StatusCampaign == existingCampaign.StatusCampaign)
                    {
                        campaignModel.MonitoringStatus = existingCampaign.MonitoringStatus;
                        campaignModel.NextExecutionMonitoring = existingCampaign.NextExecutionMonitoring;
                        campaignModel.HealthStatus = existingCampaign.HealthStatus;
                    }
                    else
                    {
                        Console.WriteLine($"{logPrefix} Status alterado ({existingCampaign.StatusCampaign} -> {campaignModel.StatusCampaign}).");
                        SetInitialMonitoringState(campaignModel);
                    }
                }
                else
                {
                    Console.WriteLine($"{logPrefix} Nova campanha detectada.");
                    SetInitialMonitoringState(campaignModel);
                }

                // 1. Persistência Local
                await _campaignRepository.UpdateCampaignAsync(campaignModel);

                // 2. Decisão de Processamento
                if (campaignModel.IsActive && !campaignModel.IsDeleted)
                {
                    await AnalyzeExecutionsAsync(campaignModel);
                }
                else
                {
                    // Se não está ativa, apenas registramos o agendamento futuro se existir
                    await RegisterNextExecutionAsync(campaignModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Client}] Erro ao processar campanha {CampaignId}", clientName, source.Id);
                Console.WriteLine($"{logPrefix} ❌ Erro: {ex.Message}");
            }
        }

        private async Task AnalyzeExecutionsAsync(CampaignModel campaign)
        {
            var logPrefix = $"[{DateTime.Now:HH:mm:ss}][{campaign.ClientName}][Campanha: {campaign.IdCampaign}]";

            try
            {
                var executionsRead = await _readService.GetExecutionsByCampaign(campaign.IdCampaign);
                var executionModels = new List<ExecutionModel>();
                int errorExecutionCount = 0;

                if (executionsRead != null && executionsRead.Any())
                {
                    foreach (var executionRead in executionsRead)
                    {
                        try
                        {
                            var channelData = await _channelService.GetConsolidatedChannelDataAsync(executionRead.ExecutionId.ToString());
                            var executionModel = _mapper.MapToExecutionModel(executionRead, campaign.Id, channelData);
                            if (executionModel == null) continue;

                            var diagnostic = await _analyzer.AnalyzeExecutionAsync(executionModel, campaign);

                            executionModel.HasMonitoringErrors = diagnostic.OverallHealth == HealthStatusEnum.Error ||
                                                                 diagnostic.OverallHealth == HealthStatusEnum.Critical;

                            if (executionModel.HasMonitoringErrors) errorExecutionCount++;

                            await _executionRepository.UpdateExecutionAsync(executionModel);
                            executionModels.Add(executionModel);

                            await _alertService.ProcessAlertsAsync(campaign, diagnostic);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[{Client}] Erro na execução {ExecId}", campaign.ClientName, executionRead.ExecutionId);
                        }
                    }
                }

                var campaignHealth = await _analyzer.AnalyzeCampaignHealthAsync(campaign, executionModels);
                campaign.HealthStatus = campaignHealth;
                campaign.LastCheckMonitoring = DateTime.UtcNow;
                campaign.ExecutionsWithErrors = errorExecutionCount;
                campaign.TotalExecutionsProcessed = executionModels.Count;

                // Recalcula próxima verificação
                (var newStatus, var nextCheck) = CalculateNextCheck(campaign);
                campaign.MonitoringStatus = newStatus;
                campaign.NextExecutionMonitoring = nextCheck;

                // 1. Atualiza Banco
                await _campaignRepository.UpdateCampaignAsync(campaign);

                // 2. Agenda na API
                await RegisterNextExecutionAsync(campaign);

                Console.WriteLine($"{logPrefix} Análise concluída. Próxima: {nextCheck}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Client}] Erro na análise de saúde da campanha {Id}", campaign.ClientName, campaign.IdCampaign);
            }
        }

        /// <summary>
        /// Chama a API de Scheduler para agendar o próximo "wake up" desta campanha.
        /// </summary>
        private async Task RegisterNextExecutionAsync(CampaignModel campaign)
        {
            if (campaign.NextExecutionMonitoring.HasValue && campaign.NextExecutionMonitoring > DateTime.UtcNow)
            {
                try
                {
                    await _schedulerService.ScheduleExecutionAsync(new ScheduleRequest
                    {
                        ClientName = campaign.ClientName,
                        ProjectId = campaign.ProjectId,
                        CampaignId = campaign.IdCampaign,
                        ExecuteAt = campaign.NextExecutionMonitoring.Value
                    });

                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Agendado na API para {campaign.NextExecutionMonitoring}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao registrar agendamento na API para campanha {Id}", campaign.IdCampaign);
                }
            }
        }

        private (MonitoringStatusEnum, DateTime?) CalculateNextCheck(CampaignModel campaign)
        {
            var now = DateTime.UtcNow;

            if (!campaign.IsActive || campaign.IsDeleted)
                return (MonitoringStatusEnum.Completed, null);

            if (campaign.HealthStatus?.HasIntegrationErrors == true)
                return (MonitoringStatusEnum.Failed, now.AddMinutes(5));

            if (campaign.CampaignType == CampaignTypeEnum.Pontual)
            {
                if (campaign.StatusCampaign == CampaignStatusEnum.Completed)
                {
                    if (campaign.HealthStatus?.HasPendingExecution == true)
                        return (MonitoringStatusEnum.InProgress, now.AddMinutes(10));
                    return (MonitoringStatusEnum.Completed, null);
                }

                if (campaign.StatusCampaign == CampaignStatusEnum.Scheduled && campaign.Scheduler != null && campaign.Scheduler.StartDateTime > now)
                    return (MonitoringStatusEnum.WaitingForNextExecution, campaign.Scheduler.StartDateTime.AddMinutes(-5));

                if (campaign.StatusCampaign == CampaignStatusEnum.Executing || campaign.StatusCampaign == CampaignStatusEnum.Scheduled)
                    return (MonitoringStatusEnum.InProgress, now.AddMinutes(10));

                if (campaign.StatusCampaign == CampaignStatusEnum.Error)
                    return (MonitoringStatusEnum.Failed, null);

                return (MonitoringStatusEnum.Completed, null);
            }

            if (campaign.CampaignType == CampaignTypeEnum.Recorrente)
            {
                if (campaign.Scheduler?.EndDateTime.HasValue == true && now > campaign.Scheduler.EndDateTime.Value)
                    return (MonitoringStatusEnum.Completed, null);

                if (campaign.HealthStatus?.HasPendingExecution == true)
                    return (MonitoringStatusEnum.InProgress, now.AddMinutes(10));

                if (campaign.Scheduler != null && now < campaign.Scheduler.StartDateTime)
                    return (MonitoringStatusEnum.WaitingForNextExecution, campaign.Scheduler.StartDateTime.AddMinutes(-5));

                return (MonitoringStatusEnum.WaitingForNextExecution, now.AddHours(1));
            }

            return (MonitoringStatusEnum.Pending, now.AddMinutes(30));
        }

        private void SetInitialMonitoringState(CampaignModel campaignModel)
        {
            var now = DateTime.UtcNow;
            campaignModel.FirstMonitoringAt ??= now;

            if (campaignModel.CampaignType == CampaignTypeEnum.Pontual)
            {
                if (campaignModel.StatusCampaign == CampaignStatusEnum.Scheduled && campaignModel.Scheduler?.StartDateTime > now)
                {
                    campaignModel.MonitoringStatus = MonitoringStatusEnum.WaitingForNextExecution;
                    campaignModel.NextExecutionMonitoring = campaignModel.Scheduler.StartDateTime.AddMinutes(-5);
                }
                else if (campaignModel.StatusCampaign == CampaignStatusEnum.Executing)
                {
                    campaignModel.MonitoringStatus = MonitoringStatusEnum.InProgress;
                    campaignModel.NextExecutionMonitoring = now;
                }
                else if (campaignModel.StatusCampaign == CampaignStatusEnum.Draft)
                {
                    campaignModel.MonitoringStatus = MonitoringStatusEnum.Pending;
                    campaignModel.NextExecutionMonitoring = now.AddHours(1);
                }
                else
                {
                    campaignModel.MonitoringStatus = campaignModel.StatusCampaign == CampaignStatusEnum.Error
                        ? MonitoringStatusEnum.Failed
                        : MonitoringStatusEnum.Completed;
                    campaignModel.NextExecutionMonitoring = now;
                }
            }
            else
            {
                campaignModel.MonitoringStatus = MonitoringStatusEnum.InProgress;
                campaignModel.NextExecutionMonitoring = now;
            }
        }
    }
}