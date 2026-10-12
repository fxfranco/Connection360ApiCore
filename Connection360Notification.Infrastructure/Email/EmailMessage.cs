namespace Connection360Notification.Infrastructure.Email
{
    /// <summary>Correo ya armado, independiente de la librería SMTP: destinatario, asunto y cuerpo en texto plano.</summary>
    public sealed record EmailMessage(String To, String Subject, String Body);
}
