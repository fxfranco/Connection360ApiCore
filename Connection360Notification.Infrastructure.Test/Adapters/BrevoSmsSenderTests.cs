using Connection360Notification.Infrastructure.Sms;
using FluentAssertions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Adapters
{
    /// <summary>Adaptador de Brevo probado con un <see cref="HttpMessageHandler"/> simulado (no se llama a Brevo).</summary>
    public class BrevoSmsSenderTests
    {
        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _respond;

            public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) => _respond = respond;

            public List<(HttpRequestMessage Request, String Body)> Calls { get; } = new();

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                String body = request.Content == null ? String.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
                Calls.Add((request, body));
                return await _respond(request, cancellationToken);
            }
        }

        private static StubHandler Responde(HttpStatusCode status, String body) =>
            new((_, _) => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) }));

        private static BrevoSmsSettings Configuracion(Action<BrevoSmsSettings>? ajustar = null)
        {
            var settings = new BrevoSmsSettings { ApiKey = "xkeysib-secreta", Sender = "Conexion360" };
            ajustar?.Invoke(settings);
            return settings;
        }

        private static BrevoSmsSender Crear(StubHandler handler, BrevoSmsSettings? settings = null) =>
            new(new HttpClient(handler), Options.Create(settings ?? Configuracion()));

        private static readonly SmsMessage Mensaje = new("573113232323", "Conexión360\nHola");

        [Fact]
        public void EsUnISmsSender()
        {
            Crear(Responde(HttpStatusCode.Created, "{}")).Should().BeAssignableTo<ISmsSender>();
        }

        [Fact]
        public async Task SendAsync_HacePostAlEndpointDeSmsTransaccionalConLaApiKeyEnElEncabezado()
        {
            var handler = Responde(HttpStatusCode.Created, "{\"messageId\":1511882900100020}");

            String? id = await Crear(handler).SendAsync(Mensaje, CancellationToken.None);

            id.Should().Be("1511882900100020");
            var (request, _) = handler.Calls.Should().ContainSingle().Subject;
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.ToString().Should().Be("https://api.brevo.com/v3/transactionalSMS/send");
            request.Headers.GetValues("api-key").Should().ContainSingle().Which.Should().Be("xkeysib-secreta");
            request.Headers.Accept.Should().Contain(h => h.MediaType == "application/json");
            request.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
        }

        [Fact]
        public async Task SendAsync_EnviaRemitenteDestinatarioContenidoYUnicode()
        {
            var handler = Responde(HttpStatusCode.Created, "{\"messageId\":1}");

            await Crear(handler).SendAsync(Mensaje, CancellationToken.None);

            using JsonDocument json = JsonDocument.Parse(handler.Calls.Single().Body);
            JsonElement root = json.RootElement;
            root.GetProperty("sender").GetString().Should().Be("Conexion360");
            root.GetProperty("recipient").GetString().Should().Be("573113232323");
            root.GetProperty("content").GetString().Should().Be("Conexión360\nHola");
            root.GetProperty("type").GetString().Should().Be("transactional");
            root.GetProperty("unicodeEnabled").GetBoolean().Should().BeTrue("el texto lleva tildes y comillas tipográficas");
        }

        [Theory]
        [InlineData("https://api.brevo.com/v3")]
        [InlineData("https://api.brevo.com/v3/")]
        [InlineData("  https://api.brevo.com/v3  ")]
        public async Task SendAsync_NormalizaLaUrlBase(String baseUrl)
        {
            var handler = Responde(HttpStatusCode.Created, "{}");

            await Crear(handler, Configuracion(s => s.BaseUrl = baseUrl)).SendAsync(Mensaje, CancellationToken.None);

            handler.Calls.Single().Request.RequestUri!.ToString().Should().Be("https://api.brevo.com/v3/transactionalSMS/send");
        }

        [Fact]
        public async Task SendAsync_ConUrlBaseVacia_UsaLaDePorDefecto()
        {
            var handler = Responde(HttpStatusCode.Created, "{}");

            await Crear(handler, Configuracion(s => s.BaseUrl = "")).SendAsync(Mensaje, CancellationToken.None);

            handler.Calls.Single().Request.RequestUri!.ToString().Should().Be("https://api.brevo.com/v3/transactionalSMS/send");
        }

        [Fact]
        public async Task SendAsync_ConRespuestaSinMessageId_RetornaNull()
        {
            String? id = await Crear(Responde(HttpStatusCode.Created, "")).SendAsync(Mensaje, CancellationToken.None);

            id.Should().BeNull();
        }

        [Fact]
        public async Task SendAsync_ConRespuestaDeError_LanzaHttpRequestExceptionConElCodigoYElMensajeDeBrevo()
        {
            var handler = Responde(HttpStatusCode.BadRequest, "{\"code\":\"invalid_parameter\",\"message\":\"Sender is invalid\"}");

            Func<Task> act = () => Crear(handler).SendAsync(Mensaje, CancellationToken.None);

            var ex = (await act.Should().ThrowAsync<HttpRequestException>()).Which;
            ex.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            ex.Message.Should().Contain("400").And.Contain("invalid_parameter").And.Contain("Sender is invalid");
            ex.Message.Should().NotContain("xkeysib-secreta").And.NotContain("573113232323");
        }

        [Fact]
        public async Task SendAsync_ConErrorQueNoEsJson_IncluyeElCuerpoRecortado()
        {
            var handler = Responde(HttpStatusCode.BadGateway, new String('x', 1000));

            Func<Task> act = () => Crear(handler).SendAsync(Mensaje, CancellationToken.None);

            var ex = (await act.Should().ThrowAsync<HttpRequestException>()).Which;
            ex.Message.Should().Contain("502").And.EndWith("...");
            ex.Message.Length.Should().BeLessThan(450);
        }

        [Fact]
        public async Task SendAsync_ConErrorSinCuerpo_IndicaSinDetalle()
        {
            Func<Task> act = () => Crear(Responde(HttpStatusCode.Unauthorized, "")).SendAsync(Mensaje, CancellationToken.None);

            (await act.Should().ThrowAsync<HttpRequestException>()).Which.Message.Should().Contain("401").And.Contain("sin detalle");
        }

        [Fact]
        public async Task SendAsync_SiBrevoNoRespondeAntesDelTiempoLimite_LanzaTimeoutException()
        {
            var handler = new StubHandler(async (_, token) =>
            {
                await Task.Delay(Timeout.Infinite, token);
                return new HttpResponseMessage(HttpStatusCode.Created);
            });

            Func<Task> act = () => Crear(handler, Configuracion(s => s.TimeoutSeconds = 1)).SendAsync(Mensaje, CancellationToken.None);

            await act.Should().ThrowAsync<TimeoutException>();
        }

        [Fact]
        public async Task SendAsync_SiElLlamadorCancela_PropagaLaCancelacionYNoUnTimeout()
        {
            var handler = new StubHandler(async (_, token) =>
            {
                await Task.Delay(Timeout.Infinite, token);
                return new HttpResponseMessage(HttpStatusCode.Created);
            });
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(100));

            Func<Task> act = () => Crear(handler).SendAsync(Mensaje, cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Theory]
        [InlineData("", "Conexion360")]
        [InlineData("clave", "")]
        public async Task SendAsync_ConConfiguracionIncompleta_LanzaInvalidOperationExceptionSinLlamarABrevo(String apiKey, String sender)
        {
            var handler = Responde(HttpStatusCode.Created, "{}");

            Func<Task> act = () => Crear(handler, Configuracion(s => { s.ApiKey = apiKey; s.Sender = sender; })).SendAsync(Mensaje, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
            handler.Calls.Should().BeEmpty();
        }

        [Fact]
        public async Task SendAsync_ConMensajeNulo_LanzaArgumentNullException()
        {
            Func<Task> act = () => Crear(Responde(HttpStatusCode.Created, "{}")).SendAsync(null!, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Theory]
        [InlineData("{\"messageId\":\"abc-1\"}", "abc-1")]
        [InlineData("{\"messageId\":42}", "42")]
        [InlineData("{\"otro\":1}", null)]
        [InlineData("[1,2]", null)]
        [InlineData("no es json", null)]
        [InlineData("", null)]
        public void ReadMessageId_InterpretaLasVariantesDeLaRespuesta(String body, String? esperado)
        {
            BrevoSmsSender.ReadMessageId(body).Should().Be(esperado);
        }

        [Theory]
        [InlineData("{\"code\":\"c\",\"message\":\"m\"}", "c - m")]
        [InlineData("{\"message\":\"solo mensaje\"}", "solo mensaje")]
        [InlineData("{\"code\":\"solo_codigo\"}", "solo_codigo")]
        [InlineData("{\"x\":1}", "{\"x\":1}")]
        [InlineData("texto plano", "texto plano")]
        [InlineData("  ", "sin detalle")]
        public void DescribeError_ExtraeCodigoYMensajeOCaeAlCuerpo(String body, String esperado)
        {
            BrevoSmsSender.DescribeError(body).Should().Be(esperado);
        }
    }
}
