using Auth0.ManagementApi;
using Connection360.Infrastructure.Adapters.Output;
using FluentAssertions;
using Xunit;

namespace Connection360.Infrastructure.Tests.Adapters
{
    /// <summary>Clasificación de los errores de la Management API (el 403 "Insufficient scope" de la consulta de roles).</summary>
    public class Auth0ManagementErrorsTests
    {
        [Fact]
        public void IsForbidden_ConForbiddenError_EsVerdadero()
        {
            var error = new ForbiddenError(new { error = "Forbidden", message = "Insufficient scope, expected any of: read:roles" }, null);

            Auth0ManagementErrors.IsForbidden(error).Should().BeTrue();
        }

        [Fact]
        public void IsForbidden_ConManagementApiExceptionDe403_EsVerdadero()
        {
            var error = new ManagementApiException("Insufficient scope", 403, "cuerpo", null, null);

            Auth0ManagementErrors.IsForbidden(error).Should().BeTrue();
        }

        [Theory]
        [InlineData(400)]
        [InlineData(401)]
        [InlineData(404)]
        [InlineData(429)]
        [InlineData(500)]
        public void IsForbidden_ConOtrosCodigosHttp_EsFalso(Int32 statusCode)
        {
            var error = new ManagementApiException("otro error", statusCode, "cuerpo", null, null);

            Auth0ManagementErrors.IsForbidden(error).Should().BeFalse();
        }

        [Fact]
        public void IsForbidden_ConExcepcionesNoDeAuth0_EsFalso()
        {
            Auth0ManagementErrors.IsForbidden(new InvalidOperationException("x")).Should().BeFalse();
            Auth0ManagementErrors.IsForbidden(new ArgumentException("x")).Should().BeFalse();
        }

        [Fact]
        public void RoleLookupScopes_NombraLosPermisosNecesarios()
        {
            Auth0ManagementErrors.RoleLookupScopes.Should().Contain("read:users").And.Contain("read:roles").And.Contain("read:role_members");
        }
    }
}
