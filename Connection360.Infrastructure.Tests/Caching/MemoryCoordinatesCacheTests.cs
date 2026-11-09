using Connection360.Application.DTOs;
using Connection360.Infrastructure.Caching;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Connection360.Infrastructure.Tests.Caching
{
    public class MemoryCoordinatesCacheTests
    {
        // Pausa entre inserciones/lecturas para que el "último acceso" de cada entrada sea distinto y el
        // orden de evicción (la más vieja primero) sea determinista.
        private const Int32 AccessSpacingMs = 30;

        private static OpenStreetMapDto Place(String name = "Bogota, Colombia", String lat = "4.7110", String lon = "-74.0721") =>
            new() { PlaceName = name, Latitud = lat, Longitud = lon };

        private static MemoryCoordinatesCache BuildCache(CoordinatesCacheSettings? settings = null) =>
            new(Options.Create(settings ?? new CoordinatesCacheSettings()), NullLogger<MemoryCoordinatesCache>.Instance);

        /// <summary>Settings cuyo tope de memoria admite exactamente <paramref name="entries"/> entradas del tamaño de <see cref="Place"/>.</summary>
        private static CoordinatesCacheSettings SettingsForEntries(Int32 entries, Double evictionPercentage = 0.2)
        {
            Int64 oneEntry = MemoryCoordinatesCache.EstimateEntrySize("KEY0", Place());
            return new CoordinatesCacheSettings { MaxSizeBytes = (oneEntry * entries) + 1, EvictionPercentage = evictionPercentage };
        }

        private sealed class CountingFactory
        {
            public Int32 Calls;
            public Task<OpenStreetMapDto> Create(CancellationToken _)
            {
                Interlocked.Increment(ref Calls);
                return Task.FromResult(Place());
            }
        }

        [Fact]
        public async Task GetOrCreateAsync_PrimeraConsulta_EjecutaLaFactoryYGuardaElResultado()
        {
            using MemoryCoordinatesCache cache = BuildCache();
            var factory = new CountingFactory();

            OpenStreetMapDto result = await cache.GetOrCreateAsync("OSM|Bogota", factory.Create, CancellationToken.None);

            result.PlaceName.Should().Be("Bogota, Colombia");
            result.Latitud.Should().Be("4.7110");
            result.Longitud.Should().Be("-74.0721");
            factory.Calls.Should().Be(1);
            cache.Count.Should().Be(1);
        }

        [Fact]
        public async Task GetOrCreateAsync_SegundaConsultaDelMismoLugar_NoEjecutaLaFactory()
        {
            using MemoryCoordinatesCache cache = BuildCache();
            var factory = new CountingFactory();

            await cache.GetOrCreateAsync("OSM|Bogota", factory.Create, CancellationToken.None);
            OpenStreetMapDto second = await cache.GetOrCreateAsync("OSM|Bogota", factory.Create, CancellationToken.None);

            factory.Calls.Should().Be(1);
            second.PlaceName.Should().Be("Bogota, Colombia");
            cache.Count.Should().Be(1);
        }

        [Theory]
        [InlineData("osm|bogota")]
        [InlineData("OSM|BOGOTA")]
        [InlineData("  OSM|Bogota  ")]
        public async Task GetOrCreateAsync_LaClaveNoDistingueMayusculasNiEspaciosEnLosExtremos(String variant)
        {
            using MemoryCoordinatesCache cache = BuildCache();
            var factory = new CountingFactory();

            await cache.GetOrCreateAsync("OSM|Bogota", factory.Create, CancellationToken.None);
            await cache.GetOrCreateAsync(variant, factory.Create, CancellationToken.None);

            factory.Calls.Should().Be(1);
            cache.Count.Should().Be(1);
        }

        [Fact]
        public async Task GetOrCreateAsync_LugaresDistintos_GuardaUnaEntradaPorCadaUno()
        {
            using MemoryCoordinatesCache cache = BuildCache();
            var factory = new CountingFactory();

            await cache.GetOrCreateAsync("OSM|Bogota", factory.Create, CancellationToken.None);
            await cache.GetOrCreateAsync("OSM|Shanghai", factory.Create, CancellationToken.None);

            factory.Calls.Should().Be(2);
            cache.Count.Should().Be(2);
        }

        [Fact]
        public async Task GetOrCreateAsync_SiLaFactoryFalla_PropagaLaExcepcionYNoGuardaNada()
        {
            using MemoryCoordinatesCache cache = BuildCache();
            Int32 calls = 0;
            Task<OpenStreetMapDto> Failing(CancellationToken _)
            {
                calls++;
                throw new HttpRequestException("API caida");
            }

            Func<Task> first = () => cache.GetOrCreateAsync("OSM|Bogota", Failing, CancellationToken.None);
            await first.Should().ThrowAsync<HttpRequestException>();
            cache.Count.Should().Be(0);

            // El fallo no se cachea: la siguiente consulta vuelve a intentar contra el API.
            OpenStreetMapDto recovered = await cache.GetOrCreateAsync("OSM|Bogota", _ => Task.FromResult(Place()), CancellationToken.None);

            recovered.PlaceName.Should().Be("Bogota, Colombia");
            calls.Should().Be(1);
            cache.Count.Should().Be(1);
        }

        [Fact]
        public async Task GetOrCreateAsync_DevuelveCopias_ModificarElResultadoNoAlteraLaCache()
        {
            using MemoryCoordinatesCache cache = BuildCache();
            var factory = new CountingFactory();

            OpenStreetMapDto first = await cache.GetOrCreateAsync("OSM|Bogota", factory.Create, CancellationToken.None);
            first.PlaceName = "ALTERADO";
            first.Latitud = "0";

            OpenStreetMapDto second = await cache.GetOrCreateAsync("OSM|Bogota", factory.Create, CancellationToken.None);
            second.Longitud = "999";
            OpenStreetMapDto third = await cache.GetOrCreateAsync("OSM|Bogota", factory.Create, CancellationToken.None);

            third.PlaceName.Should().Be("Bogota, Colombia");
            third.Latitud.Should().Be("4.7110");
            third.Longitud.Should().Be("-74.0721");
            factory.Calls.Should().Be(1);
        }

        [Fact]
        public async Task GetOrCreateAsync_LaMemoriaUsadaCoincideConElTamanoEstimadoDeLaEntrada()
        {
            using MemoryCoordinatesCache cache = BuildCache();
            OpenStreetMapDto value = Place();

            await cache.GetOrCreateAsync("OSM|Bogota", _ => Task.FromResult(value), CancellationToken.None);

            cache.CurrentSizeBytes.Should().Be(MemoryCoordinatesCache.EstimateEntrySize("OSM|BOGOTA", value));
        }

        [Fact]
        public void EstimateEntrySize_CrecaConLaLongitudDelTexto()
        {
            Int64 small = MemoryCoordinatesCache.EstimateEntrySize("K", Place("A", "1", "2"));
            Int64 large = MemoryCoordinatesCache.EstimateEntrySize("K", Place(new String('x', 100), "1", "2"));

            // 99 caracteres más = 198 bytes más (UTF-16).
            (large - small).Should().Be(198);
        }

        [Fact]
        public async Task GetOrCreateAsync_AlLlegarAlLimite_NuncaSuperaElTopeDeMemoria()
        {
            CoordinatesCacheSettings settings = SettingsForEntries(3);
            using MemoryCoordinatesCache cache = BuildCache(settings);

            for (Int32 i = 0; i < 20; i++)
            {
                await cache.GetOrCreateAsync($"KEY{i}", _ => Task.FromResult(Place()), CancellationToken.None);
                cache.CurrentSizeBytes.Should().BeLessThanOrEqualTo(settings.MaxSizeBytes);
            }

            cache.Count.Should().BeLessThanOrEqualTo(3);
        }

        [Fact]
        public async Task GetOrCreateAsync_AlLlegarAlLimite_EliminaPrimeroLaEntradaMasVieja()
        {
            using MemoryCoordinatesCache cache = BuildCache(SettingsForEntries(3));
            var factory = new CountingFactory();

            foreach (String key in new[] { "KEY1", "KEY2", "KEY3" })
            {
                await cache.GetOrCreateAsync(key, factory.Create, CancellationToken.None);
                await Task.Delay(AccessSpacingMs);
            }
            factory.Calls.Should().Be(3);

            // KEY4 no cabe: debe salir KEY1 (la más vieja) y conservarse KEY2 y KEY3.
            await cache.GetOrCreateAsync("KEY4", factory.Create, CancellationToken.None);
            factory.Calls.Should().Be(4);

            await cache.GetOrCreateAsync("KEY2", factory.Create, CancellationToken.None);
            await cache.GetOrCreateAsync("KEY3", factory.Create, CancellationToken.None);
            await cache.GetOrCreateAsync("KEY4", factory.Create, CancellationToken.None);
            factory.Calls.Should().Be(4, "KEY2, KEY3 y KEY4 siguen en la caché");

            await cache.GetOrCreateAsync("KEY1", factory.Create, CancellationToken.None);
            factory.Calls.Should().Be(5, "KEY1 fue la entrada eliminada");
        }

        [Fact]
        public async Task GetOrCreateAsync_AlLlegarAlLimite_ConservaLasEntradasConsultadasRecientemente()
        {
            using MemoryCoordinatesCache cache = BuildCache(SettingsForEntries(3));
            var factory = new CountingFactory();

            foreach (String key in new[] { "KEY1", "KEY2", "KEY3" })
            {
                await cache.GetOrCreateAsync(key, factory.Create, CancellationToken.None);
                await Task.Delay(AccessSpacingMs);
            }

            // KEY1 se consulta de nuevo: pasa a ser la más reciente y KEY2 queda como la más vieja.
            await cache.GetOrCreateAsync("KEY1", factory.Create, CancellationToken.None);
            await Task.Delay(AccessSpacingMs);

            await cache.GetOrCreateAsync("KEY4", factory.Create, CancellationToken.None);
            Int32 callsBefore = factory.Calls;

            await cache.GetOrCreateAsync("KEY1", factory.Create, CancellationToken.None);
            factory.Calls.Should().Be(callsBefore, "KEY1 se usó recientemente y no debe evictarse");

            await cache.GetOrCreateAsync("KEY2", factory.Create, CancellationToken.None);
            factory.Calls.Should().Be(callsBefore + 1, "KEY2 era la menos usada y fue evictada");
        }

        [Fact]
        public async Task GetOrCreateAsync_ConPocasEntradasYPorcentajeBajo_IgualLiberaAlMenosUna()
        {
            // 0,01 de 2 entradas redondea a 0; la caché debe liberar al menos una para poder guardar la nueva.
            using MemoryCoordinatesCache cache = BuildCache(SettingsForEntries(2, evictionPercentage: 0.01));
            var factory = new CountingFactory();

            await cache.GetOrCreateAsync("KEY1", factory.Create, CancellationToken.None);
            await Task.Delay(AccessSpacingMs);
            await cache.GetOrCreateAsync("KEY2", factory.Create, CancellationToken.None);
            await Task.Delay(AccessSpacingMs);
            await cache.GetOrCreateAsync("KEY3", factory.Create, CancellationToken.None);

            // KEY3 sí quedó guardada (la entrada nueva no se descarta).
            Int32 callsBefore = factory.Calls;
            await cache.GetOrCreateAsync("KEY3", factory.Create, CancellationToken.None);
            factory.Calls.Should().Be(callsBefore);
            cache.Count.Should().BeLessThanOrEqualTo(2);
        }

        [Fact]
        public async Task GetOrCreateAsync_EntradaMayorQueElTope_SeDevuelveSinCachear()
        {
            using MemoryCoordinatesCache cache = BuildCache(new CoordinatesCacheSettings { MaxSizeBytes = 100 });
            var factory = new CountingFactory();

            OpenStreetMapDto first = await cache.GetOrCreateAsync("KEY1", factory.Create, CancellationToken.None);
            await cache.GetOrCreateAsync("KEY1", factory.Create, CancellationToken.None);

            first.PlaceName.Should().Be("Bogota, Colombia");
            cache.Count.Should().Be(0);
            factory.Calls.Should().Be(2);
        }

        [Fact]
        public async Task GetOrCreateAsync_ClaveMasLargaQueElMaximo_SeConsultaSiempreSinCachear()
        {
            using MemoryCoordinatesCache cache = BuildCache(new CoordinatesCacheSettings { MaxKeyLength = 10 });
            var factory = new CountingFactory();
            String longKey = new('a', 11);

            await cache.GetOrCreateAsync(longKey, factory.Create, CancellationToken.None);
            await cache.GetOrCreateAsync(longKey, factory.Create, CancellationToken.None);

            factory.Calls.Should().Be(2);
            cache.Count.Should().Be(0);
        }

        [Fact]
        public async Task GetOrCreateAsync_ClaveConLongitudIgualAlMaximo_SiSeCachea()
        {
            using MemoryCoordinatesCache cache = BuildCache(new CoordinatesCacheSettings { MaxKeyLength = 10 });
            var factory = new CountingFactory();
            String key = new('a', 10);

            await cache.GetOrCreateAsync(key, factory.Create, CancellationToken.None);
            await cache.GetOrCreateAsync(key, factory.Create, CancellationToken.None);

            factory.Calls.Should().Be(1);
        }

        [Fact]
        public async Task GetOrCreateAsync_ExpiracionAbsoluta_LaEntradaVencidaSeVuelveAConsultar()
        {
            using MemoryCoordinatesCache cache = BuildCache(new CoordinatesCacheSettings
            {
                AbsoluteExpiration = TimeSpan.FromMilliseconds(150),
                SlidingExpiration = null
            });
            var factory = new CountingFactory();

            await cache.GetOrCreateAsync("KEY1", factory.Create, CancellationToken.None);
            await Task.Delay(400);
            await cache.GetOrCreateAsync("KEY1", factory.Create, CancellationToken.None);

            factory.Calls.Should().Be(2);
        }

        [Fact]
        public async Task GetOrCreateAsync_ExpiracionDeslizante_LaEntradaSinUsoVence()
        {
            using MemoryCoordinatesCache cache = BuildCache(new CoordinatesCacheSettings
            {
                AbsoluteExpiration = TimeSpan.FromHours(1),
                SlidingExpiration = TimeSpan.FromMilliseconds(150)
            });
            var factory = new CountingFactory();

            await cache.GetOrCreateAsync("KEY1", factory.Create, CancellationToken.None);
            await Task.Delay(400);
            await cache.GetOrCreateAsync("KEY1", factory.Create, CancellationToken.None);

            factory.Calls.Should().Be(2);
        }

        [Fact]
        public async Task GetOrCreateAsync_ConsultasConcurrentesDelMismoLugar_ConsultanElApiUnaSolaVez()
        {
            using MemoryCoordinatesCache cache = BuildCache();
            Int32 calls = 0;
            var release = new TaskCompletionSource();

            async Task<OpenStreetMapDto> SlowFactory(CancellationToken _)
            {
                Interlocked.Increment(ref calls);
                await release.Task;
                return Place();
            }

            Task<OpenStreetMapDto>[] requests = Enumerable.Range(0, 10)
                .Select(_ => Task.Run(() => cache.GetOrCreateAsync("OSM|Bogota", SlowFactory, CancellationToken.None)))
                .ToArray();

            await Task.Delay(150);
            release.SetResult();
            OpenStreetMapDto[] results = await Task.WhenAll(requests);

            calls.Should().Be(1);
            results.Should().OnlyContain(r => r.PlaceName == "Bogota, Colombia");
            results.Distinct().Should().HaveCount(10, "cada consumidor recibe su propia copia");
        }

        [Fact]
        public async Task GetOrCreateAsync_ConsultasConcurrentesDeLugaresDistintos_TodasSeResuelven()
        {
            using MemoryCoordinatesCache cache = BuildCache();

            Task<OpenStreetMapDto>[] requests = Enumerable.Range(0, 50)
                .Select(i => Task.Run(() => cache.GetOrCreateAsync($"KEY{i}", _ => Task.FromResult(Place($"Lugar {i}")), CancellationToken.None)))
                .ToArray();

            OpenStreetMapDto[] results = await Task.WhenAll(requests);

            results.Select(r => r.PlaceName).Should().OnlyHaveUniqueItems().And.HaveCount(50);
            cache.Count.Should().Be(50);
        }

        [Fact]
        public async Task GetOrCreateAsync_TokenCancelado_MientrasEsperaElLock_LanzaOperationCanceledException()
        {
            using MemoryCoordinatesCache cache = BuildCache();
            var release = new TaskCompletionSource();
            Task<OpenStreetMapDto> first = cache.GetOrCreateAsync("KEY1", async _ =>
            {
                await release.Task;
                return Place();
            }, CancellationToken.None);
            await Task.Delay(100);

            using var cts = new CancellationTokenSource();
            Task<OpenStreetMapDto> waiting = cache.GetOrCreateAsync("KEY1", _ => Task.FromResult(Place()), cts.Token);
            cts.Cancel();

            await FluentActions.Awaiting(() => waiting).Should().ThrowAsync<OperationCanceledException>();

            release.SetResult();
            await first;
        }

        [Fact]
        public async Task GetOrCreateAsync_PasaElTokenDeCancelacionALaFactory()
        {
            using MemoryCoordinatesCache cache = BuildCache();
            using var cts = new CancellationTokenSource();
            CancellationToken received = default;

            await cache.GetOrCreateAsync("KEY1", token =>
            {
                received = token;
                return Task.FromResult(Place());
            }, cts.Token);

            received.Should().Be(cts.Token);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetOrCreateAsync_ClaveVacia_LanzaArgumentException(String key)
        {
            using MemoryCoordinatesCache cache = BuildCache();

            Func<Task> act = () => cache.GetOrCreateAsync(key, _ => Task.FromResult(Place()), CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GetOrCreateAsync_ClaveNula_LanzaArgumentNullException()
        {
            using MemoryCoordinatesCache cache = BuildCache();

            Func<Task> act = () => cache.GetOrCreateAsync(null!, _ => Task.FromResult(Place()), CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GetOrCreateAsync_FactoryNula_LanzaArgumentNullException()
        {
            using MemoryCoordinatesCache cache = BuildCache();

            Func<Task> act = () => cache.GetOrCreateAsync("KEY1", null!, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }
    }
}
