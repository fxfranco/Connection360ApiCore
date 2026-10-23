using Connection360Notification.Application.Ports.Output;
using Connection360Notification.Application.Services;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Enums;
using Connection360Notification.Infrastructure.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Mail;

namespace Connection360Notification.Infrastructure.Adapters.Output
{
    /// <summary>
    /// Canal de correo electrónico. Arma el correo a partir de la notificación — asunto =
    /// <see cref="NotificationMessage.Title"/> — y lo entrega con <see cref="IEmailSender"/> (MailKit/SMTP, configurado
    /// en la sección "SmtpSettings").
    ///
    /// DESTINATARIO: ProcessIncomingNotificationUseCase deja en <see cref="INotificationRecipientContext"/> los datos del
    /// cliente (colección basicUserData, donde Document = ClientId). Si ese cliente tiene un Email válido, el correo se
    /// envía a esa dirección; si no existe, no tiene correo o no es válido, se usa <see cref="SmtpSettings.RecipientEmail"/>
    /// para garantizar que la notificación siempre se envíe.
    ///
    /// CUERPO: HTML con el formato de la plantilla de Conexion360 (<see cref="ShipmentStatusEmailTemplate"/>) y el logo
    /// incrustado. El texto plano alterno sigue siendo <see cref="NotificationMessage.Message"/>. Origen, destino, estados,
    /// comentario y fecha del comentario salen de la propia notificación (<see cref="NotificationMessage.Origen"/>,
    /// <see cref="NotificationMessage.Destino"/>, <see cref="NotificationMessage.EstadoAnterior"/>,
    /// <see cref="NotificationMessage.NuevoEstado"/>, <see cref="NotificationMessage.Comentario"/>,
    /// <see cref="NotificationMessage.FechaComentario"/>); según <see cref="NotificationMessage.EventType"/> la plantilla muestra
    /// el cambio de estado, el comentario o la actualización de información general.
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
        private readonly INotificationRecipientContext _recipientContext;
        private readonly ILogger<EmailNotificationChannel> _logger;

        public EmailNotificationChannel(
            IEmailSender emailSender,
            IOptions<SmtpSettings> settings,
            INotificationRecipientContext recipientContext,
            ILogger<EmailNotificationChannel> logger)
        {
            _emailSender = emailSender;
            _settings = settings.Value;
            _recipientContext = recipientContext;
            _logger = logger;
        }

        public NotificationChannelType Channel => NotificationChannelType.Email;

        public async Task SendAsync(NotificationMessage notification, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(notification);

            BasicUserData? recipient = _recipientContext.Recipient;
            String? clientEmail = ResolveClientEmail(recipient, notification);
            EnsureConfigured(requireFallbackRecipient: clientEmail == null);

            String html = ShipmentStatusEmailTemplate.Render(new ShipmentStatusEmailValues(
                ClientName: recipient?.FullName,
                DocumentNumber: notification.DocumentNumber,
                EventType: notification.EventType,
                Origin: notification.Origen,
                Destination: notification.Destino,
                PreviousStatus: notification.EstadoAnterior,
                NewStatus: notification.NuevoEstado,
                Comment: notification.Comentario,
                CommentDate: notification.FechaComentario,
                UpdatedAt: notification.MessageDate,
                Conexion360Url: _settings.Conexion360Url));

            var email = new EmailMessage(
                To: clientEmail ?? _settings.RecipientEmail,
                Subject: String.IsNullOrWhiteSpace(notification.Title) ? DefaultSubject : notification.Title,
                Body: notification.Message,
                HtmlBody: html,
                InlineImage: new EmailInlineImage(ShipmentStatusEmailTemplate.LogoContentId, ShipmentStatusEmailTemplate.GetLogo(), ShipmentStatusEmailTemplate.LogoMediaType));

            await _emailSender.SendAsync(email, cancellationToken);

            // No se registra el destinatario ni el contenido (datos personales); solo de dónde salió la dirección.
            _logger.LogInformation(
                "Correo de la notificación {NotificationId} enviado para el cliente {ClientId} (destinatario: {RecipientSource}).",
                notification.Id, notification.ClientId, clientEmail != null ? "correo del cliente" : "RecipientEmail de la configuración");
        }

        /// <summary>
        /// Correo del cliente si lo tiene y es una dirección válida; <c>null</c> si no hay destinatario, no tiene correo o
        /// no es válido (en este último caso se deja una advertencia, sin el valor, y se usa el correo de respaldo).
        /// </summary>
        private String? ResolveClientEmail(BasicUserData? recipient, NotificationMessage notification)
        {
            if (recipient == null || String.IsNullOrWhiteSpace(recipient.Email))
            {
                return null;
            }

            String candidate = recipient.Email.Trim();
            if (MailAddress.TryCreate(candidate, out MailAddress? address) && String.Equals(address.Address, candidate, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }

            _logger.LogWarning(
                "El correo registrado para el cliente {ClientId} no es válido; la notificación {NotificationId} se envía al correo de respaldo.",
                notification.ClientId, notification.Id);
            return null;
        }

        private void EnsureConfigured(Boolean requireFallbackRecipient)
        {
            var faltantes = new List<String>();
            if (String.IsNullOrWhiteSpace(_settings.Server)) faltantes.Add(nameof(SmtpSettings.Server));
            if (_settings.Port <= 0) faltantes.Add(nameof(SmtpSettings.Port));
            if (String.IsNullOrWhiteSpace(_settings.SenderEmail)) faltantes.Add(nameof(SmtpSettings.SenderEmail));
            if (String.IsNullOrWhiteSpace(_settings.Password)) faltantes.Add(nameof(SmtpSettings.Password));
            if (requireFallbackRecipient && String.IsNullOrWhiteSpace(_settings.RecipientEmail)) faltantes.Add(nameof(SmtpSettings.RecipientEmail));

            if (faltantes.Count > 0)
                throw new InvalidOperationException(
                    $"El canal de correo no está configurado: falta(n) {String.Join(", ", faltantes)} en la sección '{SmtpSettings.SectionName}'.");
        }
    }
}
