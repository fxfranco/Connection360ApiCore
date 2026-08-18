using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Connection360.Infrastructure.Adapters.Input
{
    [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
    //[AllowAnonymous]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            String idClient = Context.UserIdentifier; // Extraído automáticamente del claim 'sub' o NameIdentifier
            await base.OnConnectedAsync();
        }
    }
}
