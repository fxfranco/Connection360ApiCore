using Connection360Notification.Domain.Enums;

namespace Connection360Notification.Domain
{
    /// <summary>
    /// Canales de notificación habilitados para un cliente. Es la propiedad <c>NotificationChannel</c>
    /// del Payload que llega por Kafka (ver <see cref="NotificationMessage.NotificationChannel"/>):
    /// <c>{ "Application": bool, "Email": bool, "TextMessages": bool }</c>.
    ///
    /// Regla de negocio (vive aquí, en el dominio): si el cliente no tiene ningún canal habilitado
    /// (o no vino configuración), la notificación se envía SIEMPRE por el canal de aplicación.
    /// </summary>
    public sealed class NotificationChannelPreferences
    {
        /// <summary>Canal de aplicación (SignalR) habilitado.</summary>
        public Boolean Application { get; set; }

        /// <summary>Canal de correo electrónico habilitado.</summary>
        public Boolean Email { get; set; }

        /// <summary>Canal de mensaje de texto habilitado.</summary>
        public Boolean TextMessages { get; set; }

        /// <summary>true si al menos un canal está habilitado explícitamente.</summary>
        public Boolean HasAnyEnabled => Application || Email || TextMessages;

        /// <summary>Indica si el canal indicado está habilitado explícitamente (sin aplicar el canal por defecto).</summary>
        public Boolean IsEnabled(NotificationChannelType channel) => channel switch
        {
            NotificationChannelType.Application => Application,
            NotificationChannelType.Email => Email,
            NotificationChannelType.TextMessages => TextMessages,
            _ => false
        };

        /// <summary>
        /// Canales por los que debe enviarse la notificación: los habilitados o, si no hay ninguno,
        /// únicamente el canal de aplicación (canal por defecto). Nunca devuelve una lista vacía.
        /// </summary>
        public IReadOnlyList<NotificationChannelType> GetEnabledChannels()
        {
            if (!HasAnyEnabled)
                return new[] { NotificationChannelType.Application };

            var channels = new List<NotificationChannelType>(3);
            foreach (NotificationChannelType channel in Enum.GetValues<NotificationChannelType>())
            {
                if (IsEnabled(channel))
                    channels.Add(channel);
            }

            return channels;
        }
    }
}
