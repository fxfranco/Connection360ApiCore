using Connection360.Etl.Domain.Entities;
using System.Collections.Generic;
using System.Threading;

namespace Connection360.Etl.Application.Ports
{
    /// <summary>
    /// Puerto secundario (saliente): lo implementa Connection360.Etl.Infrastructure.
    /// Copiado de Connection360.Application.Ports.IExternalDataGateway, extendido con soporte de
    /// paginación (ver Connection360.Etl.Infrastructure.ExternalApi.ExternalApiSettings.PaginationEnabled).
    /// </summary>
    public interface IExternalDataGateway
    {
        /// <summary>
        /// Devuelve los datos de la API <paramref name="apiName"/> como una secuencia de "páginas".
        /// <list type="bullet">
        /// <item><description>Si la paginación está deshabilitada globalmente (o esta API en particular no tiene <c>PageSize</c> configurado), la secuencia contiene EXACTAMENTE un elemento con el 100% de los datos - mismo comportamiento que la consulta única de siempre.</description></item>
        /// <item><description>Si está habilitada, la secuencia contiene una página por cada página que devuelva la API externa (usando el tamaño de página y los nombres de parámetro configurados para esa API), y termina cuando la API devuelve una página vacía o incompleta.</description></item>
        /// </list>
        /// Es una secuencia perezosa (<see cref="IAsyncEnumerable{T}"/>): cada página solo se pide a
        /// la API externa en el momento en que el consumidor la solicita, nunca antes.
        /// </summary>
        IAsyncEnumerable<DynamicDataSet> FetchDataPagedAsync(string apiName, IDictionary<string, string> filters, CancellationToken cancellationToken);
    }
}
