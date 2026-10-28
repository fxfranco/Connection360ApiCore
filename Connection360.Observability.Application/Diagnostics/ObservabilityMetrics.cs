using System.Diagnostics.Metrics;

namespace Connection360.Observability.Application.Diagnostics
{
    /// <summary>
    /// Métricas de la propia canalización de observabilidad (cuántos registros se descartaron o
    /// se exportaron). Se recolectan como cualquier otra métrica, así se puede alertar si la
    /// telemetría empieza a perderse.
    /// </summary>
    public static class ObservabilityMetrics
    {
        public const String MeterName = "Connection360.Observability";

        private static readonly Meter Meter = new(MeterName);

        /// <summary>Registros descartados porque la cola estaba llena (etiqueta "signal": logs, metrics, traces).</summary>
        public static readonly Counter<long> Dropped = Meter.CreateCounter<long>(
            "observability.records.dropped", unit: "{record}", description: "Registros de telemetría descartados por cola llena.");

        /// <summary>Registros escritos con éxito (etiquetas "signal" y "store").</summary>
        public static readonly Counter<long> Exported = Meter.CreateCounter<long>(
            "observability.records.exported", unit: "{record}", description: "Registros de telemetría escritos en un almacenamiento.");

        /// <summary>Lotes que fallaron al escribirse (etiquetas "signal" y "store").</summary>
        public static readonly Counter<long> ExportFailures = Meter.CreateCounter<long>(
            "observability.export.failures", unit: "{batch}", description: "Lotes de telemetría que no se pudieron escribir.");
    }
}
