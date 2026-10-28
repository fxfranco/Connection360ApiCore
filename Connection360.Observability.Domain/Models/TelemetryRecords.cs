namespace Connection360.Observability.Domain.Models
{
    /// <summary>
    /// Campos comunes de cualquier registro de telemetría. Los nombres siguen las convenciones de
    /// OpenTelemetry (service.name, service.version, deployment.environment, service.instance.id)
    /// para que exportar a cualquier plataforma de monitoreo sea un simple mapeo de campos.
    /// </summary>
    public abstract class TelemetryRecord
    {
        /// <summary>Momento del evento, en UTC.</summary>
        public DateTime Timestamp { get; init; }

        /// <summary>Aplicación que generó el registro (ver <see cref="ObservedServices"/>).</summary>
        public String Service { get; init; } = String.Empty;

        public String ServiceVersion { get; init; } = String.Empty;

        public String Environment { get; init; } = String.Empty;

        public String InstanceId { get; init; } = String.Empty;

        /// <summary>Atributos / etiquetas adicionales, ya normalizados a tipos simples (texto, números, booleanos, fechas).</summary>
        public IReadOnlyDictionary<String, Object?> Attributes { get; init; } = EmptyAttributes.Instance;
    }

    internal static class EmptyAttributes
    {
        public static readonly IReadOnlyDictionary<String, Object?> Instance = new Dictionary<String, Object?>();
    }

    /// <summary>Un evento de log (equivalente a un LogRecord de OpenTelemetry).</summary>
    public sealed class LogRecord : TelemetryRecord
    {
        /// <summary>Nombre del nivel: Trace, Debug, Information, Warning, Error o Critical.</summary>
        public String Level { get; init; } = String.Empty;

        /// <summary>Número de severidad de OpenTelemetry (1-24): Trace=1, Debug=5, Information=9, Warning=13, Error=17, Critical=21.</summary>
        public Int32 SeverityNumber { get; init; }

        /// <summary>Categoría del logger (normalmente el nombre completo de la clase).</summary>
        public String Category { get; init; } = String.Empty;

        public Int32 EventId { get; init; }

        public String? EventName { get; init; }

        /// <summary>Mensaje ya formateado.</summary>
        public String Message { get; init; } = String.Empty;

        /// <summary>Plantilla original del mensaje (por ejemplo "Procesadas {Count} notificaciones"), útil para agrupar.</summary>
        public String? MessageTemplate { get; init; }

        public String? ExceptionType { get; init; }

        public String? ExceptionMessage { get; init; }

        public String? ExceptionStackTrace { get; init; }

        /// <summary>Traza (hexadecimal de 32 caracteres) en la que se emitió el log, para correlacionarlo con <see cref="TraceRecord"/>.</summary>
        public String? TraceId { get; init; }

        /// <summary>Span (hexadecimal de 16 caracteres) en el que se emitió el log.</summary>
        public String? SpanId { get; init; }
    }

    /// <summary>
    /// Medición agregada de una serie de métricas durante un intervalo. En vez de guardar cada
    /// medición individual (que serían millones), el recolector agrega por instrumento + etiquetas
    /// y emite un registro por intervalo (por defecto cada 30 s).
    /// </summary>
    public sealed class MetricRecord : TelemetryRecord
    {
        /// <summary>Inicio del intervalo agregado (<see cref="TelemetryRecord.Timestamp"/> es el fin).</summary>
        public DateTime IntervalStart { get; init; }

        /// <summary>Nombre del instrumento, por ejemplo "http.server.request.duration".</summary>
        public String Name { get; init; } = String.Empty;

        public String? Description { get; init; }

        /// <summary>Unidad UCUM, por ejemplo "s", "ms", "By", "{request}".</summary>
        public String? Unit { get; init; }

        public String MeterName { get; init; } = String.Empty;

        public String? MeterVersion { get; init; }

        /// <summary>Counter, UpDownCounter, Histogram, Gauge, ObservableCounter, ObservableUpDownCounter u ObservableGauge.</summary>
        public String Kind { get; init; } = String.Empty;

        /// <summary>"Delta" (valores del intervalo) o "Cumulative" (valor acumulado desde el inicio del proceso).</summary>
        public String Temporality { get; init; } = String.Empty;

        /// <summary>Cantidad de mediciones en el intervalo.</summary>
        public Int64 Count { get; init; }

        /// <summary>Suma de las mediciones del intervalo.</summary>
        public Double Sum { get; init; }

        public Double Min { get; init; }

        public Double Max { get; init; }

        /// <summary>Última medición del intervalo (para gauges y contadores acumulados es el valor reportado).</summary>
        public Double Last { get; init; }
    }

    /// <summary>Un span de una traza distribuida (equivalente a un Span de OpenTelemetry / Activity de .NET).</summary>
    public sealed class TraceRecord : TelemetryRecord
    {
        public String TraceId { get; init; } = String.Empty;

        public String SpanId { get; init; } = String.Empty;

        public String? ParentSpanId { get; init; }

        public String Name { get; init; } = String.Empty;

        /// <summary>Internal, Server, Client, Producer o Consumer.</summary>
        public String Kind { get; init; } = String.Empty;

        /// <summary>Nombre del ActivitySource que lo creó (por ejemplo "Microsoft.AspNetCore" o "Connection360").</summary>
        public String SourceName { get; init; } = String.Empty;

        public String? SourceVersion { get; init; }

        public DateTime StartTime { get; init; }

        public DateTime EndTime { get; init; }

        public Double DurationMs { get; init; }

        /// <summary>Unset, Ok o Error.</summary>
        public String Status { get; init; } = "Unset";

        public String? StatusDescription { get; init; }

        public IReadOnlyList<SpanEventRecord> Events { get; init; } = Array.Empty<SpanEventRecord>();

        public IReadOnlyList<SpanLinkRecord> Links { get; init; } = Array.Empty<SpanLinkRecord>();
    }

    /// <summary>Evento puntual dentro de un span (por ejemplo una excepción).</summary>
    public sealed class SpanEventRecord
    {
        public String Name { get; init; } = String.Empty;

        public DateTime Timestamp { get; init; }

        public IReadOnlyDictionary<String, Object?> Attributes { get; init; } = EmptyAttributes.Instance;
    }

    /// <summary>Enlace de un span hacia otro span (de la misma u otra traza).</summary>
    public sealed class SpanLinkRecord
    {
        public String TraceId { get; init; } = String.Empty;

        public String SpanId { get; init; } = String.Empty;

        public IReadOnlyDictionary<String, Object?> Attributes { get; init; } = EmptyAttributes.Instance;
    }
}
