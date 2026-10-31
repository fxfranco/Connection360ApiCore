using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using Connection360.Infrastructure.Caching;
using Connection360.Infrastructure.ExternalApi;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Connection360.Infrastructure.Tests.ExternalApi
{
    public class CachedExternalApiOpenStreetMapTests
    {
        private static OpenStreetMapDto Place(String name, String lat = "1", String lon = "2") =>
            new() { PlaceName = name, Latitud = lat, Longitud = lon };

        private static MemoryCoordinatesCache RealCache() =>
            new(Options.Create(new CoordinatesCacheSettings()), NullLogger<MemoryCoordinatesCache>.Instance);

        [Fact]
        public async Task GetCoordinates_LugarNoCacheado_ConsultaAlAdaptadorRealYDevuelveSuResultado()
        {
            var inner = new Mock<IExternalApiOpenStreetMap>();
            inner.Setup(i => i.GetCoordinates("OPENSTREETMAP", "Bogota", It.IsAny<CancellationToken>())).ReturnsAsync(Place("Bogota, Colombia"));
            using MemoryCoordinatesCache cache = RealCache();
            var sut = new CachedExternalApiOpenStreetMap(inner.Object, cache);

            OpenStreetMapDto result = await sut.GetCoordinates("OPENSTREETMAP", "Bogota", CancellationToken.None);

            result.PlaceName.Should().Be("Bogota, Colombia");
            inner.Verify(i => i.GetCoordinates("OPENSTREETMAP", "Bogota", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetCoordinates_LugarYaCacheado_NoVuelveAlApiExterno()
        {
            var inner = new Mock<IExternalApiOpenStreetMap>();
            inner.Setup(i => i.GetCoordinates(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<CancellationToken>())).ReturnsAsync(Place("Bogota, Colombia"));
            using MemoryCoordinatesCache cache = RealCache();
            var sut = new CachedExternalApiOpenStreetMap(inner.Object, cache);

            await sut.GetCoordinates("OPENSTREETMAP", "Bogota", CancellationToken.None);
            OpenStreetMapDto second = await sut.GetCoordinates("OPENSTREETMAP", "Bogota", CancellationToken.None);
            OpenStreetMapDto third = await sut.GetCoordinates("OPENSTREETMAP", "bogota", CancellationToken.None);

            second.PlaceName.Should().Be("Bogota, Colombia");
            third.PlaceName.Should().Be("Bogota, Colombia");
            inner.Verify(i => i.GetCoordinates(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetCoordinates_OrigenYDestinoDistintos_ConsultaCadaLugarUnaVezYLuegoLosSirveDeCache()
        {
            var inner = new Mock<IExternalApiOpenStreetMap>();
            inner.Setup(i => i.GetCoordinates(It.IsAny<String>(), "China", It.IsAny<CancellationToken>())).ReturnsAsync(Place("China"));
            inner.Setup(i => i.GetCoordinates(It.IsAny<String>(), "Colombia", It.IsAny<CancellationToken>())).ReturnsAsync(Place("Colombia"));
            using MemoryCoordinatesCache cache = RealCache();
            var sut = new CachedExternalApiOpenStreetMap(inner.Object, cache);

            // Dos detalles de envío con el mismo origen/destino (lo habitual).
            for (Int32 i = 0; i < 2; i++)
            {
                (await sut.GetCoordinates("OPENSTREETMAP", "China", CancellationToken.None)).PlaceName.Should().Be("China");
                (await sut.GetCoordinates("OPENSTREETMAP", "Colombia", CancellationToken.None)).PlaceName.Should().Be("Colombia");
            }

            inner.Verify(i => i.GetCoordinates("OPENSTREETMAP", "China", It.IsAny<CancellationToken>()), Times.Once);
            inner.Verify(i => i.GetCoordinates("OPENSTREETMAP", "Colombia", It.IsAny<CancellationToken>()), Times.Once);
            cache.Count.Should().Be(2);
        }

        [Fact]
        public async Task GetCoordinates_MismoLugarEnApisDistintas_NoComparteResultados()
        {
            var inner = new Mock<IExternalApiOpenStreetMap>();
            inner.Setup(i => i.GetCoordinates("API_A", "Bogota", It.IsAny<CancellationToken>())).ReturnsAsync(Place("De A"));
            inner.Setup(i => i.GetCoordinates("API_B", "Bogota", It.IsAny<CancellationToken>())).ReturnsAsync(Place("De B"));
            using MemoryCoordinatesCache cache = RealCache();
            var sut = new CachedExternalApiOpenStreetMap(inner.Object, cache);

            OpenStreetMapDto a = await sut.GetCoordinates("API_A", "Bogota", CancellationToken.None);
            OpenStreetMapDto b = await sut.GetCoordinates("API_B", "Bogota", CancellationToken.None);

            a.PlaceName.Should().Be("De A");
            b.PlaceName.Should().Be("De B");
        }

        [Fact]
        public async Task GetCoordinates_ElApiExternoFalla_PropagaLaExcepcionYNoCacheaElFallo()
        {
            var inner = new Mock<IExternalApiOpenStreetMap>();
            inner.SetupSequence(i => i.GetCoordinates(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("No se encontraron coordenadas"))
                .ReturnsAsync(Place("Bogota, Colombia"));
            using MemoryCoordinatesCache cache = RealCache();
            var sut = new CachedExternalApiOpenStreetMap(inner.Object, cache);

            Func<Task> first = () => sut.GetCoordinates("OPENSTREETMAP", "Bogota", CancellationToken.None);
            await first.Should().ThrowAsync<HttpRequestException>();

            OpenStreetMapDto retry = await sut.GetCoordinates("OPENSTREETMAP", "Bogota", CancellationToken.None);

            retry.PlaceName.Should().Be("Bogota, Colombia");
            inner.Verify(i => i.GetCoordinates(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetCoordinates_LugarVacio_DelegaAlAdaptadorRealSinTocarLaCache(String placeName)
        {
            var inner = new Mock<IExternalApiOpenStreetMap>();
            inner.Setup(i => i.GetCoordinates("OPENSTREETMAP", placeName, It.IsAny<CancellationToken>())).ReturnsAsync(Place("x"));
            var cache = new Mock<ICoordinatesCache>(MockBehavior.Strict);
            var sut = new CachedExternalApiOpenStreetMap(inner.Object, cache.Object);

            await sut.GetCoordinates("OPENSTREETMAP", placeName, CancellationToken.None);

            inner.Verify(i => i.GetCoordinates("OPENSTREETMAP", placeName, It.IsAny<CancellationToken>()), Times.Once);
            cache.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetCoordinates_UsaComoClaveElNombreDeLaApiYElLugar()
        {
            var inner = new Mock<IExternalApiOpenStreetMap>();
            var cache = new Mock<ICoordinatesCache>();
            cache.Setup(c => c.GetOrCreateAsync(It.IsAny<String>(), It.IsAny<Func<CancellationToken, Task<OpenStreetMapDto>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Place("x"));
            var sut = new CachedExternalApiOpenStreetMap(inner.Object, cache.Object);

            await sut.GetCoordinates("OPENSTREETMAP", "Bogota", CancellationToken.None);

            cache.Verify(c => c.GetOrCreateAsync("OPENSTREETMAP|Bogota", It.IsAny<Func<CancellationToken, Task<OpenStreetMapDto>>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetCoordinates_LugarConEspaciosEnLosExtremos_ComparteEntradaConElLugarSinEspacios()
        {
            var inner = new Mock<IExternalApiOpenStreetMap>();
            inner.Setup(i => i.GetCoordinates(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<CancellationToken>())).ReturnsAsync(Place("Bogota, Colombia"));
            using MemoryCoordinatesCache cache = RealCache();
            var sut = new CachedExternalApiOpenStreetMap(inner.Object, cache);

            await sut.GetCoordinates("OPENSTREETMAP", "Bogota", CancellationToken.None);
            await sut.GetCoordinates("OPENSTREETMAP", "  Bogota  ", CancellationToken.None);
            await sut.GetCoordinates("OPENSTREETMAP", "BOGOTA", CancellationToken.None);

            inner.Verify(i => i.GetCoordinates(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<CancellationToken>()), Times.Once);
            cache.Count.Should().Be(1);
        }

        [Fact]
        public async Task GetCoordinates_LaFactoryDeLaCacheLlamaAlAdaptadorRealConElTokenRecibido()
        {
            var inner = new Mock<IExternalApiOpenStreetMap>();
            inner.Setup(i => i.GetCoordinates("OPENSTREETMAP", "Bogota", It.IsAny<CancellationToken>())).ReturnsAsync(Place("Bogota, Colombia"));
            using var cts = new CancellationTokenSource();

            var cache = new Mock<ICoordinatesCache>();
            cache.Setup(c => c.GetOrCreateAsync(It.IsAny<String>(), It.IsAny<Func<CancellationToken, Task<OpenStreetMapDto>>>(), It.IsAny<CancellationToken>()))
                .Returns((String _, Func<CancellationToken, Task<OpenStreetMapDto>> factory, CancellationToken token) => factory(token));
            var sut = new CachedExternalApiOpenStreetMap(inner.Object, cache.Object);

            OpenStreetMapDto result = await sut.GetCoordinates("OPENSTREETMAP", "Bogota", cts.Token);

            result.PlaceName.Should().Be("Bogota, Colombia");
            inner.Verify(i => i.GetCoordinates("OPENSTREETMAP", "Bogota", cts.Token), Times.Once);
        }
    }
}
