using System.Diagnostics;
using Connection360.Observability.Application.Pipeline;
using Connection360.Observability.Domain.Models;
using Connection360.Observability.Domain.Settings;
using Microsoft.Extensions.Logging;

namespace Connection360.Observability.Application.Logging
{
    /// <summary>
    /// Proveedor de ILogger (Microsoft.Extensions.Logging) que convierte cada log en un
    /// <see cref="LogRecord"/> estructurado y lo deja en una cola en memoria. No hace ninguna
    /// operación de red ni de disco: el costo en el hilo que escribe el log es crear el registro y
    /// encolarlo.
    /// </summary>
    [ProviderAlias("Connection360Telemetry")]
    public sealed class TelemetryLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        /// <summary>Categorías internas que jamás se capturan, para no crear un bucle (la observabilidad registrando sobre sí misma).</summary>
        public static readonly String[] InternalCategoryPrefixes = { "Connection360.Observability", "MongoDB" };

        private readonly TelemetryQueue<LogRecord> _queue;
        private readonly ServiceIdentity _identity;
        private readonly LogsOptions _options;
        private readonly Int32 _maxFieldLength;
        private readonly LogLevel _defaultLevel;
        private readonly List<KeyValuePair<String, LogLevel>> _categoryLevels;
        private IExternalScopeProvider? _scopeProvider;

        public TelemetryLoggerProvider(TelemetryQueue<LogRecord> queue, ServiceIdentity identity, ObservabilityOptions options)
        {
            _queue = queue;
            _identity = identity;
            _options = options.Logs;
            _maxFieldLength = options.MaxFieldLength;
            _defaultLevel = ParseLevel(_options.MinimumLevel, LogLevel.Information);
            _categoryLevels = _options.CategoryLevels
                .Select(pair => new KeyValuePair<String, LogLevel>(pair.Key, ParseLevel(pair.Value, _defaultLevel)))
                .OrderByDescending(pair => pair.Key.Length)
                .ToList();
        }

        public ILogger CreateLogger(String categoryName)
        {
            if (!_options.Enabled || InternalCategoryPrefixes.Any(prefix => categoryName.StartsWith(prefix, StringComparison.Ordinal)))
            {
                return NullLogger.Instance;
            }

            return new TelemetryLogger(categoryName, ResolveMinimumLevel(categoryName), _queue, _identity, _maxFieldLength, () => _scopeProvider);
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopeProvider = scopeProvider;

        public void Dispose() { }

        private LogLevel ResolveMinimumLevel(String category)
        {
            foreach (var pair in _categoryLevels)
            {
                if (category.StartsWith(pair.Key, StringComparison.Ordinal))
                {
                    return pair.Value;
                }
            }

            return _defaultLevel;
        }

        internal static LogLevel ParseLevel(String? text, LogLevel fallback)
            => Enum.TryParse(text, ignoreCase: true, out LogLevel level) ? level : fallback;

        private sealed class NullLogger : ILogger
        {
            public static readonly NullLogger Instance = new();

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public Boolean IsEnabled(LogLevel logLevel) => false;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, String> formatter) { }
        }
    }

    internal sealed class TelemetryLogger : ILogger
    {
        private const String OriginalFormatKey = "{OriginalFormat}";

        private readonly String _category;
        private readonly LogLevel _minimumLevel;
        private readonly TelemetryQueue<LogRecord> _queue;
        private readonly ServiceIdentity _identity;
        private readonly Int32 _maxFieldLength;
        private readonly Func<IExternalScopeProvider?> _scopeProvider;

        public TelemetryLogger(String category, LogLevel minimumLevel, TelemetryQueue<LogRecord> queue, ServiceIdentity identity, Int32 maxFieldLength, Func<IExternalScopeProvider?> scopeProvider)
        {
            _category = category;
            _minimumLevel = minimumLevel;
            _queue = queue;
            _identity = identity;
            _maxFieldLength = maxFieldLength;
            _scopeProvider = scopeProvider;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _scopeProvider()?.Push(state);

        public Boolean IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && logLevel >= _minimumLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, String> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            try
            {
                _queue.TryEnqueue(Build(logLevel, eventId, state, exception, formatter));
            }
            catch
            {
                // Un fallo de observabilidad jamás debe propagarse a quien está escribiendo el log.
            }
        }

        private LogRecord Build<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, String> formatter)
        {
            var attributes = new Dictionary<String, Object?>();
            String? template = null;

            if (state is IEnumerable<KeyValuePair<String, Object?>> pairs)
            {
                foreach (var pair in pairs)
                {
                    if (pair.Key == OriginalFormatKey)
                    {
                        template = pair.Value as String;
                        continue;
                    }

                    attributes[pair.Key] = TelemetryValue.Normalize(pair.Value, _maxFieldLength);
                }
            }

            _scopeProvider()?.ForEachScope(static (scope, target) =>
            {
                if (scope is IEnumerable<KeyValuePair<String, Object?>> scopePairs)
                {
                    foreach (var pair in scopePairs)
                    {
                        if (pair.Key != OriginalFormatKey && !target.Dictionary.ContainsKey(pair.Key))
                        {
                            target.Dictionary[pair.Key] = TelemetryValue.Normalize(pair.Value, target.MaxLength);
                        }
                    }
                }
            }, new ScopeTarget(attributes, _maxFieldLength));

            Activity? activity = Activity.Current;

            return new LogRecord
            {
                Timestamp = DateTime.UtcNow,
                Service = _identity.ServiceName,
                ServiceVersion = _identity.ServiceVersion,
                Environment = _identity.Environment,
                InstanceId = _identity.InstanceId,
                Level = logLevel.ToString(),
                SeverityNumber = SeverityOf(logLevel),
                Category = _category,
                EventId = eventId.Id,
                EventName = eventId.Name,
                Message = TelemetryValue.Truncate(formatter(state, exception), _maxFieldLength),
                MessageTemplate = template is null ? null : TelemetryValue.Truncate(template, _maxFieldLength),
                Attributes = attributes,
                ExceptionType = exception?.GetType().FullName,
                ExceptionMessage = exception is null ? null : TelemetryValue.Truncate(exception.Message, _maxFieldLength),
                ExceptionStackTrace = exception is null ? null : TelemetryValue.Truncate(exception.ToString(), _maxFieldLength),
                TraceId = activity?.TraceId.ToHexString(),
                SpanId = activity?.SpanId.ToHexString(),
            };
        }

        private static Int32 SeverityOf(LogLevel level) => level switch
        {
            LogLevel.Trace => 1,
            LogLevel.Debug => 5,
            LogLevel.Information => 9,
            LogLevel.Warning => 13,
            LogLevel.Error => 17,
            LogLevel.Critical => 21,
            _ => 0,
        };

        private readonly record struct ScopeTarget(Dictionary<String, Object?> Dictionary, Int32 MaxLength);
    }
}
