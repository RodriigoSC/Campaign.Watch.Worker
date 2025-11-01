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

        private readonly TimeSpan _healthCheckInterval = TimeSpan.FromMinutes(1);
        private readonly TimeSpan _discoveryInterval = TimeSpan.FromMinutes(3);
        private DateTime _lastDiscoveryRun = DateTime.MinValue;

        public MultiTenantPollingWorker(ILogger<MultiTenantPollingWorker> logger, IServiceScopeFactory scopeFactory, IClientConfigService clientConfigService)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _clientConfigService = clientConfigService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Multi-Tenant Polling Worker iniciado.");
            Console.WriteLine("Multi-Tenant Polling Worker iniciado.");

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (DateTime.UtcNow - _lastDiscoveryRun > _discoveryInterval)
                    {
                        _logger.LogInformation("Iniciando ciclo de DESCOBERTA de campanhas...");
                        Console.WriteLine("Iniciando ciclo de DESCOBERTA de campanhas...");
                        await ProcessAllClientsDiscoveryAsync(stoppingToken);
                        _lastDiscoveryRun = DateTime.UtcNow;
                        _logger.LogInformation("Ciclo de DESCOBERTA de campanhas finalizado.");
                        Console.WriteLine("Ciclo de DESCOBERTA de campanhas finalizado.");
                    }

                    _logger.LogInformation("Iniciando ciclo de MONITORAMENTO de saúde...");
                    Console.WriteLine("Iniciando ciclo de MONITORAMENTO de saúde...");
                    await ProcessAllClientsHealthCheckAsync(stoppingToken);
                    _logger.LogInformation("Ciclo de MONITORAMENTO de saúde finalizado.");
                    Console.WriteLine("Ciclo de MONITORAMENTO de saúde finalizado.");

                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Erro fatal no loop de processamento principal.");
                    Console.WriteLine($"Erro fatal no loop de processamento principal: {ex}");
                }

                await Task.Delay(_healthCheckInterval, stoppingToken);
            }
        }

        private async Task ProcessAllClientsHealthCheckAsync(CancellationToken stoppingToken)
        {
            var activeClients = await _clientConfigService.GetAllActiveClientsAsync();
            if (!activeClients.Any())
            {
                _logger.LogWarning("[HealthCheck] Nenhum cliente ativo encontrado.");
                Console.WriteLine("[HealthCheck] Nenhum cliente ativo encontrado.");
                return;
            }

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = stoppingToken
            };

            await Parallel.ForEachAsync(activeClients, options, async (client, token) =>
            {
                _logger.LogInformation("[HealthCheck][{ClientName}] Iniciando verificação de saúde.", client.Name);
                Console.WriteLine($"[HealthCheck][{client.Name}] Iniciando verificação de saúde.");
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                        tenantContext.SetClient(client);

                        var processor = scope.ServiceProvider.GetRequiredService<IProcessorApplication>();

                        await processor.ProcessDueCampaignsForClientAsync();
                    }
                    _logger.LogInformation("[HealthCheck][{ClientName}] Verificação de saúde concluída.", client.Name);
                    Console.WriteLine($"[HealthCheck][{client.Name}] Verificação de saúde concluída.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[HealthCheck][{ClientName}] Falha ao processar cliente.", client.Name);
                    Console.WriteLine($"[HealthCheck][{client.Name}] Falha ao processar cliente: {ex}");
                }
            });
        }

        private async Task ProcessAllClientsDiscoveryAsync(CancellationToken stoppingToken)
        {
            var activeClients = await _clientConfigService.GetAllActiveClientsAsync();
            if (!activeClients.Any())
            {
                _logger.LogWarning("[Discovery] Nenhum cliente ativo encontrado.");
                Console.WriteLine("[Discovery] Nenhum cliente ativo encontrado.");
                return;
            }

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = stoppingToken
            };

            await Parallel.ForEachAsync(activeClients, options, async (client, token) =>
            {
                _logger.LogInformation("[Discovery][{ClientName}] Iniciando descoberta de campanhas.", client.Name);
                Console.WriteLine($"[Discovery][{client.Name}] Iniciando descoberta de campanhas.");
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                        tenantContext.SetClient(client);

                        var processor = scope.ServiceProvider.GetRequiredService<IProcessorApplication>();

                        await processor.DiscoverNewCampaignsAsync();
                    }
                    _logger.LogInformation("[Discovery][{ClientName}] Descoberta de campanhas concluída.", client.Name);
                    Console.WriteLine($"[Discovery][{client.Name}] Descoberta de campanhas concluída.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Discovery][{ClientName}] Falha ao descobrir campanhas.", client.Name);
                    Console.WriteLine($"[Discovery][{client.Name}] Falha ao descobrir campanhas: {ex}");
                }
            });
        }
    }
}