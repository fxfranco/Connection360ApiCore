using Connection360Notification.Application.Ports.Output;
using Connection360Notification.Domain.Ports.Outbound;
using Connection360Notification.Infrastructure.Adapters.Input;
using Connection360Notification.Infrastructure.Adapters.Output;
using Connection360Notification.Infrastructure.Messaging;
using Connection360Notification.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Connection360Notification.Infrastructure.DependencyInjection
{
    /// <summary>
    /// Clase que carga las configuraciones que se necesitan en la capa de infraestructure
    /// </summary>
    public static class InfrastructureServiceCollectionExtensions
    {
        /// <summary>
        /// Carga toda la configuraciòn de la capa de infrastructure
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            
            // 2. Registro de Inyección de Dependencias (Hexágonos)
            services.AddScoped<INotificationRepository, MongoNotificationRepository>();
            services.AddSingleton<IKafkaProducerService, KafkaProducerService>();

            services.AddSignalR();
            // Registrar el proveedor personalizado de ID de usuario para SignalR
            services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
            services.AddScoped<INotifierService, SignalRNotifierService>();

            // 3. Worker Background Service para Consumo de Kafka
            services.AddHostedService<KafkaConsumerHostedService>();

            return services;
        }
    }
}
