using Connection360.Application.Ports;
using Connection360.Domain.Interfaces;
using Connection360.Domain.Ports.Persistence;
using Connection360.Infrastructure.Adapters.Output;
using Connection360.Infrastructure.DependencyInjection;
using Connection360.Infrastructure.ExternalApi;
using Connection360.Infrastructure.Messaging;
using Connection360.Infrastructure.Persistence;
using Connection360.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;
using Xunit;

namespace Connection360.Infrastructure.Tests.DependencyInjection
{
    public class InfrastructureServiceCollectionExtensionsResolucionTests
    {
        private static IConfiguration BuildConfiguration(IDictionary<String, String?>? extra = null)
        {
            var values = new Dictionary<String, String?>
            {
                ["ExternalApi:Apis:BPMS:BaseUrl"] = "https://bpms.test/",
                ["ExternalApi:Apis:BPMS:DataEndpoint"] = "/data",
                ["ExternalApi:Apis:BPMS:ApiKey"] = "secreto",
                ["ExternalApi:Apis:BPMS:TimeoutSeconds"] = "12",
                ["ExternalApi:Apis:SIM:BaseUrl"] = "https://sim.test/",
                ["ExternalApi:Apis:SIM:DataEndpoint"] = "/data",
                ["ExternalApi:Apis:SIM:ApiKey"] = "   ",
                ["OutboxPublisher:PollingIntervalSeconds"] = "25",
                ["Auth0Management:Domain"] = "tenant.test"
            };
            if (extra != null)
            {
                foreach (var kv in extra) values[kv.Key] = kv.Value;
            }
            return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        }

        private static ServiceProvider BuildProvider(IConfiguration configuration)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(configuration);
            services.AddSingleton(NpgsqlDataSource.Create("Host=localhost;Database=test;Username=test;Password=test"));
            services.AddInfrastructure(configuration);
            return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        }

        [Fact]
        public async Task AddInfrastructure_ResuelveRepositoriosYUnitOfWorkDentroDeUnScope()
        {
            await using ServiceProvider provider = BuildProvider(BuildConfiguration());
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            IServiceProvider sp = scope.ServiceProvider;

            sp.GetRequiredService<ICustomerRepository>().Should().BeOfType<CustomerRepository>();
            sp.GetRequiredService<ICollaboratorRepository>().Should().BeOfType<CollaboratorRepository>();
            sp.GetRequiredService<ICustomersOfCollaboratorsRepository>().Should().BeOfType<CustomersOfCollaboratorsRepository>();
            sp.GetRequiredService<IMasterSettingsRepository>().Should().BeOfType<MasterSettingsRepository>();
            sp.GetRequiredService<ICustomerNotificationChannelsRepository>().Should().BeOfType<CustomerNotificationChannelsRepository>();
            sp.GetRequiredService<ICustomerNotificationEventRepository>().Should().BeOfType<CustomerNotificationEventRepository>();
            sp.GetRequiredService<IOutboxMessagesRepository>().Should().BeOfType<OutboxMessagesRepository>();
            sp.GetRequiredService<IApplicationDataSheetEntregadosRepository>().Should().BeOfType<ApplicationDataSheetEntregadosRepository>();
            sp.GetRequiredService<IApplicationDataSheetNoEntregadosRepository>().Should().BeOfType<ApplicationDataSheetNoEntregadosRepository>();
            sp.GetRequiredService<ILogStatusTrackingViewRepository>().Should().BeOfType<LogStatusTrackingViewRepository>();
            sp.GetRequiredService<IUnitOfWork>().Should().BeOfType<UnitOfWork>();
            sp.GetRequiredService<IAuth0UserService>().Should().BeOfType<Auth0UserService>();
            sp.GetRequiredService<IExternalApiOpenStreetMap>().Should().BeOfType<ExternalApiOpenStreetMap>();
        }

        [Fact]
        public async Task AddInfrastructure_LosRepositoriosDeUnMismoScopeCompartenLaMismaDbSession()
        {
            await using ServiceProvider provider = BuildProvider(BuildConfiguration());
            await using AsyncServiceScope scope = provider.CreateAsyncScope();

            DbSession first = scope.ServiceProvider.GetRequiredService<DbSession>();
            DbSession second = scope.ServiceProvider.GetRequiredService<DbSession>();

            first.Should().BeSameAs(second);
        }

        [Fact]
        public async Task AddInfrastructure_ScopesDistintosObtienenDbSessionDistinta()
        {
            await using ServiceProvider provider = BuildProvider(BuildConfiguration());

            await using AsyncServiceScope scopeA = provider.CreateAsyncScope();
            await using AsyncServiceScope scopeB = provider.CreateAsyncScope();

            scopeA.ServiceProvider.GetRequiredService<DbSession>().Should().NotBeSameAs(scopeB.ServiceProvider.GetRequiredService<DbSession>());
        }

