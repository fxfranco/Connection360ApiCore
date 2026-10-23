using Confluent.Kafka;
using Connection360.Domain.Dtos;
using Connection360.Domain.Interfaces;
using Connection360.Observability.Domain.Telemetry;
using Connection360Notification.Domain.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;

namespace Connection360.Infrastructure.Messaging
{
    /// <summary>
    /// Sincroniza los usuarios de Auth0 hacia Connection360Notification.Api. Se ejecuta UNA vez al arrancar la
    /// aplicación y luego de forma periódica (<see cref="Auth0UserSyncSettings.PollingIntervalMinutes"/>): lista
    /// los usuarios con <see cref="IAuth0UserService.GetBasicUsersAsync"/> (solo id, documento, nombre
    /// completo, teléfono y correo) y los publica en el tópico <see cref="Auth0UserSyncSettings.Topic"/> de Kafka, en lotes.
    /// El consumidor de Notification los guarda en la colección <c>basicUserData</c> sin duplicados.
    /// Está modelado sobre <see cref="OutboxPublisherWorker"/>.
    /// </summary>
    public class Auth0UserSyncWorker : BackgroundService
    {
        /// <summary>Intentos de la sincronización inicial si falla (p. ej. Auth0 o Kafka aún no disponibles al arrancar).</summary>
        private const Int32 StartupAttempts = 3;

        private static readonly JsonSerializerOptions MessageSerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<Auth0UserSyncWorker> _logger;
        private readonly IProducer<String, String> _producer;
        private readonly IOptionsMonitor<Auth0UserSyncSettings> _settings;

        // Métricas propias del sincronizador (librería nativa System.Diagnostics.Metrics).
        private static readonly Counter<long> SyncedUsers = Connection360Telemetry.Meter.CreateCounter<long>(
            "auth0.users.sync.users", unit: "{user}", description: "Usuarios de Auth0 publicados en Kafka para Notification.");

        private static readonly Counter<long> SyncCycles = Connection360Telemetry.Meter.CreateCounter<long>(
            "auth0.users.sync.cycles", unit: "{cycle}", description: "Ciclos de sincronización de usuarios de Auth0, por resultado.");

        public Auth0UserSyncWorker(IServiceScopeFactory scopeFactory, ILogger<Auth0UserSyncWorker> logger, IOptions<KafkaSettings> kafkaSettings, IOptionsMonitor<Auth0UserSyncSettings> settings)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _settings = settings;

            var config = new ProducerConfig
            {
                BootstrapServers = kafkaSettings.Value.BootstrapServers,
                // Si Kafka no responde, el envío falla en 30 s (en vez de los 5 min por defecto) y el ciclo se reintenta.
                MessageTimeoutMs = 30000
            };

            _producer = new ProducerBuilder<String, String>(config).Build();
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            // Cede el hilo para que el arranque del host no espere a la primera sincronización.
            await Task.Yield();

            // 1. Sincronización inicial, al arrancar la aplicación (con reintentos).
            await RunStartupSyncAsync(cancellationToken);

            // 2. Sincronización periódica. La periodicidad se lee de IOptionsMonitor (no IOptions) para poder
            //    cambiarla en caliente editando el appsettings, igual que en OutboxPublisherWorker.
            using var timer = new PeriodicTimer(GetPollingInterval());
            while (!cancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync(cancellationToken))
            {
                TimeSpan currentInterval = GetPollingInterval();
                if (timer.Period != currentInterval)
                {
                    timer.Period = currentInterval;
                }

                await RunSyncCycleAsync(cancellationToken);
            }
        }

        private async Task RunStartupSyncAsync(CancellationToken cancellationToken)
        {
            for (Int32 attempt = 1; attempt <= StartupAttempts && !cancellationToken.IsCancellationRequested; attempt++)
            {
                if (await RunSyncCycleAsync(cancellationToken))
                {
                    return;
                }

                if (attempt < StartupAttempts)
                {
                    await Task.Delay(GetStartupRetryDelay(), cancellationToken);
                }
            }
        }

