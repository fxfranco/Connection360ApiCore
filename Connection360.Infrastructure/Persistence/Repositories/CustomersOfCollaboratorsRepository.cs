using Connection360.Domain.Dtos;
using Connection360.Domain.Entities.Persistence;
using Connection360.Domain.Ports.Persistence;
using Dapper;

namespace Connection360.Infrastructure.Persistence.Repositories
{
    public class CustomersOfCollaboratorsRepository : ICustomersOfCollaboratorsRepository
    {
        private readonly DbSession _session;

        public CustomersOfCollaboratorsRepository(DbSession session)
        {
            _session = session;
        }

        public async Task<Boolean> ExistsAsync(Int64 idCustomer, Int64 idCollaborator, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);
            const string query = @"
            SELECT EXISTS (
                SELECT 1 FROM connection360write.customers_of_collaborators
                WHERE id_customer = @IdCustomer AND id_collaborator = @IdCollaborator);";

            var command = new CommandDefinition(
                query,
                new { IdCustomer = idCustomer, IdCollaborator = idCollaborator },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken
            );

            return await _session.Connection.ExecuteScalarAsync<Boolean>(command);
        }

        public async Task<Int64> CrearAsync(Int64 idCustomer, Int64 idCollaborator, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);
            const string query = @"
            INSERT INTO connection360write.customers_of_collaborators (id_customer, id_collaborator) 
            VALUES (@IdCustomer, @IdCollaborator) 
            RETURNING id_customer_of_collaborator;";

            var command = new CommandDefinition(
                query,
                 new { IdCustomer = idCustomer, IdCollaborator = idCollaborator },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken
            );

            return await _session.Connection.ExecuteScalarAsync<Int64>(command);
        }

        public async Task<List<CustomersOfCollaboratorDtoResult>> ListCustomersByCollaboratorAsync(String idCollaborator, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);
            const string query = @"SELECT co.id_collaborator,co.identificacion AS identificacion_collaborator,cu.id_customer,
	            cu.identificacion AS identificacion_customer
            FROM CONNECTION360write.collaborators co
	            INNER JOIN CONNECTION360write.customers_of_collaborators cc ON co.id_collaborator = cc.id_collaborator
	            INNER JOIN connection360write.customers cu ON cc.id_customer=cu.id_customer
            WHERE co.identificacion = @IdCollaborator;";

            var command = new CommandDefinition(
                query,
                new { IdCollaborator = idCollaborator },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken
            );

            var result = await _session.Connection.QueryAsync<CustomersOfCollaboratorDtoResult>(command);
            return result.ToList();
        }

        public async Task<List<CustomerCollaboratorRelationDtoResult>> ListAllAsync(CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);
            const string query = @"
            SELECT cu.identificacion AS ""identificacionCustomer"",
                   co.identificacion AS ""identificacionCollaborator""
            FROM connection360write.customers_of_collaborators cc
                INNER JOIN connection360write.customers cu ON cc.id_customer = cu.id_customer
                INNER JOIN connection360write.collaborators co ON cc.id_collaborator = co.id_collaborator
            ORDER BY cu.identificacion, co.identificacion;";

            var command = new CommandDefinition(
                query,
                transaction: _session.Transaction,
                cancellationToken: cancellationToken
            );

            var result = await _session.Connection.QueryAsync<CustomerCollaboratorRelationDtoResult>(command);
            return result.ToList();
        }
    }
}
