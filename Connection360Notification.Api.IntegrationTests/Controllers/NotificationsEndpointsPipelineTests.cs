using Connection360Notification.Api.IntegrationTests.Infrastructure;
using Connection360Notification.Application.DTOs;
using FluentAssertions;
using Moq;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Connection360Notification.Api.IntegrationTests.Controllers
{
    /// <summary>
    /// Pruebas del pipeline HTTP completo (routing, versionado, autenticación/autorización, ApiResponseFilter,
    /// ExceptionHandlingMiddleware y NotFoundMiddleware) sobre la aplicación real, con casos de uso simulados.
    /// </summary>
    public class NotificationsEndpointsPipelineTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const String AllNotificationsUrl = "/api/v1/notifications/allnotifications?idClient=123";
        private const String ReadUrl = "/api/v1/notifications/readnotification/123/1";
        private const String TestEndpointUrl = "/api/v1/notifications/allnotificationslistTest";

        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public NotificationsEndpointsPipelineTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.ResetMocks();
            _client = _factory.CreateClient();
        }

        private static HttpRequestMessage Authorized(HttpMethod method, String url, String? roles = "CLIENT")
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("X-Test-Auth", "true");
            if (roles is not null)
            {
                request.Headers.Add("X-Test-Roles", roles);
            }
            return request;
        }

        private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
        {
            String content = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(content);
            return doc.RootElement.Clone();
        }

        // ---------- Envoltorio ApiResponse (ApiResponseFilter) ----------

        [Fact]
        public async Task GetAllNotifications_ConRolValido_EnvuelveLaListaEnApiResponse()
        {
            _factory.NotificationsUseCaseMock
                .Setup(u => u.ExecuteGetNotificationsByClientAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<NotificationsListResponse>
                {
                    new() { IdNotification = 5, ClientId = "123", Title = "Titulo", Message = "Mensaje" }
                });

            HttpResponseMessage response = await _client.SendAsync(Authorized(HttpMethod.Get, AllNotificationsUrl));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
            JsonElement body = await ReadJson(response);
            body.GetProperty("status").GetInt32().Should().Be(200);
            body.GetProperty("message").GetString().Should().Be("Solicitud exitosa");
            body.GetProperty("path").GetString().Should().Be("/api/v1/notifications/allnotifications");
            body.GetProperty("error").ValueKind.Should().Be(JsonValueKind.Null);
            JsonElement data = body.GetProperty("dataResponse");
            data.GetArrayLength().Should().Be(1);
            data[0].GetProperty("idNotification").GetInt64().Should().Be(5);
            data[0].GetProperty("title").GetString().Should().Be("Titulo");
        }

        [Fact]
        public async Task GetAllNotifications_EnviaElIdClientDeLaQueryStringAlCasoDeUso()
        {
            ClientSummaryRequest? captured = null;
            _factory.NotificationsUseCaseMock
                .Setup(u => u.ExecuteGetNotificationsByClientAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>()))
                .Callback<ClientSummaryRequest, CancellationToken>((r, _) => captured = r)
                .ReturnsAsync(new List<NotificationsListResponse>());

            await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/notifications/allnotifications?idClient=cliente-abc"));

            captured.Should().NotBeNull();
            captured!.IdClient.Should().Be("cliente-abc");
            captured.RoleName.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllNotifications_SinIdClient_Retorna400ConElMensajeDeValidacion()
        {
            HttpResponseMessage response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/notifications/allnotifications"));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            JsonElement body = await ReadJson(response);
            body.GetProperty("status").GetInt32().Should().Be(400);
            body.GetProperty("message").GetString().Should().Be("Errores de validación");
            body.GetProperty("error").GetString().Should().Contain("idClient");
            body.GetProperty("path").GetString().Should().Be("/api/v1/notifications/allnotifications");
            _factory.NotificationsUseCaseMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UpdateReadNotification_ConIdNotificacionNoNumerico_Retorna400ConElMensajeDeValidacion()
        {
            HttpResponseMessage response = await _client.SendAsync(Authorized(HttpMethod.Patch, "/api/v1/notifications/readnotification/123/abc"));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            JsonElement body = await ReadJson(response);
            body.GetProperty("message").GetString().Should().Be("Errores de validación");
            body.GetProperty("error").GetString().Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task UpdateReadNotification_EnviaClienteEIdDeLaRutaAlCasoDeUso()
        {
            NotificationsRequest? captured = null;
            _factory.NotificationsUseCaseMock
                .Setup(u => u.ExecuteMarkAsReadAsync(It.IsAny<NotificationsRequest>(), It.IsAny<CancellationToken>()))
                .Callback<NotificationsRequest, CancellationToken>((r, _) => captured = r)
                .ReturnsAsync(true);

            HttpResponseMessage response = await _client.SendAsync(Authorized(HttpMethod.Patch, "/api/v1/notifications/readnotification/cli-9/77"));

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            captured.Should().NotBeNull();
            captured!.IdClient.Should().Be("cli-9");
            captured.IdNotification.Should().Be(77);
        }

        [Fact]
        public async Task UpdateReadNotification_CuandoNoExiste_Retorna404ConElEnvoltorioDeError()
        {
            _factory.NotificationsUseCaseMock
                .Setup(u => u.ExecuteMarkAsReadAsync(It.IsAny<NotificationsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            HttpResponseMessage response = await _client.SendAsync(Authorized(HttpMethod.Patch, ReadUrl));

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            JsonElement body = await ReadJson(response);
            body.GetProperty("status").GetInt32().Should().Be(404);
            // Con [ApiController], NotFound() se convierte en ProblemDetails antes del ApiResponseFilter;
            // el filtro usa su título/detalle como mensaje de error (no el nombre del tipo).
            body.GetProperty("error").GetString().Should().Be("Not Found");
            body.GetProperty("message").GetString().Should().Be("Recurso no encontrado");
        }

        // ---------- Autorización por roles ----------

        [Theory]
        [InlineData("ADMIN")]
        [InlineData("CLIENT")]
        [InlineData("ANALISTAOPE")]
        [InlineData("ANALISTASAC")]
        [InlineData("OTRO,CLIENT")]
        public async Task GetAllNotifications_ConCualquierRolPermitido_Retorna200(String roles)
        {
            _factory.NotificationsUseCaseMock
                .Setup(u => u.ExecuteGetNotificationsByClientAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<NotificationsListResponse>());

            HttpResponseMessage response = await _client.SendAsync(Authorized(HttpMethod.Get, AllNotificationsUrl, roles));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Theory]
        [InlineData("INVITADO")]
        [InlineData("admin")]
        public async Task GetAllNotifications_ConRolNoPermitido_Retorna403(String roles)
        {
            HttpResponseMessage response = await _client.SendAsync(Authorized(HttpMethod.Get, AllNotificationsUrl, roles));

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            _factory.NotificationsUseCaseMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetAllNotifications_AutenticadoSinRoles_Retorna403()
        {
            HttpResponseMessage response = await _client.SendAsync(Authorized(HttpMethod.Get, AllNotificationsUrl, roles: null));

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task UpdateReadNotification_ConRolNoPermitido_Retorna403()
        {
            HttpResponseMessage response = await _client.SendAsync(Authorized(HttpMethod.Patch, ReadUrl, "INVITADO"));

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            _factory.NotificationsUseCaseMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetAllNotifications_ConEncabezadoDeAutenticacionDistintoDeTrue_Retorna401()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, AllNotificationsUrl);
            request.Headers.Add("X-Test-Auth", "false");

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ---------- Endpoint anónimo ----------

        [Fact]
        public async Task GetAllNotificationsDB_SinAutenticacion_Retorna200ConLaListaEnvuelta()
        {
            _factory.NotificationsUseCaseMock
                .Setup(u => u.ExecuteGetNotificationsAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<NotificationsListResponse> { new() { IdNotification = 1 }, new() { IdNotification = 2 } });

            HttpResponseMessage response = await _client.GetAsync(TestEndpointUrl);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            JsonElement body = await ReadJson(response);
            body.GetProperty("dataResponse").GetArrayLength().Should().Be(2);
            body.GetProperty("path").GetString().Should().Be("/api/v1/notifications/allnotificationslistTest");
        }

        // ---------- ExceptionHandlingMiddleware ----------

        [Theory]
        [InlineData(typeof(KeyNotFoundException), HttpStatusCode.NotFound, "Recurso no encontrado")]
        [InlineData(typeof(UnauthorizedAccessException), HttpStatusCode.Unauthorized, "No autorizado")]
        [InlineData(typeof(ArgumentException), HttpStatusCode.BadRequest, "Solicitud inválida")]
        [InlineData(typeof(InvalidOperationException), HttpStatusCode.InternalServerError, "Error interno del servidor")]
        public async Task ExcepcionNoControlada_SeConvierteEnRespuestaEstandarSegunElTipo(Type exceptionType, HttpStatusCode expectedStatus, String expectedMessage)
        {
            var exception = (Exception)Activator.CreateInstance(exceptionType, "detalle-del-fallo")!;
            _factory.NotificationsUseCaseMock
                .Setup(u => u.ExecuteGetNotificationsAllAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            HttpResponseMessage response = await _client.GetAsync(TestEndpointUrl);

            response.StatusCode.Should().Be(expectedStatus);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
            JsonElement body = await ReadJson(response);
            body.GetProperty("status").GetInt32().Should().Be((Int32)expectedStatus);
            body.GetProperty("message").GetString().Should().Be(expectedMessage);
            body.GetProperty("error").GetString().Should().Contain("detalle-del-fallo");
            body.GetProperty("path").GetString().Should().Be("/api/v1/notifications/allnotificationslistTest");
        }

        // ---------- NotFoundMiddleware / rutas ----------

        [Fact]
        public async Task RutaInexistente_Retorna404ConElEnvoltorioDeRutaNoEncontrada()
        {
            HttpResponseMessage response = await _client.GetAsync("/api/v1/no-existe");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            JsonElement body = await ReadJson(response);
            body.GetProperty("status").GetInt32().Should().Be(404);
            body.GetProperty("message").GetString().Should().Be("Ruta no encontrada");
            body.GetProperty("error").GetString().Should().Be("El recurso solicitado no existe");
            body.GetProperty("path").GetString().Should().Be("/api/v1/no-existe");
        }

        [Fact]
        public async Task RutaRaiz_Retorna404ConElEnvoltorioDeRutaNoEncontrada()
        {
            HttpResponseMessage response = await _client.GetAsync("/");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await ReadJson(response)).GetProperty("message").GetString().Should().Be("Ruta no encontrada");
        }

        [Fact]
        public async Task GetAllNotifications_ConMetodoHttpNoPermitido_Retorna405()
        {
            HttpResponseMessage response = await _client.SendAsync(Authorized(HttpMethod.Post, AllNotificationsUrl));

            response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        }

        [Fact]
        public async Task EndpointDeEnvioDeNotificaciones_ComentadoEnElControlador_NoEstaExpuesto()
        {
            var request = Authorized(HttpMethod.Post, "/api/v1/notifications/simulatenotifications", "ADMIN");
            request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // ---------- Versionado ----------

        [Fact]
        public async Task Respuesta_IncluyeElEncabezadoConLasVersionesSoportadas()
        {
            _factory.NotificationsUseCaseMock
                .Setup(u => u.ExecuteGetNotificationsAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<NotificationsListResponse>());

            HttpResponseMessage response = await _client.GetAsync(TestEndpointUrl);

            response.Headers.TryGetValues("api-supported-versions", out IEnumerable<String>? versions).Should().BeTrue();
            versions!.Should().Contain(v => v.Contains("1.0"));
        }

        [Fact]
        public async Task VersionDeApiNoSoportada_NoLlegaAlControlador()
        {
            HttpResponseMessage response = await _client.GetAsync("/api/v2/notifications/allnotificationslistTest");

            ((Int32)response.StatusCode).Should().BeOneOf(400, 404);
            _factory.NotificationsUseCaseMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task RutaSinSegmentoDeVersion_Retorna404()
        {
            HttpResponseMessage response = await _client.GetAsync("/api/notifications/allnotificationslistTest");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // ---------- Health / error ----------

        [Fact]
        public async Task Health_Retorna200Healthy()
        {
            HttpResponseMessage response = await _client.GetAsync("/health");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadAsStringAsync()).Should().Be("Healthy");
        }

        [Fact]
        public async Task Error_RetornaProblemDetailsConElTituloEsperado()
        {
            HttpResponseMessage response = await _client.GetAsync("/error");

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
            JsonElement body = await ReadJson(response);
            body.GetProperty("title").GetString().Should().Be("Ha ocurrido un error inesperado.");
            body.GetProperty("status").GetInt32().Should().Be(500);
        }

        // ---------- Swagger (solo Development) ----------

        [Fact]
        public async Task Swagger_EnDevelopment_PublicaElDocumentoOpenApiConLaSeguridadBearer()
        {
            HttpResponseMessage response = await _client.GetAsync("/swagger/v1/swagger.json");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            JsonElement doc = await ReadJson(response);
            doc.GetProperty("info").GetProperty("title").GetString().Should().Be("Connection 360 API Notifications");
            doc.GetProperty("info").GetProperty("version").GetString().Should().Be("v1");
            JsonElement paths = doc.GetProperty("paths");
            paths.TryGetProperty("/api/v1/notifications/allnotifications", out _).Should().BeTrue();
            paths.TryGetProperty("/api/v1/notifications/readnotification/{idClient}/{idNotification}", out _).Should().BeTrue();
            paths.TryGetProperty("/api/v1/notifications/allnotificationslistTest", out _).Should().BeTrue();
            JsonElement bearer = doc.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
            bearer.GetProperty("scheme").GetString().Should().Be("Bearer");
            bearer.GetProperty("bearerFormat").GetString().Should().Be("JWT");
        }

        [Fact]
        public async Task SwaggerUI_EnDevelopment_RetornaLaPaginaHtml()
        {
            HttpResponseMessage response = await _client.GetAsync("/swagger/index.html");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
        }
    }
}
