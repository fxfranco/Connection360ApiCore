using Connection360Notification.Api.Extensions;
using Connection360Notification.Application.Ports;
using Connection360Notification.Application.Ports.Inbound;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Connection360Notification.Api.Tests.Extensions
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddApplicationServices_RegistraTodosLosUseCasesDeLaCapaDeAplicacion()
        {
            var services = new ServiceCollection();

            services.AddApplicationServices();
            services.Should().Contain(sd => sd.ServiceType == typeof(IGetNotificationsUseCase));
            services.Should().Contain(sd => sd.ServiceType == typeof(INotificationUseCase));
            services.Should().Contain(sd => sd.ServiceType == typeof(IProcessIncomingNotificationUseCase));
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
