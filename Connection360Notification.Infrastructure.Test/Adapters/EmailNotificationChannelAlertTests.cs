using Connection360Notification.Application.Services;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Enums;
using Connection360Notification.Infrastructure.Adapters.Output;
using Connection360Notification.Infrastructure.Email;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Adapters
{
    /// <summary>Canal de correo con alertas del sistema (NotificationType.Alert): destinatario administrador o correo por defecto.</summary>
    public class EmailNotificationChannelAlertTests
    {
        private readonly Mock<IEmailSender> _sender = new();
        private readonly NotificationRecipientContext _context = new();
        private readonly List<EmailMessage> _enviados = new();

        public EmailNotificationChannelAlertTests()
        {
            _sender.Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
                .Callback<EmailMessage, CancellationToken>((m, _) => _enviados.Add(m))
                .Returns(Task.CompletedTask);
        }

        private EmailNotificationChannel Crear(String recipientEmail = "defecto@ejemplo.com") =>
            new(_sender.Object,
                Options.Create(new SmtpSettings
                {
                    Server = "smtp.gmail.com", Port = 587, SenderName = "App", SenderEmail = "remitente@gmail.com",
                    Password = "clave", RecipientEmail = recipientEmail, Conexion360Url = "https://conexion360.space"
                }),
                _context, NullLogger<EmailNotificationChannel>.Instance);

        private static NotificationMessage Alerta() =>
            new("", NotificationType.Alert, "Se generó un error 'api caida' en el proceso de ETL", title: "Alerta en proceso de ETL", id: "A-1");

        [Fact]
        public async Task SendAsync_AlertaConAdministrador_EnviaASuCorreoConSuNombreEnElSaludo()
        {
            _context.Set(new BasicUserData("auth0|1", "900", "Ana Admin", "3001112233", "ana.admin@correo.com", "ADMIN"));

            await Crear().SendAsync(Alerta(), CancellationToken.None);

            EmailMessage correo = _enviados.Should().ContainSingle().Subject;
            correo.To.Should().Be("ana.admin@correo.com");
            correo.Subject.Should().Be("Alerta en proceso de ETL");
            correo.Body.Should().Be("Se generó un error 'api caida' en el proceso de ETL");
            correo.HtmlBody.Should().Contain("Estimado/a Ana Admin,")
                .And.Contain($"Le informamos que se ha generado una alerta en el proceso de sincronización de la ETL con el error: {WebUtility.HtmlEncode(correo.Body)}.")
                .And.NotContain("Información del envío")
                .And.NotContain("Consultar mi envío");
            correo.InlineImage.Should().NotBeNull();
        }

        [Fact]
        public async Task SendAsync_AlertaSinAdministradores_EnviaAlCorreoPorDefectoConSaludoSinNombre()
        {
            _context.Set(null);

            await Crear("defecto@ejemplo.com").SendAsync(Alerta(), CancellationToken.None);

            EmailMessage correo = _enviados.Should().ContainSingle().Subject;
            correo.To.Should().Be("defecto@ejemplo.com");
            correo.HtmlBody.Should().Contain("Estimado/a,").And.Contain("con el error:");
        }

        [Fact]
        public async Task SendAsync_AlertaConAdministradorSinCorreoValido_UsaElCorreoPorDefecto()
        {
            _context.Set(new BasicUserData("auth0|1", "900", "Ana Admin", "3001112233", "no-es-un-correo", "ADMIN"));

            await Crear("defecto@ejemplo.com").SendAsync(Alerta(), CancellationToken.None);

            _enviados.Should().ContainSingle().Which.To.Should().Be("defecto@ejemplo.com");
        }

        [Fact]
        public async Task SendAsync_AlertaConAdministradorSinCorreoYSinCorreoPorDefecto_LanzaConfiguracionIncompleta()
        {
            _context.Set(new BasicUserData("auth0|1", "900", "Ana Admin", "3001112233", "", "ADMIN"));

            Func<Task> act = () => Crear(recipientEmail: "").SendAsync(Alerta(), CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*RecipientEmail*");
            _enviados.Should().BeEmpty();
        }

        [Fact]
        public async Task SendAsync_AlertaConAdministradorYSinCorreoPorDefecto_NoExigeElCorreoPorDefecto()
        {
            _context.Set(new BasicUserData("auth0|1", "900", "Ana Admin", "3001112233", "ana.admin@correo.com", "ADMIN"));

            await Crear(recipientEmail: "").SendAsync(Alerta(), CancellationToken.None);

            _enviados.Should().ContainSingle().Which.To.Should().Be("ana.admin@correo.com");
        }

        [Fact]
        public async Task SendAsync_AlertaDeVariosAdministradores_CadaCorreoLlevaSuPropioNombre()
        {
            var canal = Crear();
            _context.Set(new BasicUserData("auth0|1", "900", "Ana Admin", "300", "ana@correo.com", "ADMIN"));
            await canal.SendAsync(Alerta(), CancellationToken.None);
            _context.Set(new BasicUserData("auth0|2", "901", "Luis Admin", "301", "luis@correo.com", "ADMIN"));
            await canal.SendAsync(Alerta(), CancellationToken.None);

            _enviados.Select(e => e.To).Should().Equal("ana@correo.com", "luis@correo.com");
            _enviados[0].HtmlBody.Should().Contain("Estimado/a Ana Admin,");
            _enviados[1].HtmlBody.Should().Contain("Estimado/a Luis Admin,");
        }
    }
}
