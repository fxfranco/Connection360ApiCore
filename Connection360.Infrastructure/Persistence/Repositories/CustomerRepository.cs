using Connection360.Domain.Ports.Persistence;
using Dapper;

namespace Connection360.Infrastructure.Persistence.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly DbSession _session;

        public CustomerRepository(DbSession session)
        {
            _session = session;
        }

        public async Task<Int64> CrearAsync(String id, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);
            const string query = @"
            INSERT INTO connection360write.customers (identificacion) 
            VALUES (@Id) 
            RETURNING id_customer;";

            var command = new CommandDefinition(
                query,
                 new { Id = id },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken
            );

            return await _session.Connection.ExecuteScalarAsync<Int64>(command);
        }

        public async Task<Int64?> GetCustomerByIdAsync(String id, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);
            const string query = @"SELECT id_customer FROM connection360write.customers WHERE identificacion = @Id;";

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
