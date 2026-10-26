using Connection360Notification.Application.Ports.Output;
using Connection360Notification.Application.Services;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Enums;
using Connection360Notification.Infrastructure.Sms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Connection360Notification.Infrastructure.Adapters.Output
{
    /// <summary>
    /// Canal de mensaje de texto (SMS). Compone un texto corto según el tipo de notificación
    /// (<see cref="ShipmentStatusSmsTemplate"/>) y lo entrega con <see cref="ISmsSender"/> (hoy Brevo, configurado en la
    /// sección "BrevoSettingsSMS").
    ///
    /// DESTINATARIO: ProcessIncomingNotificationUseCase deja en <see cref="INotificationRecipientContext"/> los datos del
    /// cliente (colección basicUserData, donde Document = ClientId). El SMS va al <see cref="BasicUserData.Phone"/> del
    /// cliente, normalizado a formato internacional (<see cref="SmsPhoneNumber"/>). A diferencia del correo no hay un
    /// número de respaldo: si el cliente no existe, no tiene celular o este no es válido, se registra una advertencia
    /// (sin el número) y la notificación sigue por los demás canales.
    ///
    /// TEXTO: número de guía = <see cref="NotificationMessage.DocumentNumber"/>, nuevo estado =
    /// <see cref="NotificationMessage.NuevoEstado"/> y URL = <see cref="BrevoSmsSettings.Conexion360Url"/>.
    ///
    /// Si la configuración está incompleta o Brevo falla, lanza la excepción: el despachador la aísla, así que el resto
    /// de los canales (p. ej. la aplicación) se entrega igual.
    /// </summary>
    public class SmsNotificationChannel : INotificationChannel
    {
        private readonly ISmsSender _smsSender;
        private readonly BrevoSmsSettings _settings;
        private readonly INotificationRecipientContext _recipientContext;
        private readonly ILogger<SmsNotificationChannel> _logger;

        public SmsNotificationChannel(
            ISmsSender smsSender,
            IOptions<BrevoSmsSettings> settings,
            INotificationRecipientContext recipientContext,
            ILogger<SmsNotificationChannel> logger)
        {
            _smsSender = smsSender;
            _settings = settings.Value;
            _recipientContext = recipientContext;
            _logger = logger;
        }

        public NotificationChannelType Channel => NotificationChannelType.TextMessages;

        public async Task SendAsync(NotificationMessage notification, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(notification);

            EnsureConfigured();

            BasicUserData? recipient = _recipientContext.Recipient;
            String? phone = SmsPhoneNumber.Normalize(recipient?.Phone, _settings.DefaultCountryCode);
            if (phone == null)
            {
                // No se registra el número (dato personal), solo el motivo.
                _logger.LogWarning(
                    "[SMS] La notificación {NotificationId} del cliente {ClientId} no se envió: {Reason}.",
                    notification.Id, notification.ClientId,
                    recipient == null ? "el cliente no existe en basicUserData"
                    : String.IsNullOrWhiteSpace(recipient.Phone) ? "el cliente no tiene celular registrado"
                    : "el celular registrado no es válido");
                return;
            }

            String content = ShipmentStatusSmsTemplate.Render(
                notification.EventType, notification.DocumentNumber, notification.NuevoEstado, _settings.Conexion360Url);

            String? messageId = await _smsSender.SendAsync(new SmsMessage(phone, content), cancellationToken);

            // No se registra el celular ni el contenido (datos personales).
            _logger.LogInformation(
                "[SMS] Notificación {NotificationId} enviada para el cliente {ClientId} (messageId de Brevo: {MessageId}).",
                notification.Id, notification.ClientId, messageId ?? "-");
        }

        private void EnsureConfigured()
        {
            var faltantes = new List<String>();
            if (String.IsNullOrWhiteSpace(_settings.ApiKey)) faltantes.Add(nameof(BrevoSmsSettings.ApiKey));
            if (String.IsNullOrWhiteSpace(_settings.Sender)) faltantes.Add(nameof(BrevoSmsSettings.Sender));

            if (faltantes.Count > 0)
                throw new InvalidOperationException(
                    $"El canal de SMS no está configurado: falta(n) {String.Join(", ", faltantes)} en la sección '{BrevoSmsSettings.SectionName}'.");
        }
    }
}
