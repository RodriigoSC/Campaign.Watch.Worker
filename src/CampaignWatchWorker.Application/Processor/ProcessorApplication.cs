using CampaignWatchWorker.Application.Mappers;
using CampaignWatchWorker.Domain.Models.Interfaces.Repositories;
using CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Campaign;

namespace CampaignWatchWorker.Application.Processor
{
    public class ProcessorApplication : IProcessorApplication
    {
        private readonly ICampaignReadModelService _campaignReadModelService;
        private readonly ICampaignModelRepository _campaignModelRepository;
        private readonly IExecutionModelRepository _executionModelRepository;
        private readonly ICampaignMapper _campaignMapper;

        public ProcessorApplication(
            ICampaignReadModelService campaignReadModelService,
            ICampaignModelRepository campaignModelRepository,
            IExecutionModelRepository executionModelRepository,
            ICampaignMapper campaignMapper)
        {
            _campaignReadModelService = campaignReadModelService;
            _campaignModelRepository = campaignModelRepository;
            _executionModelRepository = executionModelRepository;
            _campaignMapper = campaignMapper;
        }

        public void Process(object obj)
        {
            string campaignId = null;
            try
            {
                campaignId = obj?.ToString();
                if (string.IsNullOrEmpty(campaignId))
                {
                    Console.WriteLine("ID da Campanha nulo ou vazio. Mensagem ignorada.");
                    return;
                }

                Console.WriteLine($"Iniciando processamento para a Campanha ID: {campaignId}");

                // 1. Busca os dados brutos da campanha.
                var campaignReadModel = _campaignReadModelService.GetCampaignById(campaignId).GetAwaiter().GetResult();
                if (campaignReadModel == null)
                {
                    Console.WriteLine($"Campanha com ID {campaignId} não encontrada no sistema de origem.");
                    return;
                }

                // 2. Usa o mapper para converter e depois salva/atualiza a campanha no banco de monitoramento.
                var campaignModel = _campaignMapper.MapToCampaignModel(campaignReadModel);
                _campaignModelRepository.AtualizarCampanhaAsync(campaignModel).GetAwaiter().GetResult();

                // 3. Busca as execuções da campanha.
                var executions = _campaignReadModelService.GetExecutionsByCampaign(campaignId).GetAwaiter().GetResult();
                if (executions != null && executions.Any())
                {
                    foreach (var executionRead in executions)
                    {
                        try
                        {
                            
                            var executionModel = _campaignMapper.MapToExecutionModel(executionRead, campaignModel.Id);
                            _executionModelRepository.AtualizarExecucaoAsync(executionModel).GetAwaiter().GetResult();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"ERRO ao processar a execução ID: {executionRead.ExecutionId}. Detalhes: {ex.Message}");
                            continue;
                        }
                    }
                }

                Console.WriteLine($"Campanha '{campaignModel.Name}' processada com sucesso.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERRO FATAL ao processar a mensagem para a Campanha ID: {campaignId}. Detalhes: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }
    }
}