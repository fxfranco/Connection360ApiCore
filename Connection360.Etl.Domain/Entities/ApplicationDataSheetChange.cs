using System;

namespace Connection360.Etl.Domain.Entities
{
    /// <summary>
    /// Cambio detectado para un documento de transporte entre su valor anterior en
    /// connection360write.application_data_sheet y el que trajo la corrida actual de las APIs
    /// externas (ver Connection360.Etl.Domain.Interfaces.IApplicationDataSheetChangeDetector). Solo
    /// existe una instancia por documento cuando hubo AL MENOS un cambio relevante (estado,
    /// comentario/fecha de comentario y/o "otra información" -ver <see cref="OtherInfoChanged"/>-); si no hubo cambios, el documento no aparece en el resultado.
    /// </summary>
    public sealed class ApplicationDataSheetChange
    {
        /// <summary>Número de documento de transporte (HBL).</summary>
        public String DocumentoTransporteHbl { get; set; } = String.Empty;

        /// <summary>NIT del cliente, para el campo ClientId del payload de outbox_messages.</summary>
        public String NitCliente { get; set; } = String.Empty;

        /// <summary>
        /// Id (BIGSERIAL) de la fila del documento en connection360write.application_data_sheet,
        /// para log_status_tracking.id_operacion (ver ApplicationDataSheetChangeSnapshot.Id). Para
        /// un documento NUEVO (<see cref="IsNewDocument"/> true) este valor recién se conoce
        /// DESPUÉS del upsert de esa ronda (ver IApplicationDataSheetRepository.GetIdsByDocumentAsync)
        /// y se completa en el llamador (RunEtlProcessUseCase), no aquí.
        /// </summary>
        public Int64 IdOperacion { get; set; }

        /// <summary>
        /// true si el documento de transporte no existía previamente en
        /// connection360write.application_data_sheet (su aparición se trata igual que un cambio de
        /// ESTADO/COMENTARIO: pasa de "nada" a su valor actual). No se genera para la migración
        /// inicial: ahí no se detectan cambios en absoluto.
        /// </summary>
        public Boolean IsNewDocument { get; set; }

        /// <summary>true si el campo ESTADO cambió respecto al valor almacenado.</summary>
        public Boolean StateChanged { get; set; }

        /// <summary>Valor de ESTADO antes del cambio (el que había en la base de datos).</summary>
        public String EstadoAnterior { get; set; } = String.Empty;

        /// <summary>Valor de ESTADO después del cambio (el que trajo la API externa).</summary>
        public String NuevoEstado { get; set; } = String.Empty;

        /// <summary>true si COMENTARIO y/o FECHA COMENTARIO cambiaron respecto al valor almacenado.</summary>
        public Boolean CommentChanged { get; set; }

        /// <summary>
        /// true si cambió al menos uno de los campos de "otra información" del envío: TRANSPORTISTA,
        /// FECHA SOLICITUD ANTICIPO, VALOR ANTICIPO, FECHA PAGO ANTICIPO, FECHA BODEGA ORIGEN, FECHA ETD,
        /// FECHA ATD, FECHA ETA, FECHA ATA, FECHA BODEGA DESTINO, FECHA NACIONALIZACION, FECHA DESPACHO
        /// DESTINO, FECHA PLANILLA, FECHA ENTREGA CONTENEDOR, DIAS LIBRES, DIAS RESTANTES ENTREGA, FECHA
        /// DEVOLUCION REAL CONTENEDOR, DIAS DEMORA CONTENEDOR y VALOR TOTAL DEMORA. Un documento NUEVO
        /// no se marca por este motivo (su aparición ya se notifica por estado/comentario).
        /// </summary>
        public Boolean OtherInfoChanged { get; set; }

        /// <summary>Origen del envío (valor actual), para el payload de outbox_messages.</summary>
        public String Origen { get; set; } = String.Empty;

        /// <summary>Destino del envío (valor actual), para el payload de outbox_messages.</summary>
        public String Destino { get; set; } = String.Empty;

        /// <summary>
        /// Comentario del envío: si <see cref="CommentChanged"/> es true es el comentario NUEVO; si no,
        /// es el que ya estaba (que coincide con el actual, porque no cambió).
        /// </summary>
        public String Comentario { get; set; } = String.Empty;

        /// <summary>
        /// Fecha del comentario: la NUEVA si <see cref="CommentChanged"/> es true; si no, la que ya estaba
        /// (<see cref="DateTime.MinValue"/> cuando el envío no tiene fecha de comentario).
        /// </summary>
        public DateTime FechaComentario { get; set; }

        /// <summary>
        /// Usuario que realizó el cambio, según las APIs externas (campo "changeuser"). Vacío si no vino;
        /// en ese caso EtlChangeNotifier usa el usuario de sistema configurado en appsettings.
        /// </summary>
        public String ChangeUser { get; set; } = String.Empty;
    }
}
