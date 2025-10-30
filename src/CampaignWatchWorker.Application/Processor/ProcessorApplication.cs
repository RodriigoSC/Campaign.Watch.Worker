using CampaignWatchWorker.Application.Analyzer;
using CampaignWatchWorker.Application.Mappers;
using CampaignWatchWorker.Domain.Models;
using CampaignWatchWorker.Domain.Models.Enums;
using CampaignWatchWorker.Domain.Models.Interfaces; // Novo
using CampaignWatchWorker.Domain.Models.Interfaces.Repositories;
using CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Campaign;
using CampaignWatchWorker.Domain.Models.Read.Campaign; // Novo
using MongoDB.Bson;
//using Microsoft.Extensions.Logging;

namespace CampaignWatchWorker.Application.Processor
{
    public class ProcessorApplication : IProcessorApplication
    {
        private readonly ICampaignReadModelService _campaignReadModelService;
        private readonly ICampaignModelRepository _campaignModelRepository;
        private readonly IExecutionModelRepository _executionModelRepository;
        private readonly ICampaignMapper _campaignMapper;
        private readonly ICampaignHealthAnalyzer _healthAnalyzer;
        private readonly ITenantContext _tenantContext; // Novo
        private readonly IChannelReadModelService _channelReadModelService; // Novo
        //private readonly ILogger<ProcessorApplication> _logger;

        public ProcessorApplication(
            ICampaignReadModelService campaignReadModelService,
            ICampaignModelRepository campaignModelRepository,
            IExecutionModelRepository executionModelRepository,
            ICampaignMapper campaignMapper,
            ICampaignHealthAnalyzer healthAnalyzer,
            ITenantContext tenantContext, // Novo
            IChannelReadModelService channelReadModelService // Novo
            /*, ILogger<ProcessorApplication> logger*/)
        {
            _campaignReadModelService = campaignReadModelService;
            _campaignModelRepository = campaignModelRepository;
            _executionModelRepository = executionModelRepository;
            _campaignMapper = campaignMapper;
            _healthAnalyzer = healthAnalyzer;
            _tenantContext = tenantContext; // Novo
            _channelReadModelService = channelReadModelService; // Novo
            //_logger = logger;
        }

        // Método antigo 'Process(object obj)' é removido.

