using System;
using System.Collections.Generic;
using System.Text;

namespace Connection360.Infrastructure.ExternalApi
{
    public class ExternalApisDetail
    {
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
