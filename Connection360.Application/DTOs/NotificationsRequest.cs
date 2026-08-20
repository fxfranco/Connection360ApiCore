namespace Connection360.Application.DTOs
{
    public class NotificationsRequest
    {
        public String IdClient { get; set; } = default!;
        public Int64 IdNotification { get ; set; }
        public String RoleName { get; set; } = default!;
    }
}
