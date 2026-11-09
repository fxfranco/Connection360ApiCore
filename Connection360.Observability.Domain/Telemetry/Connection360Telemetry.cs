using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace Connection360.Observability.Domain.Telemetry
{
    /// <summary>
    /// Punto único para instrumentar código propio con las librerías nativas de .NET
    /// (System.Diagnostics.ActivitySource y System.Diagnostics.Metrics.Meter), sin depender de
    /// ningún paquete ni de ningún almacenamiento. Cualquier capa de cualquier proyecto puede
    /// usarlo; si la observabilidad está desactivada, StartActivity devuelve null y los
    /// instrumentos no hacen nada.
    /// </summary>
    public static class Connection360Telemetry
    {
        /// <summary>Nombre del ActivitySource y del Meter propios de la solución.</summary>
        public const String Name = "Connection360";

        private static readonly String Version =
            typeof(Connection360Telemetry).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(Connection360Telemetry).Assembly.GetName().Version?.ToString()
            ?? "1.0.0";

        /// <summary>Fuente de trazas. Ejemplo: <c>using var span = Connection360Telemetry.Source.StartActivity("etl.run");</c></summary>
        public static readonly ActivitySource Source = new(Name, Version);

        /// <summary>Fábrica de instrumentos de métricas. Ejemplo: <c>Meter.CreateCounter&lt;long&gt;("etl.runs")</c></summary>
        public static readonly Meter Meter = new(Name, Version);
    }
}
