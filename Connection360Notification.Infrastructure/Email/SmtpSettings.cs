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
        /// Correo destinatario de respaldo. El canal de correo envía al Email del cliente (colección basicUserData, donde
        /// Document = ClientId); solo cuando el cliente no existe, no tiene correo o este no es válido se usa esta dirección,
        /// para garantizar que la notificación siempre se envíe.
        /// </summary>
        public String RecipientEmail { get; set; } = String.Empty;

        /// <summary>
        /// URL de Conexion360 que se muestra como "Consultar mi envío" en el correo de cambio de estado (por ejemplo
        /// https://app.conexion360.com). Si está vacía o no es una URL http/https, el correo se envía sin enlace.
        /// </summary>
        public String Conexion360Url { get; set; } = String.Empty;

        /// <summary>Tiempo máximo, en segundos, para conectar/enviar. Evita bloquear el procesamiento si el servidor no responde.</summary>
        public Int32 TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Si es true (por defecto) se consulta la revocación (CRL/OCSP) del certificado del servidor SMTP. Ponerlo en
        /// false SOLO si el equipo no puede alcanzar los servidores de revocación (firewall, proxy, sin internet hacia
        /// esos hosts) y falla con "la función de revocación no puede comprobar la revocación". Con false se siguen
        /// validando la cadena de confianza, la vigencia y el nombre del servidor; solo se omite la consulta de revocación.
        /// </summary>
        public Boolean CheckCertificateRevocation { get; set; } = true;
    }
}
