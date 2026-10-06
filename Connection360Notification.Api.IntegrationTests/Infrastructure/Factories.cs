using Connection360Notification.Api.IntegrationTests.Infrastructure;

namespace Connection360Notification.Api.IntegrationTests.Infrastructure
{
    /// <summary>Arranca la aplicación en entorno Production (rama else de Program.cs: HSTS + /error).</summary>
    public class ProductionWebApplicationFactory : CustomWebApplicationFactory
    {
        protected override String EnvironmentName => "Production";
    }

    /// <summary>Conserva el <c>SignalRNotifierService</c> real para probar el envío por el hub de SignalR.</summary>
    public class RealNotifierWebApplicationFactory : CustomWebApplicationFactory
    {
        protected override Boolean MockNotifierService => false;
    }

    /// <summary>Conserva los casos de uso reales de la capa de aplicación (con repositorio y Kafka simulados).</summary>
    public class RealUseCasesWebApplicationFactory : CustomWebApplicationFactory
    {
        protected override Boolean MockGetNotificationsUseCase => false;
    }

    /// <summary>Configura valores distintos a los de appsettings para comprobar el binding de configuración.</summary>
    public class CustomConfigurationWebApplicationFactory : CustomWebApplicationFactory
    {
        protected override IDictionary<String, String?> ExtraSettings => new Dictionary<String, String?>
        {
            ["KafkaSettings:BootstrapServers"] = "kafka-test:9999",
            ["KafkaSettings:GroupId"] = "grupo-de-prueba",
            ["KafkaSettings:Topic"] = "topic-de-prueba",
            ["MongoDbSettings:ConnectionString"] = "mongodb://mongo-test:27017",
            ["MongoDbSettings:DatabaseName"] = "db_prueba",
            ["MongoDbSettings:CollectionName"] = "coleccion_prueba",
            ["Jwt:Roles"] = "https://test/roles"
        };
    }

    /// <summary>Configura un proveedor de persistencia no soportado: el arranque debe fallar.</summary>
    public class UnsupportedPersistenceWebApplicationFactory : CustomWebApplicationFactory
    {
        protected override IDictionary<String, String?> ExtraSettings => new Dictionary<String, String?>
        {
            ["Persistence:Provider"] = "DynamoDB"
        };
    }
}
