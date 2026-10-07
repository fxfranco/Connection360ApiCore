using Connection360.Observability.Domain.Ports;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Connection360.Observability.Infrastructure.Mongo.Persistence
{
    /// <summary>
    /// Crea los índices de las 3 colecciones: consultas más comunes (por aplicación, nivel, traza,
    /// nombre de métrica) y un índice TTL sobre "timestamp" para que MongoDB elimine solo los
    /// datos viejos (retención configurable). CreateMany es idempotente.
    /// </summary>
    public sealed class MongoObservabilityInitializer : ITelemetryStoreInitializer
    {
        private readonly IMongoObservabilityContext _context;

        public MongoObservabilityInitializer(IMongoObservabilityContext context) => _context = context;

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            MongoObservabilityOptions options = _context.Options;
            if (!options.CreateIndexes)
            {
                return;
            }

            await CreateAsync(options.LogsCollection, BuildIndexes(options.LogsRetentionDays,
                Compound("ix_service_timestamp", ("service", 1), ("timestamp", -1)),
                Compound("ix_level_timestamp", ("level", 1), ("timestamp", -1)),
                Compound("ix_traceId", ("traceId", 1))), cancellationToken).ConfigureAwait(false);

            await CreateAsync(options.MetricsCollection, BuildIndexes(options.MetricsRetentionDays,
                Compound("ix_service_name_timestamp", ("service", 1), ("name", 1), ("timestamp", -1))), cancellationToken).ConfigureAwait(false);

            await CreateAsync(options.TracesCollection, BuildIndexes(options.TracesRetentionDays,
                Compound("ix_traceId_startTime", ("traceId", 1), ("startTime", 1)),
                Compound("ix_service_name_timestamp", ("service", 1), ("name", 1), ("timestamp", -1))), cancellationToken).ConfigureAwait(false);
        }

        private async Task CreateAsync(String collectionName, List<CreateIndexModel<BsonDocument>> indexes, CancellationToken cancellationToken)
        {
            IMongoCollection<BsonDocument> collection = _context.GetCollection(collectionName);
            foreach (CreateIndexModel<BsonDocument> index in indexes)
            {
                try
                {
                    await collection.Indexes.CreateOneAsync(index, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                catch (MongoCommandException)
                {
                    // Un índice con el mismo nombre pero otras opciones (por ejemplo se cambió la retención):
                    // se deja el existente; cambiarlo requiere un collMod manual. No es un error de arranque.
                }
            }
        }

        private static List<CreateIndexModel<BsonDocument>> BuildIndexes(Int32 retentionDays, params CreateIndexModel<BsonDocument>[] queryIndexes)
        {
            var indexes = new List<CreateIndexModel<BsonDocument>>(queryIndexes);
            if (retentionDays > 0)
            {
                indexes.Add(new CreateIndexModel<BsonDocument>(
                    new BsonDocument("timestamp", 1),
                    new CreateIndexOptions { Name = "ttl_timestamp", ExpireAfter = TimeSpan.FromDays(retentionDays) }));
            }

            return indexes;
        }

        private static CreateIndexModel<BsonDocument> Compound(String name, params (String Field, Int32 Direction)[] fields)
        {
            var keys = new BsonDocument();
            foreach (var (field, direction) in fields)
            {
                keys[field] = direction;
            }

            return new CreateIndexModel<BsonDocument>(keys, new CreateIndexOptions { Name = name });
        }
    }
}
