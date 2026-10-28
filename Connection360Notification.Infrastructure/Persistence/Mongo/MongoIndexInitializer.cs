using Connection360Notification.Domain.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Connection360Notification.Infrastructure.Persistence.Mongo
{
    /// <summary>
    /// Crea (si no existen) los índices que necesitan las consultas del repositorio de
    /// notificaciones. CreateManyAsync es idempotente: si el índice ya existe con las mismas
    /// opciones, MongoDB no hace nada.
    ///
    /// StartAsync NO espera a que termine la creación de índices: la dispara en segundo plano y
    /// devuelve inmediatamente. Esto es intencional para que el arranque del proceso (y, en
    /// pruebas de integración, la construcción de WebApplicationFactory) nunca quede bloqueado ni
    /// falle porque MongoDB no esté disponible en ese momento; si la creación de índices falla, se
    /// registra como warning y la aplicación sigue funcionando con normalidad (las consultas
    /// simplemente no tendrán el índice hasta el próximo arranque exitoso).
    /// </summary>
    public sealed class MongoIndexInitializer : IHostedService
    {
        private readonly IMongoDbContext _dbContext;
        private readonly MongoDbSettings _settings;
        private readonly ILogger<MongoIndexInitializer> _logger;

        public MongoIndexInitializer(IMongoDbContext dbContext, IOptions<MongoDbSettings> settings, ILogger<MongoIndexInitializer> logger)
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
                var collection = _dbContext.GetCollection<NotificationDocument>(_settings.CollectionName);

                var clientIndex = new CreateIndexModel<NotificationDocument>(
                    Builders<NotificationDocument>.IndexKeys
                        .Ascending(n => n.ClientId)
                        .Descending(n => n.NotificationDate));

                var idNotificationIndex = new CreateIndexModel<NotificationDocument>(
                    Builders<NotificationDocument>.IndexKeys
                        .Ascending(n => n.ClientId)
                        .Ascending(n => n.IdNotification));

                await collection.Indexes.CreateManyAsync(new[] { clientIndex, idNotificationIndex }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No fue posible crear/verificar los índices de notificaciones en MongoDB al arrancar.");
            }
        }
    }
}
