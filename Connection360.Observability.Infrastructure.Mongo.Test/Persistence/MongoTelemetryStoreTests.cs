using Connection360.Observability.Domain.Models;
using Connection360.Observability.Infrastructure.Mongo.Persistence;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Connection360.Observability.Infrastructure.Mongo.Test.Persistence
{
    public class MongoTelemetryStoreTests
    {
        private readonly Mock<IMongoObservabilityContext> _context = new();
        private readonly Mock<IMongoCollection<BsonDocument>> _collection = new();
        private readonly List<BsonDocument> _inserted = new();
        private InsertManyOptions? _options;
        private String? _collectionName;

        public MongoTelemetryStoreTests()
        {
            _context.SetupGet(c => c.Options).Returns(new MongoObservabilityOptions
            {
                LogsCollection = "mis-logs", MetricsCollection = "mis-metricas", TracesCollection = "mis-trazas",
            });
            _context.Setup(c => c.GetCollection(It.IsAny<String>())).Callback<String>(name => _collectionName = name).Returns(_collection.Object);
            _collection.Setup(c => c.InsertManyAsync(It.IsAny<IEnumerable<BsonDocument>>(), It.IsAny<InsertManyOptions>(), It.IsAny<CancellationToken>()))
                .Callback<IEnumerable<BsonDocument>, InsertManyOptions, CancellationToken>((docs, options, _) => { _inserted.AddRange(docs); _options = options; })
                .Returns(Task.CompletedTask);
        }

        [Fact]
        public async Task LogStore_InsertaElLoteEnLaColeccionDeLogs()
        {
            var store = new MongoLogStore(_context.Object);

            await store.WriteBatchAsync(new[] { new LogRecord { Message = "a", Service = "Etl" }, new LogRecord { Message = "b", Service = "Etl" } }, CancellationToken.None);

            _collectionName.Should().Be("mis-logs");
            _inserted.Select(d => d["message"].AsString).Should().Equal("a", "b");
            _inserted.Should().OnlyContain(d => d["service"] == "Etl");
        }

        [Fact]
        public async Task MetricStore_InsertaEnLaColeccionDeMetricas()
        {
            var store = new MongoMetricStore(_context.Object);

            await store.WriteBatchAsync(new[] { new MetricRecord { Name = "m", Service = "ApiCore" } }, CancellationToken.None);

            _collectionName.Should().Be("mis-metricas");
            _inserted.Should().ContainSingle().Which["name"].AsString.Should().Be("m");
        }

        [Fact]
        public async Task TraceStore_InsertaEnLaColeccionDeTrazas()
        {
            var store = new MongoTraceStore(_context.Object);

            await store.WriteBatchAsync(new[] { new TraceRecord { Name = "t", Service = "ApiNotification", TraceId = "x", SpanId = "y" } }, CancellationToken.None);

            _collectionName.Should().Be("mis-trazas");
            _inserted.Should().ContainSingle().Which["service"].AsString.Should().Be("ApiNotification");
        }

        [Fact]
        public async Task WriteBatchAsync_InsertaSinOrdenParaNoDetenerseEnUnDocumentoMalo()
        {
            var store = new MongoLogStore(_context.Object);

            await store.WriteBatchAsync(new[] { new LogRecord { Message = "a" } }, CancellationToken.None);

            _options!.IsOrdered.Should().BeFalse();
        }

        [Fact]
        public async Task WriteBatchAsync_ConLoteVacio_NoTocaMongo()
        {
            var store = new MongoLogStore(_context.Object);

            await store.WriteBatchAsync(Array.Empty<LogRecord>(), CancellationToken.None);

            _context.Verify(c => c.GetCollection(It.IsAny<String>()), Times.Never);
        }

        [Fact]
        public async Task WriteBatchAsync_SiMongoFalla_PropagaLaExcepcionParaQueElEscritorReintente()
        {
            _collection.Setup(c => c.InsertManyAsync(It.IsAny<IEnumerable<BsonDocument>>(), It.IsAny<InsertManyOptions>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutException("sin servidor"));
            var store = new MongoLogStore(_context.Object);

            Func<Task> act = () => store.WriteBatchAsync(new[] { new LogRecord { Message = "a" } }, CancellationToken.None);

            await act.Should().ThrowAsync<TimeoutException>();
        }

        [Fact]
        public void Name_EsMongodb()
        {
            new MongoLogStore(_context.Object).Name.Should().Be("mongodb");
            new MongoMetricStore(_context.Object).Name.Should().Be("mongodb");
            new MongoTraceStore(_context.Object).Name.Should().Be("mongodb");
        }

        [Fact]
        public async Task WriteBatchAsync_PasaElTokenDeCancelacion()
        {
            using var cts = new CancellationTokenSource();
            CancellationToken? received = null;
            _collection.Setup(c => c.InsertManyAsync(It.IsAny<IEnumerable<BsonDocument>>(), It.IsAny<InsertManyOptions>(), It.IsAny<CancellationToken>()))
                .Callback<IEnumerable<BsonDocument>, InsertManyOptions, CancellationToken>((_, _, token) => received = token)
                .Returns(Task.CompletedTask);

            await new MongoLogStore(_context.Object).WriteBatchAsync(new[] { new LogRecord() }, cts.Token);

            received.Should().Be(cts.Token);
        }
    }
}
