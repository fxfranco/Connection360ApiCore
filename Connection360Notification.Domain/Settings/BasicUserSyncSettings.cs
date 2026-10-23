namespace Connection360Notification.Domain.Settings
{
    /// <summary>
    /// Configuración de la sincronización de usuarios que llega desde Connection360.Api por Kafka. Se mapea desde
    /// la sección "BasicUserSync" del appsettings de Connection360Notification.Api.
    /// </summary>
    public class BasicUserSyncSettings
    {
        /// <summary>Nombre de la sección donde está la configuración.</summary>
        public const String SectionName = "BasicUserSync";

        /// <summary>Tópico por defecto del que se consumen los usuarios (el mismo al que publica Connection360.Api).</summary>
        public const String DefaultTopic = "notification-users";

        /// <summary>Nombre por defecto de la colección de MongoDB donde se guardan los usuarios.</summary>
        public const String DefaultCollectionName = "basicUserData";

        /// <summary>Grupo de consumo por defecto si no se configura uno y tampoco hay <c>KafkaSettings:GroupId</c>.</summary>
        public const String DefaultGroupId = "connection360-notification-users";

        /// <summary>Espera por defecto, en milisegundos, tras un error de consumo (evita saturar el log si Kafka falla).</summary>
        public const Int32 DefaultErrorBackoffMilliseconds = 1000;

        /// <summary>Tópico de Kafka del que se consumen los usuarios.</summary>
        public String Topic { get; set; } = DefaultTopic;

        /// <summary>
        /// Grupo de consumo del tópico de usuarios. Vacío = se usa <c>KafkaSettings:GroupId</c> + "-users": debe ser
        /// distinto del grupo del tópico de notificaciones para que ambos consumidores trabajen de forma independiente.
        /// </summary>
        public String GroupId { get; set; } = String.Empty;

        /// <summary>Nombre de la colección de MongoDB donde se guardan los usuarios.</summary>
        public String CollectionName { get; set; } = DefaultCollectionName;

        /// <summary>Milisegundos de espera tras un error de consumo antes de volver a intentarlo.</summary>
        public Int32 ErrorBackoffMilliseconds { get; set; } = DefaultErrorBackoffMilliseconds;
    }
}
