using Connection360.Domain.Entities.Persistence;
using Connection360.Domain.Ports.Persistence;
using Dapper;

namespace Connection360.Infrastructure.Persistence.Repositories
{
    public class CustomerNotificationEventRepository : ICustomerNotificationEventRepository
    {
        private readonly DbSession _session;

        public CustomerNotificationEventRepository(DbSession session)
        {
            _session = session;
        }

        public async Task<Int32> CrearAsync(CustomerNotificationEvents CustomerNotificationEvents, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);
            const string query = @"
            INSERT INTO connection360write.customer_notification_events (id_customer, change_state, successful_delivery, with_issues, shipment_transit, delivery_reminder) 
            VALUES (@IdCustomer, @ChangeState, @SuccessfulDelivery, @WithIssues, @ShipmentTransit, @DeliveryReminder) 
            RETURNING id_notification_event;";

            var command = new CommandDefinition(
                query,
                 new
                 {
                     CustomerNotificationEvents.IdCustomer,
                     CustomerNotificationEvents.ChangeState,
                     CustomerNotificationEvents.SuccessfulDelivery,
                     CustomerNotificationEvents.WithIssues,
                     CustomerNotificationEvents.ShipmentTransit,
                     CustomerNotificationEvents.DeliveryReminder,
                 },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken
            );

            return await _session.Connection.ExecuteScalarAsync<Int32>(command);
        }

        public async Task<CustomerNotificationEvents?> GetByIdAsync(Int64 id, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);
            const string query = @"SELECT id_notification_event, id_customer, change_state, successful_delivery, with_issues, shipment_transit, delivery_reminder 
                FROM connection360write.customer_notification_events WHERE id_notification_event = @Id;";

            var command = new CommandDefinition(
                query,
                new { Id = id },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken
            );

            return await _session.Connection.QueryFirstOrDefaultAsync<CustomerNotificationEvents>(command);
        }

        public async Task<IEnumerable<CustomerNotificationEvents>> ListAllAsync(CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);
            const string query = @"SELECT id_notification_event, id_customer, change_state, successful_delivery, with_issues, shipment_transit, delivery_reminder
                FROM connection360write.customer_notification_events;";

            var command = new CommandDefinition(
                query,
                transaction: _session.Transaction,
                cancellationToken: cancellationToken
            );

            return await _session.Connection.QueryAsync<CustomerNotificationEvents>(command);
        }
    }
}
