using Connection360.Application.DTOs;
using Connection360.Domain.Entities;

namespace Connection360.Application.Ports
{
    // Puerto secundario (saliente): lo implementa Connection360.Application.Services.ApplicationDataSheetDataGateway.
    /// <summary>
    /// Expone, como un <see cref="DynamicDataSet"/> (misma forma que devuelve <see cref="IExternalDataGateway"/>),
    /// los datos ya consolidados de connection360write.application_data_sheet (vía
    /// IApplicationDataSheetEntregadosRepository / IApplicationDataSheetNoEntregadosRepository), en
    /// vez de consultar una API externa en vivo. Pensado como reemplazo directo de
    /// IExternalDataGateway.FetchDataAsync("BPMS", filters, cancellationToken) en casos de uso como
    /// GetClientSummaryUseCase, y reutilizable en otros casos de uso que necesiten este mismo dato.
    /// </summary>
    public interface IApplicationDataSheetDataGateway
    {
        /// <summary>
        /// Reemplazo directo de IExternalDataGateway.FetchDataAsync(apiName, filters, cancellationToken):
        /// combina siempre ambas vistas (<see cref="Connection360.Domain.Enums.ApplicationDataSheetViewScope.Todos"/>)
        /// y, si <paramref name="filters"/> trae la clave Connection360.Domain.Constans.ExternalDataFields.ClientNit,
        /// filtra por ese nit_cliente.
        /// </summary>
        Task<DynamicDataSet> FetchDataAsync(IDictionary<String, String> filters, CancellationToken cancellationToken);

        /// <summary>
        /// Variante explícita para otros casos de uso: permite elegir el alcance (una vista, la
        /// otra, o ambas), filtrar por nit_cliente, y pedir solo un subconjunto de columnas.
        /// </summary>
        Task<DynamicDataSet> FetchDataAsync(ApplicationDataSheetDataRequest request, CancellationToken cancellationToken);
    }
}
