using Connection360.Etl.Infrastructure.Configuration;
using Connection360.Etl.Infrastructure.ExternalApi;
using Connection360.Etl.Infrastructure.ExternalApi.DTOs;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace Connection360.Etl.Infrastructure.Tests.Configuration
{
    public class SettingsTests
    {
        [Fact]
        public void EtlChangeTrackingSettings_ValoresPorDefecto()
        {
            EtlChangeTrackingSettings.SectionName.Should().Be("EtlChangeTracking");
            new EtlChangeTrackingSettings().SystemUser.Should().Be("ETL_CONNECTION360");
        }

        [Fact]
        public void EtlChangeTrackingSettings_SystemUser_SePuedeSobrescribir()
        {
            new EtlChangeTrackingSettings { SystemUser = "OTRO" }.SystemUser.Should().Be("OTRO");
        }

        [Fact]
        public void EtlJobControlSettings_ValoresPorDefecto()
        {
            EtlJobControlSettings.SectionName.Should().Be("EtlJobControl");
            new EtlJobControlSettings().ApplicationDataSheetRetentionDays.Should().Be(30);
        }

        [Fact]
        public void EtlJobControlSettings_RetentionDays_SePuedeSobrescribir()
        {
            new EtlJobControlSettings { ApplicationDataSheetRetentionDays = 7 }.ApplicationDataSheetRetentionDays.Should().Be(7);
        }

        [Fact]
        public void ExternalApiSettings_ValoresPorDefecto()
        {
            var settings = new ExternalApiSettings();

            ExternalApiSettings.SectionName.Should().Be("ExternalApi");
            settings.Apis.Should().BeEmpty();
            settings.PaginationEnabled.Should().BeFalse();
            settings.PageSize.Should().Be(0);
        }

        [Fact]
        public void ExternalApisDetail_ValoresPorDefecto()
        {
            var detail = new ExternalApisDetail();

            detail.ApiKey.Should().BeNull();
            detail.TimeoutSeconds.Should().Be(30);
            detail.PageNumberParam.Should().Be("pageNumber");
            detail.PageSizeParam.Should().Be("pageSize");
        }

        [Fact]
        public void ExternalApiResponseDto_ValoresPorDefecto()
        {
            var dto = new ExternalApiResponseDto();

            dto.RequestedFields.Should().BeEmpty();
            dto.MissingColumns.Should().BeEmpty();
            dto.Rows.Should().BeEmpty();
        }

        [Fact]
        public void ExternalApiResponseDto_DeserializaConLosNombresJsonDelContrato()
        {
            const String json = "{\"requestedFields\":[\"A\"],\"missingColumns\":[\"Z\"],\"rows\":[{\"A\":\"x\"}]}";

            ExternalApiResponseDto dto = JsonSerializer.Deserialize<ExternalApiResponseDto>(json)!;

            dto.RequestedFields.Should().Equal("A");
            dto.MissingColumns.Should().Equal("Z");
            dto.Rows.Should().ContainSingle();
            dto.Rows[0]["A"].Should().BeOfType<JsonElement>().Which.GetString().Should().Be("x");
        }

        [Fact]
        public void ExternalApiResponseDto_IgnoraNombresEnPascalCase()
        {
            const String json = "{\"RequestedFields\":[\"A\"]}";

            ExternalApiResponseDto dto = JsonSerializer.Deserialize<ExternalApiResponseDto>(json)!;

            dto.RequestedFields.Should().BeEmpty("los nombres JSON del contrato son camelCase y sensibles a mayúsculas por defecto");
        }
    }
}
