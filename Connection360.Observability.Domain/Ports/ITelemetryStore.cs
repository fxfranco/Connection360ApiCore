using Connection360.Observability.Domain.Models;

namespace Connection360.Observability.Domain.Ports
{
    /// <summary>
    /// Puerto de salida genérico: cualquier almacenamiento (MongoDB, Elasticsearch, Loki, un
    /// colector OTLP, un archivo...) se conecta a la observabilidad implementando este contrato.
    /// Recibe lotes (nunca registros sueltos) y se invoca siempre en segundo plano.
    /// </summary>
    /// <typeparam name="TRecord">Tipo de registro: <see cref="LogRecord"/>, <see cref="MetricRecord"/> o <see cref="TraceRecord"/>.</typeparam>
    public interface ITelemetryStore<TRecord> where TRecord : TelemetryRecord
    {
        /// <summary>Nombre del almacenamiento, usado en logs y métricas internas ("mongodb", "otlp"...).</summary>
        String Name { get; }

        /// <summary>
        /// Guarda un lote. Debe lanzar excepción si falla: el escritor la contabiliza y la registra
        /// sin afectar a la aplicación ni a los demás almacenamientos.
        /// </summary>
        Task WriteBatchAsync(IReadOnlyList<TRecord> records, CancellationToken cancellationToken);
    }

    /// <summary>Almacenamiento de logs.</summary>
    public interface ILogStore : ITelemetryStore<LogRecord> { }

    /// <summary>Almacenamiento de métricas.</summary>
    public interface IMetricStore : ITelemetryStore<MetricRecord> { }

    /// <summary>Almacenamiento de trazas.</summary>
    public interface ITraceStore : ITelemetryStore<TraceRecord> { }

    /// <summary>
    /// Preparación opcional de un almacenamiento (por ejemplo crear índices). Se ejecuta una vez,
    /// en segundo plano, al arrancar; un fallo solo genera una advertencia.
    /// </summary>
    public interface ITelemetryStoreInitializer
    {
        Task InitializeAsync(CancellationToken cancellationToken);
    }
}
