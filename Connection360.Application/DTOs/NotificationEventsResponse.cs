namespace Connection360.Application.DTOs
{
    public class NotificationEventsResponse
    {
        public String ClientId { get; set; }
        public Int64 NotificationEventId { get; set; }
        public Boolean ChangeState { get; set; }
        public Boolean SuccessfulDelivery { get; set; }
        public Boolean WithIssues { get; set; }
        public Boolean ShipmentTransit { get; set; }
        public Boolean DeliveryReminder { get; set; }
    }
}
