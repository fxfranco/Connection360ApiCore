using System;

namespace Connection360.Etl.Domain.Entities
{
    /// <summary>
    /// Fila a encolar en connection360write.outbox_messages (ver Documents/scriptoutboxmessagesSQL.sql).
    /// La construye Connection360.Etl.Application.UseCases.EtlChangeNotifier con <see cref="Payload"/>
    /// ya serializado a JSON (ver Connection360.Etl.Application.DTOs.EtlOutboxPayload, con su
    /// propiedad EventType); esta clase es
    /// puramente la forma de la fila, sin lógica de serialización -igual división de responsabilidad
    /// que <see cref="LogStatusTracking"/> frente a su mapeador-.
    /// </summary>
    public sealed class OutboxMessage
    {
        /// <summary>Identificador único del mensaje (columna UUID sin default en la base: lo genera la aplicación).</summary>
        public Guid Id { get; set; }

        /// <summary>Tipo de evento (ver <see cref="Connection360.Etl.Domain.Enums.EtlChangeEventType"/>).</summary>
        public String EventType { get; set; } = String.Empty;

        /// <summary>Contenido del evento en formato JSON, ya serializado.</summary>
        public String Payload { get; set; } = String.Empty;

        /// <summary>Momento (UTC) en que se encoló el mensaje.</summary>
        public DateTime CreatedAt { get; set; }
    }
}
