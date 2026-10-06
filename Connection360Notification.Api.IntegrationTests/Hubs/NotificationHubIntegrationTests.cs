using Connection360Notification.Api.IntegrationTests.Infrastructure;
using Connection360Notification.Application.Ports.Output;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Connection360Notification.Api.IntegrationTests.Hubs
{
    /// <summary>
    /// Pruebas del hub de SignalR (NotificationHub) mapeado en Program.cs y de SignalRNotifierService,
    /// usando el cliente WebSocket en memoria del TestServer (sin red real ni puertos).
    /// </summary>
    public class NotificationHubIntegrationTests : IClassFixture<RealNotifierWebApplicationFactory>
    {
        private const String HubPath = "/api/v1/hubs/notifications";
        private const Char RecordSeparator = '\u001e';
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);

        private readonly RealNotifierWebApplicationFactory _factory;

        public NotificationHubIntegrationTests(RealNotifierWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.ResetMocks();
        }

        // ---------- negociación HTTP ----------

        [Fact]
        public async Task Negotiate_SinAutenticacion_Retorna401()
        {
            using HttpClient client = _factory.CreateClient();

            HttpResponseMessage response = await client.PostAsync($"{HubPath}/negotiate?negotiateVersion=1&idClient=c1", content: null);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Negotiate_ConRolNoPermitido_Retorna403()
        {
            using HttpClient client = _factory.CreateClient();
            using var request = NegotiateRequest("INVITADO");

            HttpResponseMessage response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Theory]
        [InlineData("ADMIN")]
        [InlineData("CLIENT")]
        [InlineData("ANALISTAOPE")]
        [InlineData("ANALISTASAC")]
        public async Task Negotiate_ConRolPermitido_RetornaIdDeConexionYTransportes(String role)
        {
            using HttpClient client = _factory.CreateClient();
            using var request = NegotiateRequest(role);

            HttpResponseMessage response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            doc.RootElement.GetProperty("connectionId").GetString().Should().NotBeNullOrWhiteSpace();
            doc.RootElement.GetProperty("availableTransports").EnumerateArray()
                .Select(t => t.GetProperty("transport").GetString())
                .Should().Contain("WebSockets");
        }

        private static HttpRequestMessage NegotiateRequest(String role)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{HubPath}/negotiate?negotiateVersion=1&idClient=c1");
            request.Headers.Add("X-Test-Auth", "true");
            request.Headers.Add("X-Test-Roles", role);
            return request;
        }

        // ---------- WebSocket + SignalRNotifierService ----------

        [Fact]
        public async Task SendNotificationToUser_EnviaElMensajeSoloALaConexionDelClienteDestino()
        {
            using var cts = new CancellationTokenSource(Timeout);
            using WebSocket cliente1 = await ConnectAsync("cliente-1", "CLIENT", cts.Token);
            using WebSocket cliente2 = await ConnectAsync("cliente-2", "ADMIN", cts.Token);

            using (IServiceScope scope = _factory.Services.CreateScope())
            {
                var notifier = scope.ServiceProvider.GetRequiredService<INotifierService>();
                await notifier.SendNotificationToUserAsync("cliente-1", "para-uno", new { Dato = 1 });
                await notifier.SendNotificationToUserAsync("cliente-2", "para-dos", null);
            }

            JsonElement mensaje1 = await ReceiveInvocationAsync(cliente1, cts.Token);
            JsonElement mensaje2 = await ReceiveInvocationAsync(cliente2, cts.Token);

            mensaje1.GetProperty("target").GetString().Should().Be("ReceiveNotification");
            JsonElement argumentos1 = mensaje1.GetProperty("arguments")[0];
            argumentos1.GetProperty("message").GetString().Should().Be("para-uno");
            argumentos1.GetProperty("data").GetProperty("dato").GetInt32().Should().Be(1);
            argumentos1.GetProperty("timestamp").GetDateTime().Should().BeAfter(DateTime.UtcNow.AddMinutes(-5));

            mensaje2.GetProperty("target").GetString().Should().Be("ReceiveNotification");
            JsonElement argumentos2 = mensaje2.GetProperty("arguments")[0];
            argumentos2.GetProperty("message").GetString().Should().Be("para-dos");
            argumentos2.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);

            await CloseAsync(cliente1);
            await CloseAsync(cliente2);
        }

        [Fact]
        public async Task SendNotificationToUser_CuandoElClienteNoEstaConectado_NoLanzaExcepcion()
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            var notifier = scope.ServiceProvider.GetRequiredService<INotifierService>();

            Func<Task> act = () => notifier.SendNotificationToUserAsync("nadie-conectado", "hola");

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task Conexion_SinIdClientEnLaQueryString_SeAceptaPeroNoRecibeNotificacionesDirigidas()
        {
            using var cts = new CancellationTokenSource(Timeout);
            using WebSocket anonimo = await ConnectAsync(idClient: null, "CLIENT", cts.Token);
            using WebSocket conCliente = await ConnectAsync("cliente-3", "CLIENT", cts.Token);

            using (IServiceScope scope = _factory.Services.CreateScope())
            {
                var notifier = scope.ServiceProvider.GetRequiredService<INotifierService>();
                await notifier.SendNotificationToUserAsync("cliente-3", "solo-cliente-3");
            }

            // Si la conexion sin idClient hubiese recibido el mensaje, este seria su primer frame de invocacion.
            JsonElement recibido = await ReceiveInvocationAsync(conCliente, cts.Token);
            recibido.GetProperty("arguments")[0].GetProperty("message").GetString().Should().Be("solo-cliente-3");

            await CloseAsync(anonimo);
            await CloseAsync(conCliente);
        }

        [Fact]
        public async Task Conexion_SinAutenticacion_EsRechazada()
        {
            using var cts = new CancellationTokenSource(Timeout);
            WebSocketClient wsClient = _factory.Server.CreateWebSocketClient();

            Func<Task> act = () => wsClient.ConnectAsync(new Uri($"ws://localhost{HubPath}?idClient=c1"), cts.Token);

            await act.Should().ThrowAsync<Exception>();
        }

        private async Task<WebSocket> ConnectAsync(String? idClient, String role, CancellationToken cancellationToken)
        {
            WebSocketClient wsClient = _factory.Server.CreateWebSocketClient();
            wsClient.ConfigureRequest = request =>
            {
                request.Headers["X-Test-Auth"] = "true";
                request.Headers["X-Test-Roles"] = role;
            };

            String query = idClient is null ? String.Empty : $"?idClient={idClient}";
            WebSocket socket = await wsClient.ConnectAsync(new Uri($"ws://localhost{HubPath}{query}"), cancellationToken);

            // Handshake del protocolo JSON de SignalR
            await SendTextAsync(socket, "{\"protocol\":\"json\",\"version\":1}" + RecordSeparator, cancellationToken);
            String handshakeResponse = await ReceiveFrameAsync(socket, cancellationToken);
            handshakeResponse.Should().Be("{}");

            return socket;
        }

        private static async Task SendTextAsync(WebSocket socket, String text, CancellationToken cancellationToken)
        {
            await socket.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
        }

        /// <summary>Lee un mensaje de texto completo y devuelve su primer frame (sin el separador de registro).</summary>
        private static async Task<String> ReceiveFrameAsync(WebSocket socket, CancellationToken cancellationToken)
        {
            var buffer = new Byte[4096];
            var builder = new StringBuilder();
            while (true)
            {
                WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, cancellationToken);
                result.MessageType.Should().NotBe(WebSocketMessageType.Close, "el hub no debe cerrar la conexion");
                builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                String current = builder.ToString();
                Int32 separator = current.IndexOf(RecordSeparator);
                if (separator >= 0)
                {
                    return current[..separator];
                }
            }
        }

        /// <summary>Espera la siguiente invocacion de cliente (type 1), ignorando pings (type 6).</summary>
        private static async Task<JsonElement> ReceiveInvocationAsync(WebSocket socket, CancellationToken cancellationToken)
        {
            while (true)
            {
                String frame = await ReceiveFrameAsync(socket, cancellationToken);
                using JsonDocument doc = JsonDocument.Parse(frame);
                if (doc.RootElement.GetProperty("type").GetInt32() == 1)
                {
                    return doc.RootElement.Clone();
                }
            }
        }

        private static async Task CloseAsync(WebSocket socket)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "fin", cts.Token);
            }
            catch (Exception)
            {
                // El cierre es solo limpieza; un fallo aqui no invalida la prueba.
            }
        }
    }
}
