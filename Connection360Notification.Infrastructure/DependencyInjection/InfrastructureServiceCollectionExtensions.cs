using Connection360Notification.Application.Ports.Output;
using Connection360Notification.Domain.Ports.Outbound;
using Connection360Notification.Infrastructure.Adapters.Input;
using Connection360Notification.Infrastructure.Adapters.Output;
using Connection360Notification.Infrastructure.Messaging;
using Connection360Notification.Infrastructure.Persistence.Mongo;
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
        private const String MongoProvider = "MongoDB";

        /// <summary>
        /// Carga toda la configuraciòn de la capa de infrastructure
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // 2. Registro de Inyección de Dependencias (Hexágonos)
            // El proveedor de persistencia se elige por configuración ("Persistence:Provider"), sin
            // que Application/Domain (ni el resto de Infrastructure) necesiten saber cuál está
            // activo. Por defecto (o si la clave no está configurada) se usa MongoDB. Para soportar
            // DynamoDB en el futuro: agregar Persistence/DynamoDb/AddDynamoDbPersistence(...) y un
            // case "DynamoDB" aquí — ver MongoPersistenceServiceCollectionExtensions para el patrón.
            var provider = configuration["Persistence:Provider"];

            switch (provider)
            {
                case null:
                case "":
                case MongoProvider:
                    services.AddMongoPersistence(configuration);
                    break;
                default:
                    throw new NotSupportedException($"El proveedor de persistencia '{provider}' no está soportado.");
            }

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
