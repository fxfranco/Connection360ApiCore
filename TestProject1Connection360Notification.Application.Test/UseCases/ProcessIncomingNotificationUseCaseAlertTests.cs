using Connection360Notification.Application.Services;
using Connection360Notification.Application.UseCases;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Ports.Outbound;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using DomainNotificationType = Connection360Notification.Domain.Enums.NotificationType;

namespace Connection360Notification.Application.Tests.UseCases
{
    /// <summary>
    /// Alertas del sistema (NotificationType.Alert): llegan sin ClientId. Se buscan los usuarios con rol ADMIN ANTES de guardar y, por cada
    /// uno, se guarda y se despacha una copia con su Document como ClientId; si no hay ninguno, se guarda y despacha la alerta original
    /// (ClientId vacío) una sola vez sin destinatario (el correo usa el RecipientEmail por defecto).
    /// </summary>
    public class ProcessIncomingNotificationUseCaseAlertTests
    {
        private readonly Mock<INotificationRepository> _repository = new();
        private readonly Mock<INotificationDispatcher> _dispatcher = new();
        private readonly Mock<IBasicUserDataRepository> _users = new();
        private readonly Mock<INotificationRecipientContext> _context = new();
        private readonly List<String> _calls = new();
        private readonly List<NotificationMessage> _saved = new();
        private readonly List<NotificationMessage> _dispatched = new();
        private readonly List<BasicUserData?> _recipientsAtDispatch = new();
        private BasicUserData? _currentRecipient;
        private readonly ProcessIncomingNotificationUseCase _sut;

        private static readonly BasicUserData Ana = new("auth0|1", "900", "Ana Admin", "3001112233", "ana@correo.com", "ADMIN");
        private static readonly BasicUserData Luis = new("auth0|2", "901", "Luis Admin", "3004445566", "luis@correo.com", "ADMIN");

