using Connection360Notification.Application.Ports.Output;
using Connection360Notification.Domain.Ports.Outbound;
using Connection360Notification.Infrastructure.Adapters.Output;
using Connection360Notification.Infrastructure.DependencyInjection;
using Connection360Notification.Infrastructure.Email;
using Connection360Notification.Infrastructure.Messaging;
using Connection360Notification.Infrastructure.Persistence.Mongo;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.DependencyInjection
{
    public class InfrastructureServiceCollectionExtensionsTests
    {
        private static IConfiguration BuildConfiguration()
        {
            return new ConfigurationBuilder().Build();
        }

        [Fact]
        public void AddInfrastructure_RegistraElProveedorDeUsuarioDeSignalRComoSingleton()
        {
            var services = new ServiceCollection();

            services.AddInfrastructure(BuildConfiguration());

            services.Should().Contain(sd => sd.ServiceType == typeof(IUserIdProvider)
                && sd.ImplementationType == typeof(Connection360Notification.Infrastructure.Adapters.Input.CustomUserIdProvider)
                && sd.Lifetime == ServiceLifetime.Singleton);
        }

        [Fact]
        public void AddInfrastructure_RegistraLosCanalesDeNotificacion()
        {
            var services = new ServiceCollection();

            services.AddInfrastructure(BuildConfiguration());

            services.Count(sd => sd.ServiceType == typeof(INotificationChannel)).Should().Be(3);
        }

        [Fact]
        public void AddInfrastructure_RegistraElEnviadorDeCorreoMailKitComoSingleton()
        {
            var services = new ServiceCollection();

            services.AddInfrastructure(BuildConfiguration());

            services.Should().Contain(sd => sd.ServiceType == typeof(IEmailSender)
                && sd.ImplementationType == typeof(MailKitEmailSender)
                && sd.Lifetime == ServiceLifetime.Singleton);
        }

        [Fact]
        public void AddInfrastructure_RegistraLasImplementacionesConcretasDeLosRepositorios()
        {
            var services = new ServiceCollection();

            services.AddInfrastructure(BuildConfiguration());

            services.Should().Contain(sd => sd.ServiceType == typeof(INotificationRepository) && sd.ImplementationType == typeof(MongoNotificationRepository));
        }

        [Fact]
        public void AddInfrastructure_RegistraElKafkaProducerServiceComoSingleton()
        {
            var services = new ServiceCollection();

            services.AddInfrastructure(BuildConfiguration());

            services.Should().Contain(sd => sd.ServiceType == typeof(IKafkaProducerService)
                && sd.ImplementationType == typeof(KafkaProducerService)
                && sd.Lifetime == ServiceLifetime.Singleton);
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
