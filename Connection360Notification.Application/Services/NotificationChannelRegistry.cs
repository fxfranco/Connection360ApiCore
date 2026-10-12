using Connection360Notification.Application.Ports.Output;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Enums;

namespace Connection360Notification.Application.Services
{
    /// <summary>
    /// Registro de canales: indexa por <see cref="NotificationChannelType"/> todas las implementaciones de
    /// <see cref="INotificationChannel"/> que el contenedor de dependencias inyecta, de modo que agregar un
    /// canal nuevo no obliga a modificar esta clase (Open/Closed). La decisión de QUÉ canales usa cada
    /// usuario es regla de dominio (<see cref="NotificationChannelPreferences.GetEnabledChannels"/>).
    /// </summary>
    public sealed class NotificationChannelRegistry : INotificationChannelRegistry
    {
        private readonly Dictionary<NotificationChannelType, INotificationChannel> _channels = new();

        public NotificationChannelRegistry(IEnumerable<INotificationChannel> channels)
        {
            ArgumentNullException.ThrowIfNull(channels);

            foreach (INotificationChannel channel in channels)
            {
                // Dos implementaciones para el mismo canal serían una mala configuración: se falla rápido
                // en vez de enviar duplicado o elegir una "al azar".
                if (!_channels.TryAdd(channel.Channel, channel))
                    throw new InvalidOperationException($"Hay más de una implementación registrada para el canal de notificación '{channel.Channel}'.");
            }
        }

        public IReadOnlyList<INotificationChannel> Resolve(NotificationChannelPreferences? preferences)
        {
            IReadOnlyList<NotificationChannelType> enabled = (preferences ?? new NotificationChannelPreferences()).GetEnabledChannels();

            var resolved = new List<INotificationChannel>(enabled.Count);
            foreach (NotificationChannelType type in enabled)
            {
                if (_channels.TryGetValue(type, out INotificationChannel? channel))
                    resolved.Add(channel);
            }

            // Canal habilitado pero sin implementación registrada (p. ej. aún no se despliega): se cae al
            // canal por defecto para que la notificación nunca se pierda.
            if (resolved.Count == 0 && _channels.TryGetValue(NotificationChannelType.Application, out INotificationChannel? fallback))
                resolved.Add(fallback);

            return resolved;
        }
    }
}
