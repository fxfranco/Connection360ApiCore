using Connection360.Infrastructure.ExternalApi;
using Connection360.Infrastructure.Messaging;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Connection360.Infrastructure.Tests.Messaging
{
    public class OutboxPublisherSettingsTests
    {
        [Fact]
        public void Constantes_TienenLosValoresEsperados()
        {
            OutboxPublisherSettings.SectionName.Should().Be("OutboxPublisher");
            OutboxPublisherSettings.DefaultPollingIntervalSeconds.Should().Be(10);
        }

        [Fact]
        public void PollingIntervalSeconds_PorDefecto_EsElValorPorDefecto()
        {
            new OutboxPublisherSettings().PollingIntervalSeconds.Should().Be(OutboxPublisherSettings.DefaultPollingIntervalSeconds);
        }

        [Fact]
        public void Bind_DesdeConfiguracion_AsignaElIntervalo()
        {
            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<String, String?> { ["OutboxPublisher:PollingIntervalSeconds"] = "45" }).Build();

            OutboxPublisherSettings? settings = config.GetSection(OutboxPublisherSettings.SectionName).Get<OutboxPublisherSettings>();

            settings!.PollingIntervalSeconds.Should().Be(45);
        }

        [Fact]
        public void ExternalApisDetail_PorDefecto_TimeoutDe30SegundosYSinApiKey()
        {
            var detail = new ExternalApisDetail();

            detail.TimeoutSeconds.Should().Be(30);
            detail.ApiKey.Should().BeNull();
        }

        [Fact]
        public void ExternalApisDetail_AsignaPropiedades()
        {
            var detail = new ExternalApisDetail { BaseUrl = "https://x", DataEndpoint = "/e", ApiKey = "k", TimeoutSeconds = 5 };

            detail.BaseUrl.Should().Be("https://x");
            detail.DataEndpoint.Should().Be("/e");
            detail.ApiKey.Should().Be("k");
            detail.TimeoutSeconds.Should().Be(5);
        }

        [Fact]
        public void ExternalApiSettings_SectionNameYBusquedaInsensibleAMayusculas()
        {
            var settings = new ExternalApiSettings();
            settings.Apis["Bpms"] = new ExternalApisDetail();

            ExternalApiSettings.SectionName.Should().Be("ExternalApi");
            settings.GetConfig("BPMS").Should().NotBeNull();
        }
    }
}
