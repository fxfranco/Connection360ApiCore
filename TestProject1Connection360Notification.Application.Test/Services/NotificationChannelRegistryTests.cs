using Connection360Notification.Application.Ports.Output;
using Connection360Notification.Application.Services;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace Connection360Notification.Application.Tests.Services
{
    public class NotificationChannelRegistryTests
    {
        private static Mock<INotificationChannel> Canal(NotificationChannelType tipo)
        {
            var mock = new Mock<INotificationChannel>();
            mock.SetupGet(c => c.Channel).Returns(tipo);
            return mock;
        }

        private readonly Mock<INotificationChannel> _app = Canal(NotificationChannelType.Application);
        private readonly Mock<INotificationChannel> _email = Canal(NotificationChannelType.Email);
        private readonly Mock<INotificationChannel> _sms = Canal(NotificationChannelType.TextMessages);

        private NotificationChannelRegistry CrearConTodos() => new(new[] { _app.Object, _email.Object, _sms.Object });

        [Fact]
        public void Resolve_SinNingunCanalHabilitado_DevuelveSoloElCanalDeAplicacion()
        {
            var resultado = CrearConTodos().Resolve(new NotificationChannelPreferences());

            resultado.Should().Equal(_app.Object);
        }

        [Fact]
        public void Resolve_ConPreferenciasNulas_DevuelveSoloElCanalDeAplicacion()
        {
            CrearConTodos().Resolve(null).Should().Equal(_app.Object);
        }

        [Fact]
        public void Resolve_ConLosTresCanalesHabilitados_DevuelveLosTres()
        {
            var prefs = new NotificationChannelPreferences { Application = true, Email = true, TextMessages = true };

            CrearConTodos().Resolve(prefs).Should().BeEquivalentTo(new[] { _app.Object, _email.Object, _sms.Object });
        }

        [Fact]
        public void Resolve_SoloEmailHabilitado_DevuelveSoloEmailYNoAgregaAplicacion()
        {
            var resultado = CrearConTodos().Resolve(new NotificationChannelPreferences { Email = true });

            resultado.Should().Equal(_email.Object);
        }

        [Fact]
        public void Resolve_EmailYSmsHabilitados_DevuelveEseOrden()
        {
            var resultado = CrearConTodos().Resolve(new NotificationChannelPreferences { Email = true, TextMessages = true });

            resultado.Should().Equal(_email.Object, _sms.Object);
        }

        [Fact]
        public void Resolve_CanalHabilitadoSinImplementacionRegistrada_SeOmiteYSigueConLosDemas()
        {
            var registro = new NotificationChannelRegistry(new[] { _app.Object, _email.Object });

            var resultado = registro.Resolve(new NotificationChannelPreferences { Email = true, TextMessages = true });

            resultado.Should().Equal(_email.Object);
        }

        [Fact]
        public void Resolve_NingunoDeLosHabilitadosTieneImplementacion_CaeAlCanalDeAplicacion()
        {
            var registro = new NotificationChannelRegistry(new[] { _app.Object });

            var resultado = registro.Resolve(new NotificationChannelPreferences { TextMessages = true });

            resultado.Should().Equal(_app.Object);
        }

        [Fact]
        public void Resolve_SinCanalDeAplicacionRegistradoYNadaResoluble_DevuelveListaVacia()
        {
            var registro = new NotificationChannelRegistry(new[] { _email.Object });

            registro.Resolve(new NotificationChannelPreferences()).Should().BeEmpty();
        }

        [Fact]
        public void Constructor_DosImplementacionesDelMismoCanal_LanzaInvalidOperationException()
        {
            Action act = () => _ = new NotificationChannelRegistry(new[] { _app.Object, Canal(NotificationChannelType.Application).Object });

            act.Should().Throw<InvalidOperationException>().WithMessage("*Application*");
        }

        [Fact]
        public void Constructor_ConColeccionNula_LanzaArgumentNullException()
        {
            Action act = () => _ = new NotificationChannelRegistry(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Resolve_NoEnviaNadaSoloResuelve()
        {
            CrearConTodos().Resolve(new NotificationChannelPreferences { Application = true, Email = true });

            _app.Verify(c => c.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Never);
            _email.Verify(c => c.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
