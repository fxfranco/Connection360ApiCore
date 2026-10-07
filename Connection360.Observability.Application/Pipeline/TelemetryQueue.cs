using System.Threading.Channels;
using Connection360.Observability.Application.Diagnostics;

namespace Connection360.Observability.Application.Pipeline
{
    /// <summary>
    /// Cola en memoria, acotada y sin bloqueos entre quien produce telemetría (la aplicación) y el
    /// escritor en segundo plano. Si se llena, el elemento nuevo se descarta y se cuenta: la
    /// observabilidad nunca puede frenar ni tumbar una petición.
    /// </summary>
    public sealed class TelemetryQueue<T>
    {
        private readonly Channel<T> _channel;
        private readonly String _signal;
        private Int64 _dropped;

        public TelemetryQueue(String signal, Int32 capacity)
        {
            _signal = signal;
            _channel = Channel.CreateBounded<T>(new BoundedChannelOptions(Math.Max(1, capacity))
            {
                // Wait: TryWrite devuelve false cuando está llena (con DropWrite devolvería true y no sabríamos que se perdió).
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false,
            });
        }

        public String Signal => _signal;

        /// <summary>Cantidad de elementos descartados desde que arrancó el proceso.</summary>
        public Int64 Dropped => Interlocked.Read(ref _dropped);

        public ChannelReader<T> Reader => _channel.Reader;

        /// <summary>Encola sin esperar. Devuelve false (y cuenta el descarte) si la cola está llena o ya se cerró.</summary>
        public Boolean TryEnqueue(T item)
        {
            if (_channel.Writer.TryWrite(item))
            {
                return true;
            }

            Interlocked.Increment(ref _dropped);
            ObservabilityMetrics.Dropped.Add(1, new KeyValuePair<String, Object?>("signal", _signal));
            return false;
        }

        /// <summary>Cierra la cola: el escritor termina cuando vacía lo que quedó pendiente.</summary>
        public void Complete() => _channel.Writer.TryComplete();
    }
}
