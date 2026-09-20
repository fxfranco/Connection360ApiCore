using Connection360.Application.Services;
using Connection360.Domain.Dtos;
using Connection360.Domain.Interfaces;
using Connection360.Domain.Ports.Persistence;
using FluentAssertions;
using Moq;
using Xunit;

namespace Connection360.Application.Tests.Services
{
    public class ClientAccessResolverTests
    {
        private const String CollaboratorId = "COL-1";

        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<ICustomersOfCollaboratorsRepository> _repositoryMock = new();
        private readonly ClientAccessResolver _sut;

        public ClientAccessResolverTests()
        {
            _unitOfWorkMock
                .Setup(u => u.GetRepository<ICustomersOfCollaboratorsRepository>())
                .Returns(_repositoryMock.Object);

            _sut = new ClientAccessResolver(_unitOfWorkMock.Object);
        }

        private static List<CustomersOfCollaboratorDtoResult> BuildAssignedCustomers()
        {
            return new List<CustomersOfCollaboratorDtoResult>
            {
                new() { IdCollaborator = 1, IdentificacionCollaborator = CollaboratorId, IdCustomer = 10, IdentificacionCustomer = "CUS-10" },
                new() { IdCollaborator = 1, IdentificacionCollaborator = CollaboratorId, IdCustomer = 11, IdentificacionCustomer = "CUS-11" }
            };
        }

        private void SetupAssignedCustomers(List<CustomersOfCollaboratorDtoResult> customers)
        {
            _repositoryMock
                .Setup(r => r.ListCustomersByCollaboratorAsync(CollaboratorId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(customers);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ResolveAsync_ConIdClientNuloOVacio_LanzaArgumentException(String? idClient)
        {
            var request = new ResolveClientAccessRequest { IdClient = idClient!, RoleName = "CLIENT" };

            Func<Task> act = () => _sut.ResolveAsync(request);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Theory]
        [InlineData("CLIENT")]
        [InlineData("ADMIN")]
        [InlineData("ROL-DESCONOCIDO")]
        public async Task ResolveAsync_ConRolesSinColaboradores_RetornaListaVaciaYNoConsultaElRepositorio(String roleName)
        {
            var request = new ResolveClientAccessRequest { IdClient = "123", RoleName = roleName, AllClient = true };

            List<CustomersOfCollaboratorDtoResult> result = await _sut.ResolveAsync(request);

            result.Should().BeEmpty();
            _unitOfWorkMock.Verify(u => u.GetRepository<ICustomersOfCollaboratorsRepository>(), Times.Never);
        }

        [Theory]
        [InlineData("ANALISTAOPE")]
        [InlineData("ANALISTASAC")]
        [InlineData("analistaope")]
        [InlineData("AnalistaSac")]
        public async Task ResolveAsync_ConRolDeAnalistaYAllClient_RetornaTodosLosClientesAsignados(String roleName)
        {
            SetupAssignedCustomers(BuildAssignedCustomers());
            var request = new ResolveClientAccessRequest { IdClient = CollaboratorId, RoleName = roleName, AllClient = true };

            List<CustomersOfCollaboratorDtoResult> result = await _sut.ResolveAsync(request);

            result.Should().HaveCount(2);
            result.Select(x => x.IdentificacionCustomer).Should().BeEquivalentTo(new[] { "CUS-10", "CUS-11" });
            _repositoryMock.Verify(r => r.ListCustomersByCollaboratorAsync(CollaboratorId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ResolveAsync_SinAllClient_FiltraPorElClienteConsultado()
        {
            SetupAssignedCustomers(BuildAssignedCustomers());
            var request = new ResolveClientAccessRequest
            {
                IdClient = CollaboratorId,
                RoleName = "ANALISTAOPE",
                AllClient = false,
                IdQueryClient = "CUS-11"
            };

            List<CustomersOfCollaboratorDtoResult> result = await _sut.ResolveAsync(request);

            result.Should().ContainSingle();
            CustomersOfCollaboratorDtoResult customer = result[0];
            customer.IdCollaborator.Should().Be(1);
            customer.IdentificacionCollaborator.Should().Be(CollaboratorId);
            customer.IdCustomer.Should().Be(11);
            customer.IdentificacionCustomer.Should().Be("CUS-11");
        }

        [Fact]
        public async Task ResolveAsync_SinAllClientYClienteNoAsignado_LanzaArgumentException()
        {
            SetupAssignedCustomers(BuildAssignedCustomers());
            var request = new ResolveClientAccessRequest
            {
                IdClient = CollaboratorId,
                RoleName = "ANALISTASAC",
                AllClient = false,
                IdQueryClient = "CUS-NO-ASIGNADO"
            };

            Func<Task> act = () => _sut.ResolveAsync(request);

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("No hay clientes asignados.");
        }

        [Fact]
        public async Task ResolveAsync_ConAllClientPeroSinClientesAsignados_LanzaArgumentException()
        {
            SetupAssignedCustomers(new List<CustomersOfCollaboratorDtoResult>());
            var request = new ResolveClientAccessRequest { IdClient = CollaboratorId, RoleName = "ANALISTAOPE", AllClient = true };

            Func<Task> act = () => _sut.ResolveAsync(request);

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("No hay clientes asignados.");
        }
    }
}
