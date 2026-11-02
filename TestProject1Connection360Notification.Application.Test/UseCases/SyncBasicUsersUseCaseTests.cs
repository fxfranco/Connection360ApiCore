using Connection360Notification.Application.DTOs;
using Connection360Notification.Application.UseCases;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Ports.Outbound;
using FluentAssertions;
using Moq;
using Xunit;

namespace Connection360Notification.Application.Tests.UseCases
{
    public class SyncBasicUsersUseCaseTests
    {
        private readonly Mock<IBasicUserDataRepository> _repositoryMock = new();
        private readonly SyncBasicUsersUseCase _sut;

        public SyncBasicUsersUseCaseTests()
        {
            _sut = new SyncBasicUsersUseCase(_repositoryMock.Object);
        }

        private static BasicUserItem Usuario(String id, String document = "900123", String fullName = "Ana Perez", String phone = "+573001112233", String email = "a@b.com", String roleName = "")
        {
            return new BasicUserItem { Id = id, Document = document, FullName = fullName, Phone = phone, Email = email, RoleName = roleName };
        }

        private static BasicUsersSyncMessage Mensaje(Guid syncId, Int32 batchIndex, Int32 batchCount, Int32 totalUsers, params BasicUserItem[] users)
        {
            return new BasicUsersSyncMessage
            {
                SyncId = syncId,
                BatchIndex = batchIndex,
                BatchCount = batchCount,
                TotalUsers = totalUsers,
                GeneratedAtUtc = DateTime.UtcNow,
                Users = users.ToList()
            };
        }

