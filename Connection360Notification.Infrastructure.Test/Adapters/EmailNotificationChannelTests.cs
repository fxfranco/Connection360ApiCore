using Connection360Notification.Application.Ports.Output;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Enums;
using Connection360Notification.Infrastructure.Adapters.Output;
using Connection360Notification.Infrastructure.Email;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Adapters
{
    public class EmailNotificationChannelTests
    {
        private sealed class ListLogger<T> : ILogger<T>
        {
            public List<(LogLevel Level, String Message)> Entries { get; } = new();
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public Boolean IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, String> formatter)
                => Entries.Add((logLevel, formatter(state, exception)));
        }

        private readonly ListLogger<EmailNotificationChannel> _logger = new();
        private readonly Mock<IEmailSender> _sender = new();
        private readonly List<(EmailMessage Message, CancellationToken Token)> _enviados = new();

        public EmailNotificationChannelTests()
        {
            _sender.Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
                .Callback<EmailMessage, CancellationToken>((m, t) => _enviados.Add((m, t)))
                .Returns(Task.CompletedTask);
        }

        private static SmtpSettings Configuracion(Action<SmtpSettings>? ajustar = null)
        {
            var settings = new SmtpSettings
            {
                Server = "smtp.gmail.com",
                Port = 587,
                SenderName = "Mi Aplicación",
                SenderEmail = "remitente@gmail.com",
                Password = "clave-de-aplicacion",
                RecipientEmail = "destino@ejemplo.com"
            };
            ajustar?.Invoke(settings);
            return settings;
        }

        private EmailNotificationChannel Crear(SmtpSettings? settings = null) =>
            new(_sender.Object, Options.Create(settings ?? Configuracion()), _logger);

        private static NotificationMessage Notificacion(String? title = "Titulo visible", String message = "Contenido confidencial") =>
            new("CLI-7", NotificationType.Comment, message, title: title, id: "N-1");

        [Fact]
        public void Channel_EsEmail()
        {
            Crear().Channel.Should().Be(NotificationChannelType.Email);
        }

        [Fact]
        public void EsUnCanalDeNotificacion()
        {
            Crear().Should().BeAssignableTo<INotificationChannel>();
        }

        [Fact]
        public async Task SendAsync_UsaElTituloComoAsuntoYElMensajeComoCuerpo()
        {
            await Crear().SendAsync(Notificacion("Nuevo comentario", "Tienes un comentario nuevo"), CancellationToken.None);

            var correo = _enviados.Should().ContainSingle().Subject.Message;
            correo.Subject.Should().Be("Nuevo comentario");
            correo.Body.Should().Be("Tienes un comentario nuevo");
        }

        [Fact]
        public async Task SendAsync_EnviaSiempreAlDestinatarioConfigurado()
        {
            var channel = Crear(Configuracion(s => s.RecipientEmail = "fijo@ejemplo.com"));

            await channel.SendAsync(Notificacion(), CancellationToken.None);
            await channel.SendAsync(new NotificationMessage("OTRO-CLIENTE", NotificationType.Comment, "m", title: "t", id: "N-2"), CancellationToken.None);

            _enviados.Select(e => e.Message.To).Should().OnlyContain(to => to == "fijo@ejemplo.com").And.HaveCount(2);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SendAsync_SinTitulo_UsaElAsuntoPorDefecto(String? titulo)
        {
            await Crear().SendAsync(Notificacion(titulo), CancellationToken.None);

            _enviados.Should().ContainSingle().Which.Message.Subject.Should().Be(EmailNotificationChannel.DefaultSubject);
        }

        [Fact]
        public async Task SendAsync_ReenviaElTokenDeCancelacionAlRemitente()
        {
            using var cts = new CancellationTokenSource();

            await Crear().SendAsync(Notificacion(), cts.Token);

            _enviados.Should().ContainSingle().Which.Token.Should().Be(cts.Token);
        }

        [Fact]
        public async Task SendAsync_RegistraElEnvioConElClienteSinElDestinatarioNiElContenido()
        {
            await Crear().SendAsync(Notificacion(), CancellationToken.None);

            var entrada = _logger.Entries.Should().ContainSingle().Subject;
            entrada.Level.Should().Be(LogLevel.Information);
            entrada.Message.Should().Contain("CLI-7").And.Contain("N-1");
            entrada.Message.Should().NotContain("destino@ejemplo.com").And.NotContain("Contenido confidencial");
        }

        [Fact]
        public async Task SendAsync_SiElRemitenteFalla_PropagaLaExcepcionYNoRegistraExito()
        {
            _sender.Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("SMTP caído"));

            Func<Task> act = () => Crear().SendAsync(Notificacion(), CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("SMTP caído");
            _logger.Entries.Should().BeEmpty();
        }

        [Fact]
        public async Task SendAsync_ConNotificacionNula_LanzaArgumentNullException()
        {
            Func<Task> act = () => Crear().SendAsync(null!, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentNullException>();
            _enviados.Should().BeEmpty();
        }

        [Theory]
        [InlineData(nameof(SmtpSettings.Server))]
        [InlineData(nameof(SmtpSettings.SenderEmail))]
        [InlineData(nameof(SmtpSettings.Password))]
        [InlineData(nameof(SmtpSettings.RecipientEmail))]
        public async Task SendAsync_ConConfiguracionIncompleta_LanzaIndicandoElCampoFaltanteYNoEnvia(String campo)
        {
            var settings = Configuracion(s => typeof(SmtpSettings).GetProperty(campo)!.SetValue(s, "  "));

            Func<Task> act = () => Crear(settings).SendAsync(Notificacion(), CancellationToken.None);

            var error = await act.Should().ThrowAsync<InvalidOperationException>();
            error.Which.Message.Should().Contain(campo).And.Contain("SmtpSettings");
            _enviados.Should().BeEmpty();
        }

        [Fact]
        public async Task SendAsync_ConPuertoInvalido_LanzaIndicandoElPuerto()
        {
            Func<Task> act = () => Crear(Configuracion(s => s.Port = 0)).SendAsync(Notificacion(), CancellationToken.None);

            (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Message.Should().Contain(nameof(SmtpSettings.Port));
        }

        [Fact]
        public async Task SendAsync_SinConfiguracion_ListaTodosLosCamposFaltantes()
        {
            Func<Task> act = () => Crear(new SmtpSettings()).SendAsync(Notificacion(), CancellationToken.None);

            var mensaje = (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Message;
            mensaje.Should().Contain("Server").And.Contain("SenderEmail").And.Contain("Password").And.Contain("RecipientEmail");
            mensaje.Should().NotContain("Port", "el puerto tiene un valor por defecto válido (587)");
        }

        [Fact]
        public async Task SendAsync_ElMensajeDeConfiguracionNoRevelaLaContrasena()
        {
            var settings = Configuracion(s => { s.Server = ""; s.Password = "super-secreta"; });

            Func<Task> act = () => Crear(settings).SendAsync(Notificacion(), CancellationToken.None);

            (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Message.Should().NotContain("super-secreta");
        }
    }
}
