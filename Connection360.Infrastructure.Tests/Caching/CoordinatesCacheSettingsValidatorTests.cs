using Connection360.Infrastructure.Caching;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Connection360.Infrastructure.Tests.Caching
{
    public class CoordinatesCacheSettingsValidatorTests
    {
        private static ValidateOptionsResult Validate(CoordinatesCacheSettings settings) =>
            new CoordinatesCacheSettingsValidator().Validate(null, settings);

        [Fact]
        public void ValoresPorDefecto_SonValidos()
        {
            Validate(new CoordinatesCacheSettings()).Succeeded.Should().BeTrue();
        }

        [Fact]
        public void ValoresPorDefecto_RespetanElPresupuestoDeMemoriaDelServidor()
        {
            var defaults = new CoordinatesCacheSettings();

            defaults.Enabled.Should().BeTrue();
            defaults.MaxSizeBytes.Should().Be(10L * 1024 * 1024);
            defaults.AbsoluteExpiration.Should().Be(TimeSpan.FromDays(30));
            defaults.SlidingExpiration.Should().Be(TimeSpan.FromDays(7));
            defaults.MaxKeyLength.Should().Be(200);
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(-1L)]
        public void MaxSizeBytesNoPositivo_Falla(Int64 value)
        {
            ValidateOptionsResult result = Validate(new CoordinatesCacheSettings { MaxSizeBytes = value });

            result.Failed.Should().BeTrue();
            result.FailureMessage.Should().Contain("MaxSizeBytes");
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(-0.5)]
        [InlineData(1.01)]
        public void EvictionPercentageFueraDeRango_Falla(Double value)
        {
            ValidateOptionsResult result = Validate(new CoordinatesCacheSettings { EvictionPercentage = value });

            result.Failed.Should().BeTrue();
            result.FailureMessage.Should().Contain("EvictionPercentage");
        }

        [Theory]
        [InlineData(0.01)]
        [InlineData(1.0)]
        public void EvictionPercentageEnElRango_EsValido(Double value)
        {
            Validate(new CoordinatesCacheSettings { EvictionPercentage = value }).Succeeded.Should().BeTrue();
        }

        [Fact]
        public void AbsoluteExpirationNoPositiva_Falla()
        {
            ValidateOptionsResult result = Validate(new CoordinatesCacheSettings { AbsoluteExpiration = TimeSpan.Zero });

            result.Failed.Should().BeTrue();
            result.FailureMessage.Should().Contain("AbsoluteExpiration");
        }

        [Fact]
        public void SlidingExpirationNoPositiva_Falla()
        {
            ValidateOptionsResult result = Validate(new CoordinatesCacheSettings { SlidingExpiration = TimeSpan.FromSeconds(-1) });

            result.Failed.Should().BeTrue();
            result.FailureMessage.Should().Contain("SlidingExpiration");
        }

        [Fact]
        public void SlidingExpirationNula_EsValida()
        {
            Validate(new CoordinatesCacheSettings { SlidingExpiration = null }).Succeeded.Should().BeTrue();
        }

        [Fact]
        public void MaxKeyLengthNoPositivo_Falla()
        {
            ValidateOptionsResult result = Validate(new CoordinatesCacheSettings { MaxKeyLength = 0 });

            result.Failed.Should().BeTrue();
            result.FailureMessage.Should().Contain("MaxKeyLength");
        }

        [Fact]
        public void VariosErrores_SeReportanTodosJuntos()
        {
            ValidateOptionsResult result = Validate(new CoordinatesCacheSettings { MaxSizeBytes = 0, MaxKeyLength = 0 });

            result.FailureMessage.Should().Contain("MaxSizeBytes").And.Contain("MaxKeyLength");
        }
    }
}
