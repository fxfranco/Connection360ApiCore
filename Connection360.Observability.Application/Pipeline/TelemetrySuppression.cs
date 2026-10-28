namespace Connection360.Observability.Application.Pipeline
{
    /// <summary>
    /// Marca el flujo asíncrono actual como "interno de la observabilidad". Mientras está activa,
    /// el recolector de trazas no registra spans: así las llamadas que hace el propio escritor al
    /// almacenamiento no generan telemetría que a su vez genere más escrituras (bucle infinito).
    /// </summary>
    public static class TelemetrySuppression
    {
        private static readonly AsyncLocal<Boolean> Suppressed = new();

        public static Boolean IsSuppressed => Suppressed.Value;

        public static IDisposable Begin()
        {
            Boolean previous = Suppressed.Value;
            Suppressed.Value = true;
            return new Scope(previous);
        }

        private sealed class Scope : IDisposable
        {
            private readonly Boolean _previous;

            public Scope(Boolean previous) => _previous = previous;

            public void Dispose() => Suppressed.Value = _previous;
        }
    }
}
