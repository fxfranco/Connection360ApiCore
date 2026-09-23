using Connection360Notification.Domain;
using Connection360Notification.Domain.Enums;
using Connection360Notification.Domain.Ports.Outbound;
using Connection360Notification.Domain.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Connection360Notification.Infrastructure.Persistence.Mongo
{
    /// <summary>
    /// Adaptador de salida (hexagonal) de INotificationRepository para MongoDB. Traduce entre la
    /// entidad de dominio (NotificationMessage) y el modelo de persistencia (NotificationDocument) a
    /// través de NotificationDocumentMapper, y obtiene su colección desde IMongoDbContext: nunca crea
    /// su propio MongoClient, así que todas las instancias (aunque el repositorio esté registrado
    /// como Scoped) reutilizan la única conexión/pool compartido de todo el proceso.
    /// </summary>
    public sealed class MongoNotificationRepository : INotificationRepository
    {
        private readonly IMongoCollection<NotificationDocument> _collection;
        private readonly INotificationIdGenerator _idGenerator;

        public MongoNotificationRepository(IMongoDbContext dbContext, IOptions<MongoDbSettings> mongoSettings, INotificationIdGenerator idGenerator)
        {
            _collection = dbContext.GetCollection<NotificationDocument>(mongoSettings.Value.CollectionName);
            _idGenerator = idGenerator;
        }

        public async Task SaveAsync(NotificationMessage notification, CancellationToken cancellationToken)
        {
            if (notification.IdNotification == 0)
            {
                var nextId = await _idGenerator.NextIdAsync(cancellationToken);
                notification.AssignSequentialId(nextId);
            }

            var document = notification.ToDocument();

            await _collection.ReplaceOneAsync(
                Builders<NotificationDocument>.Filter.Eq(d => d.Id, document.Id),
                document,
                new ReplaceOptions { IsUpsert = true },
                cancellationToken);
        }

        public async Task<IReadOnlyList<NotificationMessage>> GetAllAsync(CancellationToken cancellationToken)
        {
            var documents = await _collection.Find(FilterDefinition<NotificationDocument>.Empty)
                .SortByDescending(d => d.NotificationDate)
                .ToListAsync(cancellationToken);
            return documents.Select(d => d.ToDomain()).ToList();
        }

        public async Task<IReadOnlyList<NotificationMessage>> GetByClientAsync(String clientId, CancellationToken cancellationToken)
        {
            var filter = Builders<NotificationDocument>.Filter.Eq(d => d.ClientId, clientId);

            var documents = await _collection.Find(filter)
                .SortByDescending(d => d.NotificationDate)
                .ToListAsync(cancellationToken);

            return documents.Select(d => d.ToDomain()).ToList();
        }

        public async Task<NotificationMessage?> GetByIdAsync(String clientId, Int64 idNotification, CancellationToken cancellationToken)
        {
            var filter = Builders<NotificationDocument>.Filter.And(
                Builders<NotificationDocument>.Filter.Eq(d => d.ClientId, clientId),
                Builders<NotificationDocument>.Filter.Eq(d => d.IdNotification, idNotification));

            var document = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            return document?.ToDomain();
        }

        public async Task<Boolean> MarkAsReadAsync(String clientId, Int64 idNotification, CancellationToken cancellationToken)
        {
            var filter = Builders<NotificationDocument>.Filter.And(
                Builders<NotificationDocument>.Filter.Eq(d => d.ClientId, clientId),
                Builders<NotificationDocument>.Filter.Eq(d => d.IdNotification, idNotification));

            var update = Builders<NotificationDocument>.Update.Set(d => d.Status, NotificationStatus.Read);

            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
            return result.MatchedCount > 0;
        }
    }
}
