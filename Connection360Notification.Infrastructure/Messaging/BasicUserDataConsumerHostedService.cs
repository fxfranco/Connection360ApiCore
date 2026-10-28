using Confluent.Kafka;
using Connection360.Observability.Domain.Telemetry;
using Connection360Notification.Application.DTOs;
using Connection360Notification.Application.Ports.Inbound;
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
    /// <summary>
    /// Consume del tópico de usuarios (por defecto "notification-users") los lotes que publica Connection360.Api
    /// (Auth0UserSyncWorker) y delega en <see cref="ISyncBasicUsersUseCase"/> para guardarlos en la colección
    /// <c>basicUserData</c> sin duplicados. Es independiente de <see cref="KafkaConsumerHostedService"/>: usa su propio
    /// consumidor y su propio grupo de consumo.
    /// </summary>
    public class BasicUserDataConsumerHostedService : BackgroundService
    {
        private static readonly JsonSerializerOptions MessageSerializerOptions = new(JsonSerializerDefaults.Web);

        private static readonly Counter<long> ConsumedMessages = Connection360Telemetry.Meter.CreateCounter<long>(
            "notifications.basicusers.consumed", unit: "{message}", description: "Lotes de usuarios de Kafka procesados, por resultado.");

        private static readonly Counter<long> UpsertedUsers = Connection360Telemetry.Meter.CreateCounter<long>(
            "notifications.basicusers.upserted", unit: "{user}", description: "Usuarios guardados en basicUserData.");

        private readonly IConsumer<String, String> _consumer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BasicUserDataConsumerHostedService> _logger;
        private readonly String _topic;
        private readonly Int32 _errorBackoffMilliseconds;

        public BasicUserDataConsumerHostedService(IOptions<KafkaSettings> kafkaSettings, IOptions<BasicUserSyncSettings> syncSettings, IServiceScopeFactory scopeFactory, ILogger<BasicUserDataConsumerHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            BasicUserSyncSettings sync = syncSettings.Value;
            _topic = String.IsNullOrWhiteSpace(sync.Topic) ? BasicUserSyncSettings.DefaultTopic : sync.Topic.Trim();
            _errorBackoffMilliseconds = sync.ErrorBackoffMilliseconds < 0 ? BasicUserSyncSettings.DefaultErrorBackoffMilliseconds : sync.ErrorBackoffMilliseconds;

            var config = new ConsumerConfig
            {
                BootstrapServers = kafkaSettings.Value.BootstrapServers,
                GroupId = ResolveGroupId(kafkaSettings.Value, sync),
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true
            };

            _consumer = new ConsumerBuilder<String, String>(config).Build();
        }

        /// <summary>
        /// Grupo propio del tópico de usuarios: el configurado, o el del tópico de notificaciones + "-users".
        /// Debe ser distinto del grupo de notificaciones (un mismo grupo no puede suscribirse a tópicos distintos sin interferirse).
        /// </summary>
        public static String ResolveGroupId(KafkaSettings kafka, BasicUserSyncSettings sync)
        {
            if (!String.IsNullOrWhiteSpace(sync.GroupId))
            {
                return sync.GroupId.Trim();
            }

            return String.IsNullOrWhiteSpace(kafka.GroupId) ? BasicUserSyncSettings.DefaultGroupId : $"{kafka.GroupId.Trim()}-users";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _consumer.Subscribe(_topic);

            await Task.Yield();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    ConsumeResult<String, String>? result = _consumer.Consume(stoppingToken);
                    if (result?.Message?.Value != null)
                    {
                        BasicUsersSyncMessage? message = JsonSerializer.Deserialize<BasicUsersSyncMessage>(result.Message.Value, MessageSerializerOptions);
                        if (message != null)
                        {
                            await ProcessAsync(message, stoppingToken);
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error no controlado BasicUserDataConsumerHostedService {Ex}", ex);

                    try
                    {
                        await Task.Delay(_errorBackoffMilliseconds, stoppingToken);
                    }
                    catch (OperationCanceledException) { break; }
                }
            }

            _consumer.Close();
        }

        private async Task ProcessAsync(BasicUsersSyncMessage message, CancellationToken cancellationToken)
        {
            // Un span por mensaje (null si nadie escucha trazas).
            using Activity? span = Connection360Telemetry.Source.StartActivity("basicusers.consume", ActivityKind.Consumer);
            span?.SetTag("messaging.system", "kafka");
            span?.SetTag("messaging.destination.name", _topic);
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                ISyncBasicUsersUseCase useCase = scope.ServiceProvider.GetRequiredService<ISyncBasicUsersUseCase>();
                BasicUsersSyncResult result = await useCase.ExecuteAsync(message, cancellationToken);

                ConsumedMessages.Add(1, new KeyValuePair<String, Object?>("success", true));
                UpsertedUsers.Add(result.Upserted);
                span?.SetTag("basicusers.upserted", result.Upserted);

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        "Lote {Batch}/{Batches} de usuarios (sync {SyncId}): {Upserted} guardados, {Discarded} descartados.",
                        message.BatchIndex + 1, message.BatchCount, message.SyncId, result.Upserted, result.Discarded);
                }

                if (result.IsLastBatch)
                {
                    if (result.CleanupPerformed)
                    {
                        if (_logger.IsEnabled(LogLevel.Information))
                        {
                            _logger.LogInformation("Sincronización {SyncId} completa: {Deleted} usuario(s) obsoleto(s) eliminado(s).", message.SyncId, result.Deleted);
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Sincronización {SyncId} incompleta ({Stored} de {Total} usuarios guardados): no se eliminan usuarios obsoletos.",
                            message.SyncId, result.StoredInSync, message.TotalUsers);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                span?.AddException(ex);
                span?.SetStatus(ActivityStatusCode.Error, ex.Message);
                ConsumedMessages.Add(1, new KeyValuePair<String, Object?>("success", false));
                throw;
            }
        }

        public override void Dispose()
        {
            // Primero se cancela el token (base.Dispose) para que el bucle deje de usar el cliente.
            base.Dispose();
            _consumer.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
