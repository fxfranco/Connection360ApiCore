using Connection360.Observability.Application.DependencyInjection;
using Connection360.Observability.Domain.Models;
using Connection360.Observability.Domain.Ports;
using Connection360.Observability.Infrastructure.Mongo.DependencyInjection;
using Connection360.Observability.Infrastructure.Mongo.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Connection360.Observability.Infrastructure.Mongo.Test.DependencyInjection
{
    public class MongoObservabilityRegistrationTests
    {
        private static IConfiguration Config(params (String Key, String? Value)[] values)
            => new ConfigurationBuilder().AddInMemoryCollection(values.Select(v => new KeyValuePair<String, String?>(v.Key, v.Value))).Build();

        private static ServiceProvider Build(IConfiguration configuration, String? fallback = null)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddConnection360Observability(configuration, ObservedServices.ApiNotification).AddMongoObservabilityStores(fallback);
            return services.BuildServiceProvider();
        }

        [Fact]
        public void Resolve_ConSeccionPropia_LaUsa()
        {
            MongoObservabilityOptions options = MongoObservabilityServiceCollectionExtensions.Resolve(Config(
                ("Observability:Mongo:ConnectionString", "mongodb://propio:27017"),
                ("Observability:Mongo:DatabaseName", "obs"),
                ("Observability:Mongo:LogsCollection", "otros-logs"),
                ("Observability:Mongo:TracesRetentionDays", "5")), null);

            options.ConnectionString.Should().Be("mongodb://propio:27017");
            options.DatabaseName.Should().Be("obs");
            options.LogsCollection.Should().Be("otros-logs");
            options.MetricsCollection.Should().Be("notificationMetrics");
            options.TracesCollection.Should().Be("notificationTraces");
            options.TracesRetentionDays.Should().Be(5);
            options.IsConfigured.Should().BeTrue();
        }

        [Fact]
        public void Resolve_SinSeccionPropia_TomaLaConexionDeLaSeccionAlternativa()
        {
            MongoObservabilityOptions options = MongoObservabilityServiceCollectionExtensions.Resolve(Config(
                ("MongoDbSettings:ConnectionString", "mongodb://notificaciones:27017"),
                ("MongoDbSettings:DatabaseName", "notifications")), "MongoDbSettings");

            options.ConnectionString.Should().Be("mongodb://notificaciones:27017");
            options.DatabaseName.Should().Be("notifications");
            options.IsConfigured.Should().BeTrue();
        }

        [Fact]
        public void Resolve_LaSeccionPropiaTienePrioridadSobreLaAlternativa()
        {
            MongoObservabilityOptions options = MongoObservabilityServiceCollectionExtensions.Resolve(Config(
                ("Observability:Mongo:ConnectionString", "mongodb://propio"),
                ("MongoDbSettings:ConnectionString", "mongodb://alterno"),
                ("MongoDbSettings:DatabaseName", "notifications")), "MongoDbSettings");

            options.ConnectionString.Should().Be("mongodb://propio");
            options.DatabaseName.Should().Be("notifications", "lo que falta se completa desde la sección alternativa");
        }

        [Fact]
        public void Resolve_SinNingunaConfiguracion_NoEstaConfigurado()
        {
            MongoObservabilityServiceCollectionExtensions.Resolve(Config(), "MongoDbSettings").IsConfigured.Should().BeFalse();
        }

        [Fact]
        public void Resolve_SinSeccionAlternativaSolicitada_IgnoraMongoDbSettings()
        {
            MongoObservabilityServiceCollectionExtensions.Resolve(Config(
                ("MongoDbSettings:ConnectionString", "mongodb://x"), ("MongoDbSettings:DatabaseName", "d")), null).IsConfigured.Should().BeFalse();
        }

        [Fact]
        public void AddMongoObservabilityStores_Configurado_RegistraLosTresAlmacenamientosYElInicializador()
        {
            using ServiceProvider provider = Build(Config(("Observability:Mongo:ConnectionString", "mongodb://localhost:27017"), ("Observability:Mongo:DatabaseName", "obs")));

            provider.GetServices<ILogStore>().Should().ContainSingle().Which.Should().BeOfType<MongoLogStore>();
            provider.GetServices<IMetricStore>().Should().ContainSingle().Which.Should().BeOfType<MongoMetricStore>();
            provider.GetServices<ITraceStore>().Should().ContainSingle().Which.Should().BeOfType<MongoTraceStore>();
            provider.GetServices<ITelemetryStoreInitializer>().Should().ContainSingle().Which.Should().BeOfType<MongoObservabilityInitializer>();
            provider.GetRequiredService<IOptions<MongoObservabilityOptions>>().Value.DatabaseName.Should().Be("obs");
        }

        [Fact]
        public void AddMongoObservabilityStores_ConLaConexionDeNotificaciones_UsaLaMismaBaseDeDatos()
        {
            using ServiceProvider provider = Build(Config(
                ("MongoDbSettings:ConnectionString", "mongodb://localhost:27017"), ("MongoDbSettings:DatabaseName", "notifications")), "MongoDbSettings");

            provider.GetRequiredService<IOptions<MongoObservabilityOptions>>().Value.DatabaseName.Should().Be("notifications");
            provider.GetServices<ILogStore>().Should().ContainSingle();
        }

        [Fact]
        public void AddMongoObservabilityStores_SinConfiguracion_NoRegistraNada()
        {
            using ServiceProvider provider = Build(Config());

            provider.GetServices<ILogStore>().Should().BeEmpty();
            provider.GetServices<IMetricStore>().Should().BeEmpty();
            provider.GetServices<ITraceStore>().Should().BeEmpty();
            provider.GetServices<ITelemetryStoreInitializer>().Should().BeEmpty();
            provider.GetService<IMongoObservabilityContext>().Should().BeNull();
        }

        [Fact]
        public void AddMongoObservabilityStores_ConObservabilidadDeshabilitada_NoRegistraNada()
        {
            using ServiceProvider provider = Build(Config(
                ("Observability:Enabled", "false"),
                ("Observability:Mongo:ConnectionString", "mongodb://localhost:27017"), ("Observability:Mongo:DatabaseName", "obs")));

            provider.GetServices<ILogStore>().Should().BeEmpty();
        }

        [Fact]
        public void Opciones_TienenLosValoresPorDefectoEsperados()
        {
            var options = new MongoObservabilityOptions();

            options.LogsCollection.Should().Be("notificationLogs");
            options.MetricsCollection.Should().Be("notificationMetrics");
            options.TracesCollection.Should().Be("notificationTraces");
            options.LogsRetentionDays.Should().Be(30);
            options.MetricsRetentionDays.Should().Be(90);
            options.TracesRetentionDays.Should().Be(14);
            options.CreateIndexes.Should().BeTrue();
            options.IsConfigured.Should().BeFalse();
            MongoObservabilityOptions.SectionName.Should().Be("Observability:Mongo");
        }

        [Theory]
        [InlineData("", "db")]
        [InlineData("mongodb://x", "")]
        [InlineData("  ", "  ")]
        public void IsConfigured_RequiereConexionYBaseDeDatos(String connection, String database)
        {
            new MongoObservabilityOptions { ConnectionString = connection, DatabaseName = database }.IsConfigured.Should().BeFalse();
        }

        [Fact]
        public void Contexto_SinConfigurar_LanzaAlPedirUnaColeccion()
        {
            var context = new MongoObservabilityContext(Options.Create(new MongoObservabilityOptions()));

            Action act = () => context.GetCollection("x");

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Contexto_Configurado_NoSeConectaHastaQueSePideUnaColeccion()
        {
            Action create = () => new MongoObservabilityContext(Options.Create(new MongoObservabilityOptions { ConnectionString = "mongodb://localhost:1", DatabaseName = "d" }));

            create.Should().NotThrow();
        }

        [Fact]
        public void Contexto_Configurado_DevuelveLaColeccionConElNombreIndicado()
        {
            var context = new MongoObservabilityContext(Options.Create(new MongoObservabilityOptions { ConnectionString = "mongodb://localhost:1", DatabaseName = "d" }));

            context.GetCollection("notificationLogs").CollectionNamespace.CollectionName.Should().Be("notificationLogs");
            context.GetCollection("notificationLogs").Database.DatabaseNamespace.DatabaseName.Should().Be("d");
        }
    }
}
