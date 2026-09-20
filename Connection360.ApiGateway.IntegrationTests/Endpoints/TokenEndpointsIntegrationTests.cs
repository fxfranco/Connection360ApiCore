using Connection360.ApiGateway.IntegrationTests.Infrastructure;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Connection360.ApiGateway.IntegrationTests.Endpoints
{
    public class TokenEndpointsIntegrationTests
    {
        private static Object ClientCredentialsBody(String clientId, String clientSecret) => new
        {
            grantType = "client_credentials",
            clientId,
            clientSecret
        };

        [Fact]
        public async Task ConnectToken_ConCredencialesValidas_Retorna200ConAccessToken()
        {
            using var factory = new GatewayWebApplicationFactory();
            using var client = factory.CreateClient();

            HttpResponseMessage response = await client.PostAsJsonAsync("/connect/token", ClientCredentialsBody("admin-client", "S3cureAdminSecret!"));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            json.RootElement.GetProperty("access_token").GetString().Should().NotBeNullOrEmpty();
            json.RootElement.GetProperty("token_type").GetString().Should().Be("Bearer");
        }

        [Fact]
        public async Task ConnectToken_ConCredencialesInvalidas_Retorna401()
        {
            using var factory = new GatewayWebApplicationFactory();
            using var client = factory.CreateClient();

            HttpResponseMessage response = await client.PostAsJsonAsync("/connect/token", ClientCredentialsBody("admin-client", "clave-incorrecta"));

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ConnectToken_ConClienteInexistente_Retorna401()
        {
            using var factory = new GatewayWebApplicationFactory();
            using var client = factory.CreateClient();

            HttpResponseMessage response = await client.PostAsJsonAsync("/connect/token", ClientCredentialsBody("cliente-no-existe", "cualquier-cosa"));

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ConnectToken_ConGrantTypeNoSoportado_Retorna400()
        {
            using var factory = new GatewayWebApplicationFactory();
            using var client = factory.CreateClient();

            var body = new { grantType = "password", clientId = "admin-client", clientSecret = "S3cureAdminSecret!" };

            HttpResponseMessage response = await client.PostAsJsonAsync("/connect/token", body);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ConnectToken_ElTokenEmitidoIncluyeTodosLosRolesSinImportarElClienteAutenticado()
        {
            // HALLAZGO: GenerateJwt en TokenEndpoints ignora el rol real del cliente autenticado
            // (el parámetro 'role' calculado por IsValidClient no se usa) y en su lugar añade
            // SIEMPRE la lista fija {ADMIN, CLIENT, ANALISTAOPE, ANALISTASAC} como roles del token.
            // Esto significa que un cliente "reader-client" (pensado para tener acceso de solo lectura)
            // recibe un token con TODOS los roles, incluido ADMIN. Este test documenta ese comportamiento actual.
            using var factory = new GatewayWebApplicationFactory();
            using var client = factory.CreateClient();

            HttpResponseMessage response = await client.PostAsJsonAsync("/connect/token", ClientCredentialsBody("reader-client", "S3cureReaderSecret!"));

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            String accessToken = json.RootElement.GetProperty("access_token").GetString()!;

            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(accessToken);
            var roleClaims = jwt.Claims.Where(c => c.Type == "https://conexion360.space/roles").Select(c => c.Value);

            roleClaims.Should().Contain(new[] { "ADMIN", "CLIENT", "ANALISTAOPE", "ANALISTASAC" });
        }

        [Fact]
        public async Task ConnectToken_ExcedeElLimiteDeLaPoliticaAuthPolicy_Retorna429()
        {
            // AuthPolicy: PermitLimit = 5 por ventana de 1 minuto (ver RateLimitingExtensions).
            using var factory = new GatewayWebApplicationFactory();
            using var client = factory.CreateClient();
            var body = ClientCredentialsBody("cliente-inexistente", "x"); // 401 rápido, no importa el resultado del login

            var responses = new List<HttpResponseMessage>();
            for (Int32 i = 0; i < 6; i++)
            {
                responses.Add(await client.PostAsJsonAsync("/connect/token", body));
            }

            responses.Take(5).Should().OnlyContain(r => r.StatusCode == HttpStatusCode.Unauthorized);
            responses[5].StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        }
    }
}
