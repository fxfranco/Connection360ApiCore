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

        public async Task<CustomerNotificationChannels?> GetByCustomerIdAsync(Int64 customerId, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);
            const string query = "SELECT id_notification_channel, id_customer, application, email, text_messages FROM connection360write.customer_notification_channels WHERE id_customer = @IdCustomer;";

            var command = new CommandDefinition(
                query,
                new { IdCustomer = customerId },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken
            );

            return await _session.Connection.QueryFirstOrDefaultAsync<CustomerNotificationChannels>(command);
        }

        public async Task<Int64> CrearAsync(CustomerNotificationChannels customerNotificationChannels, CancellationToken cancellationToken = default)
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

            return await _session.Connection.ExecuteScalarAsync<Int64>(command);

        }

        public async Task<Boolean> UpdateAsync(CustomerNotificationChannels customerNotificationChannels, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);

            const string query = @"
                UPDATE connection360write.customer_notification_channels
                SET
                    application     = @Application,
                    email           = @Email,
                    text_messages   = @TextMessages
                WHERE id_notification_channel = @IdNotificationChannel;";

            var command = new CommandDefinition(
                query,
                 new
                 {
                     customerNotificationChannels.Application,
                     customerNotificationChannels.Email,
                     customerNotificationChannels.TextMessages,
                     customerNotificationChannels.IdNotificationChannel
                 },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken
            );

            Int16 rowsAffected = (Int16)await _session.Connection.ExecuteAsync(command);
            return rowsAffected > 0;
        }


    }
}
