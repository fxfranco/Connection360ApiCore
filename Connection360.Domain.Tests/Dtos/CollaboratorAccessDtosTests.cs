using Connection360.Domain.Dtos;
using Connection360.Domain.Enums;
using Connection360.Domain.Interfaces;
using FluentAssertions;
using Xunit;

namespace Connection360.Domain.Tests.Dtos
{
    public class CustomersOfCollaboratorDtoResultTests
    {
        [Fact]
        public void PropiedadesPorDefecto_SonLasEsperadas()
        {
            var dto = new CustomersOfCollaboratorDtoResult();

            dto.IdCollaborator.Should().Be(0);
            dto.IdCustomer.Should().Be(0);
            dto.IdentificacionCollaborator.Should().Be(String.Empty);
            dto.IdentificacionCustomer.Should().Be(String.Empty);
        }

        [Fact]
        public void Propiedades_AsignanYRetornanElValorProvisto()
        {
            var dto = new CustomersOfCollaboratorDtoResult
            {
                IdCollaborator = 1,
                IdentificacionCollaborator = "COL-1",
                IdCustomer = 2,
                IdentificacionCustomer = "CUS-2"
            };

            dto.IdCollaborator.Should().Be(1);
            dto.IdentificacionCollaborator.Should().Be("COL-1");
            dto.IdCustomer.Should().Be(2);
            dto.IdentificacionCustomer.Should().Be("CUS-2");
        }
    }

    public class ResolveClientAccessRequestTests
    {
        [Fact]
        public void Propiedades_AsignanYRetornanElValorProvisto()
        {
            var request = new ResolveClientAccessRequest
            {
                IdClient = "cli-1",
                RoleName = "CLIENT",
                FilterValue = "900",
                IdQueryClient = "cli-2",
                AllClient = true
            };

            request.IdClient.Should().Be("cli-1");
            request.RoleName.Should().Be("CLIENT");
            request.FilterValue.Should().Be("900");
            request.IdQueryClient.Should().Be("cli-2");
            request.AllClient.Should().BeTrue();
        }
    }

    public class UserRoleApplicationTests
    {
        [Theory]
        [InlineData(UserRoleApplication.ADMIN, "ADMIN")]
        [InlineData(UserRoleApplication.CLIENT, "CLIENT")]
        [InlineData(UserRoleApplication.ANALISTAOPE, "ANALISTAOPE")]
        [InlineData(UserRoleApplication.ANALISTASAC, "ANALISTASAC")]
        public void ToString_RetornaElNombreDelRol(UserRoleApplication role, String expected)
        {
            role.ToString().Should().Be(expected);
        }
    }
}
