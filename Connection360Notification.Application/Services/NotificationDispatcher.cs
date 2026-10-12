using Connection360Notification.Application.Ports.Output;
using Connection360Notification.Domain;
using System.Runtime.ExceptionServices;

namespace Connection360Notification.Application.Services
{
    /// <summary>
    /// Fan-out de la notificación hacia los canales habilitados del usuario. Los canales son independientes
    /// entre sí, así que se envían en paralelo y cada uno aísla sus fallos: si el correo falla, igual se
    /// entrega por la aplicación (y viceversa). El error se reporta al final para que el consumidor de
    /// mensajería lo registre como hasta ahora.
    /// </summary>
    public sealed class NotificationDispatcher : INotificationDispatcher
    {
        private readonly INotificationChannelRegistry _registry;

        public NotificationDispatcher(INotificationChannelRegistry registry)
        {
            _registry = registry;
        }

        public async Task DispatchAsync(NotificationMessage notification, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(notification);

            IReadOnlyList<INotificationChannel> channels = _registry.Resolve(notification.NotificationChannel);

            Exception?[] outcomes = await Task.WhenAll(channels.Select(channel => SendIsolatedAsync(channel, notification, cancellationToken)));

            List<Exception> failures = outcomes.OfType<Exception>().ToList();
            if (failures.Count == 1)
            {
                // Se conserva la excepción original (y su stack trace) cuando solo falló un canal.
                ExceptionDispatchInfo.Capture(failures[0]).Throw();
            }

            if (failures.Count > 1)
            {
                throw new AggregateException("Falló el envío de la notificación por más de un canal.", failures);
            }
        }

        private static async Task<Exception?> SendIsolatedAsync(INotificationChannel channel, NotificationMessage notification, CancellationToken cancellationToken)
        {
            try
            {
                await channel.SendAsync(notification, cancellationToken);
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}
