using Connection360.ApiGateway.Configuration;
using Connection360.ApiGateway.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Connection360.ApiGateway.Tests.Extensions
{
    public class AuthenticationExtensionsTests
    {
        private static IConfiguration BuildConfig(Dictionary<String, String?> values) =>
            new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        [Fact]
        public void AddGatewayAuthentication_SinSeccionJwt_LanzaInvalidOperationException()
        {
            var services = new ServiceCollection();
            var config = BuildConfig(new Dictionary<String, String?>());

            Action act = () => services.AddGatewayAuthentication(config);

            act.Should().Throw<InvalidOperationException>().WithMessage("*Jwt*");
        }

        [Fact]
        public void AddGatewayAuthentication_ConSeccionValida_RegistraJwtSettingsComoSingleton()
        {
            var services = new ServiceCollection();
            var config = BuildConfig(new Dictionary<String, String?>
            {
                ["Jwt:Issuer"] = "https://issuer.test",
                ["Jwt:Audience"] = "https://audience.test",
                ["Jwt:Secret"] = "un-secreto-de-prueba-suficientemente-largo",
                ["Jwt:Roles"] = "https://roles.test"
            });

            services.AddGatewayAuthentication(config);
            var provider = services.BuildServiceProvider();

            var jwtSettings = provider.GetRequiredService<JwtSettings>();
            jwtSettings.Issuer.Should().Be("https://issuer.test");
        }

        [Fact]
        public void AddGatewayAuthentication_RegistraAutenticacionYAutorizacion()
        {
            var services = new ServiceCollection();
            var config = BuildConfig(new Dictionary<String, String?>
            {
                ["Jwt:Issuer"] = "https://issuer.test",
                ["Jwt:Audience"] = "https://audience.test",
                ["Jwt:Secret"] = "un-secreto-de-prueba",
                ["Jwt:Roles"] = "https://roles.test"
            });

            services.AddGatewayAuthentication(config);

            services.Should().Contain(sd => sd.ServiceType == typeof(Microsoft.AspNetCore.Authorization.IAuthorizationService));
        }
    }
}
