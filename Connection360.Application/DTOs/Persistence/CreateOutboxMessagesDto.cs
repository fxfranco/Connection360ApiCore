namespace Connection360.Application.DTOs.Persistence
{
    /// <summary>
    /// Mensaje a encolar en el patrón Outbox para su posterior publicación (por ejemplo, a Kafka).
    /// Cuerpo de la solicitud de prueba <c>SettingsController.GeneratenotificationOutbox</c>.
    /// </summary>
    /// <param name="ClientId">Identificador del cliente destinatario del mensaje.</param>
    /// <param name="EventType">Tipo de evento de negocio que origina el mensaje.</param>
    /// <param name="DocumentNumber">Número de documento del envío relacionado, si aplica.</param>
    /// <param name="Title">Título corto del mensaje.</param>
    /// <param name="Message">Contenido del mensaje.</param>
    /// <param name="MessageDate">Fecha del mensaje; si no se especifica, se usa la fecha y hora actuales.</param>
    public record CreateOutboxMessagesDto(String ClientId, String EventType, String DocumentNumber, String Title, String Message, DateTime? MessageDate = null) 
    {
        /// <summary>Fecha del mensaje; si no se especifica al construir el registro, se usa la fecha y hora actuales.</summary>
        public DateTime? MessageDate { get; init; } = MessageDate ?? DateTime.Now;
    };
}
