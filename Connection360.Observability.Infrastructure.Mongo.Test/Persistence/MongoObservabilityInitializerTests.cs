using Connection360.Observability.Infrastructure.Mongo.Persistence;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Connection360.Observability.Infrastructure.Mongo.Test.Persistence
{
    public class MongoObservabilityInitializerTests
    {
        private sealed class Fixture
        {
            public Dictionary<String, List<CreateIndexModel<BsonDocument>>> Created { get; } = new();
            public Mock<IMongoObservabilityContext> Context { get; } = new();

            public Fixture(MongoObservabilityOptions options, Func<CreateIndexModel<BsonDocument>, Exception?>? failWith = null)
            {
                Context.SetupGet(c => c.Options).Returns(options);
                Context.Setup(c => c.GetCollection(It.IsAny<String>())).Returns<String>(name =>
                {
                    var indexes = new Mock<IMongoIndexManager<BsonDocument>>();
                    indexes.Setup(i => i.CreateOneAsync(It.IsAny<CreateIndexModel<BsonDocument>>(), It.IsAny<CreateOneIndexOptions>(), It.IsAny<CancellationToken>()))
                        .Returns<CreateIndexModel<BsonDocument>, CreateOneIndexOptions, CancellationToken>((model, _, _) =>
                        {
                            if (!Created.TryGetValue(name, out var list)) Created[name] = list = new();
                            list.Add(model);
                            Exception? failure = failWith?.Invoke(model);
                            return failure is null ? Task.FromResult(model.Options.Name) : Task.FromException<String>(failure);
                        });
                    var collection = new Mock<IMongoCollection<BsonDocument>>();
                    collection.SetupGet(c => c.Indexes).Returns(indexes.Object);
                    return collection.Object;
                });
            }
        }

        private static readonly RenderArgs<BsonDocument> RenderArgs = new(BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry);

        private static CreateIndexModel<BsonDocument> Index(Fixture f, String collection, String name)
            => f.Created[collection].Single(i => i.Options.Name == name);

        [Fact]
        public async Task InitializeAsync_CreaLosIndicesDeLasTresColecciones()
        {
            var f = new Fixture(new MongoObservabilityOptions());

            await new MongoObservabilityInitializer(f.Context.Object).InitializeAsync(CancellationToken.None);

            f.Created.Keys.Should().BeEquivalentTo("notificationLogs", "notificationMetrics", "notificationTraces");
            f.Created["notificationLogs"].Select(i => i.Options.Name).Should().BeEquivalentTo("ix_service_timestamp", "ix_level_timestamp", "ix_traceId", "ttl_timestamp");
            f.Created["notificationMetrics"].Select(i => i.Options.Name).Should().BeEquivalentTo("ix_service_name_timestamp", "ttl_timestamp");
            f.Created["notificationTraces"].Select(i => i.Options.Name).Should().BeEquivalentTo("ix_traceId_startTime", "ix_service_name_timestamp", "ttl_timestamp");
        }

        [Fact]
        public async Task InitializeAsync_ElIndiceTtlUsaLaRetencionConfiguradaDeCadaColeccion()
        {
            var f = new Fixture(new MongoObservabilityOptions { LogsRetentionDays = 7, MetricsRetentionDays = 60, TracesRetentionDays = 3 });

            await new MongoObservabilityInitializer(f.Context.Object).InitializeAsync(CancellationToken.None);

            Index(f, "notificationLogs", "ttl_timestamp").Options.ExpireAfter.Should().Be(TimeSpan.FromDays(7));
            Index(f, "notificationMetrics", "ttl_timestamp").Options.ExpireAfter.Should().Be(TimeSpan.FromDays(60));
            Index(f, "notificationTraces", "ttl_timestamp").Options.ExpireAfter.Should().Be(TimeSpan.FromDays(3));
            Index(f, "notificationLogs", "ttl_timestamp").Keys.Render(RenderArgs).ToJson().Should().Be(new BsonDocument("timestamp", 1).ToJson());
        }

        [Fact]
        public async Task InitializeAsync_ConRetencionCero_NoCreaIndiceTtl()
        {
            var f = new Fixture(new MongoObservabilityOptions { LogsRetentionDays = 0, MetricsRetentionDays = 0, TracesRetentionDays = 0 });

            await new MongoObservabilityInitializer(f.Context.Object).InitializeAsync(CancellationToken.None);

            f.Created.Values.SelectMany(v => v).Should().NotContain(i => i.Options.Name == "ttl_timestamp");
        }

        [Fact]
        public async Task InitializeAsync_IndicesDeConsultaTienenLasClavesEsperadas()
        {
            var f = new Fixture(new MongoObservabilityOptions());

            await new MongoObservabilityInitializer(f.Context.Object).InitializeAsync(CancellationToken.None);

            Index(f, "notificationLogs", "ix_service_timestamp").Keys.Render(RenderArgs).ToJson().Should().Be(new BsonDocument { { "service", 1 }, { "timestamp", -1 } }.ToJson());
            Index(f, "notificationLogs", "ix_traceId").Keys.Render(RenderArgs).ToJson().Should().Be(new BsonDocument("traceId", 1).ToJson());
            Index(f, "notificationTraces", "ix_traceId_startTime").Keys.Render(RenderArgs).ToJson().Should().Be(new BsonDocument { { "traceId", 1 }, { "startTime", 1 } }.ToJson());
        }

        [Fact]
        public async Task InitializeAsync_UsaLosNombresDeColeccionConfigurados()
        {
            var f = new Fixture(new MongoObservabilityOptions { LogsCollection = "a", MetricsCollection = "b", TracesCollection = "c" });

            await new MongoObservabilityInitializer(f.Context.Object).InitializeAsync(CancellationToken.None);

            f.Created.Keys.Should().BeEquivalentTo("a", "b", "c");
        }

        [Fact]
        public async Task InitializeAsync_ConCreateIndexesEnFalso_NoHaceNada()
        {
            var f = new Fixture(new MongoObservabilityOptions { CreateIndexes = false });

            await new MongoObservabilityInitializer(f.Context.Object).InitializeAsync(CancellationToken.None);

            f.Created.Should().BeEmpty();
            f.Context.Verify(c => c.GetCollection(It.IsAny<String>()), Times.Never);
        }

        [Fact]
        public async Task InitializeAsync_SiMongoFalla_PropagaLaExcepcion()
        {
            var f = new Fixture(new MongoObservabilityOptions(), _ => new TimeoutException("sin servidor"));

            Func<Task> act = () => new MongoObservabilityInitializer(f.Context.Object).InitializeAsync(CancellationToken.None);

            await act.Should().ThrowAsync<TimeoutException>();
        }
    }
}
