using AppNotificationType = Connection360Notification.Application.Enum.NotificationType;
using AppNotificationStatus = Connection360Notification.Application.Enum.NotificationStatus;
using DomainNotificationType = Connection360Notification.Domain.Enums.NotificationType;
using DomainNotificationStatus = Connection360Notification.Domain.Enums.NotificationStatus;
using Connection360Notification.Application.DTOs;
using Connection360Notification.Domain;

namespace Connection360Notification.Application.Mapping
{
    /// <summary>
    /// Traduce entre los enums de contrato de la API (Connection360Notification.Application.Enum,
    /// usados en NotificationsListResponse/CreateNotificationRequest) y los enums de dominio
    /// (Connection360Notification.Domain.Enums, usados en NotificationMessage), y mapea la entidad
    /// de dominio a la respuesta pública de listado (NotificationsListResponse).
    ///
    /// Se usan alias de tipo (AppNotificationType/DomainNotificationType, etc.) en vez del nombre
    /// corto "NotificationType" a propósito: este archivo vive bajo el árbol de namespaces
    /// Connection360Notification.Application, donde también existe el namespace
    /// Connection360Notification.Application.Enum (en singular). La resolución de namespaces de C#
    /// prioriza esa cadena de namespaces externos sobre los "using" normales, así que un nombre
    /// corto como "Enum" o referencias ambiguas podrían resolver al namespace equivocado.
    /// </summary>
    public static class NotificationMappingExtensions
    {
        public static DomainNotificationType ToDomain(this AppNotificationType type) => type switch
        {
            AppNotificationType.ChangeState => DomainNotificationType.ChangeState,
            AppNotificationType.Comment => DomainNotificationType.Comment,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Tipo de notificación no soportado.")
        };

        public static AppNotificationType ToDto(this DomainNotificationType type) => type switch
        {
            DomainNotificationType.ChangeState => AppNotificationType.ChangeState,
            DomainNotificationType.Comment => AppNotificationType.Comment,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Tipo de notificación no soportado.")
        };

        public static DomainNotificationStatus ToDomain(this AppNotificationStatus status) => status switch
        {
            AppNotificationStatus.Unread => DomainNotificationStatus.Unread,
            AppNotificationStatus.Read => DomainNotificationStatus.Read,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Estado de notificación no soportado.")
        };

        public static AppNotificationStatus ToDto(this DomainNotificationStatus status) => status switch
        {
            DomainNotificationStatus.Unread => AppNotificationStatus.Unread,
            DomainNotificationStatus.Read => AppNotificationStatus.Read,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Estado de notificación no soportado.")
        };

        /// <summary>
        /// Mapea la entidad de dominio a la estructura pública de listado (NotificationsListResponse).
        /// </summary>
        public static NotificationsListResponse ToListResponse(this NotificationMessage notification)
        {
            return new NotificationsListResponse
            {
                Id = notification.Id,
                IdNotification = notification.IdNotification,
                ClientId = notification.ClientId,
                NotificationType = notification.Type.ToDto(),
                DocumentNumber = notification.DocumentNumber,
                Title = notification.Title,
                Message = notification.Message,
                MessageDate = notification.MessageDate,
                NotificationStatus = notification.Status.ToDto(),
                NotificationDate = notification.NotificationDate
            };
        }
    }
}
