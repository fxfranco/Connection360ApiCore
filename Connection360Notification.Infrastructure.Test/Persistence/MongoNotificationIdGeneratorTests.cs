using Connection360Notification.Infrastructure.Persistence.Mongo;
using Connection360Notification.Infrastructure.Tests.Support;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Persistence
{
    public class MongoNotificationIdGeneratorTests
    {
        private readonly FakeCollectionState _proxy = new();
        private readonly FakeMongoDbContext _context;

        public MongoNotificationIdGeneratorTests()
        {
            // CounterDocument es un tipo privado anidado: se instancia el doble generico por reflexion.
            _context = new FakeMongoDbContext
            {
                CollectionFactory = (tipo, _) => Activator.CreateInstance(typeof(FakeMongoCollection<>).MakeGenericType(tipo), _proxy)!
            };
        }

        [Fact]
        public void Constructor_UsaLaColeccionCounters()
        {
            _ = new MongoNotificationIdGenerator(_context);

            _context.RequestedCollections.Should().Equal("counters");
        }

        [Fact]
        public async Task NextIdAsync_RetornaLaSecuenciaDelContadorActualizado()
        {
            _proxy.NextSequence = 41;
            var sut = new MongoNotificationIdGenerator(_context);

            var id = await sut.NextIdAsync(CancellationToken.None);

            id.Should().Be(41);
        }

        [Fact]
        public async Task NextIdAsync_IncrementaConUpsertYRetornaElDocumentoDespuesDeActualizar()
        {
            var sut = new MongoNotificationIdGenerator(_context);

            await sut.NextIdAsync(CancellationToken.None);

            _proxy.Calls.Should().ContainSingle();
            var llamada = _proxy.Calls[0];
            var opciones = llamada.Options;
            opciones.GetType().GetProperty("IsUpsert")!.GetValue(opciones).Should().Be(true);
            opciones.GetType().GetProperty("ReturnDocument")!.GetValue(opciones).Should().Be(ReturnDocument.After);
        }

        [Fact]
        public async Task NextIdAsync_PropagaElTokenDeCancelacion()
        {
            using var cts = new CancellationTokenSource();
            var sut = new MongoNotificationIdGenerator(_context);

            await sut.NextIdAsync(cts.Token);

            _proxy.Calls[0].Token.Should().Be(cts.Token);
        }

        [Fact]
        public async Task NextIdAsync_LlamadasSucesivas_ConsultanElContadorCadaVez()
        {
            var sut = new MongoNotificationIdGenerator(_context);

            await sut.NextIdAsync(CancellationToken.None);
            await sut.NextIdAsync(CancellationToken.None);

            _proxy.Calls.Should().HaveCount(2);
        }

        [Fact]
        public async Task NextIdAsync_SiLaBaseFalla_PropagaLaExcepcion()
        {
            _proxy.ToThrow = new InvalidOperationException("sin conexion");
            var sut = new MongoNotificationIdGenerator(_context);

            Func<Task> act = () => sut.NextIdAsync(CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
