using Connection360.Domain.Entities;
using Connection360.Infrastructure.ExternalApi;
using Connection360.Infrastructure.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using Xunit;

namespace Connection360.Infrastructure.Tests.ExternalApi
{
    public class ExternalDataApiGatewayTests
    {
        private static ExternalApiSettings BuildSettings(String apiName = "BPMS")
        {
            var settings = new ExternalApiSettings();
            settings.Apis[apiName] = new ExternalApisDetail { BaseUrl = "https://api.test", DataEndpoint = "https://api.test/data" };
            return settings;
        }

        [Fact]
        public async Task FetchDataAsync_ApiNoConfigurada_DebeLanzarArgumentException()
        {
            var settings = BuildSettings();
            var factory = new FakeHttpClientFactory(FakeHttpMessageHandler.ReturningJson("{}"));
            var gateway = new ExternalDataApiGateway(factory, Options.Create(settings), NullLogger<ExternalDataApiGateway>.Instance);

            Func<Task> act = () => gateway.FetchDataAsync("NO_EXISTE", new Dictionary<String, String>(), CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task FetchDataAsync_RespuestaExitosa_DebeMapearAlDynamicDataSet()
        {
            var settings = BuildSettings();
            const String json = """
            {
                "requestedFields": ["ID", "NOMBRE"],
                "missingColumns": [],
                "rows": [ { "ID": "1", "NOMBRE": "Pedro" } ]
            }
            """;
            var factory = new FakeHttpClientFactory(FakeHttpMessageHandler.ReturningJson(json));
            var gateway = new ExternalDataApiGateway(factory, Options.Create(settings), NullLogger<ExternalDataApiGateway>.Instance);

            DynamicDataSet result = await gateway.FetchDataAsync("BPMS", new Dictionary<String, String>(), CancellationToken.None);

            result.AvailableFields.Should().BeEquivalentTo(new[] { "ID", "NOMBRE" });
            result.Rows.Should().ContainSingle();
            result.Rows.Single()["NOMBRE"].Should().Be("Pedro");
        }

        [Fact]
        public async Task FetchDataAsync_RespuestaConErrorHttp_DebeLanzarHttpRequestException()
        {
            var settings = BuildSettings();
            var factory = new FakeHttpClientFactory(FakeHttpMessageHandler.ReturningStatus(HttpStatusCode.InternalServerError, "error interno"));
            var gateway = new ExternalDataApiGateway(factory, Options.Create(settings), NullLogger<ExternalDataApiGateway>.Instance);

            Func<Task> act = () => gateway.FetchDataAsync("BPMS", new Dictionary<String, String>(), CancellationToken.None);

            await act.Should().ThrowAsync<HttpRequestException>();
        }

        [Fact]
        public async Task FetchDataAsync_ConFiltros_DebeConstruirQueryStringEnLaUrl()
        {
            var settings = BuildSettings();
            const String json = """{ "requestedFields": [], "missingColumns": [], "rows": [] }""";
            var handler = FakeHttpMessageHandler.ReturningJson(json);
            var factory = new FakeHttpClientFactory(handler);
            var gateway = new ExternalDataApiGateway(factory, Options.Create(settings), NullLogger<ExternalDataApiGateway>.Instance);

            var filters = new Dictionary<String, String> { { "clientNit", "900123456" } };

            await gateway.FetchDataAsync("BPMS", filters, CancellationToken.None);

            handler.LastRequest!.RequestUri!.ToString().Should().Contain("clientNit=900123456");
        }

        [Fact]
        public async Task FetchDataAsync_SinFiltros_DebeUsarSoloElEndpointBase()
        {
            var settings = BuildSettings();
            const String json = """{ "requestedFields": [], "missingColumns": [], "rows": [] }""";
            var handler = FakeHttpMessageHandler.ReturningJson(json);
            var factory = new FakeHttpClientFactory(handler);
            var gateway = new ExternalDataApiGateway(factory, Options.Create(settings), NullLogger<ExternalDataApiGateway>.Instance);

            await gateway.FetchDataAsync("BPMS", new Dictionary<String, String>(), CancellationToken.None);

            handler.LastRequest!.RequestUri!.ToString().Should().Be("https://api.test/data");
        }
    }
}
