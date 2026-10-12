using Connection360Notification.Application.Ports.Output;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Enums;
using Connection360Notification.Infrastructure.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Connection360Notification.Infrastructure.Adapters.Output
{
    /// <summary>
    /// Canal de correo electrónico. Arma el correo a partir de la notificación — asunto =
    /// <see cref="NotificationMessage.Title"/>, cuerpo = <see cref="NotificationMessage.Message"/> — y lo entrega
    /// con <see cref="IEmailSender"/> (MailKit/SMTP, configurado en la sección "SmtpSettings").
    ///
    /// DESTINATARIO TEMPORAL: el Payload solo trae el ClientId (no su correo) y aún no existe un directorio de
    /// contactos, así que por ahora se envía SIEMPRE a <see cref="SmtpSettings.RecipientEmail"/>. Cuando exista el
    /// directorio, basta con resolver aquí el correo del <see cref="NotificationMessage.ClientId"/> (idealmente tras
    /// un puerto de salida nuevo) en lugar de leerlo de la configuración.
    ///
    /// Si la configuración está incompleta o el servidor falla, lanza la excepción: el despachador la aísla, así que
    /// el resto de los canales (p. ej. la aplicación) se entrega igual.
    /// </summary>
    public class EmailNotificationChannel : INotificationChannel
    {
        /// <summary>Asunto usado cuando la notificación no trae título (un correo sin asunto suele caer en spam).</summary>
        public const String DefaultSubject = "Notificación";

        private readonly IEmailSender _emailSender;
        private readonly SmtpSettings _settings;
        private readonly ILogger<EmailNotificationChannel> _logger;

        public EmailNotificationChannel(IEmailSender emailSender, IOptions<SmtpSettings> settings, ILogger<EmailNotificationChannel> logger)
        {
            _emailSender = emailSender;
            _settings = settings.Value;
            _logger = logger;
        }

        public NotificationChannelType Channel => NotificationChannelType.Email;

        public async Task SendAsync(NotificationMessage notification, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(notification);
            EnsureConfigured();

            var email = new EmailMessage(
                To: _settings.RecipientEmail,
                Subject: String.IsNullOrWhiteSpace(notification.Title) ? DefaultSubject : notification.Title,
                Body: notification.Message);

            await _emailSender.SendAsync(email, cancellationToken);

            // No se registra el destinatario ni el contenido (datos personales).
            _logger.LogInformation(
                "Correo de la notificación {NotificationId} enviado para el cliente {ClientId}.",
                notification.Id, notification.ClientId);
        }

        private void EnsureConfigured()
        {
            var faltantes = new List<String>();
            if (String.IsNullOrWhiteSpace(_settings.Server)) faltantes.Add(nameof(SmtpSettings.Server));
            if (_settings.Port <= 0) faltantes.Add(nameof(SmtpSettings.Port));
            if (String.IsNullOrWhiteSpace(_settings.SenderEmail)) faltantes.Add(nameof(SmtpSettings.SenderEmail));
            if (String.IsNullOrWhiteSpace(_settings.Password)) faltantes.Add(nameof(SmtpSettings.Password));
            if (String.IsNullOrWhiteSpace(_settings.RecipientEmail)) faltantes.Add(nameof(SmtpSettings.RecipientEmail));

            if (faltantes.Count > 0)
                throw new InvalidOperationException(
                    $"El canal de correo no está configurado: falta(n) {String.Join(", ", faltantes)} en la sección '{SmtpSettings.SectionName}'.");
        }
    }
}
