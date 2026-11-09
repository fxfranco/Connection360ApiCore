using MongoDB.Driver;

namespace Connection360Notification.Infrastructure.Persistence.Mongo
{
    /// <summary>
    /// Punto único de acceso a la base de datos de MongoDB para toda la capa de infraestructura.
    /// Todos los repositorios/generadores de Mongo obtienen sus colecciones a través de esta
    /// interfaz, en vez de crear su propio MongoClient, para garantizar que comparten la misma
    /// instancia de conexión (ver MongoDbContext).
    /// </summary>
    public interface IMongoDbContext
    {
        IMongoDatabase Database { get; }

        IMongoCollection<T> GetCollection<T>(String name);
    }
}
