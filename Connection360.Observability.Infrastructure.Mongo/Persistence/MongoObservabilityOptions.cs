namespace Connection360.Observability.Infrastructure.Mongo.Persistence
{
    /// <summary>
    /// Sección "Observability:Mongo" del appsettings. Si <see cref="ConnectionString"/> o
    /// <see cref="DatabaseName"/> están vacíos se toman (cuando el arranque lo indica) de otra
    /// sección existente, por ejemplo "MongoDbSettings" en Notification.Api.
    /// </summary>
    public sealed class MongoObservabilityOptions
    {
        public const String SectionName = "Observability:Mongo";

        public String ConnectionString { get; set; } = String.Empty;

        public String DatabaseName { get; set; } = String.Empty;

        public String LogsCollection { get; set; } = "notificationLogs";

        public String MetricsCollection { get; set; } = "notificationMetrics";

        public String TracesCollection { get; set; } = "notificationTraces";

        /// <summary>Días que se conservan los logs (índice TTL). 0 = sin expiración.</summary>
        public Int32 LogsRetentionDays { get; set; } = 30;

        /// <summary>Días que se conservan las métricas (índice TTL). 0 = sin expiración.</summary>
        public Int32 MetricsRetentionDays { get; set; } = 90;

        /// <summary>Días que se conservan las trazas (índice TTL). 0 = sin expiración.</summary>
        public Int32 TracesRetentionDays { get; set; } = 14;

        /// <summary>Crear los índices (consultas y TTL) al arrancar.</summary>
        public Boolean CreateIndexes { get; set; } = true;

        /// <summary>Tiempo máximo para encontrar el servidor MongoDB; evita que un servidor caído deje escrituras colgadas.</summary>
        public Int32 ServerSelectionTimeoutSeconds { get; set; } = 5;

        public Boolean IsConfigured => !String.IsNullOrWhiteSpace(ConnectionString) && !String.IsNullOrWhiteSpace(DatabaseName);
    }
}
