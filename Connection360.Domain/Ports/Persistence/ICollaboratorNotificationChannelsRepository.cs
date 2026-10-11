using Connection360.Domain.Entities.Persistence;

namespace Connection360.Domain.Ports.Persistence
{
    /// <summary>
    /// Puerto de persistencia de los canales de notificación de los colaboradores
    /// (<c>connection360write.collaborator_notification_channels</c>).
    /// </summary>
    public interface ICollaboratorNotificationChannelsRepository
    {
        Task<IEnumerable<CollaboratorNotificationChannels>> ListAllAsync(CancellationToken cancellationToken = default);
        Task<CollaboratorNotificationChannels?> GetByIdAsync(Int64 id, CancellationToken cancellationToken = default);
        Task<CollaboratorNotificationChannels?> GetByCollaboratorIdAsync(Int64 collaboratorId, CancellationToken cancellationToken = default);
        Task<Int64> CrearAsync(CollaboratorNotificationChannels collaboratorNotificationChannels, CancellationToken cancellationToken = default);
        Task<Boolean> UpdateAsync(CollaboratorNotificationChannels collaboratorNotificationChannels, CancellationToken cancellationToken = default);
    }
}
