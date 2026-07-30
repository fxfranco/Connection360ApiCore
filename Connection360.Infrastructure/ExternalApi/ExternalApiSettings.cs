using System;
using System.Collections.Generic;
using System.Text;

namespace Connection360.Infrastructure.ExternalApi
{
    /// <summary>
    /// Clase tipo entidad para cargar la configuraciòn de las apis externas
    /// </summary>
    public class ExternalApiSettings
    {
        /// <summary>
        /// Nombre de la secciòn donde esta la configuraciòn
        /// </summary>
        public const String SectionName = "ExternalApi";

        /// <summary>
        /// Url de la api
        /// </summary>
        public String BaseUrl { get; set; } = default!;

        /// <summary>
        /// Endpoint a consultar
        /// </summary>
        public String DataEndpoint { get; set; } = default!;
        
        /// <summary>
        /// Si requiere una api key
        /// </summary>
        public String? ApiKey { get; set; }
        
        /// <summary>
        /// Tiempo de espera de la api en segundos
        /// </summary>
        public Int16 TimeoutSeconds { get; set; } = 30;
    }
}
