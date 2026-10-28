using System;

namespace Connection360.Etl.Domain.Entities
{
    /// <summary>
    /// Fila del historial de cambios de estado de una operación (log de trazabilidad/auditoría),
    /// destinada a connection360write.log_status_tracking (ver Documents/scriptlogsSQL.sql).
    /// Proceso ETL independiente del que carga connection360write.application_data_sheet: este
    /// solo consume la API externa "DATALOGS".
    /// </summary>
    public sealed class LogStatusTracking
    {
        /// <summary>
        /// Identificador tomado de ID_LOG (campo que ya usa Connection360.Domain.Services.
        /// DetailsHistoryShipmentsDomainService para ordenar el histórico por recencia): es el único
        /// campo numérico que expone hoy la API DATALOGS. Si "id_operacion" debe representar en
        /// cambio el identificador propio de la operación/envío (no del registro de log), ajustar
        /// este mapeo en LogStatusMappingService una vez se confirme cuál campo lo expone la API.
        /// </summary>
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
