using Connection360Notification.Domain.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Connection360Notification.Infrastructure.Persistence.Mongo
{
    /// <summary>
    /// Implementación única (Singleton en DI) del contexto de conexión a MongoDB.
    ///
    /// El driver oficial de MongoDB (MongoClient) ya administra internamente un pool de conexiones
    /// TCP y es thread-safe por diseño: la práctica recomendada por MongoDB es crear UNA sola
    /// instancia de MongoClient por proceso y reutilizarla en toda la aplicación, en vez de abrir un
    /// cliente nuevo por cada operación (que es lo que hacía la implementación original, creando un
    /// "new MongoClient(...)" dentro del constructor del repositorio, resuelto como Scoped, es decir
    /// una conexión nueva por cada request).
    ///
    /// Esta clase se registra como Singleton (ver MongoPersistenceServiceCollectionExtensions), y
    /// además usa Lazy&lt;IMongoDatabase&gt; con isThreadSafe: true como segunda capa de garantía:
    /// la base de datos (y el MongoClient que la respalda) solo se crea la primera vez que algo la
    /// pide, sin importar cuántos hilos concurrentes la soliciten a la vez, y esa misma instancia se
    /// reutiliza durante el resto de la vida del proceso.
    /// </summary>
    public sealed class MongoDbContext : IMongoDbContext
    {
        private readonly Lazy<IMongoDatabase> _database;

        public MongoDbContext(IOptions<MongoDbSettings> mongoSettings)
        {
            var settings = mongoSettings.Value;

            if (String.IsNullOrWhiteSpace(settings.ConnectionString))
                throw new InvalidOperationException("MongoDbSettings.ConnectionString no está configurado.");

            if (String.IsNullOrWhiteSpace(settings.DatabaseName))
                throw new InvalidOperationException("MongoDbSettings.DatabaseName no está configurado.");

            _database = new Lazy<IMongoDatabase>(() =>
            {
                var client = new MongoClient(settings.ConnectionString);
                return client.GetDatabase(settings.DatabaseName);
            }, isThreadSafe: true);
        }

        public IMongoDatabase Database => _database.Value;

        public IMongoCollection<T> GetCollection<T>(String name) => Database.GetCollection<T>(name);
    }
}
