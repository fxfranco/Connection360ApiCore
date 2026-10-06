using Connection360Notification.Api.IntegrationTests.Infrastructure;
using Connection360Notification.Application.DTOs;
using Connection360Notification.Application.Ports;
using Connection360Notification.Application.Ports.Inbound;
using Connection360Notification.Application.Ports.Output;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Enums;
using Connection360Notification.Domain.Settings;
using Connection360Notification.Infrastructure.Adapters.Input;
using Connection360Notification.Infrastructure.Messaging;
using Connection360Notification.Infrastructure.Persistence.Mongo;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Connection360Notification.Api.IntegrationTests.Hosting
{
    /// <summary>
    /// Pruebas del arranque real (Program.cs): contenedor de DI, binding de configuración y servicios hospedados.
    /// </summary>
    public class ProgramHostIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ProgramHostIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.ResetMocks();
        }

        // ---------- Contenedor de DI ----------

        [Fact]
        public void Host_ResuelveLosCasosDeUsoRealesDeLaCapaDeAplicacion()
        {
            using IServiceScope scope = _factory.Services.CreateScope();

            scope.ServiceProvider.GetRequiredService<IProcessIncomingNotificationUseCase>().Should().NotBeNull();
            scope.ServiceProvider.GetRequiredService<INotificationUseCase>().Should().NotBeNull();
            scope.ServiceProvider.GetRequiredService<IGetNotificationsUseCase>().Should().NotBeNull();
        }

        [Fact]
        public void Host_ResuelveElProveedorDeIdDeUsuarioPersonalizadoParaSignalR()
        {
            _factory.Services.GetRequiredService<IUserIdProvider>().Should().BeOfType<CustomUserIdProvider>();
        }

        [Fact]
        public void Host_ResuelveElHubContextDeNotificaciones()
        {
            _factory.Services.GetRequiredService<IHubContext<NotificationHub>>().Should().NotBeNull();
        }

        [Fact]
        public void Host_RegistraElConsumidorDeKafkaYElInicializadorDeIndicesComoServiciosHospedados()
        {
            _factory.CreateClient();

            _factory.RegisteredHostedServiceTypes.Should().Contain(typeof(KafkaConsumerHostedService));
            _factory.RegisteredHostedServiceTypes.Should().Contain(typeof(MongoIndexInitializer));
        }

        [Fact]
        public async Task ProcessIncomingNotificationUseCase_ResueltoDesdeElHost_GuardaYNotificaPorElPuertoDeSalida()
        {
            var notification = new NotificationMessage("cliente-1", NotificationType.Comment, "hola", title: "titulo");
            using IServiceScope scope = _factory.Services.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<IProcessIncomingNotificationUseCase>();

            await useCase.ExecuteAsync(notification, CancellationToken.None);

            _factory.NotificationRepositoryMock.Verify(r => r.SaveAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
            _factory.NotifierServiceMock.Verify(n => n.SendNotificationToUserAsync("cliente-1", "titulo", notification), Times.Once);
        }

        [Fact]
        public async Task NotificationUseCase_ResueltoDesdeElHost_ProduceElMensajeEnKafka()
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<INotificationUseCase>();
            var request = new CreateNotificationRequest("cliente-2", "contenido", "comment", "DOC-9", "Titulo");

            await useCase.ExecuteSendAsync(request, CancellationToken.None);

            _factory.KafkaProducerMock.Verify(k => k.ProduceNotificationAsync(
                It.Is<NotificationMessage>(n => n.ClientId == "cliente-2" && n.Message == "contenido" && n.Type == NotificationType.Comment && n.DocumentNumber == "DOC-9"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task NotificationUseCase_ConTipoInvalido_LanzaArgumentExceptionYNoProduce()
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<INotificationUseCase>();
            var request = new CreateNotificationRequest("cliente-2", "contenido", "tipo-que-no-existe");

            Func<Task> act = () => useCase.ExecuteSendAsync(request, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
            _factory.KafkaProducerMock.VerifyNoOtherCalls();
        }

        // ---------- Binding de configuración ----------

        [Fact]
        public void Configuracion_EnDevelopment_BindeaKafkaSettingsDesdeAppsettings()
        {
            KafkaSettings settings = _factory.Services.GetRequiredService<IOptions<KafkaSettings>>().Value;

            settings.BootstrapServers.Should().NotBeNullOrWhiteSpace();
            settings.GroupId.Should().NotBeNullOrWhiteSpace();
            settings.Topic.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void Configuracion_EnDevelopment_BindeaMongoDbSettingsDesdeAppsettings()
        {
            MongoDbSettings settings = _factory.Services.GetRequiredService<IOptions<MongoDbSettings>>().Value;

            settings.DatabaseName.Should().NotBeNullOrWhiteSpace();
            settings.CollectionName.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void Configuracion_JwtBearerSeRegistraConLosValoresDeAppsettings()
        {
            JwtBearerOptions options = _factory.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme);

            options.Authority.Should().NotBeNullOrWhiteSpace();
            options.Audience.Should().NotBeNullOrWhiteSpace();
            options.TokenValidationParameters.RoleClaimType.Should().NotBeNullOrWhiteSpace();
            options.Events.Should().NotBeNull();
        }
    }

    public class ProgramConfigurationBindingIntegrationTests : IClassFixture<CustomConfigurationWebApplicationFactory>
    {
        private readonly CustomConfigurationWebApplicationFactory _factory;

        public ProgramConfigurationBindingIntegrationTests(CustomConfigurationWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public void Configuracion_ValoresPersonalizados_SeBindeanAKafkaSettings()
        {
            KafkaSettings settings = _factory.Services.GetRequiredService<IOptions<KafkaSettings>>().Value;

            settings.BootstrapServers.Should().Be("kafka-test:9999");
            settings.GroupId.Should().Be("grupo-de-prueba");
            settings.Topic.Should().Be("topic-de-prueba");
        }

        [Fact]
        public void Configuracion_ValoresPersonalizados_SeBindeanAMongoDbSettings()
        {
            MongoDbSettings settings = _factory.Services.GetRequiredService<IOptions<MongoDbSettings>>().Value;

            settings.ConnectionString.Should().Be("mongodb://mongo-test:27017");
            settings.DatabaseName.Should().Be("db_prueba");
            settings.CollectionName.Should().Be("coleccion_prueba");
        }

        [Fact]
        public void Configuracion_JwtRoles_SeUsaComoTipoDeClaimDeRol()
        {
            JwtBearerOptions options = _factory.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme);

            options.TokenValidationParameters.RoleClaimType.Should().Be("https://test/roles");
        }
    }

    public class ProgramStartupFailureIntegrationTests
    {
        [Fact]
        public void Arranque_ConProveedorDePersistenciaNoSoportado_LanzaNotSupportedException()
        {
            using var factory = new UnsupportedPersistenceWebApplicationFactory();

            Action act = () => { _ = factory.Services; };

            act.Should().Throw<NotSupportedException>().WithMessage("*DynamoDB*");
        }
    }
}
