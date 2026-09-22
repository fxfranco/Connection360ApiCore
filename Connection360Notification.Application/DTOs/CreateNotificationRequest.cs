namespace Connection360Notification.Application.DTOs
{
    public record CreateNotificationRequest(String Recipient, String Content, String Type);
}
