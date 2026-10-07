using System.Diagnostics;
using Connection360.Observability.Application.Pipeline;
using Connection360.Observability.Domain.Models;
using Connection360.Observability.Domain.Settings;
using Connection360.Observability.Domain.Telemetry;
using Microsoft.Extensions.Hosting;

namespace Connection360.Observability.Application.Tracing
{
    /// <summary>
    /// Trazas: se suscribe (con un <see cref="ActivityListener"/> nativo de .NET) a los
    /// ActivitySource de la solución ("Connection360") y a los que emiten ASP.NET Core, HttpClient y
    /// Npgsql, aplica el muestreo y encola cada Activity terminado. La conversión a
    /// <see cref="TraceRecord"/> y la escritura ocurren después, en el hilo del escritor.
    /// </summary>
    public sealed class ActivityTelemetryCollector : IObservabilityComponent, IDisposable
    {
        private readonly TelemetryQueue<Activity> _queue;
        private readonly TracesOptions _options;
        private readonly String[] _sources;
        private ActivityListener? _listener;

        public ActivityTelemetryCollector(TelemetryQueue<Activity> queue, ObservabilityOptions options)
        {
            _queue = queue;
            _options = options.Traces;
            _sources = new[] { Connection360Telemetry.Name }
                .Concat(_options.IncludeSources.Where(source => !String.IsNullOrWhiteSpace(source)))
                .ToArray();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_listener is not null || !_options.Enabled)
            {
                return Task.CompletedTask;
            }

            _listener = new ActivityListener
            {
                ShouldListenTo = ShouldListenTo,
                Sample = Sample,
                SampleUsingParentId = SampleUsingParentId,
                ActivityStopped = OnActivityStopped,
            };
            ActivitySource.AddActivityListener(_listener);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Dispose();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _listener?.Dispose();
            _listener = null;
        }

        internal Boolean ShouldListenTo(ActivitySource source)
            => _sources.Any(name => source.Name.StartsWith(name, StringComparison.Ordinal));

        /// <summary>
        /// Muestreo "padre primero": si la petición ya viene muestreada (o no) desde otro servicio se
        /// respeta esa decisión; si es una traza nueva, se decide por la proporción configurada usando
        /// el propio TraceId (así la decisión es la misma en cualquier instancia).
        /// </summary>
        internal ActivitySamplingResult Sample(ref ActivityCreationOptions<ActivityContext> options)
        {
            if (TelemetrySuppression.IsSuppressed)
            {
                return ActivitySamplingResult.None;
            }

            ActivityContext parent = options.Parent;
            if (parent != default)
            {
                return (parent.TraceFlags & ActivityTraceFlags.Recorded) != 0
                    ? ActivitySamplingResult.AllDataAndRecorded
                    : ActivitySamplingResult.PropagationData;
            }

            return DecideByRatio(options.TraceId);
        }

        private ActivitySamplingResult SampleUsingParentId(ref ActivityCreationOptions<String> options)
        {
            if (TelemetrySuppression.IsSuppressed)
            {
                return ActivitySamplingResult.None;
            }

            return DecideByRatio(options.TraceId);
        }

        private ActivitySamplingResult DecideByRatio(ActivityTraceId traceId)
        {
            Double ratio = _options.SamplingRatio;
            if (ratio >= 1.0)
            {
                return ActivitySamplingResult.AllDataAndRecorded;
            }

            if (ratio <= 0.0)
            {
                return ActivitySamplingResult.PropagationData;
            }

            Span<Byte> bytes = stackalloc Byte[16];
            traceId.CopyTo(bytes);
            UInt64 sample = BitConverter.ToUInt64(bytes[8..]);
            return sample / (Double)UInt64.MaxValue < ratio
                ? ActivitySamplingResult.AllDataAndRecorded
                : ActivitySamplingResult.PropagationData;
        }

        private void OnActivityStopped(Activity activity)
        {
            if (!activity.IsAllDataRequested || IsExcluded(activity))
            {
                return;
            }

            _queue.TryEnqueue(activity);
        }

        private Boolean IsExcluded(Activity activity)
        {
            if (_options.ExcludePaths.Count == 0 || activity.Kind != ActivityKind.Server)
            {
                return false;
            }

            String? path = activity.GetTagItem("url.path") as String;
            if (path is null)
            {
                return false;
            }

            return _options.ExcludePaths.Any(excluded => path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase));
        }
    }
}
