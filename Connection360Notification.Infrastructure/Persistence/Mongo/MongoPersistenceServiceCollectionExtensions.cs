using Connection360Notification.Domain.Ports.Outbound;
using Connection360Notification.Domain.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Connection360Notification.Infrastructure.Persistence.Mongo
{
    /// <summary>
    /// Punto de extensión de DI para el proveedor de persistencia MongoDB. Se invoca desde
    /// InfrastructureServiceCollectionExtensions.AddInfrastructure cuando "Persistence:Provider" es
    /// "MongoDB" (o no está configurado, que es el valor por defecto).
    ///
    /// IMongoDbContext se registra como Singleton: es la clase responsable de que exista una única
    /// instancia de MongoClient (y su pool de conexiones) para todo el proceso, compartida por todos
    /// los repositorios/generadores de Mongo sin importar cuántas veces se resuelvan como Scoped.
    ///
    /// Para agregar un proveedor nuevo (p.ej. DynamoDB) el patrón a seguir es el mismo: un método
    /// AddDynamoDbPersistence(configuration) en Persistence/DynamoDb/, con su propio
    /// IDynamoDbContext-equivalente (o cliente ya thread-safe del SDK de AWS) registrado como
    /// Singleton, más sus propias implementaciones de INotificationRepository e
    /// INotificationIdGenerator — ninguno de los cuales requiere cambios en Domain o Application.
    /// </summary>
    public static class MongoPersistenceServiceCollectionExtensions
    {
        public static IServiceCollection AddMongoPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<MongoDbSettings>(configuration.GetSection("MongoDbSettings"));

            services.AddSingleton<IMongoDbContext, MongoDbContext>();
            services.AddSingleton<INotificationIdGenerator, MongoNotificationIdGenerator>();
            services.AddScoped<INotificationRepository, MongoNotificationRepository>();
            services.AddHostedService<MongoIndexInitializer>();

            return services;
        }
    }
}
