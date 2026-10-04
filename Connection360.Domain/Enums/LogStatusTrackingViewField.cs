namespace Connection360.Domain.Enums
{
    /// <summary>
    /// Identifica cada columna expuesta por la vista de solo lectura
    /// connection360read.vw_log_status_tracking (ver Documents/vw_log_status_tracking.sql), que
    /// lee connection360write.log_status_tracking (historial de cambios de estado de una
    /// operación). Se usa como "objeto parámetro" en
    /// <see cref="Connection360.Domain.Dtos.LogStatusTrackingViewFieldsSelectionDto"/> para pedirle
    /// a ILogStatusTrackingViewRepository que el SELECT incluya solo algunas columnas.
    /// </summary>
    public enum LogStatusTrackingViewField
    {
        Id,
        IdOperacion,
        DocumentoTransporteHbl,
        FechaCambio,
        UsuarioCambio,
        Mensaje,
        EstadoAnterior,
        NuevoEstado
    }
}
