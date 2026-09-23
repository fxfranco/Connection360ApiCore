using Connection360Notification.Application.Ports.Inbound;
using Connection360Notification.Application.Ports.Output;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Ports.Outbound;

namespace Connection360Notification.Application.UseCases
{
    /// <summary>
    /// Orquesta lo que debe pasar cuando llega una notificación nueva desde el broker de
    /// mensajería: primero se persiste (para no perderla si algo falla después) y luego se envía
    /// en tiempo real al cliente destino a través de INotifierService (SignalR hoy; cualquier otro
    /// canal en el futuro, sin que este caso de uso tenga que cambiar).
    ///
    /// El ClientId de la notificación de dominio es el identificador del destinatario en tiempo
    /// real (el mismo que CustomUserIdProvider usa para enrutar la conexión de SignalR), el Title
    /// se envía como el mensaje corto que ve el cliente, y se adjunta el objeto NotificationMessage
    /// completo como "data" adicional para que el frontend tenga todo el detalle sin necesitar una
    /// consulta aparte.
    /// </summary>
    public class ProcessIncomingNotificationUseCase : IProcessIncomingNotificationUseCase
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly INotifierService _notifierService;

        public ProcessIncomingNotificationUseCase(INotificationRepository notificationRepository, INotifierService notifierService)
        {
            _notificationRepository = notificationRepository;
            _notifierService = notifierService;
        }

        public async Task ExecuteAsync(NotificationMessage notification, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(notification);

            await _notificationRepository.SaveAsync(notification, cancellationToken);

            await _notifierService.SendNotificationToUserAsync(notification.ClientId, notification.Title, notification);
        }
    }
}
