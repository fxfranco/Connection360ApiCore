using Connection360.Observability.Application.Pipeline;
using Connection360.Observability.Domain.Ports;
using Microsoft.Extensions.Logging;

namespace Connection360.Observability.Application.Initialization
{
    /// <summary>
    /// Ejecuta una sola vez, en segundo plano, la preparación de cada almacenamiento (por ejemplo
    /// crear índices). No bloquea el arranque ni lo hace fallar si el almacenamiento no está
    /// disponible: solo registra una advertencia.
    /// </summary>
    public sealed class TelemetryStoreInitializationService : IObservabilityComponent
    {
        private readonly IReadOnlyList<ITelemetryStoreInitializer> _initializers;
        private readonly ILogger _logger;
        private Task _work = Task.CompletedTask;

        public TelemetryStoreInitializationService(IEnumerable<ITelemetryStoreInitializer> initializers, ILogger logger)
        {
            _initializers = initializers.ToList();
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_initializers.Count > 0)
            {
                _work = Task.Run(() => InitializeAsync(cancellationToken), CancellationToken.None);
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>Tarea de la inicialización en curso (para pruebas).</summary>
        internal Task Completion => _work;

        private async Task InitializeAsync(CancellationToken cancellationToken)
        {
            using IDisposable suppression = TelemetrySuppression.Begin();
            foreach (ITelemetryStoreInitializer initializer in _initializers)
            {
                try
                {
                    await initializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No fue posible preparar un almacenamiento de telemetría ({Initializer}).", initializer.GetType().Name);
                }
            }
        }
    }
}
