using Connection360.ApiGateway.IntegrationTests.Infrastructure;
using FluentAssertions;
using System.Net;
using Xunit;

namespace Connection360.ApiGateway.IntegrationTests.Endpoints
{
    /// <summary>
    /// Prueba de punta a punta del enrutamiento real de YARP (MapReverseProxy): el Gateway recibe
    /// la petición, la reenvía al "microservicio" downstream (aquí, un HttpListener falso) y
    /// devuelve la respuesta de ese downstream tal cual, aplicando las transformaciones de
    /// headers configuradas (X-Forwarded-By) definidas en appsettings.
    /// </summary>
    public class ReverseProxyIntegrationTests
    {
        [Fact]
        public async Task PeticionAApi_SeEnrutaHaciaElDestinoConfiguradoDelCluster()
        {
            using var downstream = new FakeDownstreamServer { ResponseBody = "{\"origen\":\"downstream-falso\"}" };
            downstream.Start();

            var overrides = new Dictionary<String, String?>
            {
                ["ReverseProxy:Clusters:products-cluster:Destinations:destination1:Address"] = downstream.BaseAddress,
                ["ReverseProxy:Clusters:products-cluster:HealthCheck:Active:Enabled"] = "false"
            };
            using var factory = new GatewayWebApplicationFactory(overrides);
            using var client = factory.CreateClient();

            HttpResponseMessage response = await client.GetAsync("/api/productos/1");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Be("{\"origen\":\"downstream-falso\"}");
        }

        [Fact]
        public async Task PeticionAApi_IncluyeElHeaderXForwardedByAgregadoPorLaTransformacion()
        {
            using var downstream = new FakeDownstreamServer();
            downstream.Start();

            var overrides = new Dictionary<String, String?>
            {
                ["ReverseProxy:Clusters:products-cluster:Destinations:destination1:Address"] = downstream.BaseAddress,
                ["ReverseProxy:Clusters:products-cluster:HealthCheck:Active:Enabled"] = "false"
            };
            using var factory = new GatewayWebApplicationFactory(overrides);
            using var client = factory.CreateClient();

            await client.GetAsync("/api/productos/1");

            downstream.LastReceivedForwardedByHeader.Should().Be("ApiGateway");
        }

        [Fact]
        public async Task PeticionAApi_ReescribeElPathSegunElPathPatternConfigurado()
        {
            using var downstream = new FakeDownstreamServer();
            downstream.Start();

            var overrides = new Dictionary<String, String?>
            {
                ["ReverseProxy:Clusters:products-cluster:Destinations:destination1:Address"] = downstream.BaseAddress,
                ["ReverseProxy:Clusters:products-cluster:HealthCheck:Active:Enabled"] = "false"
            };
            using var factory = new GatewayWebApplicationFactory(overrides);
            using var client = factory.CreateClient();

            await client.GetAsync("/api/productos/1?activo=true");

            // El PathPattern "/api/{**catch-all}" reescribe manteniendo el mismo prefijo /api,
            // por lo que el downstream debe recibir la misma ruta solicitada al Gateway.
            downstream.LastReceivedPath.Should().Be("/api/productos/1?activo=true");
        }

        [Fact]
        public async Task PeticionAUnaRutaSinClusterDisponible_Retorna502()
        {
            var overrides = new Dictionary<String, String?>
            {
                ["ReverseProxy:Clusters:products-cluster:Destinations:destination1:Address"] = "http://127.0.0.1:1", // puerto sin listener
                ["ReverseProxy:Clusters:products-cluster:HealthCheck:Active:Enabled"] = "false"
            };
            using var factory = new GatewayWebApplicationFactory(overrides);
            using var client = factory.CreateClient();

            HttpResponseMessage response = await client.GetAsync("/api/no-disponible");

            response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        }
    }
}
