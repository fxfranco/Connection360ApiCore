using Connection360Notification.Application.Enum;

namespace Connection360Notification.Application.DTOs
{
    public class NotificationsListResponse
    {
        public String Id { get; set; } = String.Empty;
        public Int64 IdNotification { get; set; }
        public String ClientId { get; set; } = String.Empty;
        public NotificationType NotificationType { get; set; }
        public String DocumentNumber { get; set; } = String.Empty;
        public String Title { get; set; } = String.Empty;
        public String Message { get; set; } = String.Empty;        
        public DateTime MessageDate { get; set; }
        public NotificationStatus NotificationStatus { get; set; }        
        public DateTime NotificationDate {  get; set; }
    }
}
