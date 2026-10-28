using Connection360Notification.Domain.Settings;
using FluentAssertions;
using Xunit;

namespace Connection360Notification.Domain.Tests.Settings
{
    public class SettingsTests
    {
        [Fact]
        public void MongoDbSettings_PorDefecto_TieneCadenasVacias()
        {
            var sut = new MongoDbSettings();

            sut.ConnectionString.Should().BeEmpty();
            sut.DatabaseName.Should().BeEmpty();
            sut.CollectionName.Should().BeEmpty();
        }

        [Fact]
        public void MongoDbSettings_AsignaTodasLasPropiedades()
        {
            var sut = new MongoDbSettings { ConnectionString = "mongodb://h", DatabaseName = "db", CollectionName = "col" };

            sut.ConnectionString.Should().Be("mongodb://h");
            sut.DatabaseName.Should().Be("db");
            sut.CollectionName.Should().Be("col");
        }

        [Fact]
        public void KafkaSettings_PorDefecto_TieneCadenasVacias()
        {
            var sut = new KafkaSettings();

            sut.BootstrapServers.Should().BeEmpty();
            sut.GroupId.Should().BeEmpty();
            sut.Topic.Should().BeEmpty();
        }

        [Fact]
        public void KafkaSettings_AsignaTodasLasPropiedades()
        {
            var sut = new KafkaSettings { BootstrapServers = "h:9092", GroupId = "g", Topic = "t" };

            sut.BootstrapServers.Should().Be("h:9092");
            sut.GroupId.Should().Be("g");
            sut.Topic.Should().Be("t");
        }

        [Fact]
        public void BasicUserSyncSettings_PorDefecto_UsaLosValoresDeLaSincronizacionDeUsuarios()
        {
            var sut = new BasicUserSyncSettings();

            sut.Topic.Should().Be("notification-users");
            sut.GroupId.Should().BeEmpty();
            sut.CollectionName.Should().Be("basicUserData");
            sut.ErrorBackoffMilliseconds.Should().Be(1000);
        }

        [Fact]
        public void BasicUserSyncSettings_Constantes_TienenLosValoresEsperados()
        {
            BasicUserSyncSettings.SectionName.Should().Be("BasicUserSync");
            BasicUserSyncSettings.DefaultTopic.Should().Be("notification-users");
            BasicUserSyncSettings.DefaultCollectionName.Should().Be("basicUserData");
            BasicUserSyncSettings.DefaultGroupId.Should().Be("connection360-notification-users");
            BasicUserSyncSettings.DefaultErrorBackoffMilliseconds.Should().Be(1000);
        }

        [Fact]
        public void BasicUserSyncSettings_AsignaTodasLasPropiedades()
        {
            var sut = new BasicUserSyncSettings { Topic = "t", GroupId = "g", CollectionName = "c", ErrorBackoffMilliseconds = 5 };

            sut.Topic.Should().Be("t");
            sut.GroupId.Should().Be("g");
            sut.CollectionName.Should().Be("c");
            sut.ErrorBackoffMilliseconds.Should().Be(5);
        }
    }
}
