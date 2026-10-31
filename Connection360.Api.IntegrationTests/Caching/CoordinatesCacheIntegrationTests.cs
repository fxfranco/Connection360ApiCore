using Connection360.Api.IntegrationTests.Infrastructure;
using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using Connection360.Infrastructure.Caching;
using Connection360.Infrastructure.ExternalApi;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Xunit;

namespace Connection360.Api.IntegrationTests.Caching
{
    /// <summary>
    /// Pruebas de integración de la caché de coordenadas: la aplicación real arma el grafo de dependencias
    /// (Program.cs -> AddInfrastructure) y el transporte HTTP hacia Nominatim es un fake que cuenta peticiones.
    /// Cada "petición de usuario" se simula con un scope nuevo, como hace ASP.NET Core.
    /// </summary>
    public class CoordinatesCacheIntegrationTests : IClassFixture<CoordinatesCacheWebApplicationFactory>
    {
        private const String Api = "OPENSTREETMAP";

        private readonly CoordinatesCacheWebApplicationFactory _factory;

        public CoordinatesCacheIntegrationTests(CoordinatesCacheWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.Nominatim.Reset();
        }

        private static String UniquePlace(String prefix) => $"{prefix}-{Guid.NewGuid():N}";

        private static async Task<OpenStreetMapDto> GetInNewScope(WebApplicationFactory<Program> factory, String place, CancellationToken cancellationToken = default)
        {
            using IServiceScope scope = factory.Services.CreateScope();
            IExternalApiOpenStreetMap api = scope.ServiceProvider.GetRequiredService<IExternalApiOpenStreetMap>();
            return await api.GetCoordinates(Api, place, cancellationToken);
        }

        [Fact]
        public void ElComposicionRootEnsamblaDecoradorYCacheSingleton()
        {
            using IServiceScope scope = _factory.Services.CreateScope();

            scope.ServiceProvider.GetRequiredService<IExternalApiOpenStreetMap>().Should().BeOfType<CachedExternalApiOpenStreetMap>();
            _factory.Services.GetRequiredService<ICoordinatesCache>().Should().BeOfType<MemoryCoordinatesCache>();
            _factory.Services.GetRequiredService<ICoordinatesCache>().Should().BeSameAs(_factory.Services.GetRequiredService<ICoordinatesCache>());
        }

        [Fact]
        public async Task MismoLugarEnPeticionesDistintas_ConsultaAlApiExternoUnaSolaVez()
        {
            String place = UniquePlace("Bogota");

            OpenStreetMapDto first = await GetInNewScope(_factory, place);
            OpenStreetMapDto second = await GetInNewScope(_factory, place);
            OpenStreetMapDto third = await GetInNewScope(_factory, place);

            _factory.Nominatim.RequestsFor(place).Should().Be(1);
            first.PlaceName.Should().Be($"{place}, Mundo");
            second.PlaceName.Should().Be(first.PlaceName);
            third.Latitud.Should().Be("4.7110");
            third.Longitud.Should().Be("-74.0721");
        }

        [Fact]
        public async Task OrigenYDestinoDeUnEnvio_SeConsultanUnaVezYLuegoSeSirvenDeCache()
        {
            String origin = UniquePlace("China");
            String destination = UniquePlace("Colombia");

            for (Int32 envio = 0; envio < 3; envio++)
            {
                (await GetInNewScope(_factory, origin)).PlaceName.Should().Be($"{origin}, Mundo");
                (await GetInNewScope(_factory, destination)).PlaceName.Should().Be($"{destination}, Mundo");
            }

            _factory.Nominatim.RequestsFor(origin).Should().Be(1);
            _factory.Nominatim.RequestsFor(destination).Should().Be(1);
            _factory.Nominatim.TotalRequests.Should().Be(2);
        }

        [Fact]
        public async Task ElNombreDelLugarNoDistingueMayusculasNiEspaciosExtremos()
        {
            String place = UniquePlace("Shanghai");

            await GetInNewScope(_factory, place);
            await GetInNewScope(_factory, place.ToUpperInvariant());
            await GetInNewScope(_factory, $"  {place}  ");

            _factory.Nominatim.TotalRequests.Should().Be(1);
        }

