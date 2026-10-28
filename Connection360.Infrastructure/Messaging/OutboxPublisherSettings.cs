namespace Connection360.Infrastructure.Messaging
{
    /// <summary>
    /// Configuración del worker <see cref="OutboxPublisherWorker"/>, que publica en Kafka los
    /// mensajes pendientes de la tabla outbox_messages. Se mapea desde la sección "OutboxPublisher"
    /// del appsettings de Connection360.Api y se resuelve con
    /// <see cref="Microsoft.Extensions.Options.IOptionsMonitor{TOptions}"/> (no con IOptions) para
    /// que el intervalo de sondeo se pueda cambiar en caliente, editando el appsettings, sin
    /// recompilar ni reiniciar la aplicación: el worker relee este valor en cada ciclo de ejecución.
    /// </summary>
    public class OutboxPublisherSettings
    {
        /// <summary>Nombre de la sección donde está la configuración.</summary>
        public const String SectionName = "OutboxPublisher";

        /// <summary>Valor por defecto, en segundos, usado si la configuración no define uno válido (&lt;= 0).</summary>
        public const Int16 DefaultPollingIntervalSeconds = 10;

        /// <summary>
        /// Intervalo, en segundos, con el que el worker consulta la tabla outbox_messages en busca
        /// de mensajes pendientes por publicar en Kafka.
        /// </summary>
        public Int16 PollingIntervalSeconds { get; set; } = DefaultPollingIntervalSeconds;
    }
}
