using Connection360.ApiGateway.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Connection360.ApiGateway.Tests.Extensions
{
    public class RateLimitingExtensionsTests
    {
        [Fact]
        public void AddGatewayRateLimiting_SinConfiguracion_UsaValoresPorDefectoYNoLanzaExcepcion()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();

            Action act = () => services.AddGatewayRateLimiting(config);

            act.Should().NotThrow();
        }

        [Fact]
        public void AddGatewayRateLimiting_RegistraElRejectionStatusCode429()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();

            services.AddGatewayRateLimiting(config);
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;

            options.RejectionStatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        }

        [Fact]
        public void AddGatewayRateLimiting_ConConfiguracionPersonalizada_NoLanzaExcepcion()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<String, String?>
            {
                ["RateLimiting:PermitLimit"] = "10",
                ["RateLimiting:WindowSeconds"] = "30",
                ["RateLimiting:QueueLimit"] = "2"
            }).Build();

            Action act = () => services.AddGatewayRateLimiting(config);

            act.Should().NotThrow();
        }
    }
}
