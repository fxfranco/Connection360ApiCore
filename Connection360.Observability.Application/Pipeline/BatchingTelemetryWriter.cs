using Connection360.Observability.Application.Diagnostics;
using Connection360.Observability.Domain.Models;
using Connection360.Observability.Domain.Ports;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Connection360.Observability.Application.Pipeline
{
    /// <summary>
    /// Servicio en segundo plano que saca elementos de la cola, los agrupa en lotes (por tamaño o
    /// por tiempo) y los escribe en TODOS los almacenamientos registrados, en paralelo y aislando
    /// los fallos: si un almacenamiento falla, los demás siguen recibiendo los datos y la
    /// aplicación ni se entera. Al apagar vacía la cola pendiente (hasta el tiempo límite).
    /// </summary>
    /// <typeparam name="TItem">Lo que entra a la cola (para trazas es el Activity; para logs y métricas ya es el registro).</typeparam>
    /// <typeparam name="TRecord">Lo que se escribe en el almacenamiento.</typeparam>
    public sealed class BatchingTelemetryWriter<TItem, TRecord> : IObservabilityComponent, IDisposable
        where TRecord : TelemetryRecord
    {
        private static readonly TimeSpan WarningInterval = TimeSpan.FromSeconds(60);

        private readonly TelemetryQueue<TItem> _queue;
        private readonly Func<TItem, TRecord?> _map;
        private readonly IReadOnlyList<ITelemetryStore<TRecord>> _stores;
        private readonly Int32 _batchSize;
        private readonly TimeSpan _flushInterval;
        private readonly TimeSpan _shutdownTimeout;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _abort = new();
        private DateTime _lastWarningUtc = DateTime.MinValue;
        private Boolean _warnedNoStores;
        private Task? _runTask;

        public BatchingTelemetryWriter(
            TelemetryQueue<TItem> queue,
            Func<TItem, TRecord?> map,
            IEnumerable<ITelemetryStore<TRecord>> stores,
            Int32 batchSize,
            TimeSpan flushInterval,
            TimeSpan shutdownTimeout,
            ILogger logger)
        {
            _queue = queue;
            _map = map;
            _stores = stores.ToList();
            _batchSize = Math.Max(1, batchSize);
            _flushInterval = flushInterval <= TimeSpan.Zero ? TimeSpan.FromSeconds(2) : flushInterval;
            _shutdownTimeout = shutdownTimeout;
            _logger = logger;
        }

        /// <summary>Tarea del ciclo de escritura (null si todavía no se ha iniciado).</summary>
        public Task? ExecuteTask => _runTask;

        /// <summary>
        /// Arranca el ciclo en un hilo del pool y regresa de inmediato. Se usa Task.Run sin token a
        /// propósito: si el servicio se detiene justo después de iniciarse (p. ej. un proceso corto como
        /// el ETL) el ciclo igual corre y vacía la cola, en lugar de cancelarse sin haber empezado.
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _runTask ??= Task.Run(RunAsync, CancellationToken.None);
            return Task.CompletedTask;
        }

        private async Task RunAsync()
        {
            // Todo lo que haga este escritor (incluidas las llamadas al almacenamiento) no debe generar trazas.
            using IDisposable suppression = TelemetrySuppression.Begin();

            var batch = new List<TRecord>(_batchSize);
            var reader = _queue.Reader;

            try
            {
                while (await reader.WaitToReadAsync(_abort.Token).ConfigureAwait(false))
                {
                    Drain(reader, batch);

                    if (batch.Count < _batchSize)
                    {
                        // Se espera un poco más para completar el lote antes de escribir.
                        using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(_abort.Token);
                        waitCts.CancelAfter(_flushInterval);
                        try
                        {
                            while (batch.Count < _batchSize && await reader.WaitToReadAsync(waitCts.Token).ConfigureAwait(false))
                            {
                                Drain(reader, batch);
                            }
                        }
                        catch (OperationCanceledException) when (!_abort.IsCancellationRequested)
                        {
                            // Venció el tiempo de espera del lote: se escribe lo que haya.
                        }
                    }

                    await FlushAsync(batch).ConfigureAwait(false);
                    batch.Clear();
                }
            }
            catch (OperationCanceledException) when (_abort.IsCancellationRequested)
            {
                // Se agotó el tiempo de apagado: lo que quede en la cola se pierde.
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _queue.Complete();
            _abort.CancelAfter(_shutdownTimeout);

            if (_runTask is null)
            {
                return;
            }

            try
            {
                await _runTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // El anfitrión ya no quiere esperar más: se abandona el vaciado.
            }
        }

        public void Dispose() => _abort.Dispose();

        private void Drain(System.Threading.Channels.ChannelReader<TItem> reader, List<TRecord> batch)
        {
            while (batch.Count < _batchSize && reader.TryRead(out TItem? item))
            {
                try
                {
                    TRecord? record = _map(item);
                    if (record is not null)
                    {
                        batch.Add(record);
                    }
                }
                catch (Exception ex)
                {
                    Warn(ex, "No fue posible convertir un elemento de telemetría ({Signal}); se omite.");
                }
            }
        }

        private async Task FlushAsync(List<TRecord> batch)
        {
            if (batch.Count == 0)
            {
                return;
            }

            if (_stores.Count == 0)
            {
                if (!_warnedNoStores)
                {
                    _warnedNoStores = true;
                    _logger.LogWarning("No hay almacenamientos de telemetría registrados para {Signal}; los registros se descartan.", _queue.Signal);
                }

                return;
            }

            await Task.WhenAll(_stores.Select(store => WriteToStoreAsync(store, batch))).ConfigureAwait(false);
        }

        private async Task WriteToStoreAsync(ITelemetryStore<TRecord> store, List<TRecord> batch)
        {
            var tags = new KeyValuePair<String, Object?>[]
            {
                new("signal", _queue.Signal),
                new("store", store.Name),
            };

            // Se escribe una copia de solo lectura: el lote se reutiliza para el siguiente ciclo.
            IReadOnlyList<TRecord> snapshot = batch.ToArray();

            for (Int32 attempt = 1; attempt <= 2; attempt++)
            {
                try
                {
                    await store.WriteBatchAsync(snapshot, _abort.Token).ConfigureAwait(false);
                    ObservabilityMetrics.Exported.Add(snapshot.Count, tags);
                    return;
                }
                catch (OperationCanceledException) when (_abort.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    if (attempt == 2)
                    {
                        ObservabilityMetrics.ExportFailures.Add(1, tags);
                        Warn(ex, $"No fue posible escribir {snapshot.Count} registro(s) de {{Signal}} en '{store.Name}'; el lote se descarta.");
                        return;
                    }

                    try
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(500), _abort.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }
        }

        /// <summary>Advertencia limitada a una por minuto, para no inundar la salida si el almacenamiento está caído.</summary>
        private void Warn(Exception ex, String message)
        {
            DateTime now = DateTime.UtcNow;
            if (now - _lastWarningUtc < WarningInterval)
            {
                return;
            }

            _lastWarningUtc = now;
            _logger.LogWarning(ex, message, _queue.Signal);
        }
    }
}