        [Fact]
        public void AddInfrastructure_EnlazaLaSeccionOutboxPublisherConIOptionsMonitor()
        {
            using ServiceProvider provider = BuildProvider(BuildConfiguration());

            Int16 seconds = provider.GetRequiredService<IOptionsMonitor<OutboxPublisherSettings>>().CurrentValue.PollingIntervalSeconds;

            seconds.Should().Be(25);
        }

        [Fact]
        public void AddInfrastructure_SinSeccionOutboxPublisher_UsaElValorPorDefecto()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<String, String?>()).Build();
            using ServiceProvider provider = BuildProvider(configuration);

            provider.GetRequiredService<IOptionsMonitor<OutboxPublisherSettings>>().CurrentValue.PollingIntervalSeconds
                .Should().Be(OutboxPublisherSettings.DefaultPollingIntervalSeconds);
        }

        [Fact]
        public void AddInfrastructure_EnlazaLaSeccionExternalApi()
        {
            using ServiceProvider provider = BuildProvider(BuildConfiguration());

            ExternalApiSettings settings = provider.GetRequiredService<IOptions<ExternalApiSettings>>().Value;

            settings.Apis.Should().ContainKeys("BPMS", "SIM");
            settings.GetConfig("bpms")!.TimeoutSeconds.Should().Be(12);
            settings.GetConfig("SIM")!.TimeoutSeconds.Should().Be(30);
        }

        [Fact]
        public void AddInfrastructure_ConfiguraBaseAddressTimeoutYApiKeyDelHttpClientNombrado()
        {
            using ServiceProvider provider = BuildProvider(BuildConfiguration());
            var factory = provider.GetRequiredService<IHttpClientFactory>();

            HttpClient bpms = factory.CreateClient("BPMS");

            bpms.BaseAddress.Should().Be(new Uri("https://bpms.test/"));
            bpms.Timeout.Should().Be(TimeSpan.FromSeconds(12));
            bpms.DefaultRequestHeaders.GetValues("X-Api-Key").Should().Equal("secreto");
        }

        [Fact]
        public void AddInfrastructure_ConApiKeyEnBlanco_NoAgregaElHeaderXApiKey()
        {
            using ServiceProvider provider = BuildProvider(BuildConfiguration());
            var factory = provider.GetRequiredService<IHttpClientFactory>();

            HttpClient sim = factory.CreateClient("SIM");

            sim.DefaultRequestHeaders.Contains("X-Api-Key").Should().BeFalse();
            sim.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        }

        [Fact]
        public void AddInfrastructure_SinSeccionExternalApi_NoRegistraHttpClientFactory()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<String, String?>()).Build();
            using ServiceProvider provider = BuildProvider(configuration);

            provider.GetRequiredService<IOptions<ExternalApiSettings>>().Value.Apis.Should().BeEmpty();
            // Comportamiento actual: sin APIs configuradas no se llama AddHttpClient, asi que IHttpClientFactory no queda registrado.
            provider.GetService<IHttpClientFactory>().Should().BeNull();
        }

        [Fact]
        public void AddInfrastructure_RegistraElOutboxPublisherWorkerComoServicioHospedado()
        {
            var services = new ServiceCollection();

            services.AddInfrastructure(BuildConfiguration());

            services.Should().Contain(sd => sd.ServiceType == typeof(IHostedService) && sd.ImplementationType == typeof(OutboxPublisherWorker));
        }

        [Fact]
        public void AddInfrastructure_RegistraLosRepositoriosDeVistasYLogComoScoped()
        {
            var services = new ServiceCollection();

            services.AddInfrastructure(BuildConfiguration());

            services.Should().Contain(sd => sd.ServiceType == typeof(ILogStatusTrackingViewRepository) && sd.ImplementationType == typeof(LogStatusTrackingViewRepository) && sd.Lifetime == ServiceLifetime.Scoped);
            services.Should().Contain(sd => sd.ServiceType == typeof(IApplicationDataSheetEntregadosRepository) && sd.Lifetime == ServiceLifetime.Scoped);
            services.Should().Contain(sd => sd.ServiceType == typeof(IApplicationDataSheetNoEntregadosRepository) && sd.Lifetime == ServiceLifetime.Scoped);
            services.Should().Contain(sd => sd.ServiceType == typeof(IOutboxMessagesRepository) && sd.Lifetime == ServiceLifetime.Scoped);
        }
    }
}
