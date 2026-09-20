using Connection360.Application.Ports;
using Connection360.Application.Ports.Output;
using Connection360.Domain.Interfaces;
using Connection360.Domain.Ports.Persistence;
using Connection360.Infrastructure.DependencyInjection;
using Connection360.Infrastructure.Persistence;
using Connection360.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Connection360.Infrastructure.Tests.DependencyInjection
{
    public class InfrastructureServiceCollectionExtensionsTests
    {
        private static IConfiguration BuildConfiguration()
        {
            var inMemorySettings = new Dictionary<String, String?>
            {
                ["ExternalApi:Apis:BPMS:BaseUrl"] = "https://bpms.test",
                ["ExternalApi:Apis:BPMS:DataEndpoint"] = "/data",
                ["ExternalApi:Apis:SIM:BaseUrl"] = "https://sim.test",
                ["ExternalApi:Apis:SIM:DataEndpoint"] = "/data",
            };

            return new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        }

        [Fact]
        public void AddInfrastructure_RegistraElExternalDataGatewayYOpenStreetMap()
        {
            var services = new ServiceCollection();

            services.AddInfrastructure(BuildConfiguration());

            services.Should().Contain(sd => sd.ServiceType == typeof(IExternalDataGateway));
            services.Should().Contain(sd => sd.ServiceType == typeof(IExternalApiOpenStreetMap));
        }

        [Fact]
        public void AddInfrastructure_RegistraElProveedorDeUsuarioDeSignalRComoSingleton()
        {
            var services = new ServiceCollection();

            services.AddInfrastructure(BuildConfiguration());

            services.Should().Contain(sd => sd.ServiceType == typeof(IUserIdProvider)
                && sd.ImplementationType == typeof(Connection360.Infrastructure.Adapters.Input.CustomUserIdProvider)
                && sd.Lifetime == ServiceLifetime.Singleton);
        }

        [Fact]
        public void AddInfrastructure_RegistraElNotifierServiceYAuth0UserService()
        {
            var services = new ServiceCollection();

            services.AddInfrastructure(BuildConfiguration());

            services.Should().Contain(sd => sd.ServiceType == typeof(INotifierService));
            services.Should().Contain(sd => sd.ServiceType == typeof(IAuth0UserService));
        }

        [Fact]
        public void AddInfrastructure_RegistraLosRepositoriosYElUnitOfWorkComoScoped()
        {
            var services = new ServiceCollection();

            services.AddInfrastructure(BuildConfiguration());

            services.Should().Contain(sd => sd.ServiceType == typeof(ICustomerRepository) && sd.Lifetime == ServiceLifetime.Scoped);
            services.Should().Contain(sd => sd.ServiceType == typeof(IMasterSettingsRepository) && sd.Lifetime == ServiceLifetime.Scoped);
            services.Should().Contain(sd => sd.ServiceType == typeof(ICustomerNotificationChannelsRepository) && sd.Lifetime == ServiceLifetime.Scoped);
            services.Should().Contain(sd => sd.ServiceType == typeof(ICustomerNotificationEventRepository) && sd.Lifetime == ServiceLifetime.Scoped);
            services.Should().Contain(sd => sd.ServiceType == typeof(ICollaboratorRepository) && sd.Lifetime == ServiceLifetime.Scoped);
            services.Should().Contain(sd => sd.ServiceType == typeof(ICustomersOfCollaboratorsRepository) && sd.Lifetime == ServiceLifetime.Scoped);
            services.Should().Contain(sd => sd.ServiceType == typeof(IUnitOfWork) && sd.ImplementationType == typeof(UnitOfWork) && sd.Lifetime == ServiceLifetime.Scoped);
        }

        [Fact]
        public void AddInfrastructure_RegistraLasImplementacionesConcretasDeLosRepositoriosDeColaboradores()
        {
            var services = new ServiceCollection();

            services.AddInfrastructure(BuildConfiguration());

            services.Should().Contain(sd => sd.ServiceType == typeof(ICollaboratorRepository) && sd.ImplementationType == typeof(CollaboratorRepository));
            services.Should().Contain(sd => sd.ServiceType == typeof(ICustomersOfCollaboratorsRepository) && sd.ImplementationType == typeof(CustomersOfCollaboratorsRepository));
        }

        [Fact]
        public void AddInfrastructure_RegistraLaSesionDeBaseDeDatosComoScoped()
        {
            var services = new ServiceCollection();

            services.AddInfrastructure(BuildConfiguration());

            services.Should().Contain(sd => sd.ServiceType == typeof(DbSession) && sd.Lifetime == ServiceLifetime.Scoped);
        }

        [Fact]
        public void AddInfrastructure_RegistraUnHttpClientNombradoPorCadaApiConfigurada()
        {
            var services = new ServiceCollection();

            services.AddInfrastructure(BuildConfiguration());
            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IHttpClientFactory>();

            HttpClient bpmsClient = factory.CreateClient("BPMS");

            bpmsClient.BaseAddress.Should().Be(new Uri("https://bpms.test"));
        }

        [Fact]
        public void AddInfrastructure_RetornaLaMismaInstanciaDeServiceCollectionParaEncadenamiento()
        {
            var services = new ServiceCollection();

            IServiceCollection result = services.AddInfrastructure(BuildConfiguration());

            result.Should().BeSameAs(services);
        }
    }
}
