using Connection360Notification.Domain.Ports.Outbound;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Connection360Notification.Infrastructure.Persistence.Mongo
{
    /// <summary>
    /// Genera IdNotification (Int64 secuencial de negocio) con el patrón de contador atómico de
    /// MongoDB: un único documento contador que se incrementa con $inc a través de
    /// FindOneAndUpdate(IsUpsert: true). Esa operación es atómica a nivel de documento en MongoDB,
    /// así que es segura ante escrituras concurrentes desde múltiples requests/hilos/instancias del
    /// proceso, sin necesitar locks explícitos ni transacciones.
    /// </summary>
    public sealed class MongoNotificationIdGenerator : INotificationIdGenerator
    {
        private const String CounterId = "notificationId";

        private readonly IMongoCollection<CounterDocument> _counters;

        public MongoNotificationIdGenerator(IMongoDbContext dbContext)
        {
            _counters = dbContext.GetCollection<CounterDocument>("counters");
        }

        public async Task<Int64> NextIdAsync(CancellationToken cancellationToken)
        {
            var filter = Builders<CounterDocument>.Filter.Eq(c => c.Id, CounterId);
            var update = Builders<CounterDocument>.Update.Inc(c => c.Sequence, 1);
            var options = new FindOneAndUpdateOptions<CounterDocument>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };

            var counter = await _counters.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
            return counter.Sequence;
        }

        private sealed class CounterDocument
        {
            [BsonId]
            public String Id { get; set; } = default!;

            public Int64 Sequence { get; set; }
        }
    }
}
