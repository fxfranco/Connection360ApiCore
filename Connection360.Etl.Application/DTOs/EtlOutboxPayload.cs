using System;

namespace Connection360.Etl.Application.DTOs
{
    /// <summary>
    /// Forma exacta del JSON que se serializa en connection360write.outbox_messages.payload. Los
    /// nombres de propiedad son el contrato del mensaje (lo consume, sin transformarlo,
    /// Connection360.Infrastructure.Messaging.OutboxPublisherWorker del proceso de la API principal,
    /// republicándolo tal cual a Kafka), así que deben coincidir exactamente con los solicitados:
    /// ClientId, EventType, DocumentNumber, Title, Message, MessageDate, y los datos de contexto
    /// Origen, Destino, Comentario, FechaComentario, EstadoAnterior, NuevoEstado. No se configura ninguna
    /// política de nombres al serializar: System.Text.Json usa por defecto el PascalCase de las
    /// propiedades .NET, igual que el resto de la base de código (no hay JsonSerializerOptions con
    /// PropertyNamingPolicy configurado en ningún proyecto). <see cref="NotificationChannel"/> se
    /// agrega al final con los canales (Application, Email, TextMessages) del cliente.
    /// </summary>
    public sealed class EtlOutboxPayload
    {
        public String ClientId { get; set; } = String.Empty;
        public String EventType { get; set; } = String.Empty;
        public String DocumentNumber { get; set; } = String.Empty;
        public String Title { get; set; } = String.Empty;
        public String Message { get; set; } = String.Empty;
        public DateTime MessageDate { get; set; }

        /// <summary>Origen del envío (application_data_sheet.origen).</summary>
        public String Origen { get; set; } = String.Empty;

        /// <summary>Destino del envío (application_data_sheet.destino).</summary>
        public String Destino { get; set; } = String.Empty;

        /// <summary>
        /// Comentario vigente del documento: el nuevo si el cambio fue de comentario, o el que ya
        /// estaba en cualquier otro caso.
        /// </summary>
        public String Comentario { get; set; } = String.Empty;

        /// <summary>Fecha del comentario vigente (misma regla que <see cref="Comentario"/>).</summary>
        public DateTime FechaComentario { get; set; }

        /// <summary>Estado que tenía el documento antes de la corrida (vacío si es un documento nuevo).</summary>
        public String EstadoAnterior { get; set; } = String.Empty;

        /// <summary>Estado vigente del documento después de la corrida.</summary>
        public String NuevoEstado { get; set; } = String.Empty;

        /// <summary>Canales de notificación del cliente (ClientId); todos en false si no tiene configuración.</summary>
        public EtlNotificationChannelPayload NotificationChannel { get; set; } = new();
    }
}
