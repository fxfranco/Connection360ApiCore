using Connection360.Api.IntegrationTests.Infrastructure;
using Connection360.Application.DTOs;
using Connection360.Observability.Application.Metrics;
using Connection360.Observability.Application.Pipeline;
using Connection360.Observability.Domain.Models;
using Connection360.Observability.Domain.Ports;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using Xunit;

namespace Connection360.Api.IntegrationTests.Hosting
{
    /// <summary>
    /// Verifica, con el Program.cs real, que una petición HTTP genera los 3 pilares (traza, log y métrica)
    /// identificados como "ApiCore" y listos para guardarse en MongoDB (aquí, en memoria).
    /// </summary>
    public class ObservabilityIntegrationTests : IClassFixture<ObservabilityWebApplicationFactory>
    {
        private const String TotalsUrl = "/api/v1/home/totals?idClient=1";

        private readonly ObservabilityWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ObservabilityIntegrationTests(ObservabilityWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        /// <summary>Petición autenticada que propone su propio TraceId (cabecera W3C traceparent, muestreada) para poder rastrearla.</summary>
        private static HttpRequestMessage Authorized(String url, String traceId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Test-Auth", "true");
            request.Headers.Add("X-Test-Roles", "CLIENT");
            request.Headers.Add("traceparent", $"00-{traceId}-0123456789abcdef-01");
            return request;
        }

        private static String NewTraceId() => Guid.NewGuid().ToString("N");

        [Fact]
        public async Task Peticion_GeneraUnaTrazaDeServidorConLaIdentidadDeApiCore()
        {
            String traceId = NewTraceId();
            _factory.ClientSummaryUseCaseMock
                .Setup(u => u.ExecuteTotalsAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ClientSummaryResponse());

            HttpResponseMessage response = await _client.SendAsync(Authorized(TotalsUrl, traceId));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            await RecordingTelemetryStore.UntilAsync(
                () => _factory.Store.Traces.Any(t => t.Kind == "Server" && t.TraceId == traceId), "llegó la traza de la petición");
            TraceRecord trace = _factory.Store.Traces.First(t => t.Kind == "Server" && t.TraceId == traceId);
            trace.Service.Should().Be(ObservedServices.ApiCore);
            trace.Environment.Should().Be("Development");
            trace.Name.Should().StartWith("GET /api/v").And.EndWith("/home/totals");
            trace.Attributes["url.path"].Should().Be("/api/v1/home/totals");
            trace.Attributes["http.response.status_code"].Should().Be(200);
            trace.Attributes.Should().NotContainKey("url.query");
            trace.ParentSpanId.Should().Be("0123456789abcdef");
        }

        [Fact]
        public async Task ErrorNoControlado_GeneraLogDeErrorCorrelacionadoConLaTraza()
        {
            String traceId = NewTraceId();
            String marker = "err-" + Guid.NewGuid().ToString("N");
            _factory.ClientSummaryUseCaseMock
                .Setup(u => u.ExecuteTotalsAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("falla simulada " + marker));

            HttpResponseMessage response = await _client.SendAsync(Authorized(TotalsUrl, traceId));

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            await RecordingTelemetryStore.UntilAsync(
                () => _factory.Store.Logs.Any(l => l.TraceId == traceId && l.Level == "Error")
                      && _factory.Store.Traces.Any(t => t.Kind == "Server" && t.TraceId == traceId),
                "llegaron el log de error y la traza");

            LogRecord log = _factory.Store.Logs.First(l => l.TraceId == traceId && l.Level == "Error");
            TraceRecord trace = _factory.Store.Traces.First(t => t.Kind == "Server" && t.TraceId == traceId);
            log.Service.Should().Be(ObservedServices.ApiCore);
            log.ExceptionMessage.Should().Contain(marker);
            log.ExceptionType.Should().Be(typeof(InvalidOperationException).FullName);
            trace.Attributes["http.response.status_code"].Should().Be(500);
            trace.Status.Should().Be("Error");
        }

        [Fact]
        public async Task Peticion_GeneraMetricasDeHttpAlForzarLaEmision()
        {
            _factory.ClientSummaryUseCaseMock
                .Setup(u => u.ExecuteTotalsAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ClientSummaryResponse());
            await _client.SendAsync(Authorized(TotalsUrl, NewTraceId()));

            _factory.Services.GetRequiredService<MetricsTelemetryCollector>().Collect();

            await RecordingTelemetryStore.UntilAsync(
                () => _factory.Store.Metrics.Any(m => m.Name == "http.server.request.duration"), "llegó la métrica de duración HTTP");
            MetricRecord metric = _factory.Store.Metrics.First(m => m.Name == "http.server.request.duration");
            metric.Service.Should().Be(ObservedServices.ApiCore);
            metric.Kind.Should().Be("Histogram");
        }

        [Fact]
        public async Task LogEscritoPorLaAplicacion_LlegaAlAlmacenamientoConSuCategoria()
        {
            String marker = Guid.NewGuid().ToString("N");

            _factory.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Pruebas.Observabilidad").LogWarning("Mensaje de prueba {Marca}", marker);

            await RecordingTelemetryStore.UntilAsync(() => _factory.Store.Logs.Any(l => Equals(l.Attributes.GetValueOrDefault("Marca"), marker)), "llegó el log");
            LogRecord log = _factory.Store.Logs.First(l => Equals(l.Attributes.GetValueOrDefault("Marca"), marker));
            log.Category.Should().Be("Pruebas.Observabilidad");
            log.Service.Should().Be("ApiCore");
        }

        [Fact]
        public async Task PeticionALaRutaDeSalud_NoGeneraTrazaPeroLasDemasSi()
        {
            String healthTraceId = NewTraceId();
            String apiTraceId = NewTraceId();
            _factory.ClientSummaryUseCaseMock
                .Setup(u => u.ExecuteTotalsAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ClientSummaryResponse());
            var health = new HttpRequestMessage(HttpMethod.Get, "/health");
            health.Headers.Add("traceparent", $"00-{healthTraceId}-0123456789abcdef-01");

            await _client.SendAsync(health);
            await _client.SendAsync(Authorized(TotalsUrl, apiTraceId));

            await RecordingTelemetryStore.UntilAsync(() => _factory.Store.Traces.Any(t => t.TraceId == apiTraceId), "llegó la traza de la API");
            _factory.Store.Traces.Should().NotContain(t => t.TraceId == healthTraceId, "/health está en Observability:Traces:ExcludePaths");
        }

        [Fact]
        public async Task ElInicializadorDeAlmacenamientoSeEjecutaAlArrancar()
        {
            await RecordingTelemetryStore.UntilAsync(() => _factory.Store.Initialized, "se ejecutó el inicializador");
            _factory.Services.GetServices<IObservabilityComponent>().Should().HaveCount(6);
        }
    }

    public class ObservabilityDisabledIntegrationTests : IClassFixture<ObservabilityDisabledWebApplicationFactory>
    {
        private readonly ObservabilityDisabledWebApplicationFactory _factory;

        public ObservabilityDisabledIntegrationTests(ObservabilityDisabledWebApplicationFactory factory) => _factory = factory;

        [Fact]
        public async Task ConObservabilidadDeshabilitada_NoSeRegistraNadaYLaApiSigueFuncionando()
        {
            _factory.ClientSummaryUseCaseMock
                .Setup(u => u.ExecuteTotalsAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ClientSummaryResponse());
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/home/totals?idClient=1");
            request.Headers.Add("X-Test-Auth", "true");
            request.Headers.Add("X-Test-Roles", "CLIENT");

            HttpResponseMessage response = await _factory.CreateClient().SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            _factory.Services.GetServices<IObservabilityComponent>().Should().BeEmpty();
            _factory.Services.GetServices<ILogStore>().Should().BeEmpty();
        }
    }
}
