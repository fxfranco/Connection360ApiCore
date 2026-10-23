namespace Connection360Notification.Application.Enum
{
    /// <summary>
    /// Tipo de evento que origina una notificación, expuesto en los contratos de la API. Equivale
    /// a Connection360Notification.Domain.Enums.NotificationType; el mapeo entre ambos se hace en
    /// Connection360Notification.Application.Mapping.NotificationMappingExtensions.
    /// </summary>
    public enum NotificationType
    {
        /// <summary>La notificación corresponde a un cambio de estado (por ejemplo, de un envío).</summary>
        ChangeState,

        /// <summary>La notificación corresponde a un comentario.</summary>
        Comment,

        /// <summary>La notificación corresponde a un cambio en la información general del envío (se agrega al final para no alterar los valores numéricos existentes).</summary>
        OtherInfo,
    }
}
