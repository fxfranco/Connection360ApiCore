namespace Connection360Notification.Infrastructure.Sms
{
    /// <summary>
    /// Configuración del envío de SMS con Brevo (sección "BrevoSettingsSMS" del appsettings) usada por el canal de mensajes
    /// de texto. La API key NO debe versionarse: usar user-secrets, variable de entorno (BrevoSettingsSMS__ApiKey) o el
    /// gestor de secretos del despliegue.
    /// </summary>
    public sealed class BrevoSmsSettings
    {
        public const String SectionName = "BrevoSettingsSMS";

        /// <summary>URL base de la API de Brevo (por defecto la v3 pública).</summary>
        public const String DefaultBaseUrl = "https://api.brevo.com/v3/";

        /// <summary>Indicativo de país de Colombia, que se antepone a los celulares guardados sin indicativo.</summary>
        public const String DefaultCountryCodeColombia = "57";

        /// <summary>API key de Brevo (encabezado <c>api-key</c>).</summary>
        public String ApiKey { get; set; } = String.Empty;

        /// <summary>
        /// Remitente que ve el destinatario. Brevo admite hasta 11 caracteres si es alfanumérico (sin tildes ni espacios)
        /// o hasta 15 si es numérico; si excede ese límite Brevo responde 400 y el envío falla.
        /// </summary>
        public String Sender { get; set; } = String.Empty;

        /// <summary>
        /// URL de Conexion360 que se incluye en el texto del SMS ("Consulta los detalles y seguimiento en ..."). Si está
        /// vacía, el texto termina con el nombre "Conexión360" en lugar del enlace.
        /// </summary>
        public String Conexion360Url { get; set; } = String.Empty;

        /// <summary>
        /// Indicativo de país (solo dígitos, sin "+") que se antepone al celular cuando se guardó sin indicativo, p. ej.
        /// 3113232323 -> 573113232323. Un número que ya trae "+" o "00" se respeta tal cual.
        /// </summary>
        public String DefaultCountryCode { get; set; } = DefaultCountryCodeColombia;

        /// <summary>URL base de la API de Brevo; solo se cambia en pruebas o si Brevo publica otra versión.</summary>
        public String BaseUrl { get; set; } = DefaultBaseUrl;

        /// <summary>Tiempo máximo, en segundos, para esperar la respuesta de Brevo. Evita bloquear el procesamiento.</summary>
        public Int32 TimeoutSeconds { get; set; } = 30;
    }
}
