namespace Connection360.Infrastructure.Messaging
{
    /// <summary>
    /// Configuración del worker <see cref="Auth0UserSyncWorker"/>, que sincroniza los usuarios de Auth0
    /// hacia Connection360Notification.Api publicándolos en Kafka. Se mapea desde la sección
    /// "Auth0UserSync" del appsettings de Connection360.Api y se resuelve con
    /// <see cref="Microsoft.Extensions.Options.IOptionsMonitor{TOptions}"/> (igual que
    /// <see cref="OutboxPublisherSettings"/>) para poder cambiar la periodicidad en caliente.
    /// </summary>
    public class Auth0UserSyncSettings
    {
        /// <summary>Nombre de la sección donde está la configuración.</summary>
        public const String SectionName = "Auth0UserSync";

        /// <summary>Periodicidad por defecto, en minutos, si la configuración no define una válida (&lt;= 0).</summary>
        public const Int32 DefaultPollingIntervalMinutes = 60;

        /// <summary>Tópico por defecto al que se publican los usuarios.</summary>
        public const String DefaultTopic = "notification-users";

        /// <summary>Usuarios por mensaje de Kafka por defecto.</summary>
        public const Int32 DefaultBatchSize = 200;

        /// <summary>Espera por defecto, en segundos, entre reintentos de la sincronización inicial.</summary>
        public const Int32 DefaultStartupRetryDelaySeconds = 30;

        /// <summary>Si es <c>false</c> el worker no sincroniza (se puede activar en caliente).</summary>
        public Boolean Enabled { get; set; } = true;

        /// <summary>Tópico de Kafka al que se publican los usuarios (lo consume Connection360Notification.Api).</summary>
        public String Topic { get; set; } = DefaultTopic;

        /// <summary>
        /// Cada cuántos minutos se repite la sincronización. Además de esta periodicidad, siempre se
        /// ejecuta una vez al arrancar la aplicación.
        /// </summary>
        public Int32 PollingIntervalMinutes { get; set; } = DefaultPollingIntervalMinutes;

        /// <summary>Cuántos usuarios se envían por mensaje de Kafka (para no superar el tamaño máximo de mensaje).</summary>
        public Int32 BatchSize { get; set; } = DefaultBatchSize;

        /// <summary>
        /// Segundos de espera entre los reintentos de la sincronización inicial (máximo 3 intentos) cuando falla
        /// al arrancar, por ejemplo porque Auth0 o Kafka todavía no están disponibles.
        /// </summary>
        public Int32 StartupRetryDelaySeconds { get; set; } = DefaultStartupRetryDelaySeconds;
    }
}
