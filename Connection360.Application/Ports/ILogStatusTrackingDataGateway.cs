using Connection360.Application.DTOs;
using Connection360.Domain.Entities;

namespace Connection360.Application.Ports
{
    // Puerto secundario (saliente): lo implementa Connection360.Application.Services.LogStatusTrackingDataGateway.
    /// <summary>
    /// Expone, como un <see cref="DynamicDataSet"/> (misma forma que devuelve <see cref="IExternalDataGateway"/>),
    /// el historial de cambios de estado ya consolidado en connection360write.log_status_tracking
    /// (vía ILogStatusTrackingViewRepository / connection360read.vw_log_status_tracking), en vez de
    /// consultar la API externa "DATALOGS" en vivo. Pensado como reemplazo directo de
    /// IExternalDataGateway.FetchDataAsync("DATALOGS", filters, cancellationToken) en casos de uso
    /// como GetMyShipmentsUseCase, y reutilizable en otros casos de uso que necesiten este mismo dato.
    /// </summary>
    public interface ILogStatusTrackingDataGateway
    {
        /// <summary>
        /// Reemplazo directo de IExternalDataGateway.FetchDataAsync(apiName, filters, cancellationToken):
        /// si <paramref name="filters"/> trae la clave Connection360.Domain.Constans.ExternalDataFields.DocumentNumber,
        /// filtra por ese documento_transporte_hbl; si no, trae todo el historial.
        /// </summary>
        Task<DynamicDataSet> FetchDataAsync(IDictionary<String, String> filters, CancellationToken cancellationToken);

        /// <summary>
        /// Variante explícita para otros casos de uso: permite filtrar por documento_transporte_hbl y
        /// pedir solo un subconjunto de columnas.
        /// </summary>
        Task<DynamicDataSet> FetchDataAsync(LogStatusTrackingDataRequest request, CancellationToken cancellationToken);
    }
}
