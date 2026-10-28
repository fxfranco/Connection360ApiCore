using Connection360Notification.Application.DTOs;

namespace Connection360Notification.Application.Ports.Inbound
{
    /// <summary>
    /// Puerto de entrada que guarda en <c>basicUserData</c> un lote de usuarios sincronizado desde
    /// Connection360.Api (llega por Kafka, a través de BasicUserDataConsumerHostedService).
    /// </summary>
    public interface ISyncBasicUsersUseCase
    {
        Task<BasicUsersSyncResult> ExecuteAsync(BasicUsersSyncMessage message, CancellationToken cancellationToken);
    }
}
