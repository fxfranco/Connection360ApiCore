namespace Connection360.Application.DTOs.Persistence
{
    /// <summary>
    /// Configuración completa de notificaciones de un cliente (canales + eventos). Es el cuerpo de
    /// la solicitud y de la respuesta de <c>SettingsController.GetNotificationsSettings</c>,
    /// <c>CreateNotificationsSettings</c> y <c>UpdateNotificationsSettings</c>.
    /// </summary>
    public class CustomerNotificationsSettingsResponse
    {
        /// <summary>Canales de notificación configurados para el cliente.</summary>
        public NotificationChannelsResponse? NotificationChannels { get; set; }

        /// <summary>Eventos de notificación configurados para el cliente.</summary>
        public NotificationEventsResponse? NotificationEvents { get; set; }
    }
}
