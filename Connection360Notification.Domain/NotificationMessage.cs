namespace Connection360Notification.Domain
{
    public class NotificationMessage
    {
        public String Id { get; private set; } = Guid.NewGuid().ToString();
        public String Recipient { get; private set; }
        public String Content { get; private set; }
        public String Type { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        public NotificationMessage(String recipient, String content, String type)
        {
            Recipient = recipient;
            Content = content;
            Type = type;
        }
    }
}
