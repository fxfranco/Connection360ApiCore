namespace Connection360.Domain.Dtos
{
    /// <summary>
    /// Fila de la vista de solo lectura connection360read.vw_log_status_tracking (ver
    /// Documents/vw_log_status_tracking.sql): un cambio de estado de una operación (log de
    /// trazabilidad/auditoría). Cuando se piden solo algunos campos (ver
    /// <see cref="LogStatusTrackingViewFieldsSelectionDto"/>), las propiedades no seleccionadas
    /// quedan con su valor por defecto.
    /// </summary>
    public class LogStatusTrackingViewResultDto
    {
        /// <summary>Identificador propio del registro de log (clave primaria de la tabla).</summary>
        public Int64 Id { get; set; }
        /// <summary>Identificador de la operación asociada al cambio (hoy se carga desde ID_LOG de la API DATALOGS; ver Connection360.Etl.Domain.Entities.LogStatusTracking).</summary>
        public Int64 IdOperacion { get; set; }
        /// <summary>Número de documento de transporte (HBL) de la operación asociada al cambio.</summary>
        public String DocumentoTransporteHbl { get; set; } = String.Empty;
        /// <summary>Fecha y hora en que se registró el cambio de estado.</summary>
        public DateTime FechaCambio { get; set; }
        /// <summary>Usuario que realizó el cambio.</summary>
        public String UsuarioCambio { get; set; } = String.Empty;
        /// <summary>Mensaje/descripción asociado al cambio.</summary>
        public String Mensaje { get; set; } = String.Empty;
        /// <summary>Estado de la operación antes del cambio.</summary>
        public String EstadoAnterior { get; set; } = String.Empty;
        /// <summary>Estado de la operación después del cambio.</summary>
        public String NuevoEstado { get; set; } = String.Empty;
    }
}
