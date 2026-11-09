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
        /// Diccionario de configuraciones por nombre de aplicación/API
        /// </summary>
        public Dictionary<String, ExternalApisDetail> Apis { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Método helper para obtener la configuración de una API de forma segura
        /// </summary>
        public ExternalApisDetail? GetConfig(String apiName)
        {
            return Apis.TryGetValue(apiName, out var apiFonfig) ? apiFonfig : null;
        }
    }
}
