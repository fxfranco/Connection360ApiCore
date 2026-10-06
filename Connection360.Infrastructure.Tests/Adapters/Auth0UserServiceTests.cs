using Connection360.Domain.Dtos;
using Connection360.Domain.Interfaces;
using Connection360.Infrastructure.Adapters.Output;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Connection360.Infrastructure.Tests.Adapters
{
    /// <summary>
    /// Auth0UserService construye internamente un ManagementClient (sin posibilidad de inyectar un
    /// HttpClient), por lo que solo se prueban las rutas que no tocan la red: sin configuracion de
    /// Auth0Management el cliente no puede crearse y el servicio registra el error y lo propaga.
    /// </summary>
    public class Auth0UserServiceTests
    {
        private static Auth0UserService BuildWithoutConfiguration()
        {
            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<String, String?>()).Build();
            return new Auth0UserService(config);
        }

        [Fact]
        public void Auth0UserService_ImplementaElPuertoDeDominio()
        {
            BuildWithoutConfiguration().Should().BeAssignableTo<IAuth0UserService>();
        }

        [Fact]
        public async Task GetUsersAsync_SinConfiguracionDeAuth0_PropagaLaExcepcion()
        {
            Func<Task> act = () => BuildWithoutConfiguration().GetUsersAsync(0, 10);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task UpdateUserAsync_SinConfiguracionDeAuth0_PropagaLaExcepcion()
        {
            Func<Task> act = () => BuildWithoutConfiguration().UpdateUserAsync("auth0|1", new Auth0UserDto { UserName = "x", Email = "a@b.c" });

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GetUsersByIdAsync_SinConfiguracionDeAuth0_PropagaLaExcepcion()
        {
            Func<Task> act = () => BuildWithoutConfiguration().GetUsersByIdAsync("auth0|1");

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task DeleteUserAsync_SinConfiguracionDeAuth0_PropagaLaExcepcion()
        {
            Func<Task> act = () => BuildWithoutConfiguration().DeleteUserAsync("auth0|1");

            await act.Should().ThrowAsync<ArgumentException>();
        }
    }
}
