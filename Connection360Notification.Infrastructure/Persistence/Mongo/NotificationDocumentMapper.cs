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
                Type = notification.EventType,
                DocumentNumber = notification.DocumentNumber,
                Title = notification.Title,
                Message = notification.Message,
                // Mongo guarda DateTime en UTC y, si el Kind es Unspecified, lo interpreta con la zona de la
                // máquina. Las fechas del sistema son hora de Colombia (Unspecified): se marcan como Utc
                // para que se guarden tal cual, sin depender de la zona del servidor.
                MessageDate = DateTime.SpecifyKind(notification.MessageDate, DateTimeKind.Utc),
                Status = notification.Status,
                NotificationDate = DateTime.SpecifyKind(notification.NotificationDate, DateTimeKind.Utc),
                NotificationChannel = NotificationChannelDocument.FromDomain(notification.NotificationChannel)
            };
        }

        public static NotificationMessage ToDomain(this NotificationDocument document)
        {
            return new NotificationMessage(
                clientId: document.ClientId,
                eventType: document.Type,
                message: document.Message,
                documentNumber: document.DocumentNumber,
                title: document.Title,
                // Mongo devuelve Kind=Utc; se vuelve a Unspecified (hora de Colombia) para que, al serializar,
                // no se agregue la "Z" y el cliente no la desplace.
                messageDate: DateTime.SpecifyKind(document.MessageDate, DateTimeKind.Unspecified),
                status: document.Status,
                notificationDate: DateTime.SpecifyKind(document.NotificationDate, DateTimeKind.Unspecified),
                idNotification: document.IdNotification,
                id: document.Id,
                notificationChannel: document.NotificationChannel?.ToDomain());
        }
    }
}
