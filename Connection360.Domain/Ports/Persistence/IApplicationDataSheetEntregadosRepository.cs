namespace Connection360.Domain.Ports.Persistence
{
    /// <summary>
    /// Acceso de solo lectura a connection360read.vw_application_data_sheet_entregados (filas de
    /// connection360write.application_data_sheet con estado = 'Entregado'; ver
    /// Documents/vw_application_data_sheet_entregados.sql). Los 4 métodos de consulta los define
    /// IApplicationDataSheetViewRepository, compartida con IApplicationDataSheetNoEntregadosRepository.
    /// </summary>
    public interface IApplicationDataSheetEntregadosRepository : IApplicationDataSheetViewRepository
    {
    }
}
