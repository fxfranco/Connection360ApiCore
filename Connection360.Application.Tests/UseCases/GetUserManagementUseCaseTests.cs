using Connection360.Application.DTOs;
using Connection360.Application.UseCases;
using Connection360.Domain.Dtos;
using Connection360.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace Connection360.Application.Tests.UseCases
{
    public class GetUserManagementUseCaseTests
    {
        private readonly Mock<IAuth0UserService> _auth0ServiceMock = new();
        private readonly GetUserManagementUseCase _sut;

        public GetUserManagementUseCaseTests()
        {
            _sut = new GetUserManagementUseCase(_auth0ServiceMock.Object);
        }

        [Fact]
        public async Task GetAllUsersAsync_DelegaAlServicioConPaginacion()
        {
            var request = new UsersManagementRequest { Page = 2, Size = 10 };
            var expected = new List<Auth0UserDto> { new() { UserId = "u1" } };
            _auth0ServiceMock.Setup(s => s.GetUsersAsync(2, 10)).ReturnsAsync(expected);

            IList<Auth0UserDto> result = await _sut.GetAllUsersAsync(request);

            result.Should().BeEquivalentTo(expected);
            _auth0ServiceMock.Verify(s => s.GetUsersAsync(2, 10), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_DelegaAlServicioConElUserIdYDto()
        {
            var dto = new Auth0UserDto { UserId = "u1" };
            _auth0ServiceMock.Setup(s => s.UpdateUserAsync("u1", dto)).ReturnsAsync(true);

            Boolean result = await _sut.UpdateUserAsync("u1", dto);

            result.Should().BeTrue();
            _auth0ServiceMock.Verify(s => s.UpdateUserAsync("u1", dto), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAsync_DelegaAlServicio()
        {
            _auth0ServiceMock.Setup(s => s.DeleteUserAsync("u1")).ReturnsAsync(true);

            Boolean result = await _sut.DeleteUserAsync("u1");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task GetUsersByIdAsync_DelegaAlServicio()
        {
            var expected = new Auth0UserDto { UserId = "u1" };
            _auth0ServiceMock.Setup(s => s.GetUsersByIdAsync("u1")).ReturnsAsync(expected);

            Auth0UserDto result = await _sut.GetUsersByIdAsync("u1");

            result.Should().BeSameAs(expected);
        }

        [Fact]
        public async Task GetUsersByIdAsync_DevuelveElRolQueEntregaElServicio()
        {
            var expected = new Auth0UserDto { UserId = "u1", RoleId = "rol_1", RoleName = "ADMIN" };
            _auth0ServiceMock.Setup(s => s.GetUsersByIdAsync("u1")).ReturnsAsync(expected);

            Auth0UserDto result = await _sut.GetUsersByIdAsync("u1");

            result.RoleId.Should().Be("rol_1");
            result.RoleName.Should().Be("ADMIN");
        }

        [Fact]
        public async Task GetRolesAsync_DelegaAlServicioYPasaElToken()
        {
            using var cts = new CancellationTokenSource();
            var expected = new List<Auth0RoleDto> { new() { Id = "rol_1", Name = "ADMIN" }, new() { Id = "rol_2", Name = "CLIENT" } };
            _auth0ServiceMock.Setup(s => s.GetRolesAsync(cts.Token)).ReturnsAsync(expected);

            IList<Auth0RoleDto> result = await _sut.GetRolesAsync(cts.Token);

            result.Should().BeEquivalentTo(expected);
            _auth0ServiceMock.Verify(s => s.GetRolesAsync(cts.Token), Times.Once);
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public async Task SetUserStatusAsync_ActivarDesbloqueaEInactivarBloquea(Boolean isActive, Boolean expectedBlocked)
        {
            _auth0ServiceMock.Setup(s => s.SetUserBlockedAsync("u1", expectedBlocked)).ReturnsAsync(true);

            Boolean result = await _sut.SetUserStatusAsync("u1", isActive);

            result.Should().BeTrue();
            _auth0ServiceMock.Verify(s => s.SetUserBlockedAsync("u1", expectedBlocked), Times.Once);
            _auth0ServiceMock.Verify(s => s.SetUserBlockedAsync("u1", !expectedBlocked), Times.Never);
        }

        [Fact]
        public async Task SetUserStatusAsync_SiElServicioFalla_PropagaLaExcepcion()
        {
            _auth0ServiceMock.Setup(s => s.SetUserBlockedAsync(It.IsAny<String>(), It.IsAny<Boolean>())).ThrowsAsync(new InvalidOperationException("auth0"));

            Func<Task> act = () => _sut.SetUserStatusAsync("u1", true);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
