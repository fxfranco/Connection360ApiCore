using Connection360Notification.Domain;

namespace Connection360Notification.Application.Services
{
    /// <summary>Entrega una notificación al usuario destinatario por todos los canales que tenga habilitados.</summary>
    public interface INotificationDispatcher
    {
        /// <summary>
        /// Envía <paramref name="notification"/> por cada canal habilitado de su destinatario. Un canal que
        /// falla NO impide que se intenten los demás; al terminar, si alguno falló, se propaga el error
        /// (la excepción original si fue uno solo, o una <see cref="AggregateException"/> si fueron varios).
        /// </summary>
        Task DispatchAsync(NotificationMessage notification, CancellationToken cancellationToken);
    }
}
