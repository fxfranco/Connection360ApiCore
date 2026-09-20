using Connection360.ApiGateway.IntegrationTests.Infrastructure;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Connection360.ApiGateway.IntegrationTests.Endpoints
{
    public class HealthAndSecurityIntegrationTests
    {
        [Fact]
        public async Task Health_Retorna200()
        {
            using var factory = new GatewayWebApplicationFactory();
            using var client = factory.CreateClient();

            HttpResponseMessage response = await client.GetAsync("/health");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Health_EsAccesibleSinAutenticacion()
        {
            // Ningún endpoint del Gateway llama a RequireAuthorization(); UseAuthorization() no exige
            // nada por sí solo si los endpoints no tienen metadata de autorización asociada.
            using var factory = new GatewayWebApplicationFactory();
            using var client = factory.CreateClient();

            HttpResponseMessage response = await client.GetAsync("/health");

            response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task CualquierRespuesta_IncluyeLosHeadersDeSeguridadDeOwasp()
        {
            using var factory = new GatewayWebApplicationFactory();
            using var client = factory.CreateClient();

            HttpResponseMessage response = await client.GetAsync("/health");

            response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
            response.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");
            response.Headers.GetValues("Referrer-Policy").Should().Contain("strict-origin-when-cross-origin");
            response.Headers.Contains("Content-Security-Policy").Should().BeTrue();
        }

        [Fact]
        public async Task CualquierRespuesta_NoExponeElHeaderServer()
        {
            using var factory = new GatewayWebApplicationFactory();
            using var client = factory.CreateClient();

            HttpResponseMessage response = await client.GetAsync("/health");

            response.Headers.Contains("Server").Should().BeFalse();
        }

        [Fact]
        public async Task PeticionConOrigenNoPermitido_NoRecibeHeaderAccessControlAllowOrigin()
        {
            using var factory = new GatewayWebApplicationFactory();
            using var client = factory.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Options, "/connect/token");
            request.Headers.Add("Origin", "https://sitio-no-permitido.com");
            request.Headers.Add("Access-Control-Request-Method", "POST");

            HttpResponseMessage response = await client.SendAsync(request);

            response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
        }

        [Fact]
        public async Task PeticionConOrigenPermitido_RecibeAccessControlAllowOrigin()
        {
            using var factory = new GatewayWebApplicationFactory();
            using var client = factory.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Options, "/connect/token");
            request.Headers.Add("Origin", "https://miapp.miempresa.com");
            request.Headers.Add("Access-Control-Request-Method", "POST");

            HttpResponseMessage response = await client.SendAsync(request);

            response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain("https://miapp.miempresa.com");
        }
    }
}
