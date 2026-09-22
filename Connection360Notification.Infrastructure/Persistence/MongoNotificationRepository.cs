using Connection360Notification.Domain;
using Connection360Notification.Domain.Ports.Outbound;
using Connection360Notification.Domain.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Connection360Notification.Infrastructure.Persistence
{
    public class MongoNotificationRepository : INotificationRepository
    {
        private readonly IMongoCollection<NotificationMessage> _collection;

        public MongoNotificationRepository(IOptions<MongoDbSettings> mongoSettings)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
            _collection = database.GetCollection<NotificationMessage>(mongoSettings.Value.CollectionName);
        }

        public async Task SaveAsync(NotificationMessage notification, CancellationToken cancellationToken)
            => await _collection.InsertOneAsync(notification, cancellationToken: cancellationToken);

        public async Task<IEnumerable<NotificationMessage>> GetAllAsync(CancellationToken cancellationToken)
            => await _collection.Find(_ => true).ToListAsync(cancellationToken);
    }
}
