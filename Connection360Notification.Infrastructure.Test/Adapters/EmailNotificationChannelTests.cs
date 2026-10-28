using Connection360Notification.Application.Ports.Output;
using Connection360Notification.Application.Services;
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
        private readonly NotificationRecipientContext _context = new();
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
            new(_sender.Object, Options.Create(settings ?? Configuracion()), _context, _logger);

        private static BasicUserData Usuario(String email = "cliente@correo.com", String fullName = "Ana Perez") =>
            new("auth0|1", "CLI-7", fullName, "3001112233", email);

        private static NotificationMessage Notificacion(String? title = "Titulo visible", String message = "Contenido confidencial") =>
            new("CLI-7", NotificationType.Comment, message, title: title!, id: "N-1");

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

        [Fact]
        public async Task SendAsync_SiElClienteTieneCorreo_EnviaAEsaDireccionEnVezDeLaConfigurada()
        {
            _context.Set(Usuario("cliente@correo.com"));

            await Crear().SendAsync(Notificacion(), CancellationToken.None);

            _enviados.Should().ContainSingle().Which.Message.To.Should().Be("cliente@correo.com");
        }

        [Fact]
        public async Task SendAsync_RecortaLosEspaciosDelCorreoDelCliente()
        {
            _context.Set(Usuario("  cliente@correo.com  "));

            await Crear().SendAsync(Notificacion(), CancellationToken.None);

            _enviados.Should().ContainSingle().Which.Message.To.Should().Be("cliente@correo.com");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SendAsync_SiElClienteNoTieneCorreo_UsaElDeLaConfiguracion(String email)
        {
            _context.Set(Usuario(email));

            await Crear().SendAsync(Notificacion(), CancellationToken.None);

            _enviados.Should().ContainSingle().Which.Message.To.Should().Be("destino@ejemplo.com");
            _logger.Entries.Should().NotContain(e => e.Level == LogLevel.Warning);
        }

        [Fact]
        public async Task SendAsync_SiNoHayDestinatarioEnElContexto_UsaElDeLaConfiguracion()
        {
            await Crear().SendAsync(Notificacion(), CancellationToken.None);

            _enviados.Should().ContainSingle().Which.Message.To.Should().Be("destino@ejemplo.com");
        }

        [Theory]
        [InlineData("no-es-un-correo")]
        [InlineData("a@")]
        [InlineData("Ana Perez <ana@correo.com>")]
        [InlineData("uno@correo.com, dos@correo.com")]
        public async Task SendAsync_SiElCorreoDelClienteNoEsValido_UsaElDeLaConfiguracionYAdvierteSinMostrarlo(String invalido)
        {
            _context.Set(Usuario(invalido));

            await Crear().SendAsync(Notificacion(), CancellationToken.None);

            _enviados.Should().ContainSingle().Which.Message.To.Should().Be("destino@ejemplo.com");
            var advertencia = _logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning).Subject;
            advertencia.Message.Should().Contain("CLI-7").And.NotContain(invalido);
        }

        [Fact]
        public async Task SendAsync_SiElClienteTieneCorreo_NoExigeElCorreoDeRespaldoEnLaConfiguracion()
        {
            _context.Set(Usuario("cliente@correo.com"));
            var settings = Configuracion(s => s.RecipientEmail = "");

            await Crear(settings).SendAsync(Notificacion(), CancellationToken.None);

            _enviados.Should().ContainSingle().Which.Message.To.Should().Be("cliente@correo.com");
        }

        [Fact]
        public async Task SendAsync_SinCorreoDelClienteNiDeRespaldo_Lanza()
        {
            _context.Set(Usuario(""));
            var settings = Configuracion(s => s.RecipientEmail = "");

            Func<Task> act = () => Crear(settings).SendAsync(Notificacion(), CancellationToken.None);

            (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Message.Should().Contain(nameof(SmtpSettings.RecipientEmail));
            _enviados.Should().BeEmpty();
        }

        [Fact]
        public async Task SendAsync_AunConCorreoDelCliente_ExigeElRestoDeLaConfiguracionSmtp()
        {
            _context.Set(Usuario("cliente@correo.com"));
            var settings = Configuracion(s => s.Password = "");

            Func<Task> act = () => Crear(settings).SendAsync(Notificacion(), CancellationToken.None);

            (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Message.Should().Contain(nameof(SmtpSettings.Password));
        }

        [Fact]
        public async Task SendAsync_CambioDeEstado_ArmaElCuerpoHtmlConLosValoresDeLaNotificacion()
        {
            _context.Set(Usuario(fullName: "Ana Perez"));
            var notificacion = new NotificationMessage("CLI-7", NotificationType.ChangeState, "m", documentNumber: "GUIA-123", title: "t",
                messageDate: new DateTime(2026, 10, 21, 15, 30, 5, DateTimeKind.Utc), id: "N-1",
                origen: "Medellin, Colombia", destino: "Miami, USA", comentario: "No debe verse", fechaComentario: new DateTime(2026, 10, 20),
                estadoAnterior: "En transito", nuevoEstado: "En puerto");
            var settings = Configuracion(s => s.Conexion360Url = "https://conexion360.space");

            await Crear(settings).SendAsync(notificacion, CancellationToken.None);

            String html = _enviados.Should().ContainSingle().Subject.Message.HtmlBody!;
            html.Should().Contain("Estimado/a Ana Perez,")
                .And.Contain("GUIA-123")
                .And.Contain("Medellin, Colombia")
                .And.Contain("Miami, USA")
                .And.Contain("<strong>Estado anterior:</strong> En transito</li>")
                .And.Contain("<strong>Nuevo estado:</strong> En puerto</li>")
                .And.Contain("el estado de su envío de carga internacional ha sido actualizado en")
                .And.NotContain("Comentario:")
                .And.NotContain("Fecha comentario:")
                .And.NotContain("No debe verse")
                .And.Contain("21/10/2026 15:30:05 UTC")
                .And.Contain("href=\"https://conexion360.space\"")
                .And.Contain("cid:logoConexion360");
            html.Should().NotContain("{{").And.NotContain("PENDIENTE");
        }

        [Fact]
        public async Task SendAsync_Comentario_MuestraEstadoComentarioYFechaDelComentario()
        {
            var notificacion = new NotificationMessage("CLI-7", NotificationType.Comment, "m", documentNumber: "GUIA-123", title: "t", id: "N-1",
                origen: "Medellin, Colombia", destino: "Miami, USA", comentario: "Carga lista", fechaComentario: new DateTime(2026, 10, 20),
                estadoAnterior: "En transito", nuevoEstado: "En puerto");

            await Crear().SendAsync(notificacion, CancellationToken.None);

            String html = _enviados.Should().ContainSingle().Subject.Message.HtmlBody!;
            html.Should().Contain("un comentario ha sido ingresado en")
                .And.Contain("<strong>Estado:</strong> En puerto</li>")
                .And.NotContain("Estado anterior:")
                .And.NotContain("Nuevo estado:")
                .And.Contain("<strong>Comentario:</strong> Carga lista</li>")
                .And.Contain("<strong>Fecha comentario:</strong> 20/10/2026</li>");
        }

        [Fact]
        public async Task SendAsync_OtraInformacion_MuestraSoloElEstado()
        {
            var notificacion = new NotificationMessage("CLI-7", NotificationType.OtherInfo, "m", documentNumber: "GUIA-123", title: "t", id: "N-1",
                origen: "Medellin, Colombia", destino: "Miami, USA", comentario: "No debe verse", fechaComentario: new DateTime(2026, 10, 20),
                estadoAnterior: "En transito", nuevoEstado: "En puerto");

            await Crear().SendAsync(notificacion, CancellationToken.None);

            String html = _enviados.Should().ContainSingle().Subject.Message.HtmlBody!;
            html.Should().Contain("la información de su envío de carga internacional ha sido actualizada en")
                .And.Contain("<strong>Estado:</strong> En puerto</li>")
                .And.NotContain("Estado anterior:")
                .And.NotContain("Nuevo estado:")
                .And.NotContain("Comentario:")
                .And.NotContain("Fecha comentario:")
                .And.NotContain("No debe verse");
        }

        [Fact]
        public async Task SendAsync_SinDestinatario_SaludaComoClienteYUsaElNumeroDeGuiaDeLaNotificacion()
        {
            var notificacion = new NotificationMessage("CLI-7", NotificationType.Comment, "m", documentNumber: "GUIA-9", title: "t", id: "N-1");

            await Crear().SendAsync(notificacion, CancellationToken.None);

            String html = _enviados.Should().ContainSingle().Subject.Message.HtmlBody!;
            html.Should().Contain("Estimado/a cliente,").And.Contain("GUIA-9");
        }

        [Fact]
        public async Task SendAsync_IncluyeElLogoIncrustadoYElTextoPlano()
        {
            await Crear().SendAsync(Notificacion("Titulo", "Texto plano alterno"), CancellationToken.None);

            EmailMessage correo = _enviados.Should().ContainSingle().Subject.Message;
            correo.Body.Should().Be("Texto plano alterno");
            correo.InlineImage.Should().NotBeNull();
            correo.InlineImage!.ContentId.Should().Be(ShipmentStatusEmailTemplate.LogoContentId);
            correo.InlineImage.MediaType.Should().Be("image/png");
            correo.InlineImage.Content.Take(8).Should().Equal(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A);
        }

        [Fact]
        public async Task SendAsync_CodificaComoHtmlElNombreYElNumeroDeGuia()
        {
            _context.Set(Usuario(fullName: "<script>alert(1)</script>"));
            var notificacion = new NotificationMessage("CLI-7", NotificationType.Comment, "m", documentNumber: "<b>X</b>", title: "t", id: "N-1");

            await Crear().SendAsync(notificacion, CancellationToken.None);

            String html = _enviados.Should().ContainSingle().Subject.Message.HtmlBody!;
            html.Should().NotContain("<script>").And.NotContain("<b>X</b>");
            html.Should().Contain("&lt;script&gt;").And.Contain("&lt;b&gt;X&lt;/b&gt;");
        }

        [Fact]
        public async Task SendAsync_RegistraDeDondeSalioElDestinatarioSinMostrarlo()
        {
            _context.Set(Usuario("cliente@correo.com"));

            await Crear().SendAsync(Notificacion(), CancellationToken.None);

            var entrada = _logger.Entries.Should().ContainSingle().Subject;
            entrada.Message.Should().Contain("correo del cliente").And.NotContain("cliente@correo.com");
        }
    }
}
