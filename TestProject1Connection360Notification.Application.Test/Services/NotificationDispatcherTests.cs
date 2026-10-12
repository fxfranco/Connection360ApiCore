using Connection360Notification.Application.Ports.Output;
using Connection360Notification.Application.Services;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace Connection360Notification.Application.Tests.Services
{
    public class NotificationDispatcherTests
    {
        private readonly Mock<INotificationChannelRegistry> _registry = new();

        private static Mock<INotificationChannel> Canal(NotificationChannelType tipo)
        {
            var mock = new Mock<INotificationChannel>();
            mock.SetupGet(c => c.Channel).Returns(tipo);
            mock.Setup(c => c.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            return mock;
        }

        private static NotificationMessage Notificacion(NotificationChannelPreferences? canales = null) =>
            new("CLI-1", NotificationType.Comment, "Mensaje", title: "Titulo", notificationChannel: canales);

        private NotificationDispatcher Crear(params INotificationChannel[] canales)
        {
            _registry.Setup(r => r.Resolve(It.IsAny<NotificationChannelPreferences?>())).Returns(canales);
            return new NotificationDispatcher(_registry.Object);
        }

        [Fact]
        public async Task DispatchAsync_ResuelveLosCanalesConLaConfiguracionDeLaNotificacion()
        {
            var canales = new NotificationChannelPreferences { Email = true };
            var notificacion = Notificacion(canales);
            var sut = Crear();

            await sut.DispatchAsync(notificacion, CancellationToken.None);

            _registry.Verify(r => r.Resolve(canales), Times.Once);
        }

        [Fact]
        public async Task DispatchAsync_EnviaLaMismaNotificacionPorCadaCanalResuelto()
        {
            var app = Canal(NotificationChannelType.Application);
            var email = Canal(NotificationChannelType.Email);
            var sms = Canal(NotificationChannelType.TextMessages);
            var notificacion = Notificacion();
            var sut = Crear(app.Object, email.Object, sms.Object);

            await sut.DispatchAsync(notificacion, CancellationToken.None);

            foreach (var canal in new[] { app, email, sms })
                canal.Verify(c => c.SendAsync(notificacion, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DispatchAsync_PropagaElTokenDeCancelacionACadaCanal()
        {
            using var cts = new CancellationTokenSource();
            var app = Canal(NotificationChannelType.Application);
            var email = Canal(NotificationChannelType.Email);
            var sut = Crear(app.Object, email.Object);

            await sut.DispatchAsync(Notificacion(), cts.Token);

            app.Verify(c => c.SendAsync(It.IsAny<NotificationMessage>(), cts.Token), Times.Once);
            email.Verify(c => c.SendAsync(It.IsAny<NotificationMessage>(), cts.Token), Times.Once);
        }

        [Fact]
        public async Task DispatchAsync_SinCanalesResueltos_NoHaceNadaNiLanza()
        {
            var sut = Crear();

            Func<Task> act = () => sut.DispatchAsync(Notificacion(), CancellationToken.None);

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task DispatchAsync_UnCanalFalla_LosDemasIgualSeIntentanYSePropagaLaExcepcionOriginal()
        {
            var error = new InvalidOperationException("correo caido");
            var app = Canal(NotificationChannelType.Application);
            var email = Canal(NotificationChannelType.Email);
            email.Setup(c => c.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>())).ThrowsAsync(error);
            var sms = Canal(NotificationChannelType.TextMessages);
            var sut = Crear(app.Object, email.Object, sms.Object);

            Func<Task> act = () => sut.DispatchAsync(Notificacion(), CancellationToken.None);

            (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(error);
            app.Verify(c => c.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Once);
            sms.Verify(c => c.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DispatchAsync_ElCanalFallaDeFormaSincrona_TambienSeAisla()
        {
            var app = Canal(NotificationChannelType.Application);
            var email = Canal(NotificationChannelType.Email);
            email.Setup(c => c.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>())).Throws(new ArgumentException("sincrona"));
            var sut = Crear(email.Object, app.Object);

            Func<Task> act = () => sut.DispatchAsync(Notificacion(), CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("sincrona*");
            app.Verify(c => c.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DispatchAsync_VariosCanalesFallan_LanzaAggregateExceptionConTodosLosErrores()
        {
            var e1 = new InvalidOperationException("e1");
            var e2 = new TimeoutException("e2");
            var app = Canal(NotificationChannelType.Application);
            app.Setup(c => c.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>())).ThrowsAsync(e1);
            var email = Canal(NotificationChannelType.Email);
            email.Setup(c => c.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>())).ThrowsAsync(e2);
            var sms = Canal(NotificationChannelType.TextMessages);
            var sut = Crear(app.Object, email.Object, sms.Object);

            Func<Task> act = () => sut.DispatchAsync(Notificacion(), CancellationToken.None);

            var aggregate = (await act.Should().ThrowAsync<AggregateException>()).Which;
            aggregate.InnerExceptions.Should().BeEquivalentTo(new Exception[] { e1, e2 });
            sms.Verify(c => c.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DispatchAsync_LaCancelacionDeUnCanal_SePropagaComoOperationCanceledException()
        {
            var app = Canal(NotificationChannelType.Application);
            app.Setup(c => c.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>())).ThrowsAsync(new OperationCanceledException());
            var sut = Crear(app.Object);

            Func<Task> act = () => sut.DispatchAsync(Notificacion(), CancellationToken.None);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task DispatchAsync_LosCanalesSeEnvianEnParalelo_UnoLentoNoBloqueaAlOtro()
        {
            var lento = new TaskCompletionSource();
            var app = Canal(NotificationChannelType.Application);
            app.Setup(c => c.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>())).Returns(lento.Task);
            var llegoElCorreo = new TaskCompletionSource();
            var email = Canal(NotificationChannelType.Email);
            email.Setup(c => c.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .Returns(() => { llegoElCorreo.SetResult(); return Task.CompletedTask; });
            var sut = Crear(app.Object, email.Object);

            Task envio = sut.DispatchAsync(Notificacion(), CancellationToken.None);

            await llegoElCorreo.Task.WaitAsync(TimeSpan.FromSeconds(10));
            envio.IsCompleted.Should().BeFalse("el canal lento aún no termina");
            lento.SetResult();
            await envio.WaitAsync(TimeSpan.FromSeconds(10));
        }

        [Fact]
        public async Task DispatchAsync_ConNotificacionNula_LanzaArgumentNullException()
        {
            var sut = Crear();

            Func<Task> act = () => sut.DispatchAsync(null!, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task DispatchAsync_ConElRegistroReal_SinCanalesConfiguradosEnviaSoloPorAplicacion()
        {
            var app = Canal(NotificationChannelType.Application);
            var email = Canal(NotificationChannelType.Email);
            var sms = Canal(NotificationChannelType.TextMessages);
            var sut = new NotificationDispatcher(new NotificationChannelRegistry(new[] { app.Object, email.Object, sms.Object }));

            await sut.DispatchAsync(Notificacion(), CancellationToken.None);

            app.Verify(c => c.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Once);
            email.Verify(c => c.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Never);
            sms.Verify(c => c.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DispatchAsync_ConElRegistroReal_SoloPorLosCanalesHabilitados()
        {
            var app = Canal(NotificationChannelType.Application);
            var email = Canal(NotificationChannelType.Email);
            var sms = Canal(NotificationChannelType.TextMessages);
            var sut = new NotificationDispatcher(new NotificationChannelRegistry(new[] { app.Object, email.Object, sms.Object }));

            await sut.DispatchAsync(Notificacion(new NotificationChannelPreferences { Email = true, TextMessages = true }), CancellationToken.None);

            app.Verify(c => c.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Never);
            email.Verify(c => c.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Once);
            sms.Verify(c => c.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
