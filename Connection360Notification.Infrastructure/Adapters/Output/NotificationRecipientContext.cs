using Connection360Notification.Application.Services;
using Connection360Notification.Domain;

namespace Connection360Notification.Infrastructure.Adapters.Output
{
    /// <summary>
    /// Implementación en memoria de <see cref="INotificationRecipientContext"/>. Se registra como "scoped": existe una
    /// instancia por mensaje procesado y nunca se comparte entre notificaciones distintas.
    /// </summary>
    public sealed class NotificationRecipientContext : INotificationRecipientContext
    {
        public BasicUserData? Recipient { get; private set; }

        public void Set(BasicUserData? recipient)
        {
            Recipient = recipient;
        }
    }
}
