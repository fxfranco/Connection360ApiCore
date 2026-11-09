using Connection360Notification.Domain.Settings;
using FluentAssertions;
using Xunit;

namespace Connection360.Domain.Tests.Settings
{
    public class KafkaSettingsTests
    {
        [Fact]
        public void ValoresPorDefecto_SonCadenasVacias()
        {
            var settings = new KafkaSettings();

            settings.BootstrapServers.Should().BeEmpty();
            settings.GroupId.Should().BeEmpty();
            settings.Topic.Should().BeEmpty();
        }

        [Fact]
        public void Propiedades_AsignanYRetornanElValorProvisto()
        {
            var settings = new KafkaSettings { BootstrapServers = "localhost:9092", GroupId = "grupo", Topic = "tema" };

            settings.BootstrapServers.Should().Be("localhost:9092");
            settings.GroupId.Should().Be("grupo");
            settings.Topic.Should().Be("tema");
        }
    }
}
