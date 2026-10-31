namespace Connection360.Infrastructure.Caching
{
    /// <summary>
    /// Configuración de la caché en memoria de coordenadas (sección "CoordinatesCache" del appsettings).
    /// Las coordenadas de un lugar prácticamente no cambian, por lo que se evita consultar el API externo
    /// (Nominatim, que además limita a ~1 petición por segundo) cada vez que se pide el detalle de un envío.
    ///
    /// El límite principal es de MEMORIA (bytes estimados), no de cantidad de entradas, para que el consumo
    /// del proceso sea predecible en la instancia donde corre el API (EC2 t3.medium: 2 vCPU / 4 GiB compartidos
    /// con el resto de contenedores).
    /// </summary>
    public class CoordinatesCacheSettings
    {
        /// <summary>Nombre de la sección en el appsettings.</summary>
        public const String SectionName = "CoordinatesCache";

        /// <summary>Si es false no se registra la caché y siempre se consulta el API externo.</summary>
        public Boolean Enabled { get; set; } = true;

        /// <summary>
        /// Tope de memoria que puede ocupar la caché, en bytes (estimados: ver
        /// <see cref="MemoryCoordinatesCache.EstimateEntrySize"/>). Por defecto 10 MB (~16.000 lugares),
        /// menos del 0,3 % de los 4 GiB de un t3.medium.
        /// </summary>
        public Int64 MaxSizeBytes { get; set; } = 10L * 1024 * 1024;

        /// <summary>
        /// Fracción de las entradas (0-1] que se libera de una sola vez al alcanzar el tope. Se eliminan primero
        /// las menos recientemente usadas (las más viejas). Liberar un bloque, en vez de una sola
        /// entrada, evita compactar en cada petición cuando la caché está llena.
        /// </summary>
        public Double EvictionPercentage { get; set; } = 0.20;

        /// <summary>Vida máxima de una entrada desde que se guardó (las coordenadas casi no cambian).</summary>
        public TimeSpan AbsoluteExpiration { get; set; } = TimeSpan.FromDays(30);

        /// <summary>Tiempo sin consultarse tras el cual una entrada caduca. Null = sin expiración deslizante.</summary>
        public TimeSpan? SlidingExpiration { get; set; } = TimeSpan.FromDays(7);

        /// <summary>
        /// Longitud máxima del nombre del lugar (clave) que se cachea. Los nombres más largos se consultan
        /// siempre al API externo para que una clave anómala no ocupe memoria.
        /// </summary>
        public Int32 MaxKeyLength { get; set; } = 200;
    }
}
