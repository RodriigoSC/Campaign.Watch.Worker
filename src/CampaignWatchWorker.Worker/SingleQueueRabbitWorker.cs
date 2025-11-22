using CampaignWatchWorker.Application.DTOs;
using CampaignWatchWorker.Application.Processor;
using CampaignWatchWorker.Domain.Models.Configuration; // Namespace do Settings
using CampaignWatchWorker.Domain.Models.Interfaces;
using CampaignWatchWorker.Domain.Models.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace CampaignWatchWorker.Worker
{
    public class SingleQueueRabbitWorker : BackgroundService
    {
        private readonly ILogger<SingleQueueRabbitWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConnection _connection;
        private readonly WorkerSettings _settings;
        private IModel _channel;

        // Construtor com logs de diagnóstico
        public SingleQueueRabbitWorker(
            ILogger<SingleQueueRabbitWorker> logger,
            IServiceScopeFactory scopeFactory,
            IConnection connection,
            WorkerSettings settings)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _connection = connection;
            _settings = settings;

            // LOG DE DIAGNÓSTICO: Se aparecer isso, o worker foi criado!
            Console.WriteLine($"[Worker] CONSTRUTOR CHAMADO! Fila alvo: {_settings.QueueName}");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("[Worker] ExecuteAsync iniciado.");
            _logger.LogInformation("Iniciando Worker na fila: {QueueName}", _settings.QueueName);

            try
            {
                InitializeConsumer();

                // Loop para não encerrar a task
                while (!stoppingToken.IsCancellationRequested)
                {
                    // Aqui você pode manter o Discovery ou apenas esperar
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Falha fatal no Worker.");
                Console.WriteLine($"[Worker] ERRO FATAL: {ex.Message}");
            }
        }

        private void InitializeConsumer()
        {
            Console.WriteLine("[Worker] Criando canal e fila...");

            _channel = _connection.CreateModel();

            // Declara a fila (Cria se não existir)
            _channel.QueueDeclare(
                queue: _settings.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _channel.BasicQos(prefetchSize: 0, prefetchCount: (ushort)_settings.PrefetchCount, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += ProcessMessageAsync;

            _channel.BasicConsume(queue: _settings.QueueName, autoAck: false, consumer: consumer);

            Console.WriteLine("[Worker] Consumer registrado com sucesso! Aguardando mensagens...");
        }

        private async Task ProcessMessageAsync(object model, BasicDeliverEventArgs ea)
        {
            try
            {
                var body = ea.Body.ToArray();
                var jsonMessage = Encoding.UTF8.GetString(body);
                Console.WriteLine($"[Worker] Mensagem recebida: {jsonMessage}");

                var messageData = JsonSerializer.Deserialize<CampaignQueueMessage>(jsonMessage);

                if (messageData == null || string.IsNullOrEmpty(messageData.ClientId))
                {
                    _logger.LogWarning("Mensagem inválida.");
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                    return;
                }

                using (var scope = _scopeFactory.CreateScope())
                {
                    var configService = scope.ServiceProvider.GetRequiredService<IClientConfigService>();
                    var allClients = await configService.GetAllActiveClientsAsync();
                    var client = allClients.FirstOrDefault(c => c.Id.ToString() == messageData.ClientId);

                    if (client == null)
                    {
                        _logger.LogError("Cliente {ClientId} não encontrado.", messageData.ClientId);
                        _channel.BasicNack(ea.DeliveryTag, false, false);
                        return;
                    }

                    var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                    tenantContext.SetClient(client);

                    var processor = scope.ServiceProvider.GetRequiredService<IProcessorApplication>();
                    await processor.ProcessCampaignByEventAsync(messageData.CampaignId);
                }

                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no processamento.");
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        }

        public override void Dispose()
        {
            _channel?.Close();
            base.Dispose();
        }
    }
}