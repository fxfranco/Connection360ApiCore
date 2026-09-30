using System;

namespace Connection360.Etl.Domain.Entities
{
    /// <summary>
    /// Proyección liviana de connection360write.application_data_sheet usada únicamente para
    /// comparar el valor "antes" contra el que traen las APIs externas en la corrida actual (ver
    /// Connection360.Etl.Domain.Interfaces.IApplicationDataSheetChangeDetector). Trae solo las
    /// columnas relevantes -no la fila completa, de más de 50 columnas- para minimizar el costo de
    /// la consulta que se ejecuta en cada página/ronda del proceso ETL.
    /// </summary>
    public sealed class ApplicationDataSheetChangeSnapshot
    {
        /// <summary>
        /// Id (BIGSERIAL) de la fila en connection360write.application_data_sheet. No cambia con
        /// el upsert (el ON CONFLICT DO UPDATE nunca toca la columna "id"), así que el valor leído
        /// aquí -ANTES del upsert- sigue siendo válido después: se usa como
        /// ApplicationDataSheetChange.IdOperacion (log_status_tracking.id_operacion).
        /// </summary>
        public Int64 Id { get; set; }

        public String DocumentoTransporteHbl { get; set; } = String.Empty;
        public String Estado { get; set; } = String.Empty;
        public String Comentario { get; set; } = String.Empty;
        public DateTime FechaComentario { get; set; }
    }
}
