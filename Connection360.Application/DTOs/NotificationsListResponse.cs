using Connection360.Application.Enum;

namespace Connection360.Application.DTOs
{
    public class NotificationsListResponse
    {
        public Int64 IdNotification { get; set; }
        public NotificationType NotificationType { get; set; }
        public String DocumentNumber { get; set; } = String.Empty;
        public String Title { get; set; } = String.Empty;
        public String Message { get; set; } = String.Empty;        
        public DateTime MessageDate { get; set; }
        public NotificationStatus NotificationStatus { get; set; }        
        public DateTime NotificationDate {  get; set; }
    }
}
