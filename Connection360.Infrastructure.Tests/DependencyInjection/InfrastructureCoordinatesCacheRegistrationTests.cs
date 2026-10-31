using Connection360.Application.Ports;
using Connection360.Infrastructure.Caching;
using Connection360.Infrastructure.DependencyInjection;
using Connection360.Infrastructure.ExternalApi;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Xunit;

namespace Connection360.Infrastructure.Tests.DependencyInjection
{
    public class InfrastructureCoordinatesCacheRegistrationTests
    {
        private static ServiceProvider BuildProvider(IDictionary<String, String?>? extra = null)
        {
            var values = new Dictionary<String, String?>
            {
                ["ExternalApi:Apis:OPENSTREETMAP:BaseUrl"] = "https://nominatim.test/",
                ["ExternalApi:Apis:OPENSTREETMAP:DataEndpoint"] = "/search?q="
            };
            if (extra != null)
            {
                foreach (var kv in extra) values[kv.Key] = kv.Value;
            }

            IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(configuration);
            services.AddSingleton(NpgsqlDataSource.Create("Host=localhost;Database=test;Username=test;Password=test"));
            services.AddInfrastructure(configuration);
            return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        }

        [Fact]
        public void SinSeccionCoordinatesCache_LaCacheQuedaHabilitadaPorDefecto()
        {
            using ServiceProvider provider = BuildProvider();
            using IServiceScope scope = provider.CreateScope();

            scope.ServiceProvider.GetRequiredService<IExternalApiOpenStreetMap>().Should().BeOfType<CachedExternalApiOpenStreetMap>();
            provider.GetRequiredService<ICoordinatesCache>().Should().BeOfType<MemoryCoordinatesCache>();
        }

        [Fact]
        public void LaCacheEsSingleton_SeComparteEntreScopes()
        {
            using ServiceProvider provider = BuildProvider();
            using IServiceScope scopeA = provider.CreateScope();
            using IServiceScope scopeB = provider.CreateScope();

            ICoordinatesCache a = scopeA.ServiceProvider.GetRequiredService<ICoordinatesCache>();
            ICoordinatesCache b = scopeB.ServiceProvider.GetRequiredService<ICoordinatesCache>();

            a.Should().BeSameAs(b);
        }

        [Fact]
        public void ElAdaptadorDecorado_EsScoped_YEnvuelveAlAdaptadorRealDeOpenStreetMap()
        {
            using ServiceProvider provider = BuildProvider();
            using IServiceScope scopeA = provider.CreateScope();
            using IServiceScope scopeB = provider.CreateScope();

            IExternalApiOpenStreetMap a1 = scopeA.ServiceProvider.GetRequiredService<IExternalApiOpenStreetMap>();
            IExternalApiOpenStreetMap a2 = scopeA.ServiceProvider.GetRequiredService<IExternalApiOpenStreetMap>();
            IExternalApiOpenStreetMap b = scopeB.ServiceProvider.GetRequiredService<IExternalApiOpenStreetMap>();

            a1.Should().BeSameAs(a2);
            a1.Should().NotBeSameAs(b);
            scopeA.ServiceProvider.GetRequiredService<ExternalApiOpenStreetMap>().Should().NotBeNull();
        }

        [Fact]
        public void EnabledEnFalse_NoSeRegistraLaCache_YSeUsaElAdaptadorRealDirectamente()
        {
            using ServiceProvider provider = BuildProvider(new Dictionary<String, String?> { ["CoordinatesCache:Enabled"] = "false" });
            using IServiceScope scope = provider.CreateScope();

            scope.ServiceProvider.GetRequiredService<IExternalApiOpenStreetMap>().Should().BeOfType<ExternalApiOpenStreetMap>();
            provider.GetService<ICoordinatesCache>().Should().BeNull();
        }

        [Fact]
        public void EnlazaLaSeccionCoordinatesCacheConLosValoresConfigurados()
        {
            using ServiceProvider provider = BuildProvider(new Dictionary<String, String?>
            {
                ["CoordinatesCache:MaxSizeBytes"] = "2048",
                ["CoordinatesCache:EvictionPercentage"] = "0.5",
                ["CoordinatesCache:AbsoluteExpiration"] = "10.00:00:00",
                ["CoordinatesCache:SlidingExpiration"] = "01:00:00",
                ["CoordinatesCache:MaxKeyLength"] = "50"
            });

            CoordinatesCacheSettings settings = provider.GetRequiredService<IOptions<CoordinatesCacheSettings>>().Value;

            settings.Enabled.Should().BeTrue();
            settings.MaxSizeBytes.Should().Be(2048);
            settings.EvictionPercentage.Should().Be(0.5);
            settings.AbsoluteExpiration.Should().Be(TimeSpan.FromDays(10));
            settings.SlidingExpiration.Should().Be(TimeSpan.FromHours(1));
            settings.MaxKeyLength.Should().Be(50);
        }

        [Fact]
        public void ConfiguracionInvalida_FallaAlCrearLaCacheConUnMensajeClaro()
        {
            using ServiceProvider provider = BuildProvider(new Dictionary<String, String?> { ["CoordinatesCache:MaxSizeBytes"] = "0" });

            Action act = () => provider.GetRequiredService<ICoordinatesCache>();

            act.Should().Throw<OptionsValidationException>().WithMessage("*MaxSizeBytes*");
        }
    }
}
