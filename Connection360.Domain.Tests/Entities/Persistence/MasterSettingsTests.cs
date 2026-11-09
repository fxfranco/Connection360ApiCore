using Connection360.Domain.Entities.Persistence;
using FluentAssertions;
using Xunit;

namespace Connection360.Domain.Tests.Entities.Persistence
{
    public class MasterSettingsTests
    {
        [Fact]
        public void ConstructorVacio_DebeCrearInstanciaConValoresPorDefecto()
        {
            var entity = new MasterSettings();

            entity.IdMasterSettings.Should().Be(0);
            entity.CurrencyType.Should().BeNull();
        }

        [Fact]
        public void ConstructorConParametros_DebeAsignarTodasLasPropiedades()
        {
            var entity = new MasterSettings(1, true, false, true, "COP", "es-CO", "America/Bogota", 30);

            entity.IdMasterSettings.Should().Be(1);
            entity.AutomaticTrackingUpdate.Should().BeTrue();
            entity.RequireDocumentUpload.Should().BeFalse();
            entity.PublicMonitoring.Should().BeTrue();
            entity.CurrencyType.Should().Be("COP");
            entity.Language.Should().Be("es-CO");
            entity.TimeZone.Should().Be("America/Bogota");
            entity.DataRetentionDays.Should().Be(30);
        }

        [Fact]
        public void Update_DebeSobrescribirTodasLasPropiedades()
        {
            var entity = new MasterSettings(1, true, true, true, "COP", "es-CO", "America/Bogota", 30);

            entity.Update(false, false, false, "USD", "en-US", "UTC", 90);

            entity.AutomaticTrackingUpdate.Should().BeFalse();
            entity.RequireDocumentUpload.Should().BeFalse();
            entity.PublicMonitoring.Should().BeFalse();
            entity.CurrencyType.Should().Be("USD");
            entity.Language.Should().Be("en-US");
            entity.TimeZone.Should().Be("UTC");
            entity.DataRetentionDays.Should().Be(90);
        }

        [Fact]
        public void ConstructorSinParametros_CreaInstanciaConValoresPorDefecto()
        {
            var entity = new MasterSettings();

            entity.IdMasterSettings.Should().Be(0);
            entity.AutomaticTrackingUpdate.Should().BeFalse();
            entity.CurrencyType.Should().BeNull();
        }

        [Fact]
        public void ConstructorConParametros_AsignaTodasLasPropiedades()
        {
            var entity = new MasterSettings(1, true, false, true, "COP", "es", "America/Bogota", 30);

            entity.IdMasterSettings.Should().Be(1);
            entity.AutomaticTrackingUpdate.Should().BeTrue();
            entity.RequireDocumentUpload.Should().BeFalse();
            entity.PublicMonitoring.Should().BeTrue();
            entity.CurrencyType.Should().Be("COP");
            entity.Language.Should().Be("es");
            entity.TimeZone.Should().Be("America/Bogota");
            entity.DataRetentionDays.Should().Be(30);
        }

        [Fact]
        public void Update_ModificaTodasLasPropiedadesMenosElId()
        {
            var entity = new MasterSettings(5, true, true, true, "COP", "es", "America/Bogota", 30);

            entity.Update(false, false, false, "USD", "en", "UTC", 90);

            entity.IdMasterSettings.Should().Be(5);
            entity.AutomaticTrackingUpdate.Should().BeFalse();
            entity.RequireDocumentUpload.Should().BeFalse();
            entity.PublicMonitoring.Should().BeFalse();
            entity.CurrencyType.Should().Be("USD");
            entity.Language.Should().Be("en");
            entity.TimeZone.Should().Be("UTC");
            entity.DataRetentionDays.Should().Be(90);
        }
    }
}
