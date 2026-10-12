using Connection360Notification.Domain;

namespace Connection360Notification.Infrastructure.Persistence.Mongo
{
    /// <summary>
    /// Modelo de persistencia (MongoDB) de <see cref="NotificationChannelPreferences"/>, anidado en
    /// <see cref="NotificationDocument.NotificationChannel"/>. Separado del dominio por la misma razón que
    /// NotificationDocument: Domain no conoce el modelo de persistencia.
    /// </summary>
    public sealed class NotificationChannelDocument
    {
        public Boolean Application { get; set; }

        public Boolean Email { get; set; }

        public Boolean TextMessages { get; set; }

        public static NotificationChannelDocument FromDomain(NotificationChannelPreferences? preferences) => new()
        {
            Application = preferences?.Application ?? false,
            Email = preferences?.Email ?? false,
            TextMessages = preferences?.TextMessages ?? false
        };

        public NotificationChannelPreferences ToDomain() => new()
        {
            Application = Application,
            Email = Email,
            TextMessages = TextMessages
        };
    }
}
