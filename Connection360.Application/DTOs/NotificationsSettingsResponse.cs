using System.Text.Json.Serialization;

namespace Connection360.Application.DTOs
{
    public class NotificationsSettingsResponse
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public NotificationChannelsResponse? NotificationChannels {  get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public NotificationEventsResponse? NotificationEvents { get; set; }
    }
}
