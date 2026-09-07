namespace Connection360.Application.DTOs.Persistence
{
    public class CustomerNotificationsSettingsResponse
    {
        public NotificationChannelsResponse? NotificationChannels { get; set; }
        public NotificationEventsResponse? NotificationEvents { get; set; }
    }
}
