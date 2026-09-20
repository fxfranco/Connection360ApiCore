using Connection360.Api.Extensions;
using Connection360.Application.Ports;
using Connection360.Application.Ports.Persistence;
using Connection360.Application.Services;
using Connection360.Domain.Interfaces;
using Connection360.Domain.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Connection360.Api.Tests.Extensions
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddApplicationServices_RegistraTodosLosUseCasesDeLaCapaDeAplicacion()
        {
            var services = new ServiceCollection();

            services.AddApplicationServices();

            services.Should().Contain(sd => sd.ServiceType == typeof(IGetClientSummaryUseCase));
            services.Should().Contain(sd => sd.ServiceType == typeof(IGetMyShipmentsUseCase));
            services.Should().Contain(sd => sd.ServiceType == typeof(IGetReportsUseCase));
            services.Should().Contain(sd => sd.ServiceType == typeof(IGetNotificationsUseCase));
            services.Should().Contain(sd => sd.ServiceType == typeof(IGetUserManagementUseCase));
        }

        [Fact]
        public void AddApplicationServices_RegistraLosUseCasesDePersistencia()
        {
            var services = new ServiceCollection();

            services.AddApplicationServices();

            services.Should().Contain(sd => sd.ServiceType == typeof(ICustomerNotificationChannelsUseCase));
            services.Should().Contain(sd => sd.ServiceType == typeof(ICustomerNotificationEventsUseCase));
            services.Should().Contain(sd => sd.ServiceType == typeof(ICustomerNotificationsSettingsUseCase));
            services.Should().Contain(sd => sd.ServiceType == typeof(ICustomerUseCase));
            services.Should().Contain(sd => sd.ServiceType == typeof(IMasterSettingsUseCase));
            services.Should().Contain(sd => sd.ServiceType == typeof(ICollaboratorUseCase));
        }

        [Fact]
        public void AddApplicationServices_RegistraLosServiciosDeDominioYElResolverDeAcceso()
        {
            var services = new ServiceCollection();

            services.AddApplicationServices();

            services.Should().Contain(sd => sd.ServiceType == typeof(IClientAccessResolver) && sd.ImplementationType == typeof(ClientAccessResolver));
            services.Should().Contain(sd => sd.ServiceType == typeof(IClientRecordsFilterService) && sd.ImplementationType == typeof(ClientRecordsFilterService));
            services.Should().Contain(sd => sd.ServiceType == typeof(IClientSummaryDomainService) && sd.ImplementationType == typeof(ClientSummaryDomainService));
            services.Should().Contain(sd => sd.ServiceType == typeof(IMyShipmentsDomainService) && sd.ImplementationType == typeof(MyShipmentsDomainService));
            services.Should().Contain(sd => sd.ServiceType == typeof(IReportsDomainService) && sd.ImplementationType == typeof(ReportsDomainService));
            services.Should().Contain(sd => sd.ServiceType == typeof(IDetailsHistoryShipmentsDomainService));
            services.Should().Contain(sd => sd.ServiceType == typeof(IDynamicDataSetMerger));
        }

        [Fact]
        public void AddApplicationServices_TodosLosRegistrosSonScoped()
        {
            var services = new ServiceCollection();

            services.AddApplicationServices();

            services.Where(sd => sd.ServiceType == typeof(IGetClientSummaryUseCase) || sd.ServiceType == typeof(IMasterSettingsUseCase))
                .Should().OnlyContain(sd => sd.Lifetime == ServiceLifetime.Scoped);
        }

        [Fact]
        public void AddApiVersioningSetup_NoLanzaExcepcionYRetornaLaMismaColeccion()
        {
            var services = new ServiceCollection();

            IServiceCollection result = services.AddApiVersioningSetup();

            result.Should().BeSameAs(services);
        }

        [Fact]
        public void AddJwtAuthentication_ConConfiguracionValida_RegistraLaAutenticacionSinLanzarExcepcion()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<String, String?>
            {
                ["Jwt:Secret"] = "un-secreto-de-prueba",
                ["Jwt:Roles"] = "https://connection360/roles",
                ["Jwt:Issuer"] = "https://issuer.test/",
                ["Jwt:Audience"] = "https://audience.test"
            }).Build();

            Action act = () => services.AddJwtAuthentication(config);

            act.Should().NotThrow();
        }

        [Fact]
        public void AddJwtAuthentication_SinJwtSecret_LanzaInvalidOperationException()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<String, String?>
            {
                ["Jwt:Roles"] = "https://connection360/roles"
            }).Build();

            Action act = () => services.AddJwtAuthentication(config);

            act.Should().Throw<InvalidOperationException>().WithMessage("*Jwt:Secret*");
        }

        [Fact]
        public void AddJwtAuthentication_SinJwtRoles_LanzaInvalidOperationException()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<String, String?>
            {
                ["Jwt:Secret"] = "un-secreto-de-prueba"
            }).Build();

            Action act = () => services.AddJwtAuthentication(config);

            act.Should().Throw<InvalidOperationException>().WithMessage("*Jwt:Role*");
        }
    }
}
