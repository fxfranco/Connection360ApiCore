using Connection360.Domain.Dtos;

namespace Connection360.Domain.Ports.Persistence
{
    /// <summary>
    /// Acceso de solo lectura a connection360read.vw_log_status_tracking (historial de cambios de
    /// estado de las operaciones; ver Documents/vw_log_status_tracking.sql).
    /// </summary>
    public interface ILogStatusTrackingViewRepository
    {
        /// <summary>Todas las columnas de todas las filas de la vista.</summary>
        Task<List<LogStatusTrackingViewResultDto>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>Todas las filas de la vista, pero solo las columnas pedidas en <paramref name="fieldsSelection"/>.</summary>
        Task<List<LogStatusTrackingViewResultDto>> GetAllAsync(LogStatusTrackingViewFieldsSelectionDto fieldsSelection, CancellationToken cancellationToken = default);

        /// <summary>Todas las columnas, filtrando las filas por documento_transporte_hbl = <paramref name="documentoTransporteHbl"/>.</summary>
        Task<List<LogStatusTrackingViewResultDto>> GetByDocumentoTransporteHblAsync(String documentoTransporteHbl, CancellationToken cancellationToken = default);

        /// <summary>Solo las columnas pedidas en <paramref name="fieldsSelection"/>, filtrando las filas por documento_transporte_hbl = <paramref name="documentoTransporteHbl"/>.</summary>
        Task<List<LogStatusTrackingViewResultDto>> GetByDocumentoTransporteHblAsync(String documentoTransporteHbl, LogStatusTrackingViewFieldsSelectionDto fieldsSelection, CancellationToken cancellationToken = default);
    }
}
