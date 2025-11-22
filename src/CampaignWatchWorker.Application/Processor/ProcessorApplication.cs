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
                // 1. Busca todas as campanhas do projeto na origem (Read Model)
                // Nota: Você precisa garantir que o método GetCampaignsByProjectAsync existe na interface ICampaignReadModelService
                var sourceCampaigns = await _readService.GetCampaignsByProjectAsync(message.ProjectId);

                if (sourceCampaigns == null || !sourceCampaigns.Any())
                {
                    _logger.LogWarning("[{Client}] Nenhuma campanha encontrada para o projeto {ProjectId}.", message.ClientName, message.ProjectId);
                    return;
                }

                _logger.LogInformation("[{Client}] Sincronizando {Count} campanhas encontradas...", message.ClientName, sourceCampaigns.Count());

                // 2. Itera sobre cada campanha para sincronização e análise
                foreach (var sourceCampaign in sourceCampaigns)
                {
                    await SyncAndAnalyzeCampaignAsync(sourceCampaign);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "[{Client}] Erro fatal ao processar escopo do projeto {ProjectId}", message.ClientName, message.ProjectId);
                throw; // Relança para que o Worker possa tratar (Nack/Retry)
            }
        }

        private async Task SyncAndAnalyzeCampaignAsync(CampaignReadModel source)
        {
            var clientName = _tenantContext.Client.Name;
            try
            {
                // --- Passo A: Mapeamento e Sincronização (Upsert) ---
                var campaignModel = _mapper.MapToCampaignModel(source);

                // Busca a versão atual no banco de persistência para preservar estados de monitoramento
                var existingCampaign = await _campaignRepository.GetCampaignByIdAsync(clientName, source.Id);

                if (existingCampaign != null)
                {
                    campaignModel.Id = existingCampaign.Id; // Mantém o ObjectId do Mongo
                    campaignModel.FirstMonitoringAt = existingCampaign.FirstMonitoringAt;
                    campaignModel.CreatedAt = existingCampaign.CreatedAt;

                    // Se o status da campanha não mudou, preservamos o status do monitoramento e agendamento
                    if (campaignModel.StatusCampaign == existingCampaign.StatusCampaign)
                    {
                        campaignModel.MonitoringStatus = existingCampaign.MonitoringStatus;
                        campaignModel.NextExecutionMonitoring = existingCampaign.NextExecutionMonitoring;
                        campaignModel.HealthStatus = existingCampaign.HealthStatus; // Preserva histórico de saúde
                    }
                    else
                    {
                        // Status mudou (ex: de Executing para Completed), reinicia lógica de monitoramento
                        SetInitialMonitoringState(campaignModel);
                    }
                }
                else
                {
                    // Campanha nova
                    SetInitialMonitoringState(campaignModel);
                }

                // Salva a campanha (cria ou atualiza)
                await _campaignRepository.UpdateCampaignAsync(campaignModel);

                // --- Passo B: Análise de Execuções (Se aplicável) ---
                // Só analisamos se a campanha estiver ativa e não deletada
                if (campaignModel.IsActive && !campaignModel.IsDeleted)
                {
                    await AnalyzeExecutionsAsync(campaignModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Client}] Falha ao sincronizar/analisar campanha {CampaignId}", clientName, source.Id);
            }
        }

        private async Task AnalyzeExecutionsAsync(CampaignModel campaign)
        {
            try
            {
                // Busca execuções na origem
                var executionsRead = await _readService.GetExecutionsByCampaign(campaign.IdCampaign);
                var executionModels = new List<ExecutionModel>();
                int errorExecutionCount = 0;

                if (executionsRead != null && executionsRead.Any())
                {
                    foreach (var executionRead in executionsRead)
                    {
                        try
                        {
                            // Busca dados consolidados dos canais (Deliverability)
                            var channelData = await _channelService.GetConsolidatedChannelDataAsync(executionRead.ExecutionId.ToString());

                            // Mapeia execução
                            var executionModel = _mapper.MapToExecutionModel(executionRead, campaign.Id, channelData);
                            if (executionModel == null) continue;

                            // Analisa saúde da execução (Steps)
                            var diagnostic = await _analyzer.AnalyzeExecutionAsync(executionModel, campaign);

                            executionModel.HasMonitoringErrors = diagnostic.OverallHealth == HealthStatusEnum.Error ||
                                                                 diagnostic.OverallHealth == HealthStatusEnum.Critical;

                            if (executionModel.HasMonitoringErrors) errorExecutionCount++;

                            // Persiste execução
                            await _executionRepository.UpdateExecutionAsync(executionModel);
                            executionModels.Add(executionModel);

                            // Dispara alertas se necessário
                            await _alertService.ProcessAlertsAsync(campaign, diagnostic);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[{Client}] Erro ao processar execução {ExecutionId}", campaign.ClientName, executionRead.ExecutionId);
                        }
                    }
                }

                // Atualiza status consolidado da Campanha após processar todas as execuções
                var campaignHealth = await _analyzer.AnalyzeCampaignHealthAsync(campaign, executionModels);

                campaign.HealthStatus = campaignHealth;
                campaign.LastCheckMonitoring = DateTime.UtcNow;
                campaign.ExecutionsWithErrors = errorExecutionCount;
                campaign.TotalExecutionsProcessed = executionModels.Count;

                // Recalcula próximo agendamento baseado no novo estado
                (var newStatus, var nextCheck) = CalculateNextCheck(campaign);
                campaign.MonitoringStatus = newStatus;
                campaign.NextExecutionMonitoring = nextCheck;

                await _campaignRepository.UpdateCampaignAsync(campaign);

                _logger.LogInformation("[{Client}] Campanha {Id} analisada. Status: {Status}. Próx: {Next}",
                    campaign.ClientName, campaign.IdCampaign, campaign.MonitoringStatus, campaign.NextExecutionMonitoring);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Client}] Erro ao realizar análise de execuções da campanha {Id}", campaign.ClientName, campaign.IdCampaign);
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
                    // Se completou mas ainda tem pendência técnica, aguarda um pouco
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
            else // Recorrente
            {
                campaignModel.MonitoringStatus = MonitoringStatusEnum.InProgress;
                campaignModel.NextExecutionMonitoring = now;
            }
        }
    }
}