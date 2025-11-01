using CampaignWatchWorker.Application.Analyzer;
using CampaignWatchWorker.Application.Mappers;
using CampaignWatchWorker.Domain.Models;
using CampaignWatchWorker.Domain.Models.Enums;
using CampaignWatchWorker.Domain.Models.Interfaces;
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
        private readonly ITenantContext _tenantContext; 
        private readonly IChannelReadModelService _channelReadModelService; 
        private readonly ILogger<ProcessorApplication> _logger;

        public ProcessorApplication(
            ICampaignReadModelService campaignReadModelService,
            ICampaignModelRepository campaignModelRepository,
            IExecutionModelRepository executionModelRepository,
            ICampaignMapper campaignMapper,
            ICampaignHealthAnalyzer healthAnalyzer,
            ITenantContext tenantContext,
            IChannelReadModelService channelReadModelService, 
            ILogger<ProcessorApplication> logger)
        {
            _campaignReadModelService = campaignReadModelService;
            _campaignModelRepository = campaignModelRepository;
            _executionModelRepository = executionModelRepository;
            _campaignMapper = campaignMapper;
            _healthAnalyzer = healthAnalyzer;
            _tenantContext = tenantContext;
            _channelReadModelService = channelReadModelService;
            _logger = logger;
        }

        public async Task ProcessDueCampaignsForClientAsync()
        {
            var clientName = _tenantContext.Client.Name;
            _logger.LogInformation("[{ClientName}] Buscando campanhas para processar.", clientName);
            Console.WriteLine($"[{clientName}] Buscando campanhas para processar.");

            var dueCampaigns = await _campaignModelRepository.GetDueCampaignsForClientAsync(clientName);

            if (!dueCampaigns.Any())
            {
                _logger.LogInformation("[{ClientName}] Nenhuma campanha devida encontrada.", clientName);
                Console.WriteLine($"[{clientName}] Nenhuma campanha devida encontrada.");
                return;
            }

            _logger.LogInformation("[{ClientName}] Encontradas {Count} campanhas.", clientName, dueCampaigns.Count());
            Console.WriteLine($"[{clientName}] Encontradas {dueCampaigns.Count()} campanhas.");

            foreach (var campaignModel in dueCampaigns)
            {
                await ProcessSingleCampaignAsync(campaignModel);
            }
        }
        private async Task ProcessSingleCampaignAsync(CampaignModel campaignModel)
        {
            var clientName = campaignModel.ClientName;
            var campaignSourceId = campaignModel.IdCampaign;

            try
            {
                _logger.LogInformation("[{ClientName}] Processando Campanha ID: {CampaignId}", clientName, campaignSourceId);
                Console.WriteLine($"[{clientName}] Processando Campanha ID: {campaignSourceId}");

                var campaignReadModel = await _campaignReadModelService.GetCampaignById(campaignSourceId);
                if (campaignReadModel == null)
                {
                    _logger.LogWarning("[{ClientName}] Campanha {CampaignId} não encontrada na origem. Marcando como inativa.", clientName, campaignSourceId);
                    Console.WriteLine($"[{clientName}] Campanha {campaignSourceId} não encontrada na origem. Marcando como inativa.");
                    campaignModel.IsActive = false;
                    campaignModel.MonitoringNotes = "Campanha não encontrada na origem.";
                    await _campaignModelRepository.UpdateCampaignAsync(campaignModel);
                    return;
                }

                var updatedCampaignModel = _campaignMapper.MapToCampaignModel(campaignReadModel);

                updatedCampaignModel.Id = campaignModel.Id;
                updatedCampaignModel.CreatedAt = campaignModel.CreatedAt;
                updatedCampaignModel.FirstMonitoringAt = campaignModel.FirstMonitoringAt;


                var executionsRead = await _campaignReadModelService.GetExecutionsByCampaign(campaignSourceId);
                var executionModels = new List<ExecutionModel>();
                int errorExecutionCount = 0;

                if (executionsRead != null && executionsRead.Any())
                {
                    foreach (var executionRead in executionsRead)
                    {
                        try
                        {
                            var channelData = await _channelReadModelService.GetConsolidatedChannelDataAsync(executionRead.ExecutionId.ToString());

                            var executionModel = _campaignMapper.MapToExecutionModel(executionRead, campaignModel.Id, channelData);
                            if (executionModel == null) continue;

                            var diagnostic = await _healthAnalyzer.AnalyzeExecutionAsync(executionModel, updatedCampaignModel);
                            executionModel.HasMonitoringErrors = diagnostic.OverallHealth == HealthStatusEnum.Error ||
                                                                 diagnostic.OverallHealth == HealthStatusEnum.Critical;

                            if (executionModel.HasMonitoringErrors) errorExecutionCount++;

                            await _executionModelRepository.UpdateExecutionAsync(executionModel);
                            executionModels.Add(executionModel);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[{ClientName}] Falha ao processar execução {ExecId} da campanha {CampaignId}", clientName, executionRead?.ExecutionId, campaignSourceId);
                            Console.WriteLine($"[{clientName}] Falha ao processar execução {executionRead?.ExecutionId} da campanha {campaignSourceId}: {ex}");
                        }
                    }
                }

                var campaignHealth = await _healthAnalyzer.AnalyzeCampaignHealthAsync(updatedCampaignModel, executionModels);
                updatedCampaignModel.HealthStatus = campaignHealth;
                updatedCampaignModel.LastCheckMonitoring = DateTime.UtcNow;
                updatedCampaignModel.ExecutionsWithErrors = errorExecutionCount;
                updatedCampaignModel.TotalExecutionsProcessed = executionModels.Count;
                updatedCampaignModel.NextExecutionMonitoring = CalculateNextCheck(updatedCampaignModel);

                await _campaignModelRepository.UpdateCampaignAsync(updatedCampaignModel);

                _logger.LogInformation("[{ClientName}] Campanha {CampaignId} processada. Próxima checagem: {NextCheck}", clientName, campaignSourceId, updatedCampaignModel.NextExecutionMonitoring);
                Console.WriteLine($"[{clientName}] Campanha {campaignSourceId} processada. Próxima checagem: {updatedCampaignModel.NextExecutionMonitoring}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{ClientName}] ERRO FATAL ao processar Campanha {CampaignId}", clientName, campaignSourceId);
                Console.WriteLine($"[{clientName}] ERRO FATAL ao processar Campanha {campaignSourceId}: {ex}");

                try
                {
                    campaignModel.HealthStatus ??= new MonitoringHealthStatus();
                    campaignModel.HealthStatus.HasIntegrationErrors = true;
                    campaignModel.HealthStatus.LastMessage = $"Erro fatal no processamento: {ex.Message}";
                    campaignModel.MonitoringStatus = MonitoringStatusEnum.Failed;
                    campaignModel.LastCheckMonitoring = DateTime.UtcNow;
                    campaignModel.NextExecutionMonitoring = DateTime.UtcNow.AddMinutes(15);
                    await _campaignModelRepository.UpdateCampaignAsync(campaignModel);
                }
                catch 
                {
                    
                }
            }
        }
        private DateTime? CalculateNextCheck(CampaignModel campaign)
        {
            var now = DateTime.UtcNow;

            if (!campaign.IsActive || campaign.IsDeleted)
            {
                return null;
            }

            if (campaign.HealthStatus?.HasIntegrationErrors == true)
            {
                return now.AddMinutes(5);
            }

            if (campaign.CampaignType == CampaignTypeEnum.Pontual)
            {
                if (campaign.StatusCampaign == CampaignStatusEnum.Completed && campaign.HealthStatus?.HasIntegrationErrors == false)
                {
                    return null;
                }
                if (campaign.Scheduler != null && campaign.Scheduler.StartDateTime > now)
                {
                    return campaign.Scheduler.StartDateTime.AddMinutes(-5);
                }
                if (campaign.StatusCampaign == CampaignStatusEnum.Executing || campaign.HealthStatus?.HasPendingExecution == true)
                {
                    return now.AddMinutes(10);
                }
                return now.AddMinutes(30);
            }

            if (campaign.CampaignType == CampaignTypeEnum.Recorrente)
            {
                if (campaign.HealthStatus?.HasPendingExecution == true)
                {
                    return now.AddMinutes(10);
                }
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
                return now.AddHours(1);
            }

            return now.AddMinutes(30);
        }
        public async Task DiscoverNewCampaignsAsync()
        {
            var clientName = _tenantContext.Client.Name;
            _logger.LogInformation("[{ClientName}] Iniciando descoberta de campanhas ativas na origem.", clientName);
            Console.WriteLine($"[{clientName}] Iniciando descoberta de campanhas ativas na origem.");

            var discoverableCampaigns = await _campaignReadModelService.GetDiscoverableCampaignsAsync();

            if (!discoverableCampaigns.Any())
            {
                _logger.LogInformation("[{ClientName}] Nenhuma campanha ativa encontrada na origem.", clientName);
                Console.WriteLine($"[{clientName}] Nenhuma campanha ativa encontrada na origem.");
                return;
            }

            _logger.LogInformation("[{ClientName}] Encontradas {Count} campanhas ativas na origem. Sincronizando...", clientName, discoverableCampaigns.Count());
            Console.WriteLine($"[{clientName}] Encontradas {discoverableCampaigns.Count()} campanhas ativas na origem. Sincronizando...");

            foreach (var campaignReadModel in discoverableCampaigns)
            {
                try
                {
                    var campaignModel = _campaignMapper.MapToCampaignModel(campaignReadModel);

                    campaignModel.MonitoringStatus = MonitoringStatusEnum.Pending;
                    campaignModel.NextExecutionMonitoring = DateTime.UtcNow;
                    campaignModel.FirstMonitoringAt = DateTime.UtcNow;

                    await _campaignModelRepository.UpdateCampaignAsync(campaignModel);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[{ClientName}] Falha ao sincronizar campanha {CampaignId} da origem.", clientName, campaignReadModel.Id);
                    Console.WriteLine($"[{clientName}] Falha ao sincronizar campanha {campaignReadModel.Id} da origem: {ex}");
                }
            }
        }
    }
}