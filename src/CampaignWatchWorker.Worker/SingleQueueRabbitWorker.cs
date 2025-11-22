using CampaignWatchWorker.Application.DTOs;
using CampaignWatchWorker.Application.Processor;
using CampaignWatchWorker.Domain.Models.Interfaces;
using CampaignWatchWorker.Domain.Models.Interfaces.Services;
using DTM_Vault.Data.KeyValue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CampaignWatchWorker.Worker
{
    public class SingleQueueRabbitWorker : BackgroundService
    {
        private readonly ILogger<SingleQueueRabbitWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IVaultService _vaultService;
        private readonly string _environment;

        private IConnection _connection;
        private IModel _channel;

        // Nome da fila unificada para todos os tenants
        private const string GLOBAL_QUEUE_NAME = "campaign.monitoring.global";

        public SingleQueueRabbitWorker(
            ILogger<SingleQueueRabbitWorker> logger,
            IServiceScopeFactory scopeFactory,
            IVaultService vaultService)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _vaultService = vaultService;
            _environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Iniciando Worker de Fila Unificada (Shared Queue)...");

            try
            {
                await InitializeRabbitMq();

                // Mantém o Discovery rodando em background para sincronizar campanhas novas
                // Isso garante que campanhas criadas na origem existam no banco de leitura
                await RunDiscoveryLoopAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Falha fatal no Worker Unificado.");
            }
        }

        private async Task InitializeRabbitMq()
        {
            // 1. Busca credenciais no Vault
            var host = await _vaultService.GetKeyAsync($"monitoring/{_environment}/data/keys", "RabbitMQ.host");
            var user = await _vaultService.GetKeyAsync($"monitoring/{_environment}/data/keys", "RabbitMQ.user");
            var pass = await _vaultService.GetKeyAsync($"monitoring/{_environment}/data/keys", "RabbitMQ.pass");
            var vhost = await _vaultService.GetKeyAsync($"monitoring/{_environment}/data/keys", "RabbitMQ.virtualhost");

            var factory = new ConnectionFactory
            {
                HostName = host,
                UserName = user,
                Password = pass,
                VirtualHost = vhost,
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // 2. Declara a fila Global
            _channel.QueueDeclare(queue: GLOBAL_QUEUE_NAME, durable: true, exclusive: false, autoDelete: false, arguments: null);

            // QoS: Processa 5 mensagens por vez (ajuste conforme a capacidade da máquina)
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 5, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += ProcessMessageAsync;

            _channel.BasicConsume(queue: GLOBAL_QUEUE_NAME, autoAck: false, consumer: consumer);

            _logger.LogInformation("Consumer conectado à fila global: {QueueName}", GLOBAL_QUEUE_NAME);
        }

        private async Task ProcessMessageAsync(object model, BasicDeliverEventArgs ea)
        {
            try
            {
                var body = ea.Body.ToArray();
                var jsonMessage = Encoding.UTF8.GetString(body);

                // 3. Deserializa o payload JSON
                var messageData = JsonSerializer.Deserialize<CampaignQueueMessage>(jsonMessage);

                if (messageData == null || string.IsNullOrEmpty(messageData.ClientId))
                {
                    _logger.LogWarning("Mensagem inválida recebida (Payload nulo ou sem ClientId). Descartando.");
                    _channel.BasicNack(ea.DeliveryTag, false, false); // Envia para DLQ se existir
                    return;
                }

                using (var scope = _scopeFactory.CreateScope())
                {
                    // 4. Resolve o serviço de configuração para achar o cliente correto
                    var configService = scope.ServiceProvider.GetRequiredService<IClientConfigService>();
                    var allClients = await configService.GetAllActiveClientsAsync();

                    // Busca o cliente pelo ID (ObjectId convertido pra string) ou Nome
                    var client = allClients.FirstOrDefault(c => c.Id.ToString() == messageData.ClientId);

                    if (client == null)
                    {
                        _logger.LogError("Cliente {ClientId} não encontrado ou inativo. Mensagem descartada.", messageData.ClientId);
                        _channel.BasicNack(ea.DeliveryTag, false, false);
                        return;
                    }

                    // 5. Configura o Contexto do Tenant (CRÍTICO: Isso faz a mágica do Multi-Tenant)
                    var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                    tenantContext.SetClient(client);

                    _logger.LogInformation("[{ClientName}] Processando evento para Campanha: {CampaignId}", client.Name, messageData.CampaignId);

                    // 6. Invoca o Processor (ele usará o banco do tenant configurado acima)
                    var processor = scope.ServiceProvider.GetRequiredService<IProcessorApplication>();

                    // Requer a implementação do método ProcessCampaignByEventAsync na interface (passo 3 abaixo)
                    await processor.ProcessCampaignByEventAsync(messageData.CampaignId);
                }

                // ACK - Sucesso
                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem da fila global.");
                // Nack com Requeue=true (tenta de novo) ou false (DLQ) dependendo da estratégia de erro
                _channel.BasicNack(ea.DeliveryTag, false, false);
            }
        }

        private async Task RunDiscoveryLoopAsync(CancellationToken stoppingToken)
        {
            // Loop secundário para manter as campanhas sincronizadas
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var configService = scope.ServiceProvider.GetRequiredService<IClientConfigService>();
                    var clients = await configService.GetAllActiveClientsAsync();

                    var options = new ParallelOptions { MaxDegreeOfParallelism = 2, CancellationToken = stoppingToken };

                    await Parallel.ForEachAsync(clients, options, async (client, token) =>
                    {
                        using var innerScope = _scopeFactory.CreateScope();
                        var tenantContext = innerScope.ServiceProvider.GetRequiredService<ITenantContext>();
                        tenantContext.SetClient(client);
                        var processor = innerScope.ServiceProvider.GetRequiredService<IProcessorApplication>();

                        await processor.DiscoverNewCampaignsAsync();
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro no ciclo de Discovery background.");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
