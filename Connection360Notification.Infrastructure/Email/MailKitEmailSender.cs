using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Connection360Notification.Infrastructure.Email
{
    /// <summary>
    /// Implementación de <see cref="IEmailSender"/> con MailKit. Abre una conexión por envío (conectar, autenticar,
    /// enviar y cerrar): los correos son poco frecuentes y así no se mantienen conexiones SMTP inactivas que el
    /// servidor cierra. Si el mensaje trae <see cref="EmailMessage.HtmlBody"/> se envía como texto + HTML (con el logo
    /// incrustado por Content-ID); si no, solo como texto plano. El HTML ya llega codificado desde la plantilla.
    /// </summary>
    public sealed class MailKitEmailSender : IEmailSender
    {
        private const Int32 ImplicitSslPort = 465;

        private readonly SmtpSettings _settings;

        public MailKitEmailSender(IOptions<SmtpSettings> settings)
        {
            _settings = settings.Value;
        }

        /// <summary>Arma el mensaje MIME (remitente, destinatario, asunto y cuerpo) sin conectarse al servidor.</summary>
        public MimeMessage CreateMimeMessage(EmailMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);

            var mime = new MimeMessage();
            mime.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            mime.To.Add(MailboxAddress.Parse(message.To));
            mime.Subject = message.Subject;

            if (String.IsNullOrWhiteSpace(message.HtmlBody))
            {
                mime.Body = new TextPart("plain") { Text = message.Body };
                return mime;
            }

            var builder = new BodyBuilder { TextBody = message.Body, HtmlBody = message.HtmlBody };
            if (message.InlineImage != null)
            {
                MimeEntity image = builder.LinkedResources.Add(message.InlineImage.ContentId, message.InlineImage.Content, ContentType.Parse(message.InlineImage.MediaType));
                image.ContentId = message.InlineImage.ContentId;
            }

            mime.Body = builder.ToMessageBody();
            return mime;
        }

        public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(message);

            MimeMessage mime = CreateMimeMessage(message);

            using var client = new SmtpClient
            {
                Timeout = Math.Max(1, _settings.TimeoutSeconds) * 1000,
                CheckCertificateRevocation = _settings.CheckCertificateRevocation
            };

            // 465 = TLS desde el primer byte; cualquier otro puerto (587) = STARTTLS obligatorio.
            SecureSocketOptions security = _settings.Port == ImplicitSslPort
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            await client.ConnectAsync(_settings.Server, _settings.Port, security, cancellationToken);

            String userName = String.IsNullOrWhiteSpace(_settings.UserName) ? _settings.SenderEmail : _settings.UserName;
            await client.AuthenticateAsync(userName, _settings.Password, cancellationToken);

            await client.SendAsync(mime, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
    }
}
