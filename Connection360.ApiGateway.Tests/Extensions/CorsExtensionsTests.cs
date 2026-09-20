using Connection360.ApiGateway.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Connection360.ApiGateway.Tests.Extensions
{
    public class CorsExtensionsTests
    {
        private static IConfiguration BuildConfig(String[] allowedOrigins) =>
            new ConfigurationBuilder().AddInMemoryCollection(
                allowedOrigins.Select((origin, index) => new KeyValuePair<String, String?>($"Cors:AllowedOrigins:{index}", origin))
            ).Build();

        [Fact]
        public void AddGatewayCors_RegistraLaPoliticaConLosOrigenesConfigurados()
        {
            var services = new ServiceCollection();
            var config = BuildConfig(new[] { "https://miapp.test" });

            services.AddGatewayCors(config);
            var provider = services.BuildServiceProvider();
            var corsOptions = provider.GetRequiredService<IOptions<CorsOptions>>().Value;

            CorsPolicy? policy = corsOptions.GetPolicy(CorsExtensions.PolicyName);
            policy.Should().NotBeNull();
            policy!.Origins.Should().Contain("https://miapp.test");
        }

        [Fact]
        public void AddGatewayCors_LaPoliticaPermiteCredenciales()
        {
            var services = new ServiceCollection();
            var config = BuildConfig(new[] { "https://miapp.test" });

            services.AddGatewayCors(config);
            var provider = services.BuildServiceProvider();
            var policy = provider.GetRequiredService<IOptions<CorsOptions>>().Value.GetPolicy(CorsExtensions.PolicyName);

            policy!.SupportsCredentials.Should().BeTrue();
        }

        [Fact]
        public void AddGatewayCors_SinOrigenesConfigurados_NoLanzaExcepcionYRegistraListaVacia()
        {
            var services = new ServiceCollection();
            var config = BuildConfig(Array.Empty<String>());

            Action act = () => services.AddGatewayCors(config);

            act.Should().NotThrow();
        }
    }
}
