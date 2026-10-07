using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Text;
using Connection360.Observability.Application.Pipeline;
using Connection360.Observability.Domain.Models;
using Connection360.Observability.Domain.Settings;

namespace Connection360.Observability.Application.Metrics
{
    /// <summary>
    /// Métricas: un <see cref="MeterListener"/> nativo recibe cada medición de cualquier Meter del
    /// proceso (ASP.NET Core, HttpClient, Npgsql, runtime y los propios de la solución) y las
    /// agrega en memoria por instrumento + etiquetas. Cada intervalo (30 s por defecto) emite un
    /// <see cref="MetricRecord"/> por serie. En el camino de la medición solo hay una actualización
    /// de contadores bajo un lock diminuto: sin red, sin disco, sin colas por medición.
    /// </summary>
    public sealed class MetricsTelemetryCollector : IObservabilityComponent, IDisposable
    {
        private readonly TelemetryQueue<MetricRecord> _queue;
        private readonly ServiceIdentity _identity;
        private readonly MetricsOptions _options;
        private readonly Int32 _maxFieldLength;
        private readonly ConcurrentDictionary<Instrument, InstrumentState> _instruments = new();
        private MeterListener? _listener;
        private CancellationTokenSource? _stop;
        private Task? _loop;
        private DateTime _intervalStartUtc;

        public MetricsTelemetryCollector(TelemetryQueue<MetricRecord> queue, ServiceIdentity identity, ObservabilityOptions options)
        {
            _queue = queue;
            _identity = identity;
            _options = options.Metrics;
            _maxFieldLength = options.MaxFieldLength;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_listener is not null || !_options.Enabled)
            {
                return Task.CompletedTask;
            }

            _intervalStartUtc = DateTime.UtcNow;
            _listener = new MeterListener { InstrumentPublished = OnInstrumentPublished };
            _listener.SetMeasurementEventCallback<Byte>((i, v, t, s) => Record(s, v, t));
            _listener.SetMeasurementEventCallback<Int16>((i, v, t, s) => Record(s, v, t));
            _listener.SetMeasurementEventCallback<Int32>((i, v, t, s) => Record(s, v, t));
            _listener.SetMeasurementEventCallback<Int64>((i, v, t, s) => Record(s, v, t));
            _listener.SetMeasurementEventCallback<Single>((i, v, t, s) => Record(s, v, t));
            _listener.SetMeasurementEventCallback<Double>((i, v, t, s) => Record(s, v, t));
            _listener.SetMeasurementEventCallback<Decimal>((i, v, t, s) => Record(s, (Double)v, t));
            _listener.Start();

            _stop = new CancellationTokenSource();
            _loop = Task.Run(() => RunAsync(_stop.Token));
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_listener is null)
            {
                return;
            }

            _stop?.Cancel();
            if (_loop is not null)
            {
                try
                {
                    await _loop.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }

            // Última emisión con lo que haya quedado del intervalo en curso.
            Collect();
            Dispose();
        }

