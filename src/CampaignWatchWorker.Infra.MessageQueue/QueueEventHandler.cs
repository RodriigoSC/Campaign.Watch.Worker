using System.Text;
using CampaignWatchWorker.Application.Services.Interfaces;
using CampaignWatchWorker.Domain.Models.Interfaces;
using DTM_Logging.Service;
using DTM_MessageQueue.RabbitMQ.Consumers;
using DTM_MessageQueue.RabbitMQ.Publishers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client.Events;

namespace CampaignWatchWorker.Infra.MessageQueue
{
    public class QueueEventHandler : IQueueEventHandler
    {
        private readonly IDTM_RabbitMqConsumer _rabbitMqConsumer;
        private readonly ITenant _tenant;
        private readonly IFileLogger _fileLogger;
        private readonly IDTM_RabbitMqPublisher _messageHandler;
        private readonly IFileLogger _logger;
        private readonly IConfiguration _configuration;

        public const int maxAttempts = 3;
        public TimeSpan waitPerAttempt = TimeSpan.FromSeconds(10);


        public QueueEventHandler(
            IDTM_RabbitMqConsumer rabbitMqConsumer,
            ITenant tenant,
            IFileLogger fileLogger,
            IDTM_RabbitMqPublisher messageHandler,
            IFileLogger logger,
            IConfiguration configuration)
        {
            _rabbitMqConsumer = rabbitMqConsumer;
            _tenant = tenant;
            _fileLogger = fileLogger;
            _messageHandler = messageHandler;
            _logger = logger;
            _configuration = configuration;
        }

        public void SendToExchange(object data, string exchange, string route)
        {
            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetry(maxAttempts, i => waitPerAttempt);
            var attempt = 1;
            retryPolicy.Execute(() =>
            {
                _logger.Log(
                    $"Enviando para exchange: {_configuration[exchange]}. Tentativa {attempt}",
                    projectId: _tenant.Id,
                    projectName: _tenant.Name);
                _messageHandler.SendToExchange(data, _configuration[exchange], route);
                attempt++;
            });
        }

        public void Get(Action<object?, object?> action)
        {
            _rabbitMqConsumer.Register(_tenant.QueueNameMonitoring, new EventHandler<BasicDeliverEventArgs>((model, ea) =>
            {
                _fileLogger.Log("Buscando item da fila...", projectId: _tenant.Id, projectName: _tenant.Name);
                var channel = ((EventingBasicConsumer)model).Model;
                try
                {
                    var messageBodyString = Encoding.UTF8.GetString(ea.Body);

                    var messageToLog = $"Mensagem recebida da fila \"{_tenant.QueueNameMonitoring}\": {messageBodyString}";
                    _fileLogger.Log(messageToLog, projectId: _tenant.Id, projectName: _tenant.Name);

                    Console.WriteLine(messageToLog);
                    Console.WriteLine(Environment.NewLine);

                    object? message;

                    message = JsonConvert.DeserializeObject<object>(messageBodyString);

                    action(model, message);
                }
                catch (Exception ex)
                {
                    var messageError = "Erro para pegar mensagem da fila";

                    Console.WriteLine(messageError);
                    Console.WriteLine($"Message: {ex.Message}");
                    Console.WriteLine($"Type: {ex.GetType().FullName}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    Console.WriteLine(Environment.NewLine);

                    _fileLogger.Log(messageError, logLevel: LogLevel.Error, exception: ex, projectId: _tenant.Id, projectName: _tenant.Name);

                    action(ex, null);
                }
                finally
                {
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
            }), 1);
        }
    }
}