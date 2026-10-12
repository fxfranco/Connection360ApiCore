using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Connection360Notification.Infrastructure.Email
{
    /// <summary>
    /// Implementación de <see cref="IEmailSender"/> con MailKit. Abre una conexión por envío (conectar, autenticar,
    /// enviar y cerrar): los correos son poco frecuentes y así no se mantienen conexiones SMTP inactivas que el
    /// servidor cierra. El cuerpo se envía como texto plano, por lo que el contenido de la notificación nunca se
    /// interpreta como HTML.
    /// </summary>
    public sealed class MailKitEmailSender : IEmailSender
    {
        private const Int32 ImplicitSslPort = 465;

        private readonly SmtpSettings _settings;

        public MailKitEmailSender(IOptions<SmtpSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(message);

            var mime = new MimeMessage();
            mime.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            mime.To.Add(MailboxAddress.Parse(message.To));
            mime.Subject = message.Subject;
            mime.Body = new TextPart("plain") { Text = message.Body };

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
