using Connection360.Api.IntegrationTests.Infrastructure;
using Connection360.Application.DTOs.Persistence;
using Connection360.Domain.Exceptions;
using FluentAssertions;
using Moq;
using System.Net;
using System.Net.Http.Json;
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
        public async Task CreateCustomerDataBase_ConRolAdmin_Retorna201()
        {
            _factory.CustomerUseCaseMock.Setup(u => u.CrearAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(77);

            HttpResponseMessage response = await _client.SendAsync(AuthorizedRequest(HttpMethod.Get, "/api/v1/settings/createcustomerdb?clientId=123", roles: "ADMIN"));

            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task CreateCollaboratorDataBase_ConRolAdmin_Retorna201()
        {
            _factory.CollaboratorUseCaseMock.Setup(u => u.CreateAsync("COL-1", It.IsAny<CancellationToken>())).ReturnsAsync(15);

            HttpResponseMessage response = await _client.SendAsync(AuthorizedRequest(HttpMethod.Get, "/api/v1/settings/createcollaboratordb?clientId=COL-1", roles: "ADMIN"));

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            _factory.CollaboratorUseCaseMock.Verify(u => u.CreateAsync("COL-1", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("/api/v1/settings/createcustomerdb?clientId=123")]
        [InlineData("/api/v1/settings/createcollaboratordb?clientId=COL-1")]
        [InlineData("/api/v1/settings/createcustomercollaboratordb?clientId=CUS-1&collaborator=COL-1")]
        public async Task EndpointsDeAprovisionamiento_SinAutenticacion_Retorna401(String url)
        {
            HttpResponseMessage response = await _client.GetAsync(url);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Theory]
        [InlineData("/api/v1/settings/createcustomerdb?clientId=123")]
        [InlineData("/api/v1/settings/createcollaboratordb?clientId=COL-1")]
        [InlineData("/api/v1/settings/createcustomercollaboratordb?clientId=CUS-1&collaborator=COL-1")]
        public async Task EndpointsDeAprovisionamiento_ConRolCliente_Retorna403PorqueRequierenAdmin(String url)
        {
            HttpResponseMessage response = await _client.SendAsync(AuthorizedRequest(HttpMethod.Get, url, roles: "CLIENT"));

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CreateCustomerCollaboratorDataBase_ConDatosValidos_Retorna201()
        {
            _factory.CollaboratorUseCaseMock
                .Setup(u => u.CreateCustomerCollaboratorAsync("CUS-1", "COL-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(99);

            HttpResponseMessage response = await _client.SendAsync(AuthorizedRequest(HttpMethod.Get, "/api/v1/settings/createcustomercollaboratordb?clientId=CUS-1&collaborator=COL-1", roles: "ADMIN"));

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            _factory.CollaboratorUseCaseMock.Verify(u => u.CreateCustomerCollaboratorAsync("CUS-1", "COL-1", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateCustomerCollaboratorDataBase_ConClienteInexistente_Retorna400ConElManejadorGlobal()
        {
            _factory.CollaboratorUseCaseMock
                .Setup(u => u.CreateCustomerCollaboratorAsync(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("El cliente no existe", "customerId"));

            HttpResponseMessage response = await _client.SendAsync(AuthorizedRequest(HttpMethod.Get, "/api/v1/settings/createcustomercollaboratordb?clientId=CUS-X&collaborator=COL-1", roles: "ADMIN"));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateCustomerDataBase_ConIdentificacionDuplicada_Retorna409ConElManejadorGlobal()
        {
            _factory.CustomerUseCaseMock
                .Setup(u => u.CrearAsync("123", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DuplicateRecordException("Ya existe un cliente con la identificación '123'."));

            HttpResponseMessage response = await _client.SendAsync(AuthorizedRequest(HttpMethod.Get, "/api/v1/settings/createcustomerdb?clientId=123", roles: "ADMIN"));

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task CreateCollaboratorDataBase_ConIdentificacionDuplicada_Retorna409ConElManejadorGlobal()
        {
            _factory.CollaboratorUseCaseMock
                .Setup(u => u.CreateAsync("COL-1", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DuplicateRecordException("Ya existe un colaborador con la identificación 'COL-1'."));

            HttpResponseMessage response = await _client.SendAsync(AuthorizedRequest(HttpMethod.Get, "/api/v1/settings/createcollaboratordb?clientId=COL-1", roles: "ADMIN"));

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task CreateCustomerCollaboratorDataBase_ConRelacionDuplicada_Retorna409ConElManejadorGlobal()
        {
            _factory.CollaboratorUseCaseMock
                .Setup(u => u.CreateCustomerCollaboratorAsync("CUS-1", "COL-1", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DuplicateRecordException("El colaborador 'COL-1' ya está asociado al cliente 'CUS-1'."));

            HttpResponseMessage response = await _client.SendAsync(AuthorizedRequest(HttpMethod.Get, "/api/v1/settings/createcustomercollaboratordb?clientId=CUS-1&collaborator=COL-1", roles: "ADMIN"));

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task CreateMasterSettings_ConConfiguracionExistente_Retorna409ConElManejadorGlobal()
        {
            _factory.MasterSettingsUseCaseMock
                .Setup(u => u.CreateAsync(It.IsAny<CreateMasterSettingsDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DuplicateRecordException("Ya existe una configuración maestra; solo se permite un registro."));

            var request = AuthorizedRequest(HttpMethod.Post, "/api/v1/settings/createmaster", roles: "ADMIN");
            request.Content = JsonContent.Create(new CreateMasterSettingsDto(true, false, true, "COP", "es-CO", "America/Bogota", 30));

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
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

        [Fact]
        public async Task GetUsersListSettings_DevuelveLosDatosDeUserMetadataEnCamelCase()
        {
            _factory.UserManagementUseCaseMock
                .Setup(u => u.GetAllUsersAsync(It.IsAny<Connection360.Application.DTOs.UsersManagementRequest>()))
                .ReturnsAsync(new List<Connection360.Domain.Dtos.Auth0UserDto>
                {
                    new() { UserId = "auth0|1", Company = "Acme SAS", Document = "900123456", FullName = "Ana Perez", Phone = "3113232323", RoleId = "rol_1", RoleName = "ADMIN" }
                });

            var request = AuthorizedRequest(HttpMethod.Get, "/api/v1/settings/listusers?page=1&size=10", roles: "ADMIN");

            HttpResponseMessage response = await _client.SendAsync(request);
            String body = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            body.Should().Contain("\"company\":\"Acme SAS\"").And.Contain("\"document\":\"900123456\"")
                .And.Contain("\"fullName\":\"Ana Perez\"").And.Contain("\"phone\":\"3113232323\"")
                .And.Contain("\"roleName\":\"ADMIN\"");
        }

        [Fact]
        public async Task GetRolesListSettings_SinAutenticacion_Retorna401()
        {
            HttpResponseMessage response = await _client.GetAsync("/api/v1/settings/listroles");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetRolesListSettings_ConRolCliente_Retorna403PorqueRequiereAdmin()
        {
            var request = AuthorizedRequest(HttpMethod.Get, "/api/v1/settings/listroles", roles: "CLIENT");

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetRolesListSettings_ConRolAdmin_Retorna200ConLosRolesYSusIds()
        {
            _factory.UserManagementUseCaseMock
                .Setup(u => u.GetRolesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Connection360.Domain.Dtos.Auth0RoleDto>
                {
                    new() { Id = "rol_1", Name = "ADMIN" },
                    new() { Id = "rol_2", Name = "CLIENT" }
                });

            var request = AuthorizedRequest(HttpMethod.Get, "/api/v1/settings/listroles", roles: "ADMIN");

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            String body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("rol_1").And.Contain("rol_2").And.Contain("ADMIN").And.Contain("CLIENT");
        }

        [Fact]
        public async Task UpdateUserStatusSettings_SinAutenticacion_Retorna401()
        {
            var request = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/settings/updateuserstatus/auth0%7C1")
            {
                Content = JsonContent.Create(new Connection360.Application.DTOs.UsersManagementStatusRequest { IsActive = false })
            };

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task UpdateUserStatusSettings_ConRolCliente_Retorna403PorqueRequiereAdmin()
        {
            var request = AuthorizedRequest(HttpMethod.Patch, "/api/v1/settings/updateuserstatus/auth0%7C1", roles: "CLIENT");
            request.Content = JsonContent.Create(new Connection360.Application.DTOs.UsersManagementStatusRequest { IsActive = false });

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UpdateUserStatusSettings_ConRolAdmin_Retorna204YPasaElEstadoAlCasoDeUso(Boolean isActive)
        {
            _factory.UserManagementUseCaseMock
                .Setup(u => u.SetUserStatusAsync("auth0|1", isActive))
                .ReturnsAsync(true);

            var request = AuthorizedRequest(HttpMethod.Patch, "/api/v1/settings/updateuserstatus/auth0%7C1", roles: "ADMIN");
            request.Content = JsonContent.Create(new Connection360.Application.DTOs.UsersManagementStatusRequest { IsActive = isActive });

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            _factory.UserManagementUseCaseMock.Verify(u => u.SetUserStatusAsync("auth0|1", isActive), Times.Once);
        }

        [Fact]
        public async Task UpdateUsersByIdSettings_ConRolAdminYRolNuevo_PasaElRoleIdAlCasoDeUso()
        {
            _factory.UserManagementUseCaseMock
                .Setup(u => u.UpdateUserAsync("auth0|1", It.IsAny<Connection360.Domain.Dtos.Auth0UserDto>()))
                .ReturnsAsync(true);

            var request = AuthorizedRequest(HttpMethod.Patch, "/api/v1/settings/updateuser/auth0%7C1", roles: "ADMIN");
            request.Content = JsonContent.Create(new Connection360.Domain.Dtos.Auth0UserDto { Email = "nuevo@correo.com", PhoneNumber = "+573113232323", RoleId = "rol_2" });

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            _factory.UserManagementUseCaseMock.Verify(u => u.UpdateUserAsync("auth0|1",
                It.Is<Connection360.Domain.Dtos.Auth0UserDto>(d => d.RoleId == "rol_2" && d.Email == "nuevo@correo.com" && d.PhoneNumber == "+573113232323")), Times.Once);
        }
    }
}