        // --- NOVO MÉTODO PRINCIPAL ---
        public async Task ProcessDueCampaignsForClientAsync()
        {
            var clientName = _tenantContext.Client.Name;
            //_logger.LogInformation("[{ClientName}] Buscando campanhas para processar.", clientName);

            // 1. Busca campanhas devidas no BD *do worker*
            var dueCampaigns = await _campaignModelRepository.ObterCampanhasDevidasParaClienteAsync(clientName);

            if (!dueCampaigns.Any())
            {
                //_logger.LogInformation("[{ClientName}] Nenhuma campanha devida encontrada.", clientName);
                return;
            }

            //_logger.LogInformation("[{ClientName}] Encontradas {Count} campanhas.", clientName, dueCampaigns.Count());

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
                //_logger.LogInformation("[{ClientName}] Processando Campanha ID: {CampaignId}", clientName, campaignSourceId);

                // --- PASSO 1: Buscar dados da campanha na ORIGEM (DB do cliente) ---
                var campaignReadModel = await _campaignReadModelService.GetCampaignById(campaignSourceId);
                if (campaignReadModel == null)
                {
                    //_logger.LogWarning("[{ClientName}] Campanha {CampaignId} não encontrada na origem. Marcando como inativa.", clientName, campaignSourceId);
                    campaignModel.IsActive = false;
                    campaignModel.MonitoringNotes = "Campanha não encontrada na origem.";
                    await _campaignModelRepository.AtualizarCampanhaAsync(campaignModel);
                    return;
                }

                // --- PASSO 2: Mapear dados base da campanha ---
                // (O Mapper não pode mais preencher tudo, o modelo existente é usado)
                var updatedCampaignModel = _campaignMapper.MapToCampaignModel(campaignReadModel);

                // Preserva dados do monitoramento
                updatedCampaignModel.Id = campaignModel.Id;
                updatedCampaignModel.CreatedAt = campaignModel.CreatedAt;
                updatedCampaignModel.FirstMonitoringAt = campaignModel.FirstMonitoringAt;


                // --- PASSO 3: Buscar e processar execuções da ORIGEM ---
                var executionsRead = await _campaignReadModelService.GetExecutionsByCampaign(campaignSourceId);
                var executionModels = new List<ExecutionModel>();
                int errorExecutionCount = 0;

                if (executionsRead != null && executionsRead.Any())
                {
                    foreach (var executionRead in executionsRead)
                    {
                        try
                        {
                            // --- PASSO 4 (NOVO): Buscar dados consolidados do canal ---
                            var channelData = await _channelReadModelService.GetConsolidatedChannelDataAsync(executionRead.ExecutionId.ToString());

                            // --- PASSO 5: Mapear execução com os dados do canal ---
                            var executionModel = _campaignMapper.MapToExecutionModel(executionRead, campaignModel.Id, channelData);
                            if (executionModel == null) continue;

                            // Analisar saúde da execução
                            var diagnostic = await _healthAnalyzer.AnalyzeExecutionAsync(executionModel, updatedCampaignModel);
                            executionModel.HasMonitoringErrors = diagnostic.OverallHealth == HealthStatusEnum.Error ||
                                                                 diagnostic.OverallHealth == HealthStatusEnum.Critical;

                            if (executionModel.HasMonitoringErrors) errorExecutionCount++;

                            // Persistir/Atualizar execução no BD de Monitoramento
                            await _executionModelRepository.AtualizarExecucaoAsync(executionModel);
                            executionModels.Add(executionModel);
                        }
                        catch (Exception ex)
                        {
                            //_logger.LogError(ex, "[{ClientName}] Falha ao processar execução {ExecId} da campanha {CampaignId}", clientName, executionRead?.ExecutionId, campaignSourceId);
                        }
                    }
                }

                // --- PASSO 6: Analisar saúde GERAL da campanha ---
                var campaignHealth = await _healthAnalyzer.AnalyzeCampaignHealthAsync(updatedCampaignModel, executionModels);
                updatedCampaignModel.HealthStatus = campaignHealth;
                updatedCampaignModel.LastCheckMonitoring = DateTime.UtcNow;
                updatedCampaignModel.ExecutionsWithErrors = errorExecutionCount;
                updatedCampaignModel.TotalExecutionsProcessed = executionModels.Count;
                updatedCampaignModel.NextExecutionMonitoring = CalculateNextCheck(updatedCampaignModel); // (Lógica de agendamento)

                // --- PASSO 7: Atualizar a campanha no BD de Monitoramento ---
                await _campaignModelRepository.AtualizarCampanhaAsync(updatedCampaignModel);

                //_logger.LogInformation("[{ClientName}] Campanha {CampaignId} processada. Próxima checagem: {NextCheck}", clientName, campaignSourceId, updatedCampaignModel.NextExecutionMonitoring);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "[{ClientName}] ERRO FATAL ao processar Campanha {CampaignId}", clientName, campaignSourceId);
                // Tenta reagendar com erro
                try
                {
                    campaignModel.HealthStatus ??= new MonitoringHealthStatus();
                    campaignModel.HealthStatus.HasIntegrationErrors = true;
                    campaignModel.HealthStatus.LastMessage = $"Erro fatal no processamento: {ex.Message}";
                    campaignModel.MonitoringStatus = MonitoringStatusEnum.Failed;
                    campaignModel.LastCheckMonitoring = DateTime.UtcNow;
                    campaignModel.NextExecutionMonitoring = DateTime.UtcNow.AddMinutes(15);
                    await _campaignModelRepository.AtualizarCampanhaAsync(campaignModel);
                }
                catch { /* Ignore */ }
            }
        }

        // (Copie o método CalculateNextCheck do ProcessorApplication.cs original)
        private DateTime? CalculateNextCheck(CampaignModel campaign)
        {
            var now = DateTime.UtcNow;

            // Se a campanha está inativa ou deletada, não precisa checar
            if (!campaign.IsActive || campaign.IsDeleted)
            {
                return null;
            }

            // Se há problemas críticos/erros de integração, verificar mais cedo
            if (campaign.HealthStatus?.HasIntegrationErrors == true)
            {
                return now.AddMinutes(5);
            }

            // Campanha pontual
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

            // Campanha recorrente
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
    }
}