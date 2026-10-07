namespace Connection360.Observability.Domain.Settings
{
    /// <summary>
    /// Configuración de la sección "Observability" del appsettings. Todo es opcional: los valores
    /// por defecto ya son razonables para producción.
    /// </summary>
    public sealed class ObservabilityOptions
    {
        public const String SectionName = "Observability";

        /// <summary>Interruptor general. Con false no se registra nada y no hay ningún costo en tiempo de ejecución.</summary>
        public Boolean Enabled { get; set; } = true;

        /// <summary>Tiempo máximo (segundos) para vaciar las colas al apagar la aplicación.</summary>
        public Int32 ShutdownTimeoutSeconds { get; set; } = 10;

        /// <summary>Longitud máxima de cualquier texto guardado (mensajes, atributos, stack traces); el excedente se trunca.</summary>
        public Int32 MaxFieldLength { get; set; } = 4096;

        public LogsOptions Logs { get; set; } = new();

        public MetricsOptions Metrics { get; set; } = new();

        public TracesOptions Traces { get; set; } = new();
    }

    /// <summary>Parámetros de la cola y del escritor por lotes de una señal.</summary>
    public class PipelineOptions
    {
        public Boolean Enabled { get; set; } = true;

        /// <summary>Capacidad de la cola en memoria. Si se llena, los registros nuevos se descartan (y se cuentan): nunca se bloquea la aplicación.</summary>
        public Int32 QueueCapacity { get; set; } = 10_000;

        /// <summary>Máximo de registros por escritura al almacenamiento.</summary>
        public Int32 BatchSize { get; set; } = 200;

        /// <summary>Cada cuántos segundos se escribe un lote incompleto.</summary>
        public Int32 FlushIntervalSeconds { get; set; } = 2;
    }

    public sealed class LogsOptions : PipelineOptions
    {
        /// <summary>Nivel mínimo: Trace, Debug, Information, Warning, Error, Critical.</summary>
        public String MinimumLevel { get; set; } = "Information";

        /// <summary>Nivel por prefijo de categoría; gana el prefijo más largo.</summary>
        public Dictionary<String, String> CategoryLevels { get; set; } = new()
        {
            ["Microsoft"] = "Warning",
            ["System"] = "Warning",
            ["Microsoft.Hosting.Lifetime"] = "Information",
        };
    }

    public sealed class MetricsOptions : PipelineOptions
    {
        /// <summary>Cada cuántos segundos se agregan y emiten las métricas.</summary>
        public Int32 CollectionIntervalSeconds { get; set; } = 30;

        /// <summary>Máximo de combinaciones de etiquetas distintas por instrumento (evita explosión de cardinalidad).</summary>
        public Int32 MaxSeriesPerInstrument { get; set; } = 500;

        /// <summary>Prefijos de Meter a recolectar. Vacío = todos.</summary>
        public List<String> IncludeMeters { get; set; } = new();

        /// <summary>Prefijos de Meter a ignorar.</summary>
        public List<String> ExcludeMeters { get; set; } = new();
    }

    public sealed class TracesOptions : PipelineOptions
    {
        /// <summary>Proporción de trazas nuevas que se registran (0 a 1). Una traza que ya viene muestreada desde otro servicio se respeta.</summary>
        public Double SamplingRatio { get; set; } = 1.0;

        /// <summary>Prefijos de ActivitySource a escuchar además de "Connection360".</summary>
        public List<String> IncludeSources { get; set; } = new()
        {
            "Microsoft.AspNetCore",
            "System.Net.Http",
            "Npgsql",
        };

        /// <summary>Rutas HTTP (prefijos) cuyos spans de servidor se ignoran, por ejemplo health checks.</summary>
        public List<String> ExcludePaths { get; set; } = new() { "/health" };
    }
}
