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
    }
}