        /// <summary>
        /// Ejecuta una sincronización completa. Devuelve <c>true</c> si terminó bien (o si está deshabilitada /
        /// no hay usuarios) y <c>false</c> si falló; nunca propaga errores (salvo la cancelación) para que el
        /// worker siga vivo y lo intente de nuevo en el siguiente ciclo.
        /// </summary>
        private async Task<Boolean> RunSyncCycleAsync(CancellationToken cancellationToken)
        {
            Auth0UserSyncSettings settings = _settings.CurrentValue;
            if (!settings.Enabled)
            {
                _logger.LogInformation("Auth0UserSyncWorker deshabilitado (Auth0UserSync:Enabled = false); se omite la sincronización.");
                return true;
            }

            _logger.LogInformation("Iniciando Auth0UserSyncWorker {Date}", DateTime.Now);

            // Un span por ciclo; si nadie escucha trazas (observabilidad desactivada) es null.
            using Activity? span = Connection360Telemetry.Source.StartActivity("auth0.users.sync", ActivityKind.Producer);
            try
            {
                IList<Auth0UserBasicDto> users;

                // CreateAsyncScope + "await using": mismo criterio que OutboxPublisherWorker (los servicios
                // del scope pueden implementar solo IAsyncDisposable).
                await using (AsyncServiceScope scope = _scopeFactory.CreateAsyncScope())
                {
                    IAuth0UserService auth0UserService = scope.ServiceProvider.GetRequiredService<IAuth0UserService>();
                    users = await auth0UserService.GetBasicUsersAsync(cancellationToken);
                }

                if (users.Count == 0)
                {
                    // Una lista vacía casi siempre indica un problema (permisos, filtro) y no que Auth0 no tenga
                    // usuarios; no se publica para no borrar en Notification los usuarios ya sincronizados.
                    _logger.LogWarning("Auth0 no devolvió usuarios; no se publica nada en Kafka.");
                    span?.SetTag("auth0.sync.users", 0);
                    SyncCycles.Add(1, new KeyValuePair<String, Object?>("success", true));
                    return true;
                }

                Int32 batches = await PublishUsersAsync(users, settings, cancellationToken);

                span?.SetTag("auth0.sync.users", users.Count);
                span?.SetTag("auth0.sync.batches", batches);
                SyncedUsers.Add(users.Count);
                SyncCycles.Add(1, new KeyValuePair<String, Object?>("success", true));
                _logger.LogInformation("Auth0UserSyncWorker publicó {Users} usuarios en {Batches} mensaje(s).", users.Count, batches);
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                span?.AddException(ex);
                span?.SetStatus(ActivityStatusCode.Error, ex.Message);
                SyncCycles.Add(1, new KeyValuePair<String, Object?>("success", false));
                _logger.LogError(ex, "Error no controlado Auth0UserSyncWorker {Ex}", ex);
                return false;
            }
        }

        /// <summary>
        /// Publica los usuarios en lotes. Todos los lotes de la sincronización usan el mismo <c>SyncId</c> como
        /// clave del mensaje, así caen en la misma partición y el consumidor los recibe en orden.
        /// </summary>
        private async Task<Int32> PublishUsersAsync(IList<Auth0UserBasicDto> users, Auth0UserSyncSettings settings, CancellationToken cancellationToken)
        {
            String topic = String.IsNullOrWhiteSpace(settings.Topic) ? Auth0UserSyncSettings.DefaultTopic : settings.Topic.Trim();
            Int32 batchSize = settings.BatchSize > 0 ? settings.BatchSize : Auth0UserSyncSettings.DefaultBatchSize;

            Guid syncId = Guid.NewGuid();
            DateTime generatedAtUtc = DateTime.UtcNow;
            List<Auth0UserBasicDto[]> batches = users.Chunk(batchSize).ToList();

            for (Int32 index = 0; index < batches.Count; index++)
            {
                var message = new Auth0UsersSyncMessage
                {
                    SyncId = syncId,
                    BatchIndex = index,
                    BatchCount = batches.Count,
                    TotalUsers = users.Count,
                    GeneratedAtUtc = generatedAtUtc,
                    Users = batches[index].ToList()
                };

                var kafkaMessage = new Message<String, String>
                {
                    Key = syncId.ToString(),
                    Value = JsonSerializer.Serialize(message, MessageSerializerOptions)
                };

                // Publicar a Apache Kafka de forma esperada: si falla, el ciclo se registra como error.
                await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);
            }

            return batches.Count;
        }

        /// <summary>
        /// Obtiene la periodicidad configurada actualmente (sección "Auth0UserSync" del appsettings de
        /// Connection360.Api), devolviendo <see cref="Auth0UserSyncSettings.DefaultPollingIntervalMinutes"/> si el
        /// valor configurado es inválido (menor o igual a cero).
        /// </summary>
        private TimeSpan GetPollingInterval()
        {
            Int32 minutes = _settings.CurrentValue.PollingIntervalMinutes;
            if (minutes <= 0)
            {
                minutes = Auth0UserSyncSettings.DefaultPollingIntervalMinutes;
            }

            return TimeSpan.FromMinutes(minutes);
        }

        private TimeSpan GetStartupRetryDelay()
        {
            Int32 seconds = _settings.CurrentValue.StartupRetryDelaySeconds;
            return TimeSpan.FromSeconds(seconds < 0 ? Auth0UserSyncSettings.DefaultStartupRetryDelaySeconds : seconds);
        }

        public override void Dispose()
        {
            // Primero se cancela el token (base.Dispose) para que el bucle deje de usar el cliente.
            base.Dispose();
            _producer.Dispose();
        }
    }
}
