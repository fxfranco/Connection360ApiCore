using Connection360.Domain.Dtos;

namespace Connection360.Domain.Ports.Persistence
{
    /// <summary>
    /// Forma común de acceso de solo lectura a una vista de connection360write.application_data_sheet
    /// (vw_application_data_sheet_entregados / vw_application_data_sheet_no_entregados), implementada
    /// por IApplicationDataSheetEntregadosRepository e IApplicationDataSheetNoEntregadosRepository.
    /// Permite resolver cualquiera de las dos vistas de forma uniforme (ver
    /// Connection360.Application.Services.ApplicationDataSheetDataGateway).
    /// </summary>
    public interface IApplicationDataSheetViewRepository
    {
        /// <summary>Todas las columnas de todas las filas de la vista.</summary>
        Task<List<ApplicationDataSheetViewResultDto>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>Todas las filas de la vista, pero solo las columnas pedidas en <paramref name="fieldsSelection"/>.</summary>
        Task<List<ApplicationDataSheetViewResultDto>> GetAllAsync(ApplicationDataSheetViewFieldsSelectionDto fieldsSelection, CancellationToken cancellationToken = default);

        /// <summary>Todas las columnas, filtrando las filas por nit_cliente = <paramref name="nitCliente"/>.</summary>
        Task<List<ApplicationDataSheetViewResultDto>> GetByNitClienteAsync(String nitCliente, CancellationToken cancellationToken = default);

        /// <summary>Solo las columnas pedidas en <paramref name="fieldsSelection"/>, filtrando las filas por nit_cliente = <paramref name="nitCliente"/>.</summary>
        Task<List<ApplicationDataSheetViewResultDto>> GetByNitClienteAsync(String nitCliente, ApplicationDataSheetViewFieldsSelectionDto fieldsSelection, CancellationToken cancellationToken = default);
    }
}
