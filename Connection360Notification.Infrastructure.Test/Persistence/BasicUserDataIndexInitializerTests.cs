using Connection360Notification.Domain.Settings;
using Connection360Notification.Infrastructure.Persistence.Mongo;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MongoDB.Driver;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Persistence
{
    public class BasicUserDataIndexInitializerTests
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

        private readonly Mock<IMongoCollection<BasicUserDataDocument>> _collectionMock = new();
        private readonly Mock<IMongoIndexManager<BasicUserDataDocument>> _indexesMock = new();
        private readonly Mock<IMongoDbContext> _contextMock = new();
        private readonly CapturingLogger<BasicUserDataIndexInitializer> _logger = new();

        public BasicUserDataIndexInitializerTests()
        {
            _collectionMock.Setup(c => c.Indexes).Returns(_indexesMock.Object);
            _contextMock.Setup(c => c.GetCollection<BasicUserDataDocument>(It.IsAny<String>())).Returns(_collectionMock.Object);
        }

        private BasicUserDataIndexInitializer Crear(String collectionName = "usuarios")
        {
            return new BasicUserDataIndexInitializer(_contextMock.Object, Options.Create(new BasicUserSyncSettings { CollectionName = collectionName }), _logger);
        }

        private static String Render(IndexKeysDefinition<BasicUserDataDocument> keys) =>
            keys.Render(new RenderArgs<BasicUserDataDocument>(
                MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry.GetSerializer<BasicUserDataDocument>(),
                MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry)).ToString();

        [Fact]
        public async Task StartAsync_CreaLosIndicesPorSyncIdYDocument()
        {
            var creados = new TaskCompletionSource<List<CreateIndexModel<BasicUserDataDocument>>>(TaskCreationOptions.RunContinuationsAsynchronously);
            _indexesMock
                .Setup(i => i.CreateManyAsync(It.IsAny<IEnumerable<CreateIndexModel<BasicUserDataDocument>>>(), It.IsAny<CancellationToken>()))
                .Callback<IEnumerable<CreateIndexModel<BasicUserDataDocument>>, CancellationToken>((m, _) => creados.TrySetResult(m.ToList()))
                .ReturnsAsync(new List<String> { "a", "b" });

            await Crear().StartAsync(CancellationToken.None);

            List<CreateIndexModel<BasicUserDataDocument>> modelos = await creados.Task.WaitAsync(TimeSpan.FromSeconds(10));
            modelos.Should().HaveCount(2);
            Render(modelos[0].Keys).Should().Contain("SyncId");
            Render(modelos[1].Keys).Should().Contain("Document");
        }

        [Fact]
        public async Task StartAsync_UsaLaColeccionConfigurada()
        {
            var llamado = new TaskCompletionSource<Boolean>(TaskCreationOptions.RunContinuationsAsynchronously);
            _indexesMock
                .Setup(i => i.CreateManyAsync(It.IsAny<IEnumerable<CreateIndexModel<BasicUserDataDocument>>>(), It.IsAny<CancellationToken>()))
                .Callback(() => llamado.TrySetResult(true))
                .ReturnsAsync(new List<String>());

            await Crear("mis_usuarios").StartAsync(CancellationToken.None);
            await llamado.Task.WaitAsync(TimeSpan.FromSeconds(10));

            _contextMock.Verify(c => c.GetCollection<BasicUserDataDocument>("mis_usuarios"), Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task StartAsync_SinNombreDeColeccion_UsaElNombrePorDefecto(String nombre)
        {
            var llamado = new TaskCompletionSource<Boolean>(TaskCreationOptions.RunContinuationsAsynchronously);
            _indexesMock
                .Setup(i => i.CreateManyAsync(It.IsAny<IEnumerable<CreateIndexModel<BasicUserDataDocument>>>(), It.IsAny<CancellationToken>()))
                .Callback(() => llamado.TrySetResult(true))
                .ReturnsAsync(new List<String>());

            await Crear(nombre).StartAsync(CancellationToken.None);
            await llamado.Task.WaitAsync(TimeSpan.FromSeconds(10));

            _contextMock.Verify(c => c.GetCollection<BasicUserDataDocument>(BasicUserSyncSettings.DefaultCollectionName), Times.Once);
        }

        [Fact]
        public async Task StartAsync_SiLaCreacionFalla_NoLanzaYRegistraUnWarning()
        {
            var error = new InvalidOperationException("mongo no disponible");
            _indexesMock
                .Setup(i => i.CreateManyAsync(It.IsAny<IEnumerable<CreateIndexModel<BasicUserDataDocument>>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(error);

            Func<Task> act = () => Crear().StartAsync(CancellationToken.None);

            await act.Should().NotThrowAsync();
            var registro = await _logger.Registro.Task.WaitAsync(TimeSpan.FromSeconds(10));
            registro.Level.Should().Be(LogLevel.Warning);
            registro.Exception.Should().BeSameAs(error);
            registro.Message.Should().Contain("basicUserData");
        }

        [Fact]
        public async Task StopAsync_CompletaSinHacerNada()
        {
            await Crear().StopAsync(CancellationToken.None);

            _contextMock.Verify(c => c.GetCollection<BasicUserDataDocument>(It.IsAny<String>()), Times.Never);
        }
    }
}
