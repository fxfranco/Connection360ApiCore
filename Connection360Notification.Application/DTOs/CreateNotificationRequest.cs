namespace Connection360Notification.Application.DTOs
{
    /// <summary>
    /// Solicitud para generar una notificación. DocumentNumber, Title y MessageDate son opcionales
    /// para no romper a los consumidores actuales que solo envían Recipient/Content/Type; si no se
    /// envían, el caso de uso y la entidad de dominio aplican valores por defecto razonables.
    /// </summary>
    public sealed record CreateNotificationRequest(
        String Recipient,
        String Content,
        String Type,
        String? DocumentNumber = null,
        String? Title = null,
        DateTime? MessageDate = null);
}
