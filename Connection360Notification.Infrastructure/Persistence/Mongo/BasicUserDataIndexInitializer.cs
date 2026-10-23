using Connection360Notification.Domain.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Connection360Notification.Infrastructure.Persistence.Mongo
{
    /// <summary>
    /// Crea (si no existen) los índices de la colección <c>basicUserData</c>: por SyncId (para contar/borrar usuarios
    /// de una sincronización) y por Document (para buscar a la persona al notificarla). La unicidad por usuario
    /// la garantiza el "_id" (Id de Auth0), así que no hace falta un índice único adicional.
    ///
    /// Igual que MongoIndexInitializer, StartAsync no espera la creación de índices ni deja que un fallo de MongoDB
    /// impida arrancar el proceso: lo registra como advertencia.
    /// </summary>
    public sealed class BasicUserDataIndexInitializer : IHostedService
    {
        private readonly IMongoDbContext _dbContext;
        private readonly BasicUserSyncSettings _settings;
        private readonly ILogger<BasicUserDataIndexInitializer> _logger;

        public BasicUserDataIndexInitializer(IMongoDbContext dbContext, IOptions<BasicUserSyncSettings> settings, ILogger<BasicUserDataIndexInitializer> logger)
        {
            _dbContext = dbContext;
            _settings = settings.Value;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _ = CreateIndexesAsync(cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private async Task CreateIndexesAsync(CancellationToken cancellationToken)
        {
            try
            {
                String collectionName = String.IsNullOrWhiteSpace(_settings.CollectionName)
                    ? BasicUserSyncSettings.DefaultCollectionName
                    : _settings.CollectionName.Trim();

                var collection = _dbContext.GetCollection<BasicUserDataDocument>(collectionName);

                var syncIndex = new CreateIndexModel<BasicUserDataDocument>(
                    Builders<BasicUserDataDocument>.IndexKeys.Ascending(d => d.SyncId));

                var documentIndex = new CreateIndexModel<BasicUserDataDocument>(
                    Builders<BasicUserDataDocument>.IndexKeys.Ascending(d => d.Document));

                await collection.Indexes.CreateManyAsync(new[] { syncIndex, documentIndex }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No fue posible crear/verificar los índices de basicUserData en MongoDB al arrancar.");
            }
        }
    }
}