        public ProcessIncomingNotificationUseCaseAlertTests()
        {
            _repository.Setup(r => r.SaveAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .Callback((NotificationMessage n, CancellationToken _) => { _calls.Add($"save:{n.ClientId}"); _saved.Add(n); })
                .Returns(Task.CompletedTask);
            _context.Setup(c => c.Set(It.IsAny<BasicUserData?>())).Callback((BasicUserData? r) => _currentRecipient = r);
            _dispatcher.Setup(d => d.DispatchAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .Callback((NotificationMessage n, CancellationToken _) =>
                {
                    _calls.Add($"dispatch:{n.ClientId}");
                    _dispatched.Add(n);
                    _recipientsAtDispatch.Add(_currentRecipient);
                })
                .Returns(Task.CompletedTask);
            _users.Setup(u => u.GetByRoleAsync("ADMIN", It.IsAny<CancellationToken>())).ReturnsAsync(new List<BasicUserData> { Ana, Luis });

            _sut = new ProcessIncomingNotificationUseCase(_repository.Object, _dispatcher.Object, _users.Object, _context.Object,
                NullLogger<ProcessIncomingNotificationUseCase>.Instance);
        }

        private static NotificationMessage Alerta() => new("", DomainNotificationType.Alert, "Se generó un error 'api caida' en el proceso de ETL", title: "Alerta en proceso de ETL");

        private void SinAdministradores() =>
            _users.Setup(u => u.GetByRoleAsync("ADMIN", It.IsAny<CancellationToken>())).ReturnsAsync(new List<BasicUserData>());

        // ---------- Con administradores: un registro y un envío por cada uno ----------

        [Fact]
        public async Task ExecuteAsync_Alerta_BuscaALosAdministradoresAntesDeGuardar()
        {
            var orden = new List<String>();
            _users.Setup(u => u.GetByRoleAsync("ADMIN", It.IsAny<CancellationToken>()))
                .Callback(() => orden.Add("buscar")).ReturnsAsync(new List<BasicUserData> { Ana });
            _repository.Setup(r => r.SaveAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .Callback(() => orden.Add("guardar")).Returns(Task.CompletedTask);

            await _sut.ExecuteAsync(Alerta(), CancellationToken.None);

            orden.Should().Equal("buscar", "guardar");
            _users.Verify(u => u.GetByRoleAsync(BasicUserRoles.Admin, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_Alerta_NoBuscaAlUsuarioPorDocumento()
        {
            await _sut.ExecuteAsync(Alerta(), CancellationToken.None);

            _users.Verify(u => u.GetByDocumentAsync(It.IsAny<String>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_AlertaConVariosAdministradores_GuardaUnRegistroPorCadaUnoConSuDocumentoComoClientId()
        {
            await _sut.ExecuteAsync(Alerta(), CancellationToken.None);

            _saved.Select(n => n.ClientId).Should().Equal("900", "901");
        }

        [Fact]
        public async Task ExecuteAsync_AlertaConVariosAdministradores_CadaRegistroTieneUnIdPropioDistintoAlDeLaOriginal()
        {
            NotificationMessage alerta = Alerta();

            await _sut.ExecuteAsync(alerta, CancellationToken.None);

            _saved.Select(n => n.Id).Should().OnlyHaveUniqueItems().And.NotContain(alerta.Id);
        }

        [Fact]
        public async Task ExecuteAsync_AlertaConAdministradores_NoGuardaLaAlertaOriginalSinClientId()
        {
            NotificationMessage alerta = Alerta();

            await _sut.ExecuteAsync(alerta, CancellationToken.None);

            _repository.Verify(r => r.SaveAsync(alerta, It.IsAny<CancellationToken>()), Times.Never);
            _saved.Should().OnlyContain(n => n.ClientId != String.Empty);
        }

        [Fact]
        public async Task ExecuteAsync_AlertaConAdministradores_CadaRegistroConservaElContenidoDeLaAlerta()
        {
            NotificationMessage alerta = Alerta();

            await _sut.ExecuteAsync(alerta, CancellationToken.None);

            _saved.Should().OnlyContain(n =>
                n.EventType == DomainNotificationType.Alert && n.Message == alerta.Message && n.Title == alerta.Title);
        }

        [Fact]
        public async Task ExecuteAsync_AlertaConAdministradores_NoModificaLaAlertaOriginal()
        {
            NotificationMessage alerta = Alerta();

            await _sut.ExecuteAsync(alerta, CancellationToken.None);

            alerta.ClientId.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteAsync_AlertaConVariosAdministradores_GuardaYDespachaCadaCopiaEnOrdenConSuContexto()
        {
            await _sut.ExecuteAsync(Alerta(), CancellationToken.None);

            _calls.Should().Equal("save:900", "dispatch:900", "save:901", "dispatch:901");
            _recipientsAtDispatch.Should().Equal(Ana, Luis);
        }

        [Fact]
        public async Task ExecuteAsync_AlertaConAdministradores_DespachaLasMismasCopiasQueGuardo()
        {
            await _sut.ExecuteAsync(Alerta(), CancellationToken.None);

            _dispatched.Should().Equal(_saved);
        }

        [Fact]
        public async Task ExecuteAsync_AlertaConAdministradorSinDocumento_RegistraYDespachaUnaCopiaConClientIdVacio()
        {
            _users.Setup(u => u.GetByRoleAsync("ADMIN", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<BasicUserData> { new("auth0|3", "", "Sin Doc", "300", "s@correo.com", "ADMIN"), Luis });

            await _sut.ExecuteAsync(Alerta(), CancellationToken.None);

            _saved.Select(n => n.ClientId).Should().Equal("", "901");
            _recipientsAtDispatch.Select(r => r?.FullName).Should().Equal("Sin Doc", "Luis Admin");
        }

        // ---------- Sin administradores: queda como antes (ClientId vacío) ----------

        [Fact]
        public async Task ExecuteAsync_AlertaSinAdministradores_GuardaLaAlertaOriginalConClientIdVacio()
        {
            SinAdministradores();
            NotificationMessage alerta = Alerta();

            await _sut.ExecuteAsync(alerta, CancellationToken.None);

            _repository.Verify(r => r.SaveAsync(alerta, It.IsAny<CancellationToken>()), Times.Once);
            _saved.Should().ContainSingle().Which.ClientId.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteAsync_AlertaSinAdministradores_GuardaYDespachaUnaVezSinDestinatario()
        {
            SinAdministradores();
            NotificationMessage alerta = Alerta();

            await _sut.ExecuteAsync(alerta, CancellationToken.None);

            _calls.Should().Equal("save:", "dispatch:");
            _dispatcher.Verify(d => d.DispatchAsync(alerta, It.IsAny<CancellationToken>()), Times.Once);
            _recipientsAtDispatch.Should().ContainSingle().Which.Should().BeNull();
            _context.Verify(c => c.Set(null), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_AlertaConConsultaDeAdministradoresNula_SeTrataComoSinAdministradores()
        {
            _users.Setup(u => u.GetByRoleAsync("ADMIN", It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BasicUserData>)null!);

            await _sut.ExecuteAsync(Alerta(), CancellationToken.None);

            _saved.Should().ContainSingle().Which.ClientId.Should().BeEmpty();
            _recipientsAtDispatch.Should().ContainSingle().Which.Should().BeNull();
        }

        [Fact]
        public async Task ExecuteAsync_AlertaConFalloEnLaConsultaDeAdministradores_GuardaYDespachaSinDestinatario()
        {
            _users.Setup(u => u.GetByRoleAsync("ADMIN", It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("mongo caído"));

            await _sut.ExecuteAsync(Alerta(), CancellationToken.None);

            _saved.Should().ContainSingle().Which.ClientId.Should().BeEmpty();
            _recipientsAtDispatch.Should().ContainSingle().Which.Should().BeNull();
        }

        [Fact]
        public async Task ExecuteAsync_AlertaConConsultaCancelada_PropagaLaCancelacionYNoGuardaNiDespacha()
        {
            _users.Setup(u => u.GetByRoleAsync("ADMIN", It.IsAny<CancellationToken>())).ThrowsAsync(new OperationCanceledException());

            Func<Task> act = () => _sut.ExecuteAsync(Alerta(), CancellationToken.None);

            await act.Should().ThrowAsync<OperationCanceledException>();
            _repository.Verify(r => r.SaveAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Never);
            _dispatcher.Verify(d => d.DispatchAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_AlertaSinAdministradoresDondeFallaGuardar_PropagaElErrorYNoDespacha()
        {
            SinAdministradores();
            _repository.Setup(r => r.SaveAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("mongo caído"));

            Func<Task> act = () => _sut.ExecuteAsync(Alerta(), CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("mongo caído");
            _dispatcher.Verify(d => d.DispatchAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_AlertaSinAdministradoresDondeFallaElEnvio_PropagaElError()
        {
            SinAdministradores();
            _dispatcher.Setup(d => d.DispatchAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("smtp caído"));

            Func<Task> act = () => _sut.ExecuteAsync(Alerta(), CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("smtp caído");
        }

        // ---------- Fallos con administradores ----------

        [Fact]
        public async Task ExecuteAsync_AlertaDondeFallaElEnvioAUnAdministrador_IgualProcesaALosDemasYReportaElError()
        {
            var intentos = 0;
            _dispatcher.Setup(d => d.DispatchAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .Callback((NotificationMessage n, CancellationToken _) => _recipientsAtDispatch.Add(_currentRecipient))
                .Returns(() => ++intentos == 1 ? Task.FromException(new InvalidOperationException("smtp caído")) : Task.CompletedTask);

            Func<Task> act = () => _sut.ExecuteAsync(Alerta(), CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("smtp caído");
            _saved.Select(n => n.ClientId).Should().Equal("900", "901");
            _recipientsAtDispatch.Should().Equal(Ana, Luis);
        }

        [Fact]
        public async Task ExecuteAsync_AlertaDondeFallaGuardarParaUnAdministrador_NoLoDespachaPeroProcesaALosDemas()
        {
            var intentos = 0;
            _repository.Setup(r => r.SaveAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .Callback((NotificationMessage n, CancellationToken _) => _calls.Add($"save:{n.ClientId}"))
                .Returns(() => ++intentos == 1 ? Task.FromException(new InvalidOperationException("mongo caído")) : Task.CompletedTask);

            Func<Task> act = () => _sut.ExecuteAsync(Alerta(), CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("mongo caído");
            _calls.Should().Equal("save:900", "save:901", "dispatch:901");
            _recipientsAtDispatch.Should().Equal(Luis);
        }

        [Fact]
        public async Task ExecuteAsync_AlertaDondeFallaElProcesoATodos_LanzaUnAggregateException()
        {
            _dispatcher.Setup(d => d.DispatchAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("smtp caído"));

            Func<Task> act = () => _sut.ExecuteAsync(Alerta(), CancellationToken.None);

            (await act.Should().ThrowAsync<AggregateException>()).Which.InnerExceptions.Should().HaveCount(2);
        }

        [Fact]
        public async Task ExecuteAsync_AlertaPropagaElTokenALaConsultaAlGuardadoYAlDespacho()
        {
            using var cts = new CancellationTokenSource();

            await _sut.ExecuteAsync(Alerta(), cts.Token);

            _users.Verify(u => u.GetByRoleAsync("ADMIN", cts.Token), Times.Once);
            _repository.Verify(r => r.SaveAsync(It.IsAny<NotificationMessage>(), cts.Token), Times.Exactly(2));
            _dispatcher.Verify(d => d.DispatchAsync(It.IsAny<NotificationMessage>(), cts.Token), Times.Exactly(2));
        }

        // ---------- Notificaciones de cliente: el flujo no cambia ----------

        [Theory]
        [InlineData(DomainNotificationType.ChangeState)]
        [InlineData(DomainNotificationType.Comment)]
        [InlineData(DomainNotificationType.OtherInfo)]
        public async Task ExecuteAsync_NotificacionDeCliente_NoCambiaSuFlujo(DomainNotificationType tipo)
        {
            var cliente = new BasicUserData("auth0|9", "123", "Cliente", "300", "c@correo.com");
            _users.Setup(u => u.GetByDocumentAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(cliente);
            var notificacion = new NotificationMessage("123", tipo, "m");

            await _sut.ExecuteAsync(notificacion, CancellationToken.None);

            _users.Verify(u => u.GetByRoleAsync(It.IsAny<String>(), It.IsAny<CancellationToken>()), Times.Never);
            _calls.Should().Equal("save:123", "dispatch:123");
            _repository.Verify(r => r.SaveAsync(notificacion, It.IsAny<CancellationToken>()), Times.Once);
            _recipientsAtDispatch.Should().ContainSingle().Which.Should().Be(cliente);
        }
    }
}
