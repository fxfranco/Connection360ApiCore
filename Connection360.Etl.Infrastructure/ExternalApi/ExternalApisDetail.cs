namespace Connection360.Etl.Infrastructure.ExternalApi
{
    /// <summary>
    /// Detalle de conexión de una API externa. Copiado de
    /// Connection360.Infrastructure.ExternalApi.ExternalApisDetail.
    /// </summary>
    public class ExternalApisDetail
    {
        /// <summary>Url de la api.</summary>
        public String BaseUrl { get; set; } = default!;

        /// <summary>Endpoint a consultar.</summary>
        public String DataEndpoint { get; set; } = default!;

        /// <summary>Si requiere una api key.</summary>
        public String? ApiKey { get; set; }

        /// <summary>Tiempo de espera de la api en segundos.</summary>
        public Int16 TimeoutSeconds { get; set; } = 30;

        /// <summary>Nombre del parámetro de query string que espera esta API para el número de página (base 1).</summary>
        public String PageNumberParam { get; set; } = "pageNumber";

        /// <summary>Nombre del parámetro de query string que espera esta API para el tamaño de página.</summary>
        public String PageSizeParam { get; set; } = "pageSize";
    }
}
