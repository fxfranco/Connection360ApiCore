using Connection360Notification.Domain;
using Connection360Notification.Domain.Enums;
using Connection360Notification.Domain.Ports.Outbound;
using Connection360Notification.Domain.Settings;
using Connection360Notification.Infrastructure.Persistence.Mongo;
using Connection360Notification.Infrastructure.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using MongoDB.Driver;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Persistence
{
    public class MongoNotificationRepositoryTests
    {
        private static readonly DateTime Fecha = new(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc);

        private readonly Mock<IMongoCollection<NotificationDocument>> _collectionMock = new();
        private readonly Mock<IMongoDbContext> _contextMock = new();
        private readonly Mock<INotificationIdGenerator> _idGeneratorMock = new();
        private readonly MongoNotificationRepository _sut;

        public MongoNotificationRepositoryTests()
        {
            _contextMock.Setup(c => c.GetCollection<NotificationDocument>("notifs")).Returns(_collectionMock.Object);
            _sut = new MongoNotificationRepository(
                _contextMock.Object,
                Options.Create(new MongoDbSettings { CollectionName = "notifs" }),
                _idGeneratorMock.Object);
        }

        private static NotificationDocument Documento(String id = "ID-1", Int64 idNotification = 1, String cliente = "CLI-1") => new()
        {
            Id = id, IdNotification = idNotification, ClientId = cliente, Type = NotificationType.Comment,
            DocumentNumber = "HBL-1", Title = "Titulo", Message = "Mensaje", MessageDate = Fecha,
            Status = NotificationStatus.Read, NotificationDate = Fecha.AddHours(1)
        };

        private void ConfigurarFind(params NotificationDocument[] resultados)
        {
            _collectionMock
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<NotificationDocument>>(),
                    It.IsAny<FindOptions<NotificationDocument, NotificationDocument>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<NotificationDocument>, FindOptions<NotificationDocument, NotificationDocument>, CancellationToken>((f, o, _) =>
                {
                    UltimoFiltro = f;
                    UltimasOpciones = o;
                })
                .ReturnsAsync(() => new ListCursor<NotificationDocument>(resultados));
        }

        private FilterDefinition<NotificationDocument>? UltimoFiltro { get; set; }
        private FindOptions<NotificationDocument, NotificationDocument>? UltimasOpciones { get; set; }

        [Fact]
        public void Constructor_ObtieneLaColeccionConfiguradaDesdeElContexto()
        {
            _contextMock.Verify(c => c.GetCollection<NotificationDocument>("notifs"), Times.Once);
        }

        [Fact]
        public async Task SaveAsync_SinIdNotification_AsignaElSiguienteIdDelGenerador()
        {
            var notificacion = new NotificationMessage("CLI-1", NotificationType.Comment, "Mensaje");
            _idGeneratorMock.Setup(g => g.NextIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(77);
            NotificationDocument? guardado = null;
            _collectionMock
                .Setup(c => c.ReplaceOneAsync(It.IsAny<FilterDefinition<NotificationDocument>>(), It.IsAny<NotificationDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<NotificationDocument>, NotificationDocument, ReplaceOptions, CancellationToken>((_, d, _, _) => guardado = d)
                .ReturnsAsync(new ReplaceOneResult.Acknowledged(1, 1, null));

            await _sut.SaveAsync(notificacion, CancellationToken.None);

            notificacion.IdNotification.Should().Be(77);
            guardado.Should().NotBeNull();
            guardado!.IdNotification.Should().Be(77);
            _idGeneratorMock.Verify(g => g.NextIdAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SaveAsync_ConIdNotificationYaAsignado_NoUsaElGenerador()
        {
            var notificacion = new NotificationMessage("CLI-1", NotificationType.Comment, "Mensaje", idNotification: 5);
            _collectionMock
                .Setup(c => c.ReplaceOneAsync(It.IsAny<FilterDefinition<NotificationDocument>>(), It.IsAny<NotificationDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReplaceOneResult.Acknowledged(1, 1, null));

            await _sut.SaveAsync(notificacion, CancellationToken.None);

            notificacion.IdNotification.Should().Be(5);
            _idGeneratorMock.Verify(g => g.NextIdAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SaveAsync_HaceUpsertFiltrandoPorIdYMapeaTodosLosCampos()
        {
            var notificacion = new NotificationMessage(
                "CLI-1", NotificationType.ChangeState, "Mensaje", "HBL-9", "Titulo",
                Fecha, NotificationStatus.Read, Fecha.AddDays(1), 12, "ID-12");
            FilterDefinition<NotificationDocument>? filtro = null;
            NotificationDocument? guardado = null;
            ReplaceOptions? opciones = null;
            _collectionMock
                .Setup(c => c.ReplaceOneAsync(It.IsAny<FilterDefinition<NotificationDocument>>(), It.IsAny<NotificationDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<NotificationDocument>, NotificationDocument, ReplaceOptions, CancellationToken>((f, d, o, _) => { filtro = f; guardado = d; opciones = o; })
                .ReturnsAsync(new ReplaceOneResult.Acknowledged(0, 0, null));

            await _sut.SaveAsync(notificacion, CancellationToken.None);

            opciones!.IsUpsert.Should().BeTrue();
            MongoRender.Filter(filtro!)["_id"].AsString.Should().Be("ID-12");
            guardado.Should().BeEquivalentTo(new NotificationDocument
            {
                Id = "ID-12", IdNotification = 12, ClientId = "CLI-1", Type = NotificationType.ChangeState,
                DocumentNumber = "HBL-9", Title = "Titulo", Message = "Mensaje", MessageDate = Fecha,
                Status = NotificationStatus.Read, NotificationDate = Fecha.AddDays(1)
            });
        }

        [Fact]
        public async Task SaveAsync_PropagaElTokenDeCancelacion()
        {
            using var cts = new CancellationTokenSource();
            var notificacion = new NotificationMessage("CLI-1", NotificationType.Comment, "Mensaje");
            _idGeneratorMock.Setup(g => g.NextIdAsync(cts.Token)).ReturnsAsync(1);
            _collectionMock
                .Setup(c => c.ReplaceOneAsync(It.IsAny<FilterDefinition<NotificationDocument>>(), It.IsAny<NotificationDocument>(), It.IsAny<ReplaceOptions>(), cts.Token))
                .ReturnsAsync(new ReplaceOneResult.Acknowledged(1, 1, null));

            await _sut.SaveAsync(notificacion, cts.Token);

            _collectionMock.Verify(c => c.ReplaceOneAsync(It.IsAny<FilterDefinition<NotificationDocument>>(), It.IsAny<NotificationDocument>(), It.IsAny<ReplaceOptions>(), cts.Token), Times.Once);
        }

        [Fact]
        public async Task SaveAsync_SiElGeneradorFalla_PropagaYNoGuarda()
        {
            var notificacion = new NotificationMessage("CLI-1", NotificationType.Comment, "Mensaje");
            _idGeneratorMock.Setup(g => g.NextIdAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("contador"));

            Func<Task> act = () => _sut.SaveAsync(notificacion, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
            _collectionMock.Verify(c => c.ReplaceOneAsync(It.IsAny<FilterDefinition<NotificationDocument>>(), It.IsAny<NotificationDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetAllAsync_SinFiltroYOrdenadoPorFechaDescendente_MapeaADominio()
        {
            ConfigurarFind(Documento("A", 1), Documento("B", 2));

            var resultado = await _sut.GetAllAsync(CancellationToken.None);

            resultado.Should().HaveCount(2);
            resultado[0].Id.Should().Be("A");
            resultado[1].IdNotification.Should().Be(2);
            MongoRender.Filter(UltimoFiltro!).ElementCount.Should().Be(0);
            MongoRender.Sort(UltimasOpciones!.Sort)["NotificationDate"].ToInt32().Should().Be(-1);
        }

        [Fact]
        public async Task GetAllAsync_SinDocumentos_RetornaListaVacia()
        {
            ConfigurarFind();

            var resultado = await _sut.GetAllAsync(CancellationToken.None);

            resultado.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllAsync_MapeaTodosLosCamposDelDocumentoAlDominio()
        {
            ConfigurarFind(Documento("ID-9", 9, "CLI-9"));

            var resultado = await _sut.GetAllAsync(CancellationToken.None);

            var n = resultado.Single();
            n.Id.Should().Be("ID-9");
            n.IdNotification.Should().Be(9);
            n.ClientId.Should().Be("CLI-9");
            n.Type.Should().Be(NotificationType.Comment);
            n.DocumentNumber.Should().Be("HBL-1");
            n.Title.Should().Be("Titulo");
            n.Message.Should().Be("Mensaje");
            n.MessageDate.Should().Be(Fecha);
            n.Status.Should().Be(NotificationStatus.Read);
            n.NotificationDate.Should().Be(Fecha.AddHours(1));
        }

        [Fact]
        public async Task GetByClientAsync_FiltraPorClienteYOrdenaDescendente()
        {
            ConfigurarFind(Documento("A", 1, "CLI-7"));

            var resultado = await _sut.GetByClientAsync("CLI-7", CancellationToken.None);

            resultado.Should().ContainSingle();
            MongoRender.Filter(UltimoFiltro!)["ClientId"].AsString.Should().Be("CLI-7");
            MongoRender.Sort(UltimasOpciones!.Sort)["NotificationDate"].ToInt32().Should().Be(-1);
        }

        [Fact]
        public async Task GetByClientAsync_SinDocumentos_RetornaListaVacia()
        {
            ConfigurarFind();

            var resultado = await _sut.GetByClientAsync("CLI-7", CancellationToken.None);

            resultado.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByIdAsync_ExisteElDocumento_RetornaLaNotificacionDeDominio()
        {
            ConfigurarFind(Documento("A", 4, "CLI-4"));

            var resultado = await _sut.GetByIdAsync("CLI-4", 4, CancellationToken.None);

            resultado.Should().NotBeNull();
            resultado!.IdNotification.Should().Be(4);
            var filtro = MongoRender.Filter(UltimoFiltro!).ToString();
            filtro.Should().Contain("CLI-4");
            filtro.Should().Contain("IdNotification");
        }

        [Fact]
        public async Task GetByIdAsync_NoExisteElDocumento_RetornaNull()
        {
            ConfigurarFind();

            var resultado = await _sut.GetByIdAsync("CLI-4", 4, CancellationToken.None);

            resultado.Should().BeNull();
        }

        [Fact]
        public async Task MarkAsReadAsync_CuandoHayCoincidencia_RetornaTrueYMarcaLeida()
        {
            FilterDefinition<NotificationDocument>? filtro = null;
            UpdateDefinition<NotificationDocument>? actualizacion = null;
            _collectionMock
                .Setup(c => c.UpdateOneAsync(It.IsAny<FilterDefinition<NotificationDocument>>(), It.IsAny<UpdateDefinition<NotificationDocument>>(), It.IsAny<UpdateOptions>(), It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<NotificationDocument>, UpdateDefinition<NotificationDocument>, UpdateOptions, CancellationToken>((f, u, _, _) => { filtro = f; actualizacion = u; })
                .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

            var resultado = await _sut.MarkAsReadAsync("CLI-1", 3, CancellationToken.None);

            resultado.Should().BeTrue();
            MongoRender.Filter(filtro!).ToString().Should().Contain("CLI-1").And.Contain("IdNotification");
            MongoRender.Update(actualizacion!).ToString().Should().Contain("Status").And.Contain("Read");
        }

        [Fact]
        public async Task MarkAsReadAsync_SinCoincidencias_RetornaFalse()
        {
            _collectionMock
                .Setup(c => c.UpdateOneAsync(It.IsAny<FilterDefinition<NotificationDocument>>(), It.IsAny<UpdateDefinition<NotificationDocument>>(), It.IsAny<UpdateOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));

            var resultado = await _sut.MarkAsReadAsync("CLI-1", 3, CancellationToken.None);

            resultado.Should().BeFalse();
        }

        [Fact]
        public async Task MarkAsReadAsync_CuandoYaEstabaLeida_RetornaTrueAunqueNoSeModifique()
        {
            _collectionMock
                .Setup(c => c.UpdateOneAsync(It.IsAny<FilterDefinition<NotificationDocument>>(), It.IsAny<UpdateDefinition<NotificationDocument>>(), It.IsAny<UpdateOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UpdateResult.Acknowledged(1, 0, null));

            var resultado = await _sut.MarkAsReadAsync("CLI-1", 3, CancellationToken.None);

            resultado.Should().BeTrue();
        }
    }
}
