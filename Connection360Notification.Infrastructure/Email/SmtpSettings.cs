namespace Connection360Notification.Infrastructure.Email
{
    /// <summary>
    /// Configuración del servidor SMTP (sección "SmtpSettings" del appsettings) usada por el canal de correo.
    /// Para Gmail: Server=smtp.gmail.com, Port=587 (STARTTLS) o 465 (SSL directo) y como contraseña una
    /// "contraseña de aplicación" de la cuenta (no la contraseña normal). La contraseña NO debe versionarse:
    /// usar user-secrets, variable de entorno (SmtpSettings__Password) o el gestor de secretos del despliegue.
    /// </summary>
    public sealed class SmtpSettings
    {
        public const String SectionName = "SmtpSettings";

        /// <summary>Servidor SMTP (p. ej. smtp.gmail.com).</summary>
        public String Server { get; set; } = String.Empty;

        /// <summary>Puerto: 587 usa STARTTLS; 465 usa SSL desde el inicio de la conexión.</summary>
        public Int32 Port { get; set; } = 587;

        /// <summary>Nombre que ve el destinatario como remitente.</summary>
        public String SenderName { get; set; } = String.Empty;

        /// <summary>Correo remitente (en Gmail también es el usuario de autenticación).</summary>
        public String SenderEmail { get; set; } = String.Empty;

        /// <summary>Contraseña de aplicación de la cuenta remitente.</summary>
        public String Password { get; set; } = String.Empty;

        /// <summary>
        /// Usuario de autenticación, solo si es distinto de <see cref="SenderEmail"/>. Vacío = se usa SenderEmail.
        /// </summary>
        public String UserName { get; set; } = String.Empty;

        /// <summary>
        /// Correo destinatario FIJO de todas las notificaciones. Es temporal: mientras no exista un directorio con
        /// el correo de cada cliente (ClientId), el canal de correo envía siempre a esta dirección.
        /// </summary>
        public String RecipientEmail { get; set; } = String.Empty;

        /// <summary>Tiempo máximo, en segundos, para conectar/enviar. Evita bloquear el procesamiento si el servidor no responde.</summary>
        public Int32 TimeoutSeconds { get; set; } = 30;
    }
}
