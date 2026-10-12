using Connection360Notification.Application.Ports.Inbound;
using Connection360Notification.Application.Services;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Ports.Outbound;

namespace Connection360Notification.Application.UseCases
{
    /// <summary>
    /// Orquesta lo que debe pasar cuando llega una notificación nueva desde el broker de
    /// mensajería: primero se persiste (para no perderla si algo falla después) y luego se entrega
    /// al usuario destino (ClientId) por los canales que tenga habilitados (aplicación/SignalR,
    /// correo, SMS), a través de INotificationDispatcher. Si el usuario no tiene ningún canal
    /// habilitado se envía por el canal de aplicación. Agregar canales no obliga a cambiar este caso de uso.
    ///
    /// El Title es el mensaje corto que ve el cliente y el objeto NotificationMessage completo viaja
    /// como detalle para que el frontend no necesite una consulta aparte (ver SignalRNotificationChannel).
    /// </summary>
    public class ProcessIncomingNotificationUseCase : IProcessIncomingNotificationUseCase
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly INotificationDispatcher _notificationDispatcher;

        public ProcessIncomingNotificationUseCase(INotificationRepository notificationRepository, INotificationDispatcher notificationDispatcher)
        {
            _notificationRepository = notificationRepository;
            _notificationDispatcher = notificationDispatcher;
        }

        public async Task ExecuteAsync(NotificationMessage notification, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(notification);

            await _notificationRepository.SaveAsync(notification, cancellationToken);

            await _notificationDispatcher.DispatchAsync(notification, cancellationToken);
        }
    }
}
