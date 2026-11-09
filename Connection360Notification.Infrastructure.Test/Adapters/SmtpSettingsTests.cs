using Connection360Notification.Infrastructure.Email;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Adapters
{
    public class SmtpSettingsTests
    {
        [Fact]
        public void SectionName_EsSmtpSettings()
        {
            SmtpSettings.SectionName.Should().Be("SmtpSettings");
        }

        [Fact]
        public void ValoresPorDefecto_SonSeguros()
        {
            var sut = new SmtpSettings();

            sut.Port.Should().Be(587);
            sut.TimeoutSeconds.Should().Be(30);
            sut.CheckCertificateRevocation.Should().BeTrue("la revocación del certificado se valida salvo que se desactive explícitamente");
            sut.Server.Should().BeEmpty();
            sut.SenderEmail.Should().BeEmpty();
            sut.Password.Should().BeEmpty();
            sut.RecipientEmail.Should().BeEmpty();
            sut.UserName.Should().BeEmpty();
            sut.Conexion360Url.Should().BeEmpty();
        }

        [Fact]
        public void SeEnlazaDesdeLaSeccionDeConfiguracion()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<String, String?>
            {
                ["SmtpSettings:Server"] = "smtp.gmail.com",
                ["SmtpSettings:Port"] = "465",
                ["SmtpSettings:SenderName"] = "Mi Aplicación",
                ["SmtpSettings:SenderEmail"] = "tucorreo@gmail.com",
                ["SmtpSettings:Password"] = "clave",
                ["SmtpSettings:RecipientEmail"] = "destino@ejemplo.com",
                ["SmtpSettings:Conexion360Url"] = "https://conexion360.space",
                ["SmtpSettings:TimeoutSeconds"] = "10",
                ["SmtpSettings:CheckCertificateRevocation"] = "false"
            }).Build();

            var sut = configuration.GetSection(SmtpSettings.SectionName).Get<SmtpSettings>()!;

            sut.Server.Should().Be("smtp.gmail.com");
            sut.Port.Should().Be(465);
            sut.SenderName.Should().Be("Mi Aplicación");
            sut.SenderEmail.Should().Be("tucorreo@gmail.com");
            sut.Password.Should().Be("clave");
            sut.RecipientEmail.Should().Be("destino@ejemplo.com");
            sut.Conexion360Url.Should().Be("https://conexion360.space");
            sut.TimeoutSeconds.Should().Be(10);
            sut.CheckCertificateRevocation.Should().BeFalse();
        }
    }
}
