using Connection360Notification.Domain.Settings;
using Connection360Notification.Infrastructure.Persistence.Mongo;
using Connection360Notification.Infrastructure.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MongoDB.Driver;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Persistence
{
    public class MongoIndexInitializerTests
    {
        private sealed class CapturingLogger<T> : ILogger<T>
        {
            public TaskCompletionSource<(LogLevel Level, Exception? Exception, String Message)> Registro { get; } =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public Boolean IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, String> formatter)
                => Registro.TrySetResult((logLevel, exception, formatter(state, exception)));
        }

        private readonly Mock<IMongoCollection<NotificationDocument>> _collectionMock = new();
        private readonly Mock<IMongoIndexManager<NotificationDocument>> _indexesMock = new();
        private readonly Mock<IMongoDbContext> _contextMock = new();
        private readonly CapturingLogger<MongoIndexInitializer> _logger = new();
        private readonly MongoIndexInitializer _sut;

        public MongoIndexInitializerTests()
        {
            _collectionMock.Setup(c => c.Indexes).Returns(_indexesMock.Object);
            _contextMock.Setup(c => c.GetCollection<NotificationDocument>("notifs")).Returns(_collectionMock.Object);
            _sut = new MongoIndexInitializer(_contextMock.Object, Options.Create(new MongoDbSettings { CollectionName = "notifs" }), _logger);
        }

        [Fact]
        public async Task StartAsync_CreaLosDosIndicesEsperadosEnSegundoPlano()
        {
            var creados = new TaskCompletionSource<IEnumerable<CreateIndexModel<NotificationDocument>>>(TaskCreationOptions.RunContinuationsAsynchronously);
            _indexesMock
                .Setup(i => i.CreateManyAsync(It.IsAny<IEnumerable<CreateIndexModel<NotificationDocument>>>(), It.IsAny<CancellationToken>()))
                .Callback<IEnumerable<CreateIndexModel<NotificationDocument>>, CancellationToken>((m, _) => creados.TrySetResult(m.ToList()))
                .ReturnsAsync(new List<String> { "a", "b" });

            await _sut.StartAsync(CancellationToken.None);

            var modelos = (await creados.Task.WaitAsync(TimeSpan.FromSeconds(10))).ToList();
            modelos.Should().HaveCount(2);
            var claves = modelos.Select(m => Render(m.Keys)).ToList();
            claves[0].Should().Contain("ClientId").And.Contain("NotificationDate");
            claves[1].Should().Contain("ClientId").And.Contain("IdNotification");
        }

        private static String Render(IndexKeysDefinition<NotificationDocument> keys) =>
            keys.Render(new RenderArgs<NotificationDocument>(
                MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry.GetSerializer<NotificationDocument>(),
                MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry)).ToString();

        [Fact]
        public async Task StartAsync_ConsultaLaColeccionConfigurada()
        {
            var llamado = new TaskCompletionSource<Boolean>(TaskCreationOptions.RunContinuationsAsynchronously);
            _indexesMock
                .Setup(i => i.CreateManyAsync(It.IsAny<IEnumerable<CreateIndexModel<NotificationDocument>>>(), It.IsAny<CancellationToken>()))
                .Callback(() => llamado.TrySetResult(true))
                .ReturnsAsync(new List<String>());

            await _sut.StartAsync(CancellationToken.None);
            await llamado.Task.WaitAsync(TimeSpan.FromSeconds(10));

            _contextMock.Verify(c => c.GetCollection<NotificationDocument>("notifs"), Times.Once);
        }

        [Fact]
        public async Task StartAsync_SiLaCreacionFalla_NoLanzaYRegistraUnWarning()
        {
            var error = new InvalidOperationException("mongo no disponible");
            _indexesMock
                .Setup(i => i.CreateManyAsync(It.IsAny<IEnumerable<CreateIndexModel<NotificationDocument>>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(error);

            Func<Task> act = () => _sut.StartAsync(CancellationToken.None);

            await act.Should().NotThrowAsync();
            var registro = await _logger.Registro.Task.WaitAsync(TimeSpan.FromSeconds(10));
            registro.Level.Should().Be(LogLevel.Warning);
            registro.Exception.Should().BeSameAs(error);
            registro.Message.Should().Contain("índices");
        }

        [Fact]
        public async Task StartAsync_SiObtenerLaColeccionFalla_RegistraUnWarning()
        {
            _contextMock.Setup(c => c.GetCollection<NotificationDocument>("notifs")).Throws(new InvalidOperationException("sin contexto"));

            await _sut.StartAsync(CancellationToken.None);

            var registro = await _logger.Registro.Task.WaitAsync(TimeSpan.FromSeconds(10));
            registro.Level.Should().Be(LogLevel.Warning);
        }

        [Fact]
        public async Task StopAsync_CompletaSinHacerNada()
        {
            await _sut.StopAsync(CancellationToken.None);

            _contextMock.Verify(c => c.GetCollection<NotificationDocument>(It.IsAny<String>()), Times.Never);
        }
    }
}
