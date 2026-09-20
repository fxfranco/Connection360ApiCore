using Connection360.Api.IntegrationTests.Infrastructure;
using Connection360.Application.DTOs.Persistence;
using FluentAssertions;
using Moq;
using System.Net;
using Xunit;

namespace Connection360.Api.IntegrationTests.Controllers
{
    public class SettingsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public SettingsControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.ResetMocks();
            _client = _factory.CreateClient();
        }

        private static HttpRequestMessage AuthorizedRequest(HttpMethod method, String url, String roles)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("X-Test-Auth", "true");
            request.Headers.Add("X-Test-Roles", roles);
            return request;
        }

        [Fact]
        public async Task GetMasterSettings_SinAutenticacion_Retorna401()
        {
            HttpResponseMessage response = await _client.GetAsync("/api/v1/settings/viewmaster");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetMasterSettings_ConRolCliente_Retorna403PorqueRequiereAdmin()
        {
            var request = AuthorizedRequest(HttpMethod.Get, "/api/v1/settings/viewmaster", roles: "CLIENT");

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetMasterSettings_ConRolAdmin_Retorna200()
        {
            _factory.MasterSettingsUseCaseMock.Setup(u => u.GetAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Connection360.Application.DTOs.MasterSettingsResponse { IdMasterSettings = 1 });

            var request = AuthorizedRequest(HttpMethod.Get, "/api/v1/settings/viewmaster", roles: "ADMIN");

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task CreateCustomerDataBase_EsPublicoPorAllowAnonymousExplicito_Retorna201()
        {
            _factory.CustomerUseCaseMock.Setup(u => u.CrearAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(77);

            HttpResponseMessage response = await _client.GetAsync("/api/v1/settings/createcustomerdb?clientId=123");

            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task CreateCollaboratorDataBase_EsPublicoPorAllowAnonymousExplicito_Retorna201()
        {
            _factory.CollaboratorUseCaseMock.Setup(u => u.CreateAsync("COL-1", It.IsAny<CancellationToken>())).ReturnsAsync(15);

            HttpResponseMessage response = await _client.GetAsync("/api/v1/settings/createcollaboratordb?clientId=COL-1");

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            _factory.CollaboratorUseCaseMock.Verify(u => u.CreateAsync("COL-1", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateCustomerCollaboratorDataBase_ConDatosValidos_Retorna201()
        {
            _factory.CollaboratorUseCaseMock
                .Setup(u => u.CreateCustomerCollaboratorAsync("CUS-1", "COL-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(99);

            HttpResponseMessage response = await _client.GetAsync("/api/v1/settings/createcustomercollaboratordb?clientId=CUS-1&collaborator=COL-1");

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            _factory.CollaboratorUseCaseMock.Verify(u => u.CreateCustomerCollaboratorAsync("CUS-1", "COL-1", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateCustomerCollaboratorDataBase_ConClienteInexistente_Retorna400ConElManejadorGlobal()
        {
            _factory.CollaboratorUseCaseMock
                .Setup(u => u.CreateCustomerCollaboratorAsync(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("El cliente no existe", "customerId"));

            HttpResponseMessage response = await _client.GetAsync("/api/v1/settings/createcustomercollaboratordb?clientId=CUS-X&collaborator=COL-1");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetUsersListSettings_ConRolAdmin_Retorna200()
        {
            _factory.UserManagementUseCaseMock
                .Setup(u => u.GetAllUsersAsync(It.IsAny<Connection360.Application.DTOs.UsersManagementRequest>()))
                .ReturnsAsync(new List<Connection360.Domain.Dtos.Auth0UserDto>());

            var request = AuthorizedRequest(HttpMethod.Get, "/api/v1/settings/listusers?page=1&size=10", roles: "ADMIN");

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
