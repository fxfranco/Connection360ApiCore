using Connection360.Domain.Entities.Persistence;
using Connection360.Domain.Ports.Persistence;
using Dapper;

namespace Connection360.Infrastructure.Persistence.Repositories
{
    public class CollaboratorNotificationChannelsRepository : ICollaboratorNotificationChannelsRepository
    {
        private readonly DbSession _session;

        public CollaboratorNotificationChannelsRepository(DbSession session)
        {
            _session = session;
        }

        public async Task<IEnumerable<CollaboratorNotificationChannels>> ListAllAsync(CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);
            const string query = "SELECT id_notification_channel, id_collaborator, application, email, text_messages FROM connection360write.collaborator_notification_channels;";

            var command = new CommandDefinition(
                query,
                transaction: _session.Transaction,
                cancellationToken: cancellationToken
            );

            return await _session.Connection.QueryAsync<CollaboratorNotificationChannels>(command);
        }

        public async Task<CollaboratorNotificationChannels?> GetByIdAsync(Int64 id, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);
            const string query = "SELECT id_notification_channel, id_collaborator, application, email, text_messages FROM connection360write.collaborator_notification_channels WHERE id_notification_channel = @Id;";

            var command = new CommandDefinition(
                query,
                new { Id = id },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken
            );

            return await _session.Connection.QueryFirstOrDefaultAsync<CollaboratorNotificationChannels>(command);
        }

        public async Task<CollaboratorNotificationChannels?> GetByCollaboratorIdAsync(Int64 collaboratorId, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);
            const string query = "SELECT id_notification_channel, id_collaborator, application, email, text_messages FROM connection360write.collaborator_notification_channels WHERE id_collaborator = @IdCollaborator;";

            var command = new CommandDefinition(
                query,
                new { IdCollaborator = collaboratorId },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken
            );

            return await _session.Connection.QueryFirstOrDefaultAsync<CollaboratorNotificationChannels>(command);
        }

        public async Task<Int64> CrearAsync(CollaboratorNotificationChannels collaboratorNotificationChannels, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);

            const string query = @"
            INSERT INTO connection360write.collaborator_notification_channels (id_collaborator, application, email, text_messages) 
            VALUES (@IdCollaborator, @Application, @Email, @TextMessages) 
            RETURNING id_notification_channel;";

            var command = new CommandDefinition(
                query,
                new
                {
                    collaboratorNotificationChannels.IdCollaborator,
                    collaboratorNotificationChannels.Application,
                    collaboratorNotificationChannels.Email,
                    collaboratorNotificationChannels.TextMessages
                },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken
            );

            return await _session.Connection.ExecuteScalarAsync<Int64>(command);
        }

        public async Task<Boolean> UpdateAsync(CollaboratorNotificationChannels collaboratorNotificationChannels, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);

            // Se filtra también por id_collaborator para que un colaborador no pueda modificar el registro de otro.
            const string query = @"
                UPDATE connection360write.collaborator_notification_channels
                SET
                    application     = @Application,
                    email           = @Email,
                    text_messages   = @TextMessages
                WHERE id_notification_channel = @IdNotificationChannel
                  AND id_collaborator = @IdCollaborator;";

            var command = new CommandDefinition(
                query,
                new
                {
                    collaboratorNotificationChannels.Application,
                    collaboratorNotificationChannels.Email,
                    collaboratorNotificationChannels.TextMessages,
                    collaboratorNotificationChannels.IdNotificationChannel,
                    collaboratorNotificationChannels.IdCollaborator
                },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken
            );

            Int32 rowsAffected = await _session.Connection.ExecuteAsync(command);
            return rowsAffected > 0;
        }
    }
}
