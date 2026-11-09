using Connection360Notification.Application.Ports.Output;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Enums;
using Connection360Notification.Domain.Time;
using Connection360Notification.Infrastructure.Adapters.Input;
using Microsoft.AspNetCore.SignalR;

namespace Connection360Notification.Infrastructure.Adapters.Output
{
    /// <summary>
    /// Canal de aplicación: entrega la notificación en tiempo real al usuario conectado por SignalR.
    /// Es la lógica que antes vivía en SignalRNotifierService.SendNotificationToUserAsync, trasladada
    /// sin cambios de comportamiento: mismo evento ("ReceiveNotification") y misma forma del mensaje
    /// ({ Message = Title, Data = notificación completa, Timestamp }), enrutado por el UserId de
    /// <see cref="CustomUserIdProvider"/> (el ClientId).
    /// </summary>
    public class SignalRNotificationChannel : INotificationChannel
    {
        /// <summary>Nombre del evento que escucha el frontend.</summary>
        public const String ReceiveNotificationEvent = "ReceiveNotification";

        private readonly IHubContext<NotificationHub> _hubContext;

        public SignalRNotificationChannel(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public NotificationChannelType Channel => NotificationChannelType.Application;

        public async Task SendNotificationToUserAsync(String idClient, String message, Object? data = null, CancellationToken cancellationToken = default)
        {
            // Envía EXCLUSIVAMENTE al usuario autenticado conectado con ese UserId
            await _hubContext.Clients.User(idClient).SendAsync(ReceiveNotificationEvent, new
            {
                Message = message,
                Data = data,
                Timestamp = ColombiaClock.Now
            }, cancellationToken);
        }

        public Task SendAsync(NotificationMessage notification, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(notification);

            return SendNotificationToUserAsync(notification.ClientId, notification.Title, notification, cancellationToken);
        }
    }
}