        [Fact]
        public async Task PeticionesSimultaneasDelMismoLugar_ConsultanAlApiExternoUnaSolaVez()
        {
            String place = UniquePlace("Rotterdam");
            _factory.Nominatim.Latency = TimeSpan.FromMilliseconds(200);

            OpenStreetMapDto[] results = await Task.WhenAll(Enumerable.Range(0, 15).Select(_ => Task.Run(() => GetInNewScope(_factory, place))));

            _factory.Nominatim.RequestsFor(place).Should().Be(1);
            results.Should().OnlyContain(r => r.PlaceName == $"{place}, Mundo");
        }

        [Fact]
        public async Task ErrorDelApiExterno_SePropagaYNoSeCacheaElFallo()
        {
            String place = UniquePlace("Hamburgo");
            _factory.Nominatim.FailWith = HttpStatusCode.InternalServerError;

            Func<Task> failing = () => GetInNewScope(_factory, place);
            await failing.Should().ThrowAsync<HttpRequestException>();

            // El API se recupera: la siguiente consulta vuelve a intentarlo y ahora sí se cachea.
            _factory.Nominatim.FailWith = null;
            OpenStreetMapDto recovered = await GetInNewScope(_factory, place);
            await GetInNewScope(_factory, place);

            recovered.PlaceName.Should().Be($"{place}, Mundo");
            _factory.Nominatim.RequestsFor(place).Should().Be(2, "una fallida y una exitosa; la tercera salió de la caché");
        }

        [Fact]
        public async Task ConElLimiteDeMemoriaLleno_ExpulsaLosLugaresMasViejosYNuncaSuperaElTope()
        {
            // El tope solo admite ~3 lugares de este tamaño.
            String suffix = Guid.NewGuid().ToString("N");
            String PlaceN(Int32 n) => $"L{n}-{suffix}";
            OpenStreetMapDto sample = new() { PlaceName = $"{PlaceN(1)}, Mundo", Latitud = "4.7110", Longitud = "-74.0721" };
            Int64 oneEntry = MemoryCoordinatesCache.EstimateEntrySize($"{Api}|{PlaceN(1)}".ToUpperInvariant(), sample);
            Int64 limit = (oneEntry * 3) + 1;

            using WebApplicationFactory<Program> small = _factory.WithWebHostBuilder(b => b.UseSetting("CoordinatesCache:MaxSizeBytes", limit.ToString()));
            var cache = (MemoryCoordinatesCache)small.Services.GetRequiredService<ICoordinatesCache>();

            for (Int32 n = 1; n <= 3; n++)
            {
                await GetInNewScope(small, PlaceN(n));
                await Task.Delay(30);
            }
            _factory.Nominatim.TotalRequests.Should().Be(3);

            // El cuarto lugar no cabe: sale el más viejo (L1) y los demás siguen en la caché.
            await GetInNewScope(small, PlaceN(4));
            cache.CurrentSizeBytes.Should().BeLessThanOrEqualTo(limit);

            await GetInNewScope(small, PlaceN(2));
            await GetInNewScope(small, PlaceN(3));
            await GetInNewScope(small, PlaceN(4));
            _factory.Nominatim.TotalRequests.Should().Be(4, "L2, L3 y L4 se sirven de la caché");

            await GetInNewScope(small, PlaceN(1));
            _factory.Nominatim.RequestsFor(PlaceN(1)).Should().Be(2, "L1 fue expulsado y hay que volver a pedirlo");
            cache.CurrentSizeBytes.Should().BeLessThanOrEqualTo(limit);
        }

        [Fact]
        public async Task CacheDeshabilitada_CadaConsultaVaAlApiExterno()
        {
            String place = UniquePlace("Lima");

            using WebApplicationFactory<Program> disabled = _factory.WithWebHostBuilder(b => b.UseSetting("CoordinatesCache:Enabled", "false"));

            await GetInNewScope(disabled, place);
            await GetInNewScope(disabled, place);

            _factory.Nominatim.RequestsFor(place).Should().Be(2);
            disabled.Services.GetService<ICoordinatesCache>().Should().BeNull();
            using IServiceScope scope = disabled.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<IExternalApiOpenStreetMap>().Should().BeOfType<ExternalApiOpenStreetMap>();
        }

        [Fact]
        public void ConfiguracionDeCacheInvalida_ImpideCrearLaCache()
        {
            using WebApplicationFactory<Program> invalid = _factory.WithWebHostBuilder(b => b.UseSetting("CoordinatesCache:MaxSizeBytes", "0"));

            Action act = () => { _ = invalid.Services; };

            // ValidateOnStart hace que el host ni siquiera arranque con un tope de memoria inválido.
            act.Should().Throw<Exception>().Which.ToString().Should().Contain("MaxSizeBytes");
        }
    }
}
