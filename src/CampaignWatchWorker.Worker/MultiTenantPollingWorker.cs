using CampaignWatchWorker.Application.Processor;
using CampaignWatchWorker.Domain.Models.Interfaces;
using CampaignWatchWorker.Domain.Models.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CampaignWatchWorker.Worker
{
    public class MultiTenantPollingWorker : BackgroundService
    {
        private readonly ILogger<MultiTenantPollingWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IClientConfigService _clientConfigService; 
        private readonly TimeSpan _pollingInterval = TimeSpan.FromMinutes(1);

        public MultiTenantPollingWorker(ILogger<MultiTenantPollingWorker> logger, IServiceScopeFactory scopeFactory, IClientConfigService clientConfigService)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _clientConfigService = clientConfigService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Multi-Tenant Polling Worker iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessAllClientsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Erro fatal no loop de processamento principal.");
                }

                await Task.Delay(_pollingInterval, stoppingToken);
            }
        }

        private async Task ProcessAllClientsAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Buscando clientes ativos...");
            var activeClients = await _clientConfigService.GetAllActiveClientsAsync();

            if (!activeClients.Any())
            {
                _logger.LogWarning("Nenhum cliente ativo encontrado na configuração.");
                return;
            }

            _logger.LogInformation($"Encontrados {activeClients.Count} clientes ativos. Iniciando processamento paralelo.");

            // Processa clientes em paralelo
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = stoppingToken
            };

            await Parallel.ForEachAsync(activeClients, options, async (client, token) =>
            {
                _logger.LogInformation("[{ClientName}] Iniciando verificação.", client.Name);
                try
                {
                    // Cria um escopo de DI para este cliente
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        // Configura o contexto do tenant para este escopo
                        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                        tenantContext.SetClient(client);

                        // Obtém o processador (que agora usará o TenantContext)
                        var processor = scope.ServiceProvider.GetRequiredService<IProcessorApplication>();

                        // Processa todas as campanhas devidas para este cliente
                        await processor.ProcessDueCampaignsForClientAsync();
                    }
                    _logger.LogInformation("[{ClientName}] Verificação concluída.", client.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[{ClientName}] Falha ao processar cliente.", client.Name);
                }
            });

            _logger.LogInformation("Ciclo de processamento de todos os clientes finalizado.");
        }
    }
}