        [Fact]
        public async Task ExecuteAsync_MensajeNulo_LanzaArgumentNullException()
        {
            Func<Task> act = () => _sut.ExecuteAsync(null!, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ExecuteAsync_LoteIntermedio_GuardaLosUsuariosYNoLimpia()
        {
            Guid syncId = Guid.NewGuid();
            IReadOnlyCollection<BasicUserData>? guardados = null;
            _repositoryMock
                .Setup(r => r.UpsertBatchAsync(It.IsAny<IReadOnlyCollection<BasicUserData>>(), syncId, It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyCollection<BasicUserData>, Guid, CancellationToken>((users, _, _) => guardados = users)
                .Returns(Task.CompletedTask);

            BasicUsersSyncResult result = await _sut.ExecuteAsync(Mensaje(syncId, 0, 2, 3, Usuario("auth0|1"), Usuario("auth0|2")), CancellationToken.None);

            guardados.Should().BeEquivalentTo(new[]
            {
                new BasicUserData("auth0|1", "900123", "Ana Perez", "+573001112233", "a@b.com"),
                new BasicUserData("auth0|2", "900123", "Ana Perez", "+573001112233", "a@b.com")
            });
            result.Upserted.Should().Be(2);
            result.Discarded.Should().Be(0);
            result.IsLastBatch.Should().BeFalse();
            result.CleanupPerformed.Should().BeFalse();
            _repositoryMock.Verify(r => r.CountBySyncAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _repositoryMock.Verify(r => r.DeleteNotInSyncAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_UltimoLoteCompleto_EliminaLosObsoletos()
        {
            Guid syncId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.CountBySyncAsync(syncId, It.IsAny<CancellationToken>())).ReturnsAsync(3L);
            _repositoryMock.Setup(r => r.DeleteNotInSyncAsync(syncId, It.IsAny<CancellationToken>())).ReturnsAsync(4L);

            BasicUsersSyncResult result = await _sut.ExecuteAsync(Mensaje(syncId, 1, 2, 3, Usuario("auth0|3")), CancellationToken.None);

            result.IsLastBatch.Should().BeTrue();
            result.CleanupPerformed.Should().BeTrue();
            result.Deleted.Should().Be(4);
            result.StoredInSync.Should().Be(3);
            _repositoryMock.Verify(r => r.DeleteNotInSyncAsync(syncId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_UltimoLoteConSincronizacionIncompleta_NoEliminaNada()
        {
            Guid syncId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.CountBySyncAsync(syncId, It.IsAny<CancellationToken>())).ReturnsAsync(2L);

            BasicUsersSyncResult result = await _sut.ExecuteAsync(Mensaje(syncId, 1, 2, 3, Usuario("auth0|3")), CancellationToken.None);

            result.IsLastBatch.Should().BeTrue();
            result.CleanupPerformed.Should().BeFalse();
            result.Deleted.Should().Be(0);
            result.StoredInSync.Should().Be(2);
            _repositoryMock.Verify(r => r.DeleteNotInSyncAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_LoteUnico_EsElUltimoYLimpia()
        {
            Guid syncId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.CountBySyncAsync(syncId, It.IsAny<CancellationToken>())).ReturnsAsync(1L);
            _repositoryMock.Setup(r => r.DeleteNotInSyncAsync(syncId, It.IsAny<CancellationToken>())).ReturnsAsync(0L);

            BasicUsersSyncResult result = await _sut.ExecuteAsync(Mensaje(syncId, 0, 1, 1, Usuario("auth0|1")), CancellationToken.None);

            result.IsLastBatch.Should().BeTrue();
            result.CleanupPerformed.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_BatchCountEnCero_NuncaLimpia()
        {
            BasicUsersSyncResult result = await _sut.ExecuteAsync(Mensaje(Guid.NewGuid(), 0, 0, 1, Usuario("auth0|1")), CancellationToken.None);

            result.IsLastBatch.Should().BeFalse();
            _repositoryMock.Verify(r => r.DeleteNotInSyncAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_IdsRepetidosEnElLote_GuardaSoloUnoPorId()
        {
            IReadOnlyCollection<BasicUserData>? guardados = null;
            _repositoryMock
                .Setup(r => r.UpsertBatchAsync(It.IsAny<IReadOnlyCollection<BasicUserData>>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyCollection<BasicUserData>, Guid, CancellationToken>((users, _, _) => guardados = users)
                .Returns(Task.CompletedTask);

            BasicUsersSyncResult result = await _sut.ExecuteAsync(
                Mensaje(Guid.NewGuid(), 0, 2, 5, Usuario("auth0|1", fullName: "Nombre Viejo"), Usuario(" auth0|1 ", fullName: "Nombre Nuevo"), Usuario("auth0|2")),
                CancellationToken.None);

            guardados.Should().HaveCount(2);
            guardados!.Single(u => u.Id == "auth0|1").FullName.Should().Be("Nombre Nuevo");
            result.Upserted.Should().Be(2);
        }

        [Fact]
        public async Task ExecuteAsync_UsuariosSinId_SeDescartan()
        {
            IReadOnlyCollection<BasicUserData>? guardados = null;
            _repositoryMock
                .Setup(r => r.UpsertBatchAsync(It.IsAny<IReadOnlyCollection<BasicUserData>>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyCollection<BasicUserData>, Guid, CancellationToken>((users, _, _) => guardados = users)
                .Returns(Task.CompletedTask);

            BasicUsersSyncResult result = await _sut.ExecuteAsync(
                Mensaje(Guid.NewGuid(), 0, 2, 4, Usuario("auth0|1"), Usuario(""), Usuario("   "), null!),
                CancellationToken.None);

            guardados.Should().ContainSingle().Which.Id.Should().Be("auth0|1");
            result.Upserted.Should().Be(1);
            result.Discarded.Should().Be(3);
        }

        [Fact]
        public async Task ExecuteAsync_CamposNulos_SeGuardanComoCadenaVacia()
        {
            IReadOnlyCollection<BasicUserData>? guardados = null;
            _repositoryMock
                .Setup(r => r.UpsertBatchAsync(It.IsAny<IReadOnlyCollection<BasicUserData>>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyCollection<BasicUserData>, Guid, CancellationToken>((users, _, _) => guardados = users)
                .Returns(Task.CompletedTask);
            var usuario = new BasicUserItem { Id = "auth0|1", Document = null!, FullName = null!, Phone = null!, Email = null! };

            await _sut.ExecuteAsync(Mensaje(Guid.NewGuid(), 0, 2, 2, usuario), CancellationToken.None);

            guardados.Should().ContainSingle().Which.Should().Be(new BasicUserData("auth0|1", String.Empty, String.Empty, String.Empty, String.Empty));
        }

        [Fact]
        public async Task ExecuteAsync_ConRolDelUsuario_LoGuardaEnElUsuarioBasico()
        {
            IReadOnlyCollection<BasicUserData>? guardados = null;
            _repositoryMock
                .Setup(r => r.UpsertBatchAsync(It.IsAny<IReadOnlyCollection<BasicUserData>>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyCollection<BasicUserData>, Guid, CancellationToken>((users, _, _) => guardados = users)
                .Returns(Task.CompletedTask);

            await _sut.ExecuteAsync(
                Mensaje(Guid.NewGuid(), 0, 2, 3, Usuario("auth0|1", roleName: "ADMIN"), Usuario("auth0|2", roleName: "CLIENT"), Usuario("auth0|3")),
                CancellationToken.None);

            guardados!.Select(u => (u.Id, u.RoleName)).Should().Equal(("auth0|1", "ADMIN"), ("auth0|2", "CLIENT"), ("auth0|3", String.Empty));
        }

        [Fact]
        public async Task ExecuteAsync_ConRolNulo_SeGuardaComoCadenaVacia()
        {
            IReadOnlyCollection<BasicUserData>? guardados = null;
            _repositoryMock
                .Setup(r => r.UpsertBatchAsync(It.IsAny<IReadOnlyCollection<BasicUserData>>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyCollection<BasicUserData>, Guid, CancellationToken>((users, _, _) => guardados = users)
                .Returns(Task.CompletedTask);
            var usuario = new BasicUserItem { Id = "auth0|1", RoleName = null! };

            await _sut.ExecuteAsync(Mensaje(Guid.NewGuid(), 0, 2, 2, usuario), CancellationToken.None);

            guardados.Should().ContainSingle().Which.RoleName.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteAsync_UsuarioRepetidoEnElLote_ConservaElRolDeLaUltimaAparicion()
        {
            IReadOnlyCollection<BasicUserData>? guardados = null;
            _repositoryMock
                .Setup(r => r.UpsertBatchAsync(It.IsAny<IReadOnlyCollection<BasicUserData>>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyCollection<BasicUserData>, Guid, CancellationToken>((users, _, _) => guardados = users)
                .Returns(Task.CompletedTask);

            await _sut.ExecuteAsync(
                Mensaje(Guid.NewGuid(), 0, 2, 2, Usuario("auth0|1", roleName: "CLIENT"), Usuario("auth0|1", roleName: "ADMIN")),
                CancellationToken.None);

            guardados.Should().ContainSingle().Which.RoleName.Should().Be("ADMIN");
        }

        [Fact]
        public async Task ExecuteAsync_LoteSinUsuarios_NoLlamaAlUpsert()
        {
            await _sut.ExecuteAsync(Mensaje(Guid.NewGuid(), 0, 2, 2), CancellationToken.None);

            _repositoryMock.Verify(r => r.UpsertBatchAsync(It.IsAny<IReadOnlyCollection<BasicUserData>>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_UsersNulo_SeTrataComoLoteVacio()
        {
            var message = new BasicUsersSyncMessage { SyncId = Guid.NewGuid(), BatchIndex = 0, BatchCount = 2, TotalUsers = 1, Users = null! };

            BasicUsersSyncResult result = await _sut.ExecuteAsync(message, CancellationToken.None);

            result.Upserted.Should().Be(0);
            _repositoryMock.Verify(r => r.UpsertBatchAsync(It.IsAny<IReadOnlyCollection<BasicUserData>>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_ErrorDelRepositorio_SePropaga()
        {
            _repositoryMock
                .Setup(r => r.UpsertBatchAsync(It.IsAny<IReadOnlyCollection<BasicUserData>>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("mongo caído"));

            Func<Task> act = () => _sut.ExecuteAsync(Mensaje(Guid.NewGuid(), 0, 1, 1, Usuario("auth0|1")), CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("mongo caído");
        }

        [Fact]
        public async Task ExecuteAsync_EnviaElSyncIdDelMensajeAlRepositorio()
        {
            Guid syncId = Guid.NewGuid();

            await _sut.ExecuteAsync(Mensaje(syncId, 0, 2, 1, Usuario("auth0|1")), CancellationToken.None);

            _repositoryMock.Verify(r => r.UpsertBatchAsync(It.IsAny<IReadOnlyCollection<BasicUserData>>(), syncId, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
