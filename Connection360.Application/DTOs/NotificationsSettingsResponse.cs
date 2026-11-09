using System.Text.Json.Serialization;

namespace Connection360.Application.DTOs
{
    /// <summary>
    /// Configuración de notificaciones de un cliente: qué canales usar y ante qué eventos
    /// notificar. Modelo interno de aplicación; el contrato expuesto por
    /// <c>SettingsController</c> usa el equivalente en <c>DTOs.Persistence.CustomerNotificationsSettingsResponse</c>.
    /// </summary>
    public class NotificationsSettingsResponse
    {
        /// <summary>Canales de notificación configurados. Se omite del JSON cuando es <c>null</c>.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public NotificationChannelsResponse? NotificationChannels {  get; set; }

        /// <summary>Eventos de notificación configurados. Se omite del JSON cuando es <c>null</c>.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public NotificationEventsResponse? NotificationEvents { get; set; }
    }
}
