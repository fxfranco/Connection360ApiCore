using Connection360Notification.Application.Ports.Output;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Connection360Notification.Infrastructure.Adapters.Output
{
    /// <summary>
    /// Canal de mensaje de texto (SMS). IMPLEMENTACIÓN BÁSICA DE EJEMPLO: por ahora solo registra el envío
    /// que haría. Cuando se implemente de verdad solo hay que cambiar el cuerpo de <see cref="SendAsync"/>:
    /// <list type="number">
    /// <item><description>Obtener el celular del destinatario a partir de <see cref="NotificationMessage.ClientId"/> (el Payload no lo trae: hará falta un puerto de salida de directorio de contactos).</description></item>
    /// <item><description>Componer un texto corto (los SMS tienen límite de caracteres) con <see cref="NotificationMessage.Title"/>.</description></item>
    /// <item><description>Enviarlo con el proveedor elegido (Twilio, SNS, ...), idealmente detrás de otro puerto propio.</description></item>
    /// </list>
    /// Si el envío falla debe lanzar la excepción: el despachador la aísla y no afecta a los demás canales.
    /// </summary>
    public class SmsNotificationChannel : INotificationChannel
    {
        private readonly ILogger<SmsNotificationChannel> _logger;

        public SmsNotificationChannel(ILogger<SmsNotificationChannel> logger)
        {
            _logger = logger;
        }

        public NotificationChannelType Channel => NotificationChannelType.TextMessages;

        public Task SendAsync(NotificationMessage notification, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(notification);

            // No se registra el contenido del mensaje (puede tener datos del cliente).
            _logger.LogInformation(
                "[SMS] (simulado) Notificación {NotificationId} para el cliente {ClientId}. Texto: {Title}.",
                notification.Id, notification.ClientId, notification.Title);

            return Task.CompletedTask;
        }
    }
}