        public void Dispose()
        {
            _listener?.Dispose();
            _listener = null;
            _stop?.Dispose();
            _stop = null;
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, _options.CollectionIntervalSeconds)));
            try
            {
                while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
                {
                    Collect();
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void OnInstrumentPublished(Instrument instrument, MeterListener listener)
        {
            if (!IsIncluded(instrument.Meter.Name))
            {
                return;
            }

            var state = new InstrumentState(instrument);
            if (_instruments.TryAdd(instrument, state))
            {
                listener.EnableMeasurementEvents(instrument, state);
            }
        }

        internal Boolean IsIncluded(String meterName)
        {
            if (_options.ExcludeMeters.Any(prefix => meterName.StartsWith(prefix, StringComparison.Ordinal)))
            {
                return false;
            }

            return _options.IncludeMeters.Count == 0
                || _options.IncludeMeters.Any(prefix => meterName.StartsWith(prefix, StringComparison.Ordinal));
        }

        private void Record(Object? state, Double value, ReadOnlySpan<KeyValuePair<String, Object?>> tags)
        {
            if (state is InstrumentState instrumentState)
            {
                instrumentState.GetSeries(tags, _options.MaxSeriesPerInstrument).Add(value);
            }
        }

        /// <summary>Lee los instrumentos observables y emite un registro por cada serie con actividad. Es público para poder probarlo y para forzar una emisión.</summary>
        public void Collect()
        {
            MeterListener? listener = _listener;
            try
            {
                listener?.RecordObservableInstruments();
            }
            catch
            {
                // Un instrumento observable defectuoso no debe detener la recolección.
            }

            DateTime end = DateTime.UtcNow;
            DateTime start = _intervalStartUtc;
            _intervalStartUtc = end;

            foreach (InstrumentState instrument in _instruments.Values)
            {
                foreach (Series series in instrument.Series)
                {
                    SeriesSnapshot? snapshot = series.TakeSnapshot(instrument.IsDelta, instrument.TracksRunningTotal);
                    if (snapshot is null)
                    {
                        continue;
                    }

                    _queue.TryEnqueue(ToRecord(instrument, series, snapshot.Value, start, end));
                }
            }
        }

        private MetricRecord ToRecord(InstrumentState instrument, Series series, SeriesSnapshot snapshot, DateTime start, DateTime end)
        {
            var attributes = new Dictionary<String, Object?>(series.Tags.Length);
            foreach (var tag in series.Tags)
            {
                attributes[tag.Key] = TelemetryValue.Normalize(tag.Value, _maxFieldLength);
            }

            if (series.IsOverflow)
            {
                attributes["otel.metric.overflow"] = true;
            }

            return new MetricRecord
            {
                Timestamp = end,
                IntervalStart = start,
                Service = _identity.ServiceName,
                ServiceVersion = _identity.ServiceVersion,
                Environment = _identity.Environment,
                InstanceId = _identity.InstanceId,
                Name = instrument.Name,
                Description = instrument.Description,
                Unit = instrument.Unit,
                MeterName = instrument.MeterName,
                MeterVersion = instrument.MeterVersion,
                Kind = instrument.Kind,
                Temporality = instrument.IsDelta ? "Delta" : "Cumulative",
                Count = snapshot.Count,
                Sum = snapshot.Sum,
                Min = snapshot.Min,
                Max = snapshot.Max,
                Last = snapshot.Last,
                Attributes = attributes,
            };
        }

        private sealed class InstrumentState
        {
            private readonly ConcurrentDictionary<String, Series> _series = new();
            private Series? _overflow;

            public InstrumentState(Instrument instrument)
            {
                Name = instrument.Name;
                Description = instrument.Description;
                Unit = instrument.Unit;
                MeterName = instrument.Meter.Name;
                MeterVersion = instrument.Meter.Version;

                String type = instrument.GetType().Name;
                Int32 tick = type.IndexOf('`');
                Kind = tick > 0 ? type[..tick] : type;

                // Counter e Histogram se reportan como "delta" (lo ocurrido en el intervalo). Los observables y
                // los gauges reportan el valor actual, y los UpDownCounter además llevan un total corriente.
                IsDelta = Kind is "Counter" or "Histogram" or "UpDownCounter";
                TracksRunningTotal = Kind == "UpDownCounter";
            }

            public String Name { get; }
            public String? Description { get; }
            public String? Unit { get; }
            public String MeterName { get; }
            public String? MeterVersion { get; }
            public String Kind { get; }
            public Boolean IsDelta { get; }
            public Boolean TracksRunningTotal { get; }

            public IEnumerable<Series> Series => _overflow is null ? _series.Values : _series.Values.Append(_overflow);

            public Series GetSeries(ReadOnlySpan<KeyValuePair<String, Object?>> tags, Int32 maxSeries)
            {
                String key = BuildKey(tags);
                if (_series.TryGetValue(key, out Series? existing))
                {
                    return existing;
                }

                if (_series.Count >= maxSeries)
                {
                    return _overflow ??= new Series(Array.Empty<KeyValuePair<String, Object?>>(), isOverflow: true);
                }

                KeyValuePair<String, Object?>[] copy = tags.ToArray();
                return _series.GetOrAdd(key, _ => new Series(copy, isOverflow: false));
            }

            [ThreadStatic]
            private static StringBuilder? _keyBuilder;

            private static String BuildKey(ReadOnlySpan<KeyValuePair<String, Object?>> tags)
            {
                if (tags.Length == 0)
                {
                    return String.Empty;
                }

                StringBuilder builder = _keyBuilder ??= new StringBuilder(128);
                builder.Clear();
                foreach (var tag in tags)
                {
                    builder.Append(tag.Key).Append('=').Append(tag.Value).Append('\u001f');
                }

                return builder.ToString();
            }
        }

        private readonly record struct SeriesSnapshot(Int64 Count, Double Sum, Double Min, Double Max, Double Last);

        private sealed class Series
        {
            private readonly Object _gate = new();
            private Int64 _count;
            private Double _sum;
            private Double _min = Double.MaxValue;
            private Double _max = Double.MinValue;
            private Double _last;
            private Double _runningTotal;

            public Series(KeyValuePair<String, Object?>[] tags, Boolean isOverflow)
            {
                Tags = tags;
                IsOverflow = isOverflow;
            }

            public KeyValuePair<String, Object?>[] Tags { get; }

            public Boolean IsOverflow { get; }

            public void Add(Double value)
            {
                lock (_gate)
                {
                    _count++;
                    _sum += value;
                    _runningTotal += value;
                    _last = value;
                    if (value < _min) _min = value;
                    if (value > _max) _max = value;
                }
            }

            public SeriesSnapshot? TakeSnapshot(Boolean delta, Boolean runningTotal)
            {
                lock (_gate)
                {
                    if (_count == 0)
                    {
                        return null;
                    }

                    var snapshot = new SeriesSnapshot(_count, _sum, _min, _max, runningTotal ? _runningTotal : _last);
                    _count = 0;
                    _sum = 0;
                    _min = Double.MaxValue;
                    _max = Double.MinValue;
                    if (!delta)
                    {
                        _last = snapshot.Last;
                    }

                    return snapshot;
                }
            }
        }
    }
}
