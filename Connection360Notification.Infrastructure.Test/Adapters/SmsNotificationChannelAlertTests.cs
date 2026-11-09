using Connection360Notification.Application.Services;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Enums;
using Connection360Notification.Infrastructure.Adapters.Output;
using Connection360Notification.Infrastructure.Sms;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Adapters
{
    /// <summary>Canal de SMS con alertas del sistema (NotificationType.Alert).</summary>
    public class SmsNotificationChannelAlertTests
    {
        private readonly Mock<ISmsSender> _sender = new();
        private readonly NotificationRecipientContext _context = new();
        private readonly List<SmsMessage> _enviados = new();

        public SmsNotificationChannelAlertTests()
        {
            _sender.Setup(s => s.SendAsync(It.IsAny<SmsMessage>(), It.IsAny<CancellationToken>()))
                .Callback<SmsMessage, CancellationToken>((m, _) => _enviados.Add(m))
                .ReturnsAsync("9001");
        }

        private SmsNotificationChannel Crear() =>
            new(_sender.Object,
                Options.Create(new BrevoSmsSettings { ApiKey = "api-key", Sender = "Conexion360", Conexion360Url = "https://conexion360.space" }),
                _context, NullLogger<SmsNotificationChannel>.Instance);

        private static NotificationMessage Alerta() =>
            new("", NotificationType.Alert, "Se generó un error 'api caida' en el proceso de ETL", title: "Alerta en proceso de ETL", id: "A-1");

        [Fact]
        public async Task SendAsync_AlertaConAdministrador_EnviaAlCelularConElTextoDeAlertaSinEnlace()
        {
            _context.Set(new BasicUserData("auth0|1", "900", "Ana Admin", "3113232323", "ana@correo.com", "ADMIN"));

            await Crear().SendAsync(Alerta(), CancellationToken.None);

            SmsMessage sms = _enviados.Should().ContainSingle().Subject;
            sms.Recipient.Should().Be("573113232323");
            sms.Content.Should().Be(
                "Conexión360\n"
                + "Alerta en el proceso de sincronización de la ETL: Se generó un error 'api caida' en el proceso de ETL\n"
                + "“Visibilidad total. Decisiones inteligentes”");
        }

        [Fact]
        public async Task SendAsync_AlertaSinAdministradores_NoEnviaNingunSms()
        {
            _context.Set(null);

            await Crear().SendAsync(Alerta(), CancellationToken.None);

            _enviados.Should().BeEmpty();
        }

        [Fact]
        public async Task SendAsync_AlertaConAdministradorSinCelular_NoEnviaSms()
        {
            _context.Set(new BasicUserData("auth0|1", "900", "Ana Admin", "", "ana@correo.com", "ADMIN"));

            await Crear().SendAsync(Alerta(), CancellationToken.None);

            _enviados.Should().BeEmpty();
        }

        [Fact]
        public async Task SendAsync_AlertaDeVariosAdministradores_UnSmsPorCadaCelular()
        {
            var canal = Crear();
            _context.Set(new BasicUserData("auth0|1", "900", "Ana Admin", "3113232323", "ana@correo.com", "ADMIN"));
            await canal.SendAsync(Alerta(), CancellationToken.None);
            _context.Set(new BasicUserData("auth0|2", "901", "Luis Admin", "3004445566", "luis@correo.com", "ADMIN"));
            await canal.SendAsync(Alerta(), CancellationToken.None);

            _enviados.Select(s => s.Recipient).Should().Equal("573113232323", "573004445566");
        }
    }
}
