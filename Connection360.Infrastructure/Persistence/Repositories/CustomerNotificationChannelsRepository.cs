using Connection360.Domain.Entities.Persistence;
using Connection360.Domain.Ports.Persistence;
using Dapper;

namespace Connection360.Infrastructure.Persistence.Repositories
{
    public class CustomerNotificationChannelsRepository : ICustomerNotificationChannelsRepository
    {
        private readonly DbSession _session;

        public CustomerNotificationChannelsRepository(DbSession session)
        {
            _session = session;
        }

        public async Task<IEnumerable<CustomerNotificationChannels>> ListAllAsync(CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);
            const string query = "SELECT id_notification_channel, id_customer, application, email, text_messages FROM connection360write.customer_notification_channels;";

            var command = new CommandDefinition(
                query,
                transaction: _session.Transaction,
                cancellationToken: cancellationToken
            );

            return await _session.Connection.QueryAsync<CustomerNotificationChannels>(command);
        }

        public async Task<CustomerNotificationChannels?> GetByIdAsync(Int64 id, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);
            const string query = "SELECT id_notification_channel, id_customer, application, email, text_messages FROM connection360write.customer_notification_channels WHERE id_notification_channel = @Id;";

            var command = new CommandDefinition(
                query,
                new { Id = id },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken
            );

            return await _session.Connection.QueryFirstOrDefaultAsync<CustomerNotificationChannels>(command);
        }

        public async Task<Int32> CrearAsync(CustomerNotificationChannels customerNotificationChannels, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);

            const string query = @"
            INSERT INTO connection360write.customer_notification_channels (id_customer, application, email, text_messages) 
            VALUES (@IdCustomer, @Application, @Email, @TextMessages) 
            RETURNING id_notification_channel;";

            var command = new CommandDefinition(
                query,
                 new
                 {
                     customerNotificationChannels.IdCustomer,
                     customerNotificationChannels.Application,
                     customerNotificationChannels.Email,
                     customerNotificationChannels.TextMessages

                 },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken
            );

            return await _session.Connection.ExecuteScalarAsync<Int32>(command);

        }
    }
}
