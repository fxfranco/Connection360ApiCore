using Connection360Notification.Application.Ports.Inbound;
using Connection360Notification.Application.Services;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Enums;
using Connection360Notification.Domain.Ports.Outbound;
using Microsoft.Extensions.Logging;
using System.Runtime.ExceptionServices;

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
    /// ALERTAS (<see cref="NotificationType.Alert"/>): no pertenecen a ningún cliente (no traen ClientId), así que el destinatario
    /// no se busca por documento: se consultan en basicUserData los usuarios con rol <see cref="BasicUserRoles.Admin"/> ANTES de
    /// guardar, y por cada administrador se crea una copia de la alerta con su Document como ClientId. Cada copia se guarda como un
    /// registro propio y se despacha dejando sus datos (FullName, Phone, Email) en el contexto del destinatario (así SignalR, que
    /// enruta por ClientId, le llega a su sesión). Si no hay ningún administrador (o la consulta falla) se guarda y se despacha la
    /// alerta original, con el ClientId vacío y sin destinatario: el canal de correo usa entonces el RecipientEmail de la
    /// configuración y los demás canales no tienen a quién escribir. El flujo de las demás notificaciones no cambia.
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

            if (notification.EventType == NotificationType.Alert)
            {
                await ProcessAlertAsync(notification, cancellationToken);
                return;
            }

            await _notificationRepository.SaveAsync(notification, cancellationToken);

            _recipientContext.Set(await FindRecipientAsync(notification, cancellationToken));

            await _notificationDispatcher.DispatchAsync(notification, cancellationToken);
        }

        /// <summary>
        /// Guarda y entrega una alerta. Primero se buscan los administradores y, por cada uno, se crea una copia de la alerta con su
        /// Document como ClientId: cada copia se guarda como un registro propio (antes de entregarla) y se despacha con los datos
        /// de ese administrador en el contexto. Si no hay ningún administrador (o la consulta falla) se guarda y despacha la alerta
        /// original, con el ClientId vacío. Un fallo con un administrador no impide a los demás: los errores se acumulan y se
        /// reportan al final (la excepción original si fue uno solo, o un AggregateException).
        /// </summary>
        private async Task ProcessAlertAsync(NotificationMessage alert, CancellationToken cancellationToken)
        {
            IReadOnlyList<BasicUserData> administrators = await FindAdministratorsAsync(alert, cancellationToken);

            if (administrators.Count == 0)
            {
                await _notificationRepository.SaveAsync(alert, cancellationToken);
                _recipientContext.Set(null);
                await _notificationDispatcher.DispatchAsync(alert, cancellationToken);
                return;
            }

            var failures = new List<Exception>();
            foreach (BasicUserData administrator in administrators)
            {
                try
                {
                    NotificationMessage copy = alert.CopyForClient(administrator.Document);

                    await _notificationRepository.SaveAsync(copy, cancellationToken);

                    _recipientContext.Set(administrator);
                    await _notificationDispatcher.DispatchAsync(copy, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failures.Add(ex);
                }
            }

            if (failures.Count == 1)
            {
                ExceptionDispatchInfo.Capture(failures[0]).Throw();
            }

            if (failures.Count > 1)
            {
                throw new AggregateException("Falló el registro o el envío de la alerta a más de un administrador.", failures);
            }
        }

        /// <summary>
        /// Usuarios con rol administrador. Si la consulta falla se registra y se devuelve una lista vacía (la alerta ya está
        /// guardada y se entrega al correo por defecto); solo la cancelación se propaga.
        /// </summary>
        private async Task<IReadOnlyList<BasicUserData>> FindAdministratorsAsync(NotificationMessage notification, CancellationToken cancellationToken)
        {
            try
            {
                return await _basicUserDataRepository.GetByRoleAsync(BasicUserRoles.Admin, cancellationToken)
                    ?? Array.Empty<BasicUserData>();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex,
                    "No se pudieron consultar los administradores para la alerta {NotificationId}; se envía al correo por defecto.",
                    notification.Id);
                return Array.Empty<BasicUserData>();
            }
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
