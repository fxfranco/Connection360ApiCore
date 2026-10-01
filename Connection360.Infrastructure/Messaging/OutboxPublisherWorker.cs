using Confluent.Kafka;
using Connection360.Domain.Dtos;
using Connection360.Domain.Ports.Persistence;
using Connection360Notification.Domain.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Connection360.Infrastructure.Messaging
{
    public class OutboxPublisherWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxPublisherWorker> _logger;
        private readonly IProducer<String, String> _producer;
        private readonly KafkaSettings _kafkaSettings;
        private readonly IOptionsMonitor<OutboxPublisherSettings> _outboxPublisherSettings;

        public OutboxPublisherWorker(IServiceScopeFactory scopeFactory, ILogger<OutboxPublisherWorker> logger, IOptions<KafkaSettings> kafkaSettings, IOptionsMonitor<OutboxPublisherSettings> outboxPublisherSettings)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _kafkaSettings = kafkaSettings.Value;
            _outboxPublisherSettings = outboxPublisherSettings;

            var config = new ProducerConfig
            {
                BootstrapServers = _kafkaSettings.BootstrapServers
            };

            _producer = new ProducerBuilder<String, String>(config).Build();
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            // El intervalo se obtiene de IOptionsMonitor (no IOptions) justamente para poder
            // cambiarlo en caliente: ASP.NET Core recarga el appsettings automáticamente
            // (reloadOnChange) e IOptionsMonitor.CurrentValue siempre refleja el último valor leído
            // del archivo, sin necesidad de recompilar ni reiniciar la aplicación.
            using var timer = new PeriodicTimer(GetPollingInterval());
            while (!cancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync(cancellationToken))
            {
                // Si el valor configurado cambió desde el ciclo anterior, se reasigna el Period del
                // propio PeriodicTimer para que el nuevo intervalo tenga efecto a partir del
                // siguiente tick, sin reiniciar el worker.
                TimeSpan currentInterval = GetPollingInterval();
                if (timer.Period != currentInterval)
                {
                    timer.Period = currentInterval;
                }

                // 2. Creación del Scope manual para este ciclo de ejecución
                DateTime date = DateTime.Now;
                _logger.LogInformation("Iniciando OutboxPublisherWorker {Date}", date);
                // Se usa CreateAsyncScope + "await using" (y no CreateScope + "using") porque
                // IUnitOfWork (UnitOfWork) solo implementa IAsyncDisposable: al cerrar un scope
                // síncrono, el contenedor de DI intenta llamar Dispose() sobre todos los servicios
                // resueltos en él, y para un servicio que solo tiene DisposeAsync() eso lanza
                // "InvalidOperationException: ... type only implements IAsyncDisposable. Use
                // DisposeAsync to dispose the container." AsyncServiceScope sabe llamar
                // DisposeAsync() correctamente en cada servicio que lo soporte.
                await using (var scope = _scopeFactory.CreateAsyncScope())
                {
                    // 3. Resolvemos IUnitOfWork de manera aislada dentro del Scope
                    IUnitOfWork _unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    IOutboxMessagesRepository outboxMessagesRepository = _unitOfWork.GetRepository<IOutboxMessagesRepository>();
                    // 0. Uso explícito de Unit of Work para garantizar la atomicidad transaccional
                    await _unitOfWork.BeginTransactionAsync(cancellationToken);
                    try
                    {
                        // 1. Obtener mensajes no procesados (Bloqueo por fila para evitar colisiones si escalas la API)
                        List<OutboxMessagesResultDto> outboxMessages = await outboxMessagesRepository.GetListAsync(cancellationToken);

                        if (outboxMessages.Count() > 0)
                        {
                            foreach (OutboxMessagesResultDto messages in outboxMessages)
                            {
                                var kafkaMessage = new Message<String, String>
                                {
                                    Key = messages.Id.ToString(),
                                    Value = messages.Payload
                                };

                                // 2. Publicar a Apache Kafka de forma síncrona/esperada
                                await _producer.ProduceAsync(_kafkaSettings.Topic, kafkaMessage, cancellationToken);

                                // 3. Marcar como procesado en la BD
                                await outboxMessagesRepository.UpdateprocessedAsync(messages.Id, cancellationToken);
                            }
                        }
                        await _unitOfWork.CommitAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        await _unitOfWork.RollbackAsync(cancellationToken);
                        _logger.LogError(ex, "Error no controlado OutboxPublisherWorker {Ex}", ex);
                    }
                }
            }
        }

        /// <summary>
        /// Obtiene el intervalo de sondeo configurado actualmente (sección "OutboxPublisher" del
        /// appsettings de Connection360.Api), devolviendo
        /// <see cref="OutboxPublisherSettings.DefaultPollingIntervalSeconds"/> si el valor
        /// configurado es inválido (menor o igual a cero).
        /// </summary>
        private TimeSpan GetPollingInterval()
        {
            Int16 seconds = _outboxPublisherSettings.CurrentValue.PollingIntervalSeconds;
            if (seconds <= 0)
            {
                seconds = OutboxPublisherSettings.DefaultPollingIntervalSeconds;
            }

            return TimeSpan.FromSeconds(seconds);
        }
    }
}
