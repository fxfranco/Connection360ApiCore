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

        // ---- Campos de "otra información" (ver ApplicationDataSheetChange.OtherInfoChanged) ----
        // Las columnas fecha_* son DATE en Postgres: el SELECT las convierte con ::timestamp para que
        // Dapper/Npgsql las entreguen como DateTime (ver ApplicationDataSheetRepository). Un valor NULL
        // queda en el valor por defecto de la propiedad (DateTime.MinValue / 0 / vacío), igual que lo
        // que escribe el ETL cuando la API externa no trae el dato.
        public String Transportista { get; set; } = String.Empty;
        public DateTime FechaSolicitudAnticipo { get; set; }
        public Decimal ValorAnticipo { get; set; }
        public DateTime FechaPagoAnticipo { get; set; }
        public DateTime FechaBodegaOrigen { get; set; }
        public DateTime FechaEtd { get; set; }
        public DateTime FechaAtd { get; set; }
        public DateTime FechaEta { get; set; }
        public DateTime FechaAta { get; set; }
        public DateTime FechaBodegaDestino { get; set; }
        public DateTime FechaNacionalizacion { get; set; }
        public DateTime FechaDespachoDestino { get; set; }
        public DateTime FechaPlanilla { get; set; }
        public DateTime FechaEntregaContenedor { get; set; }
        public Int32 DiasLibres { get; set; }
        public Int32 DiasRestantesEntrega { get; set; }
        public DateTime? FechaDevolucionRealContenedor { get; set; }
        public Int32 DiasDemoraContenedor { get; set; }
        public Decimal ValorDiaDemora { get; set; }
        public Decimal ValorTotalDemora { get; set; }
    }
}
