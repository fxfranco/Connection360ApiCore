using Connection360Notification.Application.Ports.Output;
using Connection360Notification.Application.Services;
using Connection360Notification.Domain.Ports.Outbound;
using Connection360Notification.Domain.Settings;
using Connection360Notification.Infrastructure.Adapters.Input;
using Connection360Notification.Infrastructure.Adapters.Output;
using Connection360Notification.Infrastructure.Email;
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

            // Canales de notificación (Strategy): cada uno encapsula su envío técnico. El registro de canales
            // (Application) los recibe todos como IEnumerable<INotificationChannel> y los resuelve por usuario.
            // Para agregar un canal nuevo: implementar INotificationChannel y registrarlo aquí.
            services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
            services.AddSingleton<IEmailSender, MailKitEmailSender>();
            // Datos del destinatario de la notificación en curso (los llena ProcessIncomingNotificationUseCase y los lee el
            // canal de correo). Es "scoped": una instancia por mensaje procesado.
            services.AddScoped<INotificationRecipientContext, NotificationRecipientContext>();
            services.AddScoped<INotificationChannel, SignalRNotificationChannel>();
            services.AddScoped<INotificationChannel, EmailNotificationChannel>();
            services.AddScoped<INotificationChannel, SmsNotificationChannel>();

            // 3. Worker Background Service para Consumo de Kafka
            services.AddHostedService<KafkaConsumerHostedService>();

            // 4. Worker de sincronización de usuarios Auth0 -> colección basicUserData (topic notification-users)
            services.Configure<BasicUserSyncSettings>(configuration.GetSection(BasicUserSyncSettings.SectionName));
            services.AddHostedService<BasicUserDataConsumerHostedService>();

            return services;
        }
    }
}
