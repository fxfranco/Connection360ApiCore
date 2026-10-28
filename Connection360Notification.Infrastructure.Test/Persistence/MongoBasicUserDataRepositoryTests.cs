using Connection360Notification.Domain;
using Connection360Notification.Domain.Settings;
using Connection360Notification.Infrastructure.Persistence.Mongo;
using Connection360Notification.Infrastructure.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Persistence
{
    public class MongoBasicUserDataRepositoryTests
    {
        private readonly Mock<IMongoCollection<BasicUserDataDocument>> _collectionMock = new();
        private readonly Mock<IMongoDbContext> _contextMock = new();
        private readonly MongoBasicUserDataRepository _sut;

        public MongoBasicUserDataRepositoryTests()
        {
            _contextMock.Setup(c => c.GetCollection<BasicUserDataDocument>(It.IsAny<String>())).Returns(_collectionMock.Object);
            _sut = new MongoBasicUserDataRepository(_contextMock.Object, Options.Create(new BasicUserSyncSettings { CollectionName = "usuarios" }));
        }

        private List<WriteModel<BasicUserDataDocument>> CapturarBulkWrite(out Func<BulkWriteOptions?> opciones)
        {
            var capturados = new List<WriteModel<BasicUserDataDocument>>();
            BulkWriteOptions? ultimasOpciones = null;
            _collectionMock
                .Setup(c => c.BulkWriteAsync(It.IsAny<IEnumerable<WriteModel<BasicUserDataDocument>>>(), It.IsAny<BulkWriteOptions>(), It.IsAny<CancellationToken>()))
                .Callback<IEnumerable<WriteModel<BasicUserDataDocument>>, BulkWriteOptions, CancellationToken>((requests, o, _) =>
                {
                    capturados.AddRange(requests);
                    ultimasOpciones = o;
                })
                .ReturnsAsync((BulkWriteResult<BasicUserDataDocument>)null!);
            opciones = () => ultimasOpciones;
            return capturados;
        }

        [Fact]
        public void Constructor_ObtieneLaColeccionConfiguradaDesdeElContexto()
        {
            _contextMock.Verify(c => c.GetCollection<BasicUserDataDocument>("usuarios"), Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_SinNombreDeColeccion_UsaBasicUserData(String nombre)
        {
            var contexto = new Mock<IMongoDbContext>();
            contexto.Setup(c => c.GetCollection<BasicUserDataDocument>(It.IsAny<String>())).Returns(_collectionMock.Object);

            _ = new MongoBasicUserDataRepository(contexto.Object, Options.Create(new BasicUserSyncSettings { CollectionName = nombre }));

            contexto.Verify(c => c.GetCollection<BasicUserDataDocument>("basicUserData"), Times.Once);
        }

        [Fact]
        public void Constructor_RecortaLosEspaciosDelNombreDeLaColeccion()
        {
            var contexto = new Mock<IMongoDbContext>();
            contexto.Setup(c => c.GetCollection<BasicUserDataDocument>(It.IsAny<String>())).Returns(_collectionMock.Object);

            _ = new MongoBasicUserDataRepository(contexto.Object, Options.Create(new BasicUserSyncSettings { CollectionName = "  usuarios  " }));

            contexto.Verify(c => c.GetCollection<BasicUserDataDocument>("usuarios"), Times.Once);
        }

        [Fact]
        public async Task UpsertBatchAsync_SinUsuarios_NoEscribeEnMongo()
        {
            await _sut.UpsertBatchAsync(new List<BasicUserData>(), Guid.NewGuid(), CancellationToken.None);

            _collectionMock.Verify(
                c => c.BulkWriteAsync(It.IsAny<IEnumerable<WriteModel<BasicUserDataDocument>>>(), It.IsAny<BulkWriteOptions>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task UpsertBatchAsync_HaceUnReplaceOneConUpsertPorCadaUsuario()
        {
            List<WriteModel<BasicUserDataDocument>> escritos = CapturarBulkWrite(out Func<BulkWriteOptions?> opciones);
            Guid syncId = Guid.NewGuid();
            var usuarios = new List<BasicUserData>
            {
                new("auth0|1", "900123", "Ana Uno", "+573001112233", "uno@correo.com"),
                new("auth0|2", "800456", "Beto Dos", "+573004445566", "dos@correo.com")
            };

            DateTime antes = DateTime.UtcNow;
            await _sut.UpsertBatchAsync(usuarios, syncId, CancellationToken.None);
            DateTime despues = DateTime.UtcNow;

            escritos.Should().HaveCount(2);
            escritos.Should().AllBeOfType<ReplaceOneModel<BasicUserDataDocument>>();
            var reemplazos = escritos.Cast<ReplaceOneModel<BasicUserDataDocument>>().ToList();
            reemplazos.Should().OnlyContain(r => r.IsUpsert);

            MongoRender.Filter(reemplazos[0].Filter)["_id"].AsString.Should().Be("auth0|1");
            MongoRender.Filter(reemplazos[1].Filter)["_id"].AsString.Should().Be("auth0|2");

            BasicUserDataDocument primero = reemplazos[0].Replacement;
            primero.Id.Should().Be("auth0|1");
            primero.Document.Should().Be("900123");
            primero.FullName.Should().Be("Ana Uno");
            primero.Phone.Should().Be("+573001112233");
            primero.Email.Should().Be("uno@correo.com");
            primero.SyncId.Should().Be(syncId.ToString());
            primero.UpdatedAtUtc.Should().BeOnOrAfter(antes).And.BeOnOrBefore(despues);

            reemplazos[1].Replacement.SyncId.Should().Be(syncId.ToString());
        }

        [Fact]
        public async Task UpsertBatchAsync_EscribeSinOrdenParaNoDetenerseAnteUnFallo()
        {
            CapturarBulkWrite(out Func<BulkWriteOptions?> opciones);

            await _sut.UpsertBatchAsync(new[] { new BasicUserData("auth0|1", "1", "Ana", "+57", "a@b.c") }, Guid.NewGuid(), CancellationToken.None);

            opciones()!.IsOrdered.Should().BeFalse();
        }

        [Fact]
        public async Task UpsertBatchAsync_ErrorDeMongo_SePropaga()
        {
            _collectionMock
                .Setup(c => c.BulkWriteAsync(It.IsAny<IEnumerable<WriteModel<BasicUserDataDocument>>>(), It.IsAny<BulkWriteOptions>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("mongo caído"));

            Func<Task> act = () => _sut.UpsertBatchAsync(new[] { new BasicUserData("auth0|1", "1", "Ana", "+57", "a@b.c") }, Guid.NewGuid(), CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("mongo caído");
        }

        [Fact]
        public async Task CountBySyncAsync_CuentaLosDocumentosConEseSyncId()
        {
            Guid syncId = Guid.NewGuid();
            FilterDefinition<BasicUserDataDocument>? filtro = null;
            _collectionMock
                .Setup(c => c.CountDocumentsAsync(It.IsAny<FilterDefinition<BasicUserDataDocument>>(), It.IsAny<CountOptions>(), It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<BasicUserDataDocument>, CountOptions, CancellationToken>((f, _, _) => filtro = f)
                .ReturnsAsync(42L);

            Int64 total = await _sut.CountBySyncAsync(syncId, CancellationToken.None);

            total.Should().Be(42);
            MongoRender.Filter(filtro!)["SyncId"].AsString.Should().Be(syncId.ToString());
        }

        [Fact]
        public async Task DeleteNotInSyncAsync_EliminaLosDocumentosConOtroSyncIdYDevuelveLaCantidad()
        {
            Guid syncId = Guid.NewGuid();
            FilterDefinition<BasicUserDataDocument>? filtro = null;
            _collectionMock
                .Setup(c => c.DeleteManyAsync(It.IsAny<FilterDefinition<BasicUserDataDocument>>(), It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<BasicUserDataDocument>, CancellationToken>((f, _) => filtro = f)
                .ReturnsAsync(new DeleteResult.Acknowledged(5));

            Int64 eliminados = await _sut.DeleteNotInSyncAsync(syncId, CancellationToken.None);

            eliminados.Should().Be(5);
            BsonDocument rendered = MongoRender.Filter(filtro!);
            rendered["SyncId"].AsBsonDocument["$ne"].AsString.Should().Be(syncId.ToString());
        }

        [Fact]
        public async Task DeleteNotInSyncAsync_NoUsaUnFiltroVacioQueBorreTodo()
        {
            FilterDefinition<BasicUserDataDocument>? filtro = null;
            _collectionMock
                .Setup(c => c.DeleteManyAsync(It.IsAny<FilterDefinition<BasicUserDataDocument>>(), It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<BasicUserDataDocument>, CancellationToken>((f, _) => filtro = f)
                .ReturnsAsync(new DeleteResult.Acknowledged(0));

            await _sut.DeleteNotInSyncAsync(Guid.NewGuid(), CancellationToken.None);

            MongoRender.Filter(filtro!).ElementCount.Should().BeGreaterThan(0);
        }

        private FilterDefinition<BasicUserDataDocument>? _filtroDeBusqueda;

        private void ConfigurarBusqueda(params BasicUserDataDocument[] resultados)
        {
            _collectionMock
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<BasicUserDataDocument>>(),
                    It.IsAny<FindOptions<BasicUserDataDocument, BasicUserDataDocument>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<BasicUserDataDocument>, FindOptions<BasicUserDataDocument, BasicUserDataDocument>, CancellationToken>((f, _, _) => _filtroDeBusqueda = f)
                .ReturnsAsync(() => new ListCursor<BasicUserDataDocument>(resultados));
        }

        [Fact]
        public async Task GetByDocumentAsync_BuscaPorElCampoDocument()
        {
            ConfigurarBusqueda(new BasicUserDataDocument { Id = "auth0|1", Document = "900123", FullName = "Ana Uno", Phone = "3001112233", Email = "uno@correo.com" });

            await _sut.GetByDocumentAsync("900123", CancellationToken.None);

            MongoRender.Filter(_filtroDeBusqueda!)["Document"].AsString.Should().Be("900123");
        }

        [Fact]
        public async Task GetByDocumentAsync_RecortaLosEspaciosDelDocumento()
        {
            ConfigurarBusqueda();

            await _sut.GetByDocumentAsync("  900123  ", CancellationToken.None);

            MongoRender.Filter(_filtroDeBusqueda!)["Document"].AsString.Should().Be("900123");
        }

        [Fact]
        public async Task GetByDocumentAsync_SiExiste_DevuelveSusDatos()
        {
            ConfigurarBusqueda(new BasicUserDataDocument { Id = "auth0|1", Document = "900123", FullName = "Ana Uno", Phone = "3001112233", Email = "uno@correo.com" });

            BasicUserData? usuario = await _sut.GetByDocumentAsync("900123", CancellationToken.None);

            usuario.Should().Be(new BasicUserData("auth0|1", "900123", "Ana Uno", "3001112233", "uno@correo.com"));
        }

        [Fact]
        public async Task GetByDocumentAsync_SiNoExiste_DevuelveNulo()
        {
            ConfigurarBusqueda();

            BasicUserData? usuario = await _sut.GetByDocumentAsync("no-existe", CancellationToken.None);

            usuario.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetByDocumentAsync_ConDocumentoVacio_DevuelveNuloSinConsultarMongo(String? documento)
        {
            BasicUserData? usuario = await _sut.GetByDocumentAsync(documento!, CancellationToken.None);

            usuario.Should().BeNull();
            _collectionMock.Verify(
                c => c.FindAsync(It.IsAny<FilterDefinition<BasicUserDataDocument>>(), It.IsAny<FindOptions<BasicUserDataDocument, BasicUserDataDocument>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetByDocumentAsync_ErrorDeMongo_SePropaga()
        {
            _collectionMock
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<BasicUserDataDocument>>(),
                    It.IsAny<FindOptions<BasicUserDataDocument, BasicUserDataDocument>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("mongo caído"));

            Func<Task> act = () => _sut.GetByDocumentAsync("900123", CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("mongo caído");
        }
    }
}
