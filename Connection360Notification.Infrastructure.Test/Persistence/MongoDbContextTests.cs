using Connection360Notification.Domain.Settings;
using Connection360Notification.Infrastructure.Persistence.Mongo;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Persistence
{
    public class MongoDbContextTests
    {
        private static IOptions<MongoDbSettings> Opciones(String connection, String database) =>
            Options.Create(new MongoDbSettings { ConnectionString = connection, DatabaseName = database, CollectionName = "notifications" });

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_ConConnectionStringVacia_LanzaInvalidOperationException(String connection)
        {
            Action act = () => new MongoDbContext(Opciones(connection, "db"));

            act.Should().Throw<InvalidOperationException>().WithMessage("*ConnectionString*");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_ConDatabaseNameVacio_LanzaInvalidOperationException(String database)
        {
            Action act = () => new MongoDbContext(Opciones("mongodb://127.0.0.1:27017", database));

            act.Should().Throw<InvalidOperationException>().WithMessage("*DatabaseName*");
        }

        [Fact]
        public void Constructor_ConConfiguracionValida_NoCreaElClienteHastaQueSePideLaBase()
        {
            Action act = () => new MongoDbContext(Opciones("mongodb://127.0.0.1:27017", "db"));

            act.Should().NotThrow();
        }

        [Fact]
        public void Database_ExponeLaBaseConfigurada()
        {
            var sut = new MongoDbContext(Opciones("mongodb://127.0.0.1:27017", "mi_base"));

            sut.Database.DatabaseNamespace.DatabaseName.Should().Be("mi_base");
        }

        [Fact]
        public void Database_DevuelveSiempreLaMismaInstancia()
        {
            var sut = new MongoDbContext(Opciones("mongodb://127.0.0.1:27017", "db"));

            sut.Database.Should().BeSameAs(sut.Database);
        }

        [Fact]
        public void GetCollection_DevuelveLaColeccionConElNombreIndicado()
        {
            var sut = new MongoDbContext(Opciones("mongodb://127.0.0.1:27017", "db"));

            var coleccion = sut.GetCollection<NotificationDocument>("notificaciones");

            coleccion.CollectionNamespace.CollectionName.Should().Be("notificaciones");
            coleccion.CollectionNamespace.DatabaseNamespace.DatabaseName.Should().Be("db");
        }

        [Fact]
        public void Database_ConHilosConcurrentes_ComparteUnaUnicaInstancia()
        {
            var sut = new MongoDbContext(Opciones("mongodb://127.0.0.1:27017", "db"));

            var bases = Enumerable.Range(0, 8).AsParallel().Select(_ => sut.Database).ToList();

            bases.Distinct().Should().HaveCount(1);
        }
    }
}
