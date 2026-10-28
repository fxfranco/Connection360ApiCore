using Connection360Notification.Application.Ports.Output;
using Connection360Notification.Domain;

namespace Connection360Notification.Application.Services
{
    /// <summary>
    /// Factory/Registry de canales: dada la configuración de canales de un usuario devuelve las
    /// estrategias (<see cref="INotificationChannel"/>) por las que hay que enviarle la notificación.
    /// </summary>
    public interface INotificationChannelRegistry
    {
        /// <summary>
        /// Resuelve los canales habilitados en <paramref name="preferences"/>. Si no hay ninguno habilitado
        /// (o ninguno de los habilitados tiene implementación registrada) devuelve únicamente el canal de
        /// aplicación, que es el canal por defecto.
        /// </summary>
        IReadOnlyList<INotificationChannel> Resolve(NotificationChannelPreferences? preferences);
    }
}
