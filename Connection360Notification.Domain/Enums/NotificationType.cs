namespace Connection360Notification.Domain.Enums
{
    /// <summary>
    /// Tipo de evento que origina la notificación. Vive en el dominio (y no solo en Application,
    /// donde ya existe un enum equivalente para el contrato de la API) porque es una regla de
    /// negocio del propio dominio, no un detalle de DTO. El mapeo entre ambos enums se hace de
    /// forma explícita en Connection360Notification.Application.Mapping.NotificationMappingExtensions.
    /// </summary>
    public enum NotificationType
    {
        ChangeState,
        Comment
    }
}
