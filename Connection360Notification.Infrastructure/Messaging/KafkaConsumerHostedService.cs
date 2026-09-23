using Confluent.Kafka;
using Connection360Notification.Application.Ports.Inbound;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Connection360Notification.Infrastructure.Messaging
{
    public class KafkaConsumerHostedService : BackgroundService
    {
        private readonly IConsumer<String, String> _consumer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly KafkaSettings _kafkaSettings;

        public KafkaConsumerHostedService(IOptions<KafkaSettings> kafkaSettings, IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            _kafkaSettings = kafkaSettings.Value;

            var config = new ConsumerConfig
            {
                BootstrapServers = _kafkaSettings.BootstrapServers,
                GroupId = _kafkaSettings.GroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true
            };

            _consumer = new ConsumerBuilder<String, String>(config).Build();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _consumer.Subscribe(_kafkaSettings.Topic);

            await Task.Yield();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(stoppingToken);
                    if (result?.Message?.Value != null)
                    {
                        var notification = JsonSerializer.Deserialize<NotificationMessage>(result.Message.Value);
                        if (notification != null)
                        {
                            // La orquestación real (guardar + notificar por SignalR) vive en
                            // Application: este consumidor solo deserializa el mensaje de Kafka y
                            // delega el procesamiento al caso de uso correspondiente.
                            using var scope = _scopeFactory.CreateScope();
                            var processNotificationUseCase = scope.ServiceProvider.GetRequiredService<IProcessIncomingNotificationUseCase>();
                            await processNotificationUseCase.ExecuteAsync(notification, stoppingToken);
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    // Manejo de excepciones / logger
                }
            }
            _consumer.Close();
        }
    }
}
