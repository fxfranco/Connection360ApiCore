using System;

namespace Connection360.Etl.Application.DTOs
{
    /// <summary>
    /// Forma exacta del JSON que se serializa en connection360write.outbox_messages.payload. Los
    /// nombres de propiedad son el contrato del mensaje (lo consume, sin transformarlo,
    /// Connection360.Infrastructure.Messaging.OutboxPublisherWorker del proceso de la API principal,
    /// republicándolo tal cual a Kafka), así que deben coincidir exactamente con los solicitados:
    /// ClientId, EventType, DocumentNumber, Title, Message, MessageDate. No se configura ninguna
    /// política de nombres al serializar: System.Text.Json usa por defecto el PascalCase de las
    /// propiedades .NET, igual que el resto de la base de código (no hay JsonSerializerOptions con
    /// PropertyNamingPolicy configurado en ningún proyecto).
    /// </summary>
    public sealed class EtlOutboxPayload
    {
        public String ClientId { get; set; } = String.Empty;
        public String EventType { get; set; } = String.Empty;
        public String DocumentNumber { get; set; } = String.Empty;
        public String Title { get; set; } = String.Empty;
        public String Message { get; set; } = String.Empty;
        public DateTime MessageDate { get; set; }
    }
}
