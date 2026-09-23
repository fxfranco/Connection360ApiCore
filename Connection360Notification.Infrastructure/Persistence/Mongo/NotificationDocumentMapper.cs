using Connection360Notification.Domain;

namespace Connection360Notification.Infrastructure.Persistence.Mongo
{
    /// <summary>
    /// Único lugar que traduce entre la entidad de dominio (NotificationMessage) y el modelo de
    /// persistencia de Mongo (NotificationDocument).
    /// </summary>
    internal static class NotificationDocumentMapper
    {
        public static NotificationDocument ToDocument(this NotificationMessage notification)
        {
            return new NotificationDocument
            {
                Id = notification.Id,
                IdNotification = notification.IdNotification,
                ClientId = notification.ClientId,
                Type = notification.Type,
                DocumentNumber = notification.DocumentNumber,
                Title = notification.Title,
                Message = notification.Message,
                MessageDate = notification.MessageDate,
                Status = notification.Status,
                NotificationDate = notification.NotificationDate
            };
        }

        public static NotificationMessage ToDomain(this NotificationDocument document)
        {
            return new NotificationMessage(
                clientId: document.ClientId,
                type: document.Type,
                message: document.Message,
                documentNumber: document.DocumentNumber,
                title: document.Title,
                messageDate: document.MessageDate,
                status: document.Status,
                notificationDate: document.NotificationDate,
                idNotification: document.IdNotification,
                id: document.Id);
        }
    }
}
