using Connection360.Infrastructure.Messaging;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Connection360.Infrastructure.Tests.Messaging
{
    public class Auth0UserSyncSettingsTests
    {
        [Fact]
        public void Constantes_TienenLosValoresEsperados()
        {
            Auth0UserSyncSettings.SectionName.Should().Be("Auth0UserSync");
            Auth0UserSyncSettings.DefaultTopic.Should().Be("notification-users");
            Auth0UserSyncSettings.DefaultPollingIntervalMinutes.Should().Be(60);
            Auth0UserSyncSettings.DefaultBatchSize.Should().Be(200);
            Auth0UserSyncSettings.DefaultStartupRetryDelaySeconds.Should().Be(30);
        }

        [Fact]
        public void ValoresPorDefecto_SonLosDeLasConstantes()
        {
            var settings = new Auth0UserSyncSettings();

            settings.Enabled.Should().BeTrue();
            settings.Topic.Should().Be(Auth0UserSyncSettings.DefaultTopic);
            settings.PollingIntervalMinutes.Should().Be(Auth0UserSyncSettings.DefaultPollingIntervalMinutes);
            settings.BatchSize.Should().Be(Auth0UserSyncSettings.DefaultBatchSize);
            settings.StartupRetryDelaySeconds.Should().Be(Auth0UserSyncSettings.DefaultStartupRetryDelaySeconds);
        }

        [Fact]
        public void Bind_DesdeConfiguracion_AsignaTodosLosValores()
        {
            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<String, String?>
            {
                ["Auth0UserSync:Enabled"] = "false",
                ["Auth0UserSync:Topic"] = "otro-topico",
                ["Auth0UserSync:PollingIntervalMinutes"] = "15",
                ["Auth0UserSync:BatchSize"] = "50",
                ["Auth0UserSync:StartupRetryDelaySeconds"] = "5"
            }).Build();

            Auth0UserSyncSettings? settings = config.GetSection(Auth0UserSyncSettings.SectionName).Get<Auth0UserSyncSettings>();

            settings!.Enabled.Should().BeFalse();
            settings.Topic.Should().Be("otro-topico");
            settings.PollingIntervalMinutes.Should().Be(15);
            settings.BatchSize.Should().Be(50);
            settings.StartupRetryDelaySeconds.Should().Be(5);
        }

        [Fact]
        public void Mensaje_ValoresPorDefecto_ListaVacia()
        {
            var message = new Auth0UsersSyncMessage();

            message.Users.Should().BeEmpty();
            message.SyncId.Should().Be(Guid.Empty);
        }
    }
}
