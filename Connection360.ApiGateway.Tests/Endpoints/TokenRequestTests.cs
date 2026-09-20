using Connection360.ApiGateway.Entitys;
using FluentAssertions;
using Xunit;

namespace Connection360.ApiGateway.Tests.Endpoints
{
    public class TokenRequestTests
    {
        [Fact]
        public void Propiedades_AsignanYRetornanElValorProvisto()
        {
            var request = new TokenRequest { GrantType = "client_credentials", ClientId = "admin-client", ClientSecret = "secreto" };

            request.GrantType.Should().Be("client_credentials");
            request.ClientId.Should().Be("admin-client");
            request.ClientSecret.Should().Be("secreto");
        }

        [Fact]
        public void EsUnRecord_LaIgualdadEsPorValor()
        {
            var a = new TokenRequest { GrantType = "client_credentials", ClientId = "x", ClientSecret = "y" };
            var b = new TokenRequest { GrantType = "client_credentials", ClientId = "x", ClientSecret = "y" };

            a.Should().Be(b);
        }
    }
}
