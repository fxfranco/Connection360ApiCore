using Connection360.Infrastructure.ExternalApi;
using FluentAssertions;
using Xunit;

namespace Connection360.Infrastructure.Tests.ExternalApi
{
    public class ExternalApiSettingsTests
    {
        [Fact]
        public void GetConfig_ApiExistente_DebeRetornarSuConfiguracion()
        {
            var settings = new ExternalApiSettings();
            settings.Apis["BPMS"] = new ExternalApisDetail { BaseUrl = "https://bpms.test", DataEndpoint = "/data" };

            ExternalApisDetail? result = settings.GetConfig("BPMS");

            result.Should().NotBeNull();
            result!.BaseUrl.Should().Be("https://bpms.test");
        }

        [Fact]
        public void GetConfig_ApiInexistente_DebeRetornarNull()
        {
            var settings = new ExternalApiSettings();

            ExternalApisDetail? result = settings.GetConfig("NO_EXISTE");

            result.Should().BeNull();
        }

        [Fact]
        public void GetConfig_ComparacionDeNombreDeApi_DebeSerInsensibleAMayusculas()
        {
            var settings = new ExternalApiSettings();
            settings.Apis["BPMS"] = new ExternalApisDetail { BaseUrl = "https://bpms.test", DataEndpoint = "/data" };

            ExternalApisDetail? result = settings.GetConfig("bpms");

            result.Should().NotBeNull();
        }

        [Fact]
        public void ExternalApisDetail_TimeoutSeconds_DebeTenerValorPorDefectoDeTreinta()
        {
            var detail = new ExternalApisDetail { BaseUrl = "https://x.test", DataEndpoint = "/d" };

            detail.TimeoutSeconds.Should().Be(30);
        }
    }
}
