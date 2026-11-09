using Connection360Notification.Infrastructure.Persistence.Mongo;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Connection360Notification.Infrastructure.Tests.Support
{
    /// <summary>Utilidades para renderizar definiciones de MongoDB a BSON sin necesidad de una base real.</summary>
    internal static class MongoRender
    {
        private static RenderArgs<T> Args<T>() =>
            new(BsonSerializer.SerializerRegistry.GetSerializer<T>(), BsonSerializer.SerializerRegistry);

        public static BsonDocument Filter<T>(FilterDefinition<T> filter) => filter.Render(Args<T>());

        public static BsonDocument Sort<T>(SortDefinition<T> sort) => sort.Render(Args<T>());

        public static BsonValue Update<T>(UpdateDefinition<T> update) => update.Render(Args<T>());
    }

    /// <summary>Cursor asincrono en memoria que entrega una unica pagina de resultados.</summary>
    internal sealed class ListCursor<T> : IAsyncCursor<T>
    {
        private readonly IReadOnlyList<T> _items;
        private Boolean _consumed;

        public ListCursor(IReadOnlyList<T> items) => _items = items;

        public IEnumerable<T> Current => _consumed ? _items : Enumerable.Empty<T>();

        public Boolean MoveNext(CancellationToken cancellationToken = default)
        {
            if (_consumed) return false;
            _consumed = true;
            return true;
        }

        public Task<Boolean> MoveNextAsync(CancellationToken cancellationToken = default) => Task.FromResult(MoveNext(cancellationToken));

        public void Dispose() { }
    }

    /// <summary>IMongoDbContext manual cuyo GetCollection devuelve lo que la prueba configure.</summary>
    internal sealed class FakeMongoDbContext : IMongoDbContext
    {
        public Func<Type, String, Object> CollectionFactory { get; set; } = (_, _) => throw new InvalidOperationException("Sin coleccion configurada.");

        public List<String> RequestedCollections { get; } = new();

        public IMongoDatabase Database => throw new NotSupportedException();

        public IMongoCollection<T> GetCollection<T>(String name)
        {
            RequestedCollections.Add(name);
            return (IMongoCollection<T>)CollectionFactory(typeof(T), name);
        }
    }
}
