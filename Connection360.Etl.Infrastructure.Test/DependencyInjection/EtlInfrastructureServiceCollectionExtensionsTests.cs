using Connection360.Etl.Application.Ports;
using Connection360.Etl.Domain.Interfaces;
using Connection360.Etl.Domain.Ports.Persistence;
using Connection360.Etl.Domain.Services;
using Connection360.Etl.Infrastructure.Configuration;
using Connection360.Etl.Infrastructure.DependencyInjection;
using Connection360.Etl.Infrastructure.ExternalApi;
using Connection360.Etl.Infrastructure.Persistence;
using Connection360.Etl.Infrastructure.Persistence.Repositories;
using Connection360.Etl.Infrastructure.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Net;
using Xunit;

namespace Connection360.Etl.Infrastructure.Tests.DependencyInjection
{
    public class EtlInfrastructureServiceCollectionExtensionsTests
    {
        private static IConfiguration BuildConfiguration(Dictionary<String, String?> values)
            => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        private static Dictionary<String, String?> DefaultValues() => new()
        {
            ["ExternalApi:PaginationEnabled"] = "true",
            ["ExternalApi:PageSize"] = "250",
            ["ExternalApi:Apis:BPMS:BaseUrl"] = "https://bpms.test/",
            ["ExternalApi:Apis:BPMS:DataEndpoint"] = "api/data",
            ["ExternalApi:Apis:BPMS:ApiKey"] = "clave-bpms",
            ["ExternalApi:Apis:BPMS:TimeoutSeconds"] = "12",
            ["ExternalApi:Apis:SIM:BaseUrl"] = "https://sim.test/",
            ["ExternalApi:Apis:SIM:DataEndpoint"] = "sim/data",
            ["ExternalApi:Apis:SIM:PageNumberParam"] = "p",
            ["ExternalApi:Apis:SIM:PageSizeParam"] = "s",
            ["ExternalApi:Apis:ASIS:BaseUrl"] = "https://asis.test/",
            ["ExternalApi:Apis:ASIS:DataEndpoint"] = "asis/data",
            ["ExternalApi:Apis:ASIS:ApiKey"] = "   ",
            ["EtlJobControl:ApplicationDataSheetRetentionDays"] = "14",
            ["EtlChangeTracking:SystemUser"] = "ROBOT_ETL",
        };

