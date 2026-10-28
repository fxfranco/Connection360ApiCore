using Connection360Notification.Infrastructure.Sms;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Adapters
{
    public class BrevoSmsSettingsTests
    {
        [Fact]
        public void SectionName_EsBrevoSettingsSMS()
        {
            BrevoSmsSettings.SectionName.Should().Be("BrevoSettingsSMS");
        }

        [Fact]
        public void ValoresPorDefecto_SonSeguros()
        {
            var sut = new BrevoSmsSettings();

            sut.ApiKey.Should().BeEmpty();
            sut.Sender.Should().BeEmpty();
            sut.Conexion360Url.Should().BeEmpty();
            sut.DefaultCountryCode.Should().Be("57");
            sut.BaseUrl.Should().Be("https://api.brevo.com/v3/");
            sut.TimeoutSeconds.Should().Be(30);
        }

        [Fact]
        public void SeEnlazaDesdeLaSeccionDeConfiguracion()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<String, String?>
            {
                ["BrevoSettingsSMS:ApiKey"] = "clave",
                ["BrevoSettingsSMS:Sender"] = "Conexion360",
                ["BrevoSettingsSMS:Conexion360Url"] = "https://conexion360.space",
                ["BrevoSettingsSMS:DefaultCountryCode"] = "1",
                ["BrevoSettingsSMS:BaseUrl"] = "https://otro.host/v3/",
                ["BrevoSettingsSMS:TimeoutSeconds"] = "10"
            }).Build();

            var sut = configuration.GetSection(BrevoSmsSettings.SectionName).Get<BrevoSmsSettings>()!;

            sut.ApiKey.Should().Be("clave");
            sut.Sender.Should().Be("Conexion360");
            sut.Conexion360Url.Should().Be("https://conexion360.space");
            sut.DefaultCountryCode.Should().Be("1");
            sut.BaseUrl.Should().Be("https://otro.host/v3/");
            sut.TimeoutSeconds.Should().Be(10);
        }
    }
}
