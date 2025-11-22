using CampaignWatchWorker.Application.Analyzer;
using CampaignWatchWorker.Application.DTOs;
using CampaignWatchWorker.Application.Mappers;
using CampaignWatchWorker.Application.Services.Interfaces;
using CampaignWatchWorker.Domain.Models.Entities.Campaigns;
using CampaignWatchWorker.Domain.Models.Enums;
using CampaignWatchWorker.Domain.Models.Interfaces;
using CampaignWatchWorker.Domain.Models.Interfaces.Repositories;
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
        private readonly ILogger<ProcessorApplication> _logger;

        public ProcessorApplication(ICampaignReadModelService readService, ICampaignModelRepository campaignRepository, IExecutionModelRepository executionRepository, ICampaignMapper mapper,
            ICampaignHealthAnalyzer analyzer, ITenantContext tenantContext, IChannelReadModelService channelService, IAlertService alertService, ILogger<ProcessorApplication> logger)
        {
            _readService = readService;
            _campaignRepository = campaignRepository;
            _executionRepository = executionRepository;
            _mapper = mapper;
            _analyzer = analyzer;
            _tenantContext = tenantContext;
            _channelService = channelService;
            _alertService = alertService;
            _logger = logger;
        }

        public async Task ProcessProjectScopeAsync(ProjectQueueMessage message)
        {
            _logger.LogInformation("[{Client}] 🔄 Iniciando processamento do Projeto: {ProjectId}", message.ClientName, message.ProjectId);

            try
            {
                var sourceCampaigns = await _readService.GetCampaignsByProjectAsync(message.ProjectId);
                var sourceList = sourceCampaigns?.ToList() ?? new List<CampaignReadModel>();

                _logger.LogInformation("[{Client}] Encontradas {Count} campanhas na origem.", message.ClientName, sourceList.Count);

                await CleanUpZombieCampaignsAsync(message.ClientName, message.ProjectId, sourceList);

                if (!sourceList.Any()) return;

                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 10,
                    CancellationToken = CancellationToken.None
                };

                await Parallel.ForEachAsync(sourceList, parallelOptions, async (sourceCampaign, token) =>
                {
                    await SyncAndAnalyzeCampaignAsync(sourceCampaign);
                });
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
                    _logger.LogInformation("[{Client}] Removendo {Count} campanhas obsoletas (Zumbis) do projeto {ProjectId}...",
                        clientName, zombiesToDelete.Count, projectId);

                    await _campaignRepository.DeleteManyAsync(clientName, zombiesToDelete);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Client}] Falha ao limpar campanhas zumbis do projeto {ProjectId}", clientName, projectId);
            }
        }

        private async Task SyncAndAnalyzeCampaignAsync(CampaignReadModel source)
        {
            var clientName = _tenantContext.Client.Name;
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
                        SetInitialMonitoringState(campaignModel);
                    }
                }
                else
                {
                    SetInitialMonitoringState(campaignModel);
                }

                await _campaignRepository.UpdateCampaignAsync(campaignModel);

                if (campaignModel.IsActive && !campaignModel.IsDeleted)
                {
                    await AnalyzeExecutionsAsync(campaignModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Client}] Erro ao processar campanha {CampaignId}", clientName, source.Id);
            }
        }
        private async Task AnalyzeExecutionsAsync(CampaignModel campaign)
        {
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

                (var newStatus, var nextCheck) = CalculateNextCheck(campaign);
                campaign.MonitoringStatus = newStatus;
                campaign.NextExecutionMonitoring = nextCheck;

                await _campaignRepository.UpdateCampaignAsync(campaign);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Client}] Erro na análise de saúde da campanha {Id}", campaign.ClientName, campaign.IdCampaign);
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