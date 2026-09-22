using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Connection360Notification.Infrastructure.Adapters.Input
{
    [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            String idClient = Context.UserIdentifier; // Extraído automáticamente del claim 'sub' o NameIdentifier
            await base.OnConnectedAsync();
        }
    }
}
