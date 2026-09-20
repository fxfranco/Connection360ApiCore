using Connection360.Application.DTOs;
using Connection360.Infrastructure.ExternalApi;
using Connection360.Infrastructure.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using Xunit;

namespace Connection360.Infrastructure.Tests.ExternalApi
{
    public class ExternalApiOpenStreetMapTests
    {
        private static ExternalApiSettings BuildSettings()
        {
            var settings = new ExternalApiSettings();
            settings.Apis["OPENSTREETMAP"] = new ExternalApisDetail
            {
                BaseUrl = "https://nominatim.test",
                DataEndpoint = "https://nominatim.test/search?q="
            };
            return settings;
        }

        [Fact]
        public async Task GetCoordinates_ApiNoConfigurada_DebeLanzarArgumentException()
        {
            var settings = BuildSettings();
            var factory = new FakeHttpClientFactory(FakeHttpMessageHandler.ReturningJson("[]"));
            var service = new ExternalApiOpenStreetMap(factory, Options.Create(settings), NullLogger<Connection360.Infrastructure.ExternalApi.ExternalDataApiGateway>.Instance);

            Func<Task> act = () => service.GetCoordinates("NO_EXISTE", "Bogota", CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GetCoordinates_RespuestaConResultados_DebeMapearElPrimerResultado()
        {
            var settings = BuildSettings();
            const String json = """
            [
                { "display_name": "Bogota, Colombia", "lat": "4.7110", "lon": "-74.0721" }
            ]
            """;
            var factory = new FakeHttpClientFactory(FakeHttpMessageHandler.ReturningJson(json));
            var service = new ExternalApiOpenStreetMap(factory, Options.Create(settings), NullLogger<Connection360.Infrastructure.ExternalApi.ExternalDataApiGateway>.Instance);

            OpenStreetMapDto result = await service.GetCoordinates("OPENSTREETMAP", "Bogota", CancellationToken.None);

            result.PlaceName.Should().Be("Bogota, Colombia");
            result.Latitud.Should().Be("4.7110");
            result.Longitud.Should().Be("-74.0721");
        }

        [Fact]
        public async Task GetCoordinates_SinResultados_DebeLanzarHttpRequestException()
        {
            var settings = BuildSettings();
            var factory = new FakeHttpClientFactory(FakeHttpMessageHandler.ReturningJson("[]"));
            var service = new ExternalApiOpenStreetMap(factory, Options.Create(settings), NullLogger<Connection360.Infrastructure.ExternalApi.ExternalDataApiGateway>.Instance);

            Func<Task> act = () => service.GetCoordinates("OPENSTREETMAP", "LugarInexistente", CancellationToken.None);

            await act.Should().ThrowAsync<HttpRequestException>();
        }

        [Fact]
        public async Task GetCoordinates_RespuestaConErrorHttp_DebeLanzarHttpRequestException()
        {
            var settings = BuildSettings();
            var factory = new FakeHttpClientFactory(FakeHttpMessageHandler.ReturningStatus(HttpStatusCode.BadGateway));
            var service = new ExternalApiOpenStreetMap(factory, Options.Create(settings), NullLogger<Connection360.Infrastructure.ExternalApi.ExternalDataApiGateway>.Instance);

            Func<Task> act = () => service.GetCoordinates("OPENSTREETMAP", "Bogota", CancellationToken.None);

            await act.Should().ThrowAsync<HttpRequestException>();
        }

        [Fact]
        public async Task GetCoordinates_DebeEscaparElNombreDelLugarEnLaUrl()
        {
            var settings = BuildSettings();
            const String json = """[ { "display_name": "X", "lat": "1", "lon": "2" } ]""";
            var handler = FakeHttpMessageHandler.ReturningJson(json);
            var factory = new FakeHttpClientFactory(handler);
            var service = new ExternalApiOpenStreetMap(factory, Options.Create(settings), NullLogger<Connection360.Infrastructure.ExternalApi.ExternalDataApiGateway>.Instance);

            await service.GetCoordinates("OPENSTREETMAP", "San Jose & Cia", CancellationToken.None);

            handler.LastRequest!.RequestUri!.AbsoluteUri.Should().Contain(Uri.EscapeDataString("San Jose & Cia"));
        }
    }
}
