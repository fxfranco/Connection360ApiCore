namespace Connection360Notification.Domain.Settings
{
    public class MongoDbSettings
    {
        public String ConnectionString { get; set; } = String.Empty;
        public String DatabaseName { get; set; } = String.Empty;
        public String CollectionName { get; set; } = String.Empty;
    }
}
