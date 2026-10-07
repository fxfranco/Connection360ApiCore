using Connection360.Observability.Domain.Models;
using Connection360.Observability.Domain.Ports;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Connection360.Observability.Infrastructure.Mongo.Persistence
{
    /// <summary>Base de los 3 almacenamientos: inserta cada lote con InsertMany (sin orden, un solo viaje a la red).</summary>
    public abstract class MongoTelemetryStore<TRecord> : ITelemetryStore<TRecord> where TRecord : TelemetryRecord
    {
        private static readonly InsertManyOptions UnorderedInsert = new() { IsOrdered = false };

        private readonly IMongoObservabilityContext _context;

        protected MongoTelemetryStore(IMongoObservabilityContext context) => _context = context;

        public String Name => "mongodb";

        protected abstract String CollectionName(MongoObservabilityOptions options);

        protected abstract BsonDocument ToDocument(TRecord record);

        public async Task WriteBatchAsync(IReadOnlyList<TRecord> records, CancellationToken cancellationToken)
        {
            if (records.Count == 0)
            {
                return;
            }

            var documents = new List<BsonDocument>(records.Count);
            foreach (TRecord record in records)
            {
                documents.Add(ToDocument(record));
            }

            await _context.GetCollection(CollectionName(_context.Options))
                .InsertManyAsync(documents, UnorderedInsert, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>Colección "notificationLogs".</summary>
    public sealed class MongoLogStore : MongoTelemetryStore<LogRecord>, ILogStore
    {
        public MongoLogStore(IMongoObservabilityContext context) : base(context) { }

        protected override String CollectionName(MongoObservabilityOptions options) => options.LogsCollection;

        protected override BsonDocument ToDocument(LogRecord record) => TelemetryBsonMapper.ToDocument(record);
    }

    /// <summary>Colección "notificationMetrics".</summary>
    public sealed class MongoMetricStore : MongoTelemetryStore<MetricRecord>, IMetricStore
    {
        public MongoMetricStore(IMongoObservabilityContext context) : base(context) { }

        protected override String CollectionName(MongoObservabilityOptions options) => options.MetricsCollection;

        protected override BsonDocument ToDocument(MetricRecord record) => TelemetryBsonMapper.ToDocument(record);
    }

    /// <summary>Colección "notificationTraces".</summary>
    public sealed class MongoTraceStore : MongoTelemetryStore<TraceRecord>, ITraceStore
    {
        public MongoTraceStore(IMongoObservabilityContext context) : base(context) { }

        protected override String CollectionName(MongoObservabilityOptions options) => options.TracesCollection;

        protected override BsonDocument ToDocument(TraceRecord record) => TelemetryBsonMapper.ToDocument(record);
    }
}