        private static ServiceProvider BuildProvider(IConfiguration configuration, Action<IServiceCollection>? customize = null)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            // El NpgsqlDataSource lo registra el composition root (Etl.App); aquí se crea uno que nunca se conecta.
            services.AddSingleton(NpgsqlDataSource.Create("Host=localhost;Database=fake;Username=u;Password=p"));
            services.AddEtlInfrastructure(configuration);
            customize?.Invoke(services);
            return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = false });
        }

        [Fact]
        public void AddEtlInfrastructure_RetornaLaMismaColeccionParaEncadenar()
        {
            var services = new ServiceCollection();

            IServiceCollection result = services.AddEtlInfrastructure(BuildConfiguration(DefaultValues()));

            result.Should().BeSameAs(services);
        }

        [Fact]
        public void AddEtlInfrastructure_EnlazaLasOpcionesDeExternalApiDesdeLaConfiguracion()
        {
            using var provider = BuildProvider(BuildConfiguration(DefaultValues()));

            ExternalApiSettings settings = provider.GetRequiredService<IOptions<ExternalApiSettings>>().Value;

            settings.PaginationEnabled.Should().BeTrue();
            settings.PageSize.Should().Be(250);
            settings.Apis.Keys.Should().BeEquivalentTo(new[] { "BPMS", "SIM", "ASIS" });
            settings.GetConfig("bpms")!.ApiKey.Should().Be("clave-bpms");
            settings.GetConfig("BPMS")!.TimeoutSeconds.Should().Be(12);
            settings.GetConfig("SIM")!.PageNumberParam.Should().Be("p");
            settings.GetConfig("SIM")!.PageSizeParam.Should().Be("s");
        }

        [Fact]
        public void AddEtlInfrastructure_EnlazaLasOpcionesDeControlDeJobsYSeguimientoDeCambios()
        {
            using var provider = BuildProvider(BuildConfiguration(DefaultValues()));

            provider.GetRequiredService<IOptions<EtlJobControlSettings>>().Value.ApplicationDataSheetRetentionDays.Should().Be(14);
            provider.GetRequiredService<IOptions<EtlChangeTrackingSettings>>().Value.SystemUser.Should().Be("ROBOT_ETL");
        }

        [Fact]
        public void AddEtlInfrastructure_SinSeccionesDeConfiguracion_UsaLosValoresPorDefecto()
        {
            using var provider = BuildProvider(BuildConfiguration(new Dictionary<String, String?>()));

            provider.GetRequiredService<IOptions<ExternalApiSettings>>().Value.Apis.Should().BeEmpty();
            provider.GetRequiredService<IOptions<ExternalApiSettings>>().Value.PaginationEnabled.Should().BeFalse();
            provider.GetRequiredService<IOptions<EtlJobControlSettings>>().Value.ApplicationDataSheetRetentionDays.Should().Be(30);
            provider.GetRequiredService<IOptions<EtlChangeTrackingSettings>>().Value.SystemUser.Should().Be("ETL_CONNECTION360");
        }

        [Fact]
        public void AddEtlInfrastructure_RegistraUnHttpClientNombradoPorCadaApiConBaseAddressTimeoutYApiKey()
        {
            using var provider = BuildProvider(BuildConfiguration(DefaultValues()));
            var factory = provider.GetRequiredService<IHttpClientFactory>();

            using HttpClient bpms = factory.CreateClient("BPMS");

            bpms.BaseAddress.Should().Be(new Uri("https://bpms.test/"));
            bpms.Timeout.Should().Be(TimeSpan.FromSeconds(12));
            bpms.DefaultRequestHeaders.GetValues("X-Api-Key").Should().Equal("clave-bpms");
        }

        [Fact]
        public void AddEtlInfrastructure_ApiSinApiKey_NoAgregaElEncabezadoXApiKey()
        {
            using var provider = BuildProvider(BuildConfiguration(DefaultValues()));
            var factory = provider.GetRequiredService<IHttpClientFactory>();

            using HttpClient sim = factory.CreateClient("SIM");

            sim.BaseAddress.Should().Be(new Uri("https://sim.test/"));
            sim.Timeout.Should().Be(TimeSpan.FromSeconds(30), "TimeoutSeconds por defecto");
            sim.DefaultRequestHeaders.Contains("X-Api-Key").Should().BeFalse();
        }

        [Fact]
        public void AddEtlInfrastructure_ApiKeyConSoloEspacios_NoAgregaElEncabezado()
        {
            using var provider = BuildProvider(BuildConfiguration(DefaultValues()));
            var factory = provider.GetRequiredService<IHttpClientFactory>();

            using HttpClient asis = factory.CreateClient("ASIS");

            asis.DefaultRequestHeaders.Contains("X-Api-Key").Should().BeFalse();
        }

        [Fact]
        public void AddEtlInfrastructure_BaseUrlInvalida_FallaAlCrearElCliente()
        {
            var values = new Dictionary<String, String?>
            {
                ["ExternalApi:Apis:ROTA:BaseUrl"] = "no es una url",
                ["ExternalApi:Apis:ROTA:DataEndpoint"] = "x",
            };
            using var provider = BuildProvider(BuildConfiguration(values));
            var factory = provider.GetRequiredService<IHttpClientFactory>();

            Action act = () => factory.CreateClient("ROTA");

            act.Should().Throw<UriFormatException>();
        }

        [Theory]
        [InlineData(typeof(IExternalDataGateway), typeof(ExternalDataApiGateway))]
        [InlineData(typeof(IDynamicDataSetMerger), typeof(DynamicDataSetMerger))]
        [InlineData(typeof(IShipmentsDataSheetMappingService), typeof(ShipmentsDataSheetMappingService))]
        [InlineData(typeof(ILogStatusMappingService), typeof(LogStatusMappingService))]
        [InlineData(typeof(IApplicationDataSheetChangeDetector), typeof(ApplicationDataSheetChangeDetector))]
        [InlineData(typeof(IEtlChangeMessageCatalog), typeof(EtlChangeMessageCatalog))]
        [InlineData(typeof(IApplicationDataSheetRepository), typeof(ApplicationDataSheetRepository))]
        [InlineData(typeof(ILogStatusTrackingRepository), typeof(LogStatusTrackingRepository))]
        [InlineData(typeof(IOutboxMessageRepository), typeof(OutboxMessageRepository))]
        [InlineData(typeof(IEtlJobControlRepository), typeof(EtlJobControlRepository))]
        [InlineData(typeof(IUnitOfWork), typeof(UnitOfWork))]
        [InlineData(typeof(DbSession), typeof(DbSession))]
        public async Task AddEtlInfrastructure_ResuelveCadaServicioConSuImplementacion(Type serviceType, Type implementationType)
        {
            await using var provider = BuildProvider(BuildConfiguration(DefaultValues()));
            await using AsyncServiceScope scope = provider.CreateAsyncScope();

            Object service = scope.ServiceProvider.GetRequiredService(serviceType);

            service.Should().BeOfType(implementationType);
        }

        [Fact]
        public async Task AddEtlInfrastructure_LosServiciosSonScoped_MismaInstanciaPorAlcanceYDistintaEntreAlcances()
        {
            await using var provider = BuildProvider(BuildConfiguration(DefaultValues()));

            await using AsyncServiceScope first = provider.CreateAsyncScope();
            await using AsyncServiceScope second = provider.CreateAsyncScope();

            first.ServiceProvider.GetRequiredService<IUnitOfWork>().Should().BeSameAs(first.ServiceProvider.GetRequiredService<IUnitOfWork>());
            first.ServiceProvider.GetRequiredService<DbSession>().Should().BeSameAs(first.ServiceProvider.GetRequiredService<DbSession>());
            first.ServiceProvider.GetRequiredService<IUnitOfWork>().Should().NotBeSameAs(second.ServiceProvider.GetRequiredService<IUnitOfWork>());
            first.ServiceProvider.GetRequiredService<DbSession>().Should().NotBeSameAs(second.ServiceProvider.GetRequiredService<DbSession>());
        }

        [Fact]
        public void AddEtlInfrastructure_ResolverUnServicioScopedDesdeLaRaiz_FallaConValidacionDeAlcances()
        {
            using var provider = BuildProvider(BuildConfiguration(DefaultValues()));

            Action act = () => provider.GetRequiredService<IUnitOfWork>();

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public async Task AddEtlInfrastructure_UnitOfWorkResuelveLosRepositoriosRegistradosYComparteLaSesion()
        {
            await using var provider = BuildProvider(BuildConfiguration(DefaultValues()));
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            unitOfWork.GetRepository<IEtlJobControlRepository>().Should().BeOfType<EtlJobControlRepository>();
            unitOfWork.GetRepository<IApplicationDataSheetRepository>().Should().BeOfType<ApplicationDataSheetRepository>();
            unitOfWork.GetRepository<ILogStatusTrackingRepository>().Should().BeOfType<LogStatusTrackingRepository>();
            unitOfWork.GetRepository<IOutboxMessageRepository>().Should().BeOfType<OutboxMessageRepository>();
            unitOfWork.GetRepository<IEtlJobControlRepository>().Should().BeSameAs(scope.ServiceProvider.GetRequiredService<IEtlJobControlRepository>());
        }

        [Fact]
        public async Task AddEtlInfrastructure_SinNpgsqlDataSourceRegistrado_ElUnitOfWorkNoSePuedeResolver()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddEtlInfrastructure(BuildConfiguration(DefaultValues()));
            await using ServiceProvider provider = services.BuildServiceProvider();
            await using AsyncServiceScope scope = provider.CreateAsyncScope();

            Action act = () => scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public async Task AddEtlInfrastructure_IntegracionDePasarelaConHttpClientDelContenedor_EnviaLaApiKeyYUsaLaBaseAddress()
        {
            var handler = FakeHttpMessageHandler.ReturningJson("{\"requestedFields\":[\"ID\"],\"missingColumns\":[],\"rows\":[{\"ID\":\"1\"},{\"ID\":\"2\"}]}");
            var values = DefaultValues();
            values["ExternalApi:PaginationEnabled"] = "false";
            await using var provider = BuildProvider(BuildConfiguration(values), services =>
                services.AddHttpClient("BPMS").ConfigurePrimaryHttpMessageHandler(() => handler));
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            var gateway = scope.ServiceProvider.GetRequiredService<IExternalDataGateway>();

            var pages = new List<Connection360.Etl.Domain.Entities.DynamicDataSet>();
            await foreach (var page in gateway.FetchDataPagedAsync("BPMS", new Dictionary<String, String> { ["nit"] = "1" }, CancellationToken.None))
                pages.Add(page);

            pages.Should().ContainSingle().Which.Rows.Should().HaveCount(2);
            HttpRequestMessage request = handler.Requests.Should().ContainSingle().Subject;
            request.RequestUri!.AbsoluteUri.Should().Be("https://bpms.test/api/data?nit=1");
            request.Headers.GetValues("X-Api-Key").Should().Equal("clave-bpms");
        }

        [Fact]
        public async Task AddEtlInfrastructure_IntegracionConPaginacionHabilitada_UsaElPageSizeConfigurado()
        {
            var handler = FakeHttpMessageHandler.ReturningJson("{\"requestedFields\":[\"ID\"],\"missingColumns\":[],\"rows\":[]}");
            await using var provider = BuildProvider(BuildConfiguration(DefaultValues()), services =>
                services.AddHttpClient("SIM").ConfigurePrimaryHttpMessageHandler(() => handler));
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            var gateway = scope.ServiceProvider.GetRequiredService<IExternalDataGateway>();

            await foreach (var _ in gateway.FetchDataPagedAsync("SIM", new Dictionary<String, String>(), CancellationToken.None)) { }

            handler.LastRequest!.RequestUri!.AbsoluteUri.Should().Be("https://sim.test/sim/data?p=1&s=250");
        }

        [Fact]
        public async Task AddEtlInfrastructure_LlamadoDosVeces_NoLanzaYSigueResolviendo()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(NpgsqlDataSource.Create("Host=localhost;Database=fake;Username=u;Password=p"));
            IConfiguration configuration = BuildConfiguration(DefaultValues());

            services.AddEtlInfrastructure(configuration);
            Action act = () => services.AddEtlInfrastructure(configuration);

            act.Should().NotThrow();
            await using ServiceProvider provider = services.BuildServiceProvider();
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<IUnitOfWork>().Should().NotBeNull();
        }
    }
}
