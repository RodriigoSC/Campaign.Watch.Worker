using CampaignWatchWorker.Application.DTOs;
using CampaignWatchWorker.Application.Processor;
using CampaignWatchWorker.Domain.Models.Configuration;
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

        public SingleQueueRabbitWorker(ILogger<SingleQueueRabbitWorker> logger, IServiceScopeFactory scopeFactory, IConnection connection, WorkerSettings settings)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _connection = connection;
            _settings = settings;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            _logger.LogInformation("Iniciando Worker na fila: {QueueName}", _settings.QueueName);
            Console.WriteLine($"[{timestamp}] 🚀 Worker iniciado. Alvo: {_settings.QueueName}");

            try
            {
                InitializeConsumer();
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Falha fatal no Worker.");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 💀 FALHA FATAL no Worker: {ex.Message}");
            }
        }

        private void InitializeConsumer()
        {
            try
            {
                _channel = _connection.CreateModel();

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

                _logger.LogInformation("Consumer registrado. Aguardando mensagens...");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✅ Consumer registrado e conectado ao RabbitMQ. Aguardando mensagens...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ❌ Erro ao inicializar consumer: {ex.Message}");
                throw;
            }
        }

        private async Task ProcessMessageAsync(object model, BasicDeliverEventArgs ea)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            try
            {
                var body = ea.Body.ToArray();
                var jsonMessage = Encoding.UTF8.GetString(body);

                Console.WriteLine($"[{timestamp}] 📨 Mensagem recebida (Tag: {ea.DeliveryTag}). Payload size: {body.Length} bytes.");

                var messageData = JsonSerializer.Deserialize<ProjectQueueMessage>(jsonMessage);

                if (messageData == null || string.IsNullOrEmpty(messageData.ClientName) || string.IsNullOrEmpty(messageData.ProjectId))
                {
                    _logger.LogWarning("Mensagem inválida descartada: {Message}", jsonMessage);
                    Console.WriteLine($"[{timestamp}] ⚠️ Mensagem inválida descartada (Dados incompletos). JSON: {jsonMessage}");

                    _channel.BasicNack(ea.DeliveryTag, false, false);
                    return;
                }

                using (var scope = _scopeFactory.CreateScope())
                {
                    var configService = scope.ServiceProvider.GetRequiredService<IClientConfigService>();
                    var allClients = await configService.GetAllActiveClientsAsync();

                    var client = allClients.FirstOrDefault(c =>
                        c.Name.Equals(messageData.ClientName, StringComparison.InvariantCultureIgnoreCase));

                    if (client == null)
                    {
                        _logger.LogError("Cliente '{ClientName}' não encontrado.", messageData.ClientName);
                        Console.WriteLine($"[{timestamp}] ❌ Cliente '{messageData.ClientName}' não encontrado na configuração. Mensagem rejeitada.");

                        _channel.BasicNack(ea.DeliveryTag, false, false);
                        return;
                    }

                    Console.WriteLine($"[{timestamp}] 👤 Cliente identificado: {client.Name}. Iniciando processador...");

                    var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                    tenantContext.SetClient(client);

                    var processor = scope.ServiceProvider.GetRequiredService<IProcessorApplication>();

                    await processor.ProcessProjectScopeAsync(messageData);
                }

                Console.WriteLine($"[{timestamp}] ✅ Mensagem processada com sucesso. Enviando ACK.");
                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem.");
                Console.WriteLine($"[{timestamp}] 💥 Erro no processamento da mensagem: {ex.Message}. Enviando NACK (Requeue).");

                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        }

        public override void Dispose()
        {
            _channel?.Close();
            _channel?.Dispose();
            base.Dispose();
        }
    }
}