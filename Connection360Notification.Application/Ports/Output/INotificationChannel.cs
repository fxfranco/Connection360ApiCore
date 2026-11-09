using Connection360Notification.Domain;
using Connection360Notification.Domain.Enums;

namespace Connection360Notification.Application.Ports.Output
{
    /// <summary>
    /// Puerto de salida y contrato común de la estrategia (Strategy) de envío de notificaciones:
    /// cada canal (aplicación/SignalR, correo, SMS, ...) encapsula aquí su lógica técnica de envío y
    /// Application nunca conoce los detalles de ninguno. Se entrega a UN usuario (el
    /// <see cref="NotificationMessage.ClientId"/>) por cada notificación.
    ///
    /// Para agregar un canal nuevo basta con implementar esta interfaz en Infrastructure, devolver
    /// su <see cref="NotificationChannelType"/> en <see cref="Channel"/> y registrarla en DI:
    /// el registro de canales (<see cref="Services.INotificationChannelRegistry"/>) la descubre solo.
    /// </summary>
    public interface INotificationChannel
    {
        /// <summary>Canal que implementa esta estrategia (clave con la que el registro la resuelve).</summary>
        NotificationChannelType Channel { get; }

        /// <summary>Envía la notificación al usuario destinatario (<see cref="NotificationMessage.ClientId"/>) por este canal.</summary>
        Task SendAsync(NotificationMessage notification, CancellationToken cancellationToken);
    }
}
