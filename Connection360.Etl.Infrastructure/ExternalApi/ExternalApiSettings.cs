using System;
using System.Collections.Generic;

namespace Connection360.Etl.Infrastructure.ExternalApi
{
    /// <summary>
    /// Configuración de las APIs externas a consultar. Copiado de
    /// Connection360.Infrastructure.ExternalApi.ExternalApiSettings: se mapea desde la misma
    /// sección "ExternalApi" del appsettings (ver Connection360.Etl.App/appsettings.json, copiada
    /// de Connection360.Api/appsettings.json) para no duplicar la configuración de endpoints.
    /// </summary>
    public class ExternalApiSettings
    {
        /// <summary>Nombre de la sección donde esta la configuración.</summary>
        public const String SectionName = "ExternalApi";

        /// <summary>Diccionario de configuraciones por nombre de aplicación/API.</summary>
        public Dictionary<String, ExternalApisDetail> Apis { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Interruptor GLOBAL para todo el proceso ETL: si es <c>false</c> (valor por defecto), cada
        /// API se consulta completa en una sola petición, igual que antes. Si es <c>true</c>, el
        /// proceso consulta cada API página por página (ver <see cref="PageSize"/> y los parámetros
        /// de paginación de cada API en <see cref="ExternalApisDetail"/>) y ejecuta
        /// Extract-Transform-Load por cada página en su propia transacción de base de datos.
        /// </summary>
        public Boolean PaginationEnabled { get; set; } = false;

        /// <summary>
        /// Tamaño de página GLOBAL: se usa para TODAS las APIs (BPMS, SIM, OPENCOMEX, ASISCOMEX,
        /// SYSTEMCARRIER y DATALOGS) cuando <see cref="PaginationEnabled"/> es <c>true</c>. A
        /// diferencia de una configuración por API, este valor es único a propósito: RunEtlProcessUseCase
        /// combina (merge) las 5 APIs operativas por documento de transporte, y si cada una paginara
        /// con un tamaño distinto, la página N de una no correspondería a la misma "porción" de datos
        /// que la página N de otra, rompiendo la consistencia del merge por ronda. 0 (valor por
        /// defecto) significa que, aunque <see cref="PaginationEnabled"/> esté en <c>true</c>, ninguna
        /// API pagina (se sigue consultando completa en una sola petición).
        /// </summary>
        public Int32 PageSize { get; set; } = 0;

        /// <summary>Método helper para obtener la configuración de una API de forma segura.</summary>
        public ExternalApisDetail? GetConfig(String apiName)
        {
            return Apis.TryGetValue(apiName, out var apiConfig) ? apiConfig : null;
        }
    }
}
