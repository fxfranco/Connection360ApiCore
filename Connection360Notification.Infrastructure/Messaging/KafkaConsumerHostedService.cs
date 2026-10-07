using Confluent.Kafka;
using Connection360.Observability.Domain.Telemetry;
using Connection360Notification.Application.Ports.Inbound;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;

namespace Connection360Notification.Infrastructure.Messaging
{
    public class KafkaConsumerHostedService : BackgroundService
    {
        private readonly IConsumer<String, String> _consumer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly KafkaSettings _kafkaSettings;

        private readonly ILogger<KafkaConsumerHostedService> _logger;

        // Métricas propias del consumidor (librería nativa System.Diagnostics.Metrics).
        private static readonly Counter<long> ConsumedMessages = Connection360Telemetry.Meter.CreateCounter<long>(
            "notifications.kafka.consumed", unit: "{message}", description: "Mensajes de Kafka procesados, por resultado.");

        public KafkaConsumerHostedService(IOptions<KafkaSettings> kafkaSettings, IServiceScopeFactory scopeFactory, ILogger<KafkaConsumerHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _kafkaSettings = kafkaSettings.Value;
            _logger = logger;
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
                            // Un span por mensaje (null si nadie escucha trazas).
                            using Activity? span = Connection360Telemetry.Source.StartActivity("notification.consume", ActivityKind.Consumer);
                            span?.SetTag("messaging.system", "kafka");
                            span?.SetTag("messaging.destination.name", _kafkaSettings.Topic);
                            try
                            {
                                using var scope = _scopeFactory.CreateScope();
                                var processNotificationUseCase = scope.ServiceProvider.GetRequiredService<IProcessIncomingNotificationUseCase>();
                                await processNotificationUseCase.ExecuteAsync(notification, stoppingToken);
                                ConsumedMessages.Add(1, new KeyValuePair<String, Object?>("success", true));
                            }
                            catch (Exception ex) when (ex is not OperationCanceledException)
                            {
                                span?.AddException(ex);
                                span?.SetStatus(ActivityStatusCode.Error, ex.Message);
                                ConsumedMessages.Add(1, new KeyValuePair<String, Object?>("success", false));
                                throw;
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error no controlado KafkaConsumerHostedService {Ex}", ex);
                }
            }
            _consumer.Close();
        }

        public override void Dispose()
        {
            // Primero se cancela el token (base.Dispose) para que el bucle deje de usar el cliente.
            base.Dispose();
            _consumer.Dispose();
        }
    }
}
