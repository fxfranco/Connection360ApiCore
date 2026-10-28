using Connection360.ApiGateway.Configuration;
using FluentAssertions;
using Xunit;

namespace Connection360.ApiGateway.Tests.Configuration
{
    public class JwtSettingsTests
    {
        [Fact]
        public void ValoresPorDefecto_SonLosEsperados()
        {
            var settings = new JwtSettings();

            settings.AccessTokenMinutes.Should().Be(15);
            settings.RefreshTokenDays.Should().Be(7);
        }

        [Fact]
        public void SectionName_ApuntaALaSeccionJwt()
        {
            JwtSettings.SectionName.Should().Be("Jwt");
        }

        [Fact]
        public void Propiedades_AsignanYRetornanElValorProvisto()
        {
            var settings = new JwtSettings
            {
                Issuer = "https://issuer.test",
                Audience = "https://audience.test",
                Secret = "un-secreto",
                AccessTokenMinutes = 30,
                RefreshTokenDays = 14,
                Roles = "https://roles.test"
            };

            settings.Issuer.Should().Be("https://issuer.test");
            settings.Audience.Should().Be("https://audience.test");
            settings.AccessTokenMinutes.Should().Be(30);
            settings.RefreshTokenDays.Should().Be(14);
        }
    }

    public class RateLimitSettingsTests
    {
        [Fact]
        public void ValoresPorDefecto_SonLosEsperados()
        {
            var settings = new RateLimitSettings();

            settings.PermitLimit.Should().Be(100);
            settings.WindowSeconds.Should().Be(60);
            settings.QueueLimit.Should().Be(0);
        }

        [Fact]
        public void SectionName_ApuntaALaSeccionRateLimiting()
        {
            RateLimitSettings.SectionName.Should().Be("RateLimiting");
        }

        [Fact]
        public void Propiedades_AsignanYRetornanElValorProvisto()
        {
            var settings = new RateLimitSettings { PermitLimit = 50, WindowSeconds = 30, QueueLimit = 5 };

            settings.PermitLimit.Should().Be(50);
            settings.WindowSeconds.Should().Be(30);
            settings.QueueLimit.Should().Be(5);
        }
    }
}
