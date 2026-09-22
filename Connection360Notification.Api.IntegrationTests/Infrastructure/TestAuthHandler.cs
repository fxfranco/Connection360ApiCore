using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Connection360Notification.Api.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Esquema de autenticación de prueba que reemplaza a JwtBearer durante las pruebas
    /// de integración, para poder ejercitar el pipeline HTTP real (routing, versioning,
    /// [Authorize], filtros, middleware) sin depender de un proveedor de identidad real (Auth0).
    ///
    /// Control desde el test vía encabezados HTTP:
    ///   - "X-Test-Auth": "true"  -> autentica al usuario (si se omite, la petición queda anónima)
    ///   - "X-Test-Roles": "ADMIN,CLIENT" -> roles asignados al usuario simulado
    /// </summary>
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const String SchemeName = "TestScheme";

        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-Auth", out var authHeader) || authHeader != "true")
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "test-user-id") };

            if (Request.Headers.TryGetValue("X-Test-Roles", out var rolesHeader))
            {
                claims.AddRange(rolesHeader.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(role => new Claim(ClaimTypes.Role, role.Trim())));
            }

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
