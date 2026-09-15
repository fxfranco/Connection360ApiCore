using Connection360.Domain.Ports.Persistence;
using Dapper;

namespace Connection360.Infrastructure.Persistence.Repositories
{
    public class CollaboratorRepository : ICollaboratorRepository
    {
        private readonly DbSession _session;

        public CollaboratorRepository(DbSession session)
        {
            _session = session;
        }

        public async Task<Int64> CrearAsync(String id, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);
            const string query = @"
            INSERT INTO connection360write.collaborators (identificacion) 
            VALUES (@Id) 
            RETURNING id_collaborator;";

            var command = new CommandDefinition(
                query,
                 new { Id = id },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken
            );

            return await _session.Connection.ExecuteScalarAsync<Int64>(command);
        }

        public async Task<Int64?> GetCollaboratorByIdAsync(String id, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);
            const string query = @"SELECT id_collaborator FROM connection360write.collaborators WHERE identificacion = @Id;";

            var command = new CommandDefinition(
                query,
                new { Id = id },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken
            );

            return await _session.Connection.QueryFirstOrDefaultAsync<Int64>(command);
        }
    }
}
