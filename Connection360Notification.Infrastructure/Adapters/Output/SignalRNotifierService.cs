using Connection360Notification.Application.Ports.Output;
using Connection360Notification.Infrastructure.Adapters.Input;
using Microsoft.AspNetCore.SignalR;

namespace Connection360Notification.Infrastructure.Adapters.Output
{
    public class SignalRNotifierService : INotifierService
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public SignalRNotifierService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }
        public async Task SendNotificationToUserAsync(String idClient, String message, Object? data = null)
        {
            // Envía EXCLUSIVAMENTE al usuario autenticado conectado con ese UserId
            await _hubContext.Clients.User(idClient).SendAsync("ReceiveNotification", new
            {
                Message = message,
                Data = data,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
