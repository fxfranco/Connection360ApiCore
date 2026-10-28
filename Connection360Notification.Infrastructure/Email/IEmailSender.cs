namespace Connection360Notification.Infrastructure.Email
{
    /// <summary>
    /// Envía un correo por SMTP. Separa la decisión de QUÉ enviar (EmailNotificationChannel: asunto, cuerpo,
    /// destinatario) del CÓMO se transporta (MailKitEmailSender), de modo que el canal se pruebe sin red y el
    /// proveedor se pueda cambiar sin tocarlo.
    /// </summary>
    public interface IEmailSender
    {
        /// <summary>Envía el correo. Lanza excepción si el servidor lo rechaza o no responde.</summary>
        Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
    }
}
