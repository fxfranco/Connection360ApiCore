namespace Connection360.Application.Ports.Output
{
    public interface INotifierService
    {
        Task SendNotificationToUserAsync(String idClient, String message, Object? data = null);
    }
}
