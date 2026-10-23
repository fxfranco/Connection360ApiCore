using Connection360Notification.Application.Ports.Inbound;
using Connection360Notification.Application.Services;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Ports.Outbound;
using Microsoft.Extensions.Logging;

namespace Connection360Notification.Application.UseCases
{
    /// <summary>
    /// Orquesta lo que debe pasar cuando llega una notificación nueva desde el broker de
    /// mensajería: primero se persiste (para no perderla si algo falla después) y luego se entrega
    /// al usuario destino (ClientId) por los canales que tenga habilitados (aplicación/SignalR,
    /// correo, SMS), a través de INotificationDispatcher. Si el usuario no tiene ningún canal
    /// habilitado se envía por el canal de aplicación. Agregar canales no obliga a cambiar este caso de uso.
    ///
    /// Entre guardar y despachar, consulta la colección basicUserData por el ClientId (que equivale al campo Document)
    /// y deja los datos del destinatario (Document, FullName, Phone, Email) en INotificationRecipientContext para que los
    /// canales los usen. Si no existe ese usuario, o la consulta falla, el destinatario queda en null y la notificación
    /// se entrega igual con los valores por defecto de cada canal.
    ///
    /// El Title es el mensaje corto que ve el cliente y el objeto NotificationMessage completo viaja
    /// como detalle para que el frontend no necesite una consulta aparte (ver SignalRNotificationChannel).
    /// </summary>
    public class ProcessIncomingNotificationUseCase : IProcessIncomingNotificationUseCase
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly INotificationDispatcher _notificationDispatcher;
        private readonly IBasicUserDataRepository _basicUserDataRepository;
        private readonly INotificationRecipientContext _recipientContext;
        private readonly ILogger<ProcessIncomingNotificationUseCase> _logger;

        public ProcessIncomingNotificationUseCase(
            INotificationRepository notificationRepository,
            INotificationDispatcher notificationDispatcher,
            IBasicUserDataRepository basicUserDataRepository,
            INotificationRecipientContext recipientContext,
            ILogger<ProcessIncomingNotificationUseCase> logger)
        {
            _notificationRepository = notificationRepository;
            _notificationDispatcher = notificationDispatcher;
            _basicUserDataRepository = basicUserDataRepository;
            _recipientContext = recipientContext;
            _logger = logger;
        }

        public async Task ExecuteAsync(NotificationMessage notification, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(notification);

            await _notificationRepository.SaveAsync(notification, cancellationToken);

            _recipientContext.Set(await FindRecipientAsync(notification, cancellationToken));

            await _notificationDispatcher.DispatchAsync(notification, cancellationToken);
        }

        /// <summary>
        /// Busca en basicUserData al usuario cuyo Document es el ClientId de la notificación. Devuelve null si no existe.
        /// Un fallo de la consulta no debe impedir la entrega (la notificación ya está guardada y la aplicación/SignalR no
        /// necesita estos datos), así que se registra y se continúa sin destinatario; solo la cancelación se propaga.
        /// </summary>
        private async Task<BasicUserData?> FindRecipientAsync(NotificationMessage notification, CancellationToken cancellationToken)
        {
            try
            {
                return await _basicUserDataRepository.GetByDocumentAsync(notification.ClientId, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex,
                    "No se pudieron consultar los datos del destinatario del cliente {ClientId} (notificación {NotificationId}); se continúa con los valores por defecto.",
                    notification.ClientId, notification.Id);
                return null;
            }
        }
    }
}
