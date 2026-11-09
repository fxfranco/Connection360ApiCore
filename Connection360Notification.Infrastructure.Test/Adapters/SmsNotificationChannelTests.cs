using Connection360Notification.Application.Services;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Enums;
using Connection360Notification.Infrastructure.Adapters.Output;
using Connection360Notification.Infrastructure.Sms;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Adapters
{
    public class SmsNotificationChannelTests
    {
        private sealed class ListLogger<T> : ILogger<T>
        {
            public List<(LogLevel Level, String Message)> Entries { get; } = new();
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public Boolean IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, String> formatter)
                => Entries.Add((logLevel, formatter(state, exception)));
        }

        private readonly ListLogger<SmsNotificationChannel> _logger = new();
        private readonly Mock<ISmsSender> _sender = new();
        private readonly NotificationRecipientContext _context = new();
        private readonly List<(SmsMessage Message, CancellationToken Token)> _enviados = new();

        public SmsNotificationChannelTests()
        {
            _sender.Setup(s => s.SendAsync(It.IsAny<SmsMessage>(), It.IsAny<CancellationToken>()))
                .Callback<SmsMessage, CancellationToken>((m, t) => _enviados.Add((m, t)))
                .ReturnsAsync("9001");
        }

        private static BrevoSmsSettings Configuracion(Action<BrevoSmsSettings>? ajustar = null)
        {
            var settings = new BrevoSmsSettings
            {
                ApiKey = "api-key-de-prueba",
                Sender = "Conexion360",
                Conexion360Url = "https://conexion360.space"
            };
            ajustar?.Invoke(settings);
            return settings;
        }

        private SmsNotificationChannel Crear(BrevoSmsSettings? settings = null) =>
            new(_sender.Object, Options.Create(settings ?? Configuracion()), _context, _logger);

        private static BasicUserData Usuario(String phone = "3113232323") =>
            new("auth0|1", "CLI-7", "Ana Perez", phone, "ana@correo.com");

        private static NotificationMessage Notificacion(NotificationType tipo = NotificationType.ChangeState, String documento = "HBL-123", String nuevoEstado = "En puerto") =>
            new("CLI-7", tipo, "Contenido confidencial", documentNumber: documento, title: "Titulo visible", id: "N-1", nuevoEstado: nuevoEstado);

        [Fact]
        public void Channel_EsTextMessages()
        {
            Crear().Channel.Should().Be(NotificationChannelType.TextMessages);
        }

        [Fact]
        public async Task SendAsync_EnviaAlCelularDelClienteConIndicativoDePaisYElTextoDelCambioDeEstado()
        {
            _context.Set(Usuario("3113232323"));

            await Crear().SendAsync(Notificacion(), CancellationToken.None);

            var (message, _) = _enviados.Should().ContainSingle().Subject;
            message.Recipient.Should().Be("573113232323");
            message.Content.Should().Be(
                "Conexión360\n"
                + "Tu envío internacional con número de guía HBL-123 ha actualizado su estado a En puerto. Consulta los detalles y seguimiento en nuestro sitio web.\n"
                + "“Visibilidad total. Decisiones inteligentes”");
        }

        [Fact]
        public async Task SendAsync_ConTipoOtherInfo_UsaElTextoDeInformacionActualizada()
        {
            _context.Set(Usuario());

            await Crear().SendAsync(Notificacion(NotificationType.OtherInfo), CancellationToken.None);

            _enviados.Single().Message.Content.Should().Be(
                "Conexión360\n"
                + "Tu envío internacional con número de guía HBL-123 ha actualizado su información. Consulta los detalles y seguimiento en nuestro sitio web.\n"
                + "“Visibilidad total. Decisiones inteligentes”");
        }

        [Fact]
        public async Task SendAsync_ConTipoComment_UsaElTextoDeComentarioRegistrado()
        {
            _context.Set(Usuario());

            await Crear().SendAsync(Notificacion(NotificationType.Comment), CancellationToken.None);

            _enviados.Single().Message.Content.Should().Be(
                "Conexión360\n"
                + "Tu envío internacional con número de guía HBL-123 se le ha registrado un comentario. Consulta los detalles y seguimiento en nuestro sitio web.\n"
                + "“Visibilidad total. Decisiones inteligentes”");
        }

        [Fact]
        public async Task SendAsync_UsaElIndicativoPorDefectoConfigurado()
        {
            _context.Set(Usuario("3001112233"));

            await Crear(Configuracion(s => s.DefaultCountryCode = "1")).SendAsync(Notificacion(), CancellationToken.None);

            _enviados.Single().Message.Recipient.Should().Be("13001112233");
        }

        [Fact]
        public async Task SendAsync_ConCelularEnFormatoInternacional_LoRespeta()
        {
            _context.Set(Usuario("+34 612 345 678"));

            await Crear().SendAsync(Notificacion(), CancellationToken.None);

            _enviados.Single().Message.Recipient.Should().Be("34612345678");
        }

        [Fact]
        public async Task SendAsync_PasaElTokenDeCancelacionAlEnviador()
        {
            _context.Set(Usuario());
            using var cts = new CancellationTokenSource();

            await Crear().SendAsync(Notificacion(), cts.Token);

            _enviados.Single().Token.Should().Be(cts.Token);
        }

        [Fact]
        public async Task SendAsync_RegistraElEnvioSinElCelularNiElContenido()
        {
            _context.Set(Usuario("3113232323"));

            await Crear().SendAsync(Notificacion(), CancellationToken.None);

            var entrada = _logger.Entries.Should().ContainSingle().Subject;
            entrada.Level.Should().Be(LogLevel.Information);
            entrada.Message.Should().Contain("[SMS]").And.Contain("CLI-7").And.Contain("N-1").And.Contain("9001");
            entrada.Message.Should().NotContain("3113232323").And.NotContain("Contenido confidencial").And.NotContain("HBL-123");
        }

        [Fact]
        public async Task SendAsync_SinDestinatarioEnBasicUserData_NoEnviaYAdvierteSinLanzar()
        {
            _context.Set(null);

            await Crear().SendAsync(Notificacion(), CancellationToken.None);

            _sender.Verify(s => s.SendAsync(It.IsAny<SmsMessage>(), It.IsAny<CancellationToken>()), Times.Never);
            var entrada = _logger.Entries.Should().ContainSingle().Subject;
            entrada.Level.Should().Be(LogLevel.Warning);
            entrada.Message.Should().Contain("CLI-7").And.Contain("no existe en basicUserData");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SendAsync_ConClienteSinCelular_NoEnviaYAdvierte(String phone)
        {
            _context.Set(Usuario(phone));

            await Crear().SendAsync(Notificacion(), CancellationToken.None);

            _sender.Verify(s => s.SendAsync(It.IsAny<SmsMessage>(), It.IsAny<CancellationToken>()), Times.Never);
            _logger.Entries.Should().ContainSingle().Which.Message.Should().Contain("no tiene celular registrado");
        }

        [Fact]
        public async Task SendAsync_ConCelularInvalido_NoEnviaYAdvierteSinMostrarElNumero()
        {
            _context.Set(Usuario("abc123"));

            await Crear().SendAsync(Notificacion(), CancellationToken.None);

            _sender.Verify(s => s.SendAsync(It.IsAny<SmsMessage>(), It.IsAny<CancellationToken>()), Times.Never);
            var entrada = _logger.Entries.Should().ContainSingle().Subject;
            entrada.Message.Should().Contain("no es válido").And.NotContain("abc123");
        }

        [Fact]
        public async Task SendAsync_SinApiKey_LanzaInvalidOperationExceptionSinEnviar()
        {
            _context.Set(Usuario());

            Func<Task> act = () => Crear(Configuracion(s => s.ApiKey = " ")).SendAsync(Notificacion(), CancellationToken.None);

            (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Message.Should().Contain("ApiKey").And.Contain("BrevoSettingsSMS");
            _sender.Verify(s => s.SendAsync(It.IsAny<SmsMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SendAsync_SinRemitente_LanzaInvalidOperationExceptionSinEnviar()
        {
            _context.Set(Usuario());

            Func<Task> act = () => Crear(Configuracion(s => s.Sender = String.Empty)).SendAsync(Notificacion(), CancellationToken.None);

            (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Message.Should().Contain("Sender");
            _sender.Verify(s => s.SendAsync(It.IsAny<SmsMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SendAsync_SiBrevoFalla_PropagaLaExcepcionParaQueElDespachadorLaAisle()
        {
            _context.Set(Usuario());
            _sender.Setup(s => s.SendAsync(It.IsAny<SmsMessage>(), It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException("rechazado"));

            Func<Task> act = () => Crear().SendAsync(Notificacion(), CancellationToken.None);

            await act.Should().ThrowAsync<HttpRequestException>();
        }

        [Fact]
        public async Task SendAsync_ConNotificacionNula_LanzaArgumentNullException()
        {
            Func<Task> act = () => Crear().SendAsync(null!, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }
    }
}
