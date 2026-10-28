using Connection360Notification.Domain;
using Connection360Notification.Domain.Ports.Outbound;
using Connection360Notification.Domain.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Connection360Notification.Infrastructure.Persistence.Mongo
{
    /// <summary>
    /// Adaptador de salida (hexagonal) de <see cref="IBasicUserDataRepository"/> para MongoDB. Obtiene su colección
    /// desde IMongoDbContext (una sola conexión compartida por el proceso completo).
    ///
    /// Sin duplicados: el Id de Auth0 es el "_id" del documento y cada escritura es un ReplaceOne con upsert sobre ese
    /// Id, de modo que reenviar un usuario lo reemplaza en vez de crear otro; como "_id" es único en MongoDB, ni dos
    /// consumidores concurrentes pueden dejarlo repetido.
    /// </summary>
    public sealed class MongoBasicUserDataRepository : IBasicUserDataRepository
    {
        private readonly IMongoCollection<BasicUserDataDocument> _collection;

        public MongoBasicUserDataRepository(IMongoDbContext dbContext, IOptions<BasicUserSyncSettings> settings)
        {
            String collectionName = String.IsNullOrWhiteSpace(settings.Value.CollectionName)
                ? BasicUserSyncSettings.DefaultCollectionName
                : settings.Value.CollectionName.Trim();

            _collection = dbContext.GetCollection<BasicUserDataDocument>(collectionName);
        }

        public async Task UpsertBatchAsync(IReadOnlyCollection<BasicUserData> users, Guid syncId, CancellationToken cancellationToken)
        {
            if (users.Count == 0)
            {
                return;
            }

            String sync = syncId.ToString();
            DateTime now = DateTime.UtcNow;

            List<WriteModel<BasicUserDataDocument>> operations = users
                .Select(user => (WriteModel<BasicUserDataDocument>)new ReplaceOneModel<BasicUserDataDocument>(
                    Builders<BasicUserDataDocument>.Filter.Eq(document => document.Id, user.Id),
                    new BasicUserDataDocument
                    {
                        Id = user.Id,
                        Document = user.Document,
                        FullName = user.FullName,
                        Phone = user.Phone,
                        Email = user.Email,
                        SyncId = sync,
                        UpdatedAtUtc = now
                    })
                { IsUpsert = true })
                .ToList();

            await _collection.BulkWriteAsync(operations, new BulkWriteOptions { IsOrdered = false }, cancellationToken);
        }

        public async Task<BasicUserData?> GetByDocumentAsync(String document, CancellationToken cancellationToken)
        {
            if (String.IsNullOrWhiteSpace(document))
            {
                return null;
            }

            BasicUserDataDocument? found = await _collection
                .Find(Builders<BasicUserDataDocument>.Filter.Eq(user => user.Document, document.Trim()))
                .FirstOrDefaultAsync(cancellationToken);

            return found == null
                ? null
                : new BasicUserData(found.Id, found.Document, found.FullName, found.Phone, found.Email);
        }

        public async Task<Int64> CountBySyncAsync(Guid syncId, CancellationToken cancellationToken)
        {
            return await _collection.CountDocumentsAsync(
                Builders<BasicUserDataDocument>.Filter.Eq(document => document.SyncId, syncId.ToString()),
                cancellationToken: cancellationToken);
        }

        public async Task<Int64> DeleteNotInSyncAsync(Guid syncId, CancellationToken cancellationToken)
        {
            DeleteResult result = await _collection.DeleteManyAsync(
                Builders<BasicUserDataDocument>.Filter.Ne(document => document.SyncId, syncId.ToString()),
                cancellationToken);

            return result.DeletedCount;
        }
    }
}
