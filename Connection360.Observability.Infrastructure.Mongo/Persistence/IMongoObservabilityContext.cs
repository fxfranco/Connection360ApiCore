using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Connection360.Observability.Infrastructure.Mongo.Persistence
{
    /// <summary>Acceso a las colecciones de observabilidad. Existe como interfaz para poder simularla en pruebas.</summary>
    public interface IMongoObservabilityContext
    {
        MongoObservabilityOptions Options { get; }

        IMongoCollection<BsonDocument> GetCollection(String name);
    }

    /// <summary>
    /// Un único MongoClient (thread-safe, con su pool de conexiones) por proceso, creado de forma
    /// perezosa la primera vez que se escribe. Es independiente del cliente de la persistencia de
    /// negocio: la observabilidad no comparte conexiones ni se ve afectada por ella.
    /// </summary>
    public sealed class MongoObservabilityContext : IMongoObservabilityContext
    {
        private readonly Lazy<IMongoDatabase> _database;

        public MongoObservabilityContext(IOptions<MongoObservabilityOptions> options)
        {
            Options = options.Value;
            _database = new Lazy<IMongoDatabase>(() =>
            {
                if (!Options.IsConfigured)
                {
                    throw new InvalidOperationException("Observability:Mongo requiere ConnectionString y DatabaseName.");
                }

                MongoClientSettings settings = MongoClientSettings.FromConnectionString(Options.ConnectionString);
                if (Options.ServerSelectionTimeoutSeconds > 0)
                {
                    settings.ServerSelectionTimeout = TimeSpan.FromSeconds(Options.ServerSelectionTimeoutSeconds);
                }

                return new MongoClient(settings).GetDatabase(Options.DatabaseName);
            }, isThreadSafe: true);
        }

        public MongoObservabilityOptions Options { get; }

        public IMongoCollection<BsonDocument> GetCollection(String name) => _database.Value.GetCollection<BsonDocument>(name);
    }
}
