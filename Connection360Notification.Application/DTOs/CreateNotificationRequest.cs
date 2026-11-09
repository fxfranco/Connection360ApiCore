namespace Connection360Notification.Application.DTOs
{
    /// <summary>
    /// Solicitud para generar una notificación. DocumentNumber, Title y MessageDate son opcionales
    /// para no romper a los consumidores actuales que solo envían Recipient/Content/Type; si no se
    /// envían, el caso de uso y la entidad de dominio aplican valores por defecto razonables.
    /// </summary>
    /// <param name="Recipient">Identificador del cliente destinatario de la notificación.</param>
    /// <param name="Content">Contenido/mensaje de la notificación.</param>
    /// <param name="Type">Tipo de evento que origina la notificación (por ejemplo, ChangeState o Comment).</param>
    /// <param name="DocumentNumber">Número de documento (envío/pedido, etc.) asociado, cuando aplica. Opcional.</param>
    /// <param name="Title">Título breve de la notificación. Opcional; si no se envía, la entidad de dominio aplica un valor por defecto.</param>
    /// <param name="MessageDate">Fecha y hora del evento de negocio que origina la notificación. Opcional; si no se envía, se usa la fecha/hora actual.</param>
    public sealed record CreateNotificationRequest(
        String Recipient,
        String Content,
        String Type,
        String? DocumentNumber = null,
        String? Title = null,
        DateTime? MessageDate = null);
}
