using Connection360Notification.Domain.Ports.Outbound;
using Connection360Notification.Domain.Settings;
using Connection360Notification.Infrastructure.Persistence.Mongo;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Persistence
{
    public class MongoPersistenceServiceCollectionExtensionsTests
    {
        private static IConfiguration Config(params (String Key, String Value)[] valores) =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(valores.Select(v => new KeyValuePair<String, String?>(v.Key, v.Value)))
                .Build();

        private static readonly (String, String)[] ConfigMongo =
        {
            ("MongoDbSettings:ConnectionString", "mongodb://127.0.0.1:27017"),
            ("MongoDbSettings:DatabaseName", "base_prueba"),
            ("MongoDbSettings:CollectionName", "coleccion_prueba")
        };

        [Fact]
        public void AddMongoPersistence_RetornaLaMismaColeccionParaEncadenar()
        {
            var services = new ServiceCollection();

            services.AddMongoPersistence(Config()).Should().BeSameAs(services);
        }

        [Fact]
        public void AddMongoPersistence_RegistraLosCicloDeVidaEsperados()
        {
            var services = new ServiceCollection();

            services.AddMongoPersistence(Config());

            services.Should().Contain(sd => sd.ServiceType == typeof(IMongoDbContext) && sd.ImplementationType == typeof(MongoDbContext) && sd.Lifetime == ServiceLifetime.Singleton);
            services.Should().Contain(sd => sd.ServiceType == typeof(INotificationIdGenerator) && sd.ImplementationType == typeof(MongoNotificationIdGenerator) && sd.Lifetime == ServiceLifetime.Singleton);
            services.Should().Contain(sd => sd.ServiceType == typeof(INotificationRepository) && sd.ImplementationType == typeof(MongoNotificationRepository) && sd.Lifetime == ServiceLifetime.Scoped);
            services.Should().Contain(sd => sd.ServiceType == typeof(IHostedService) && sd.ImplementationType == typeof(MongoIndexInitializer));
        }

        [Fact]
        public void AddMongoPersistence_EnlazaLaSeccionMongoDbSettings()
        {
            var services = new ServiceCollection();
            services.AddMongoPersistence(Config(ConfigMongo));

            using var provider = services.BuildServiceProvider();
            var settings = provider.GetRequiredService<IOptions<MongoDbSettings>>().Value;

            settings.ConnectionString.Should().Be("mongodb://127.0.0.1:27017");
            settings.DatabaseName.Should().Be("base_prueba");
            settings.CollectionName.Should().Be("coleccion_prueba");
        }

        [Fact]
        public void AddMongoPersistence_ResuelveRepositorioYGeneradorDeIds()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddMongoPersistence(Config(ConfigMongo));

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();

            scope.ServiceProvider.GetRequiredService<INotificationRepository>().Should().BeOfType<MongoNotificationRepository>();
            scope.ServiceProvider.GetRequiredService<INotificationIdGenerator>().Should().BeOfType<MongoNotificationIdGenerator>();
        }

        [Fact]
        public void AddMongoPersistence_ElContextoEsUnicoEntreScopes()
        {
            var services = new ServiceCollection();
            services.AddMongoPersistence(Config(ConfigMongo));

            using var provider = services.BuildServiceProvider();
            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            scope1.ServiceProvider.GetRequiredService<IMongoDbContext>()
                .Should().BeSameAs(scope2.ServiceProvider.GetRequiredService<IMongoDbContext>());
            scope1.ServiceProvider.GetRequiredService<INotificationRepository>()
                .Should().NotBeSameAs(scope2.ServiceProvider.GetRequiredService<INotificationRepository>());
        }

        [Fact]
        public void AddMongoPersistence_SinConfiguracion_FallaAlResolverElContexto()
        {
            var services = new ServiceCollection();
            services.AddMongoPersistence(Config());

            using var provider = services.BuildServiceProvider();
            Action act = () => provider.GetRequiredService<IMongoDbContext>();

            act.Should().Throw<InvalidOperationException>().WithMessage("*ConnectionString*");
        }
    }
}
