using Microsoft.AspNetCore.SignalR;

namespace Connection360.Infrastructure.Adapters.Input
{
    public class CustomUserIdProvider : IUserIdProvider
    {
        public String? GetUserId(HubConnectionContext connection)
        {
            // Lee la variable 'userId' enviada en la Query String desde el frontend
            var httpContext = connection.GetHttpContext();
            var idClient = httpContext?.Request.Query["idClient"].ToString();

            return !String.IsNullOrEmpty(idClient) ? idClient : null;
        }
    }
}
