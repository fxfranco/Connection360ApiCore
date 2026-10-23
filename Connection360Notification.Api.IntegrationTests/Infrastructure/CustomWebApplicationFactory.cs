using Connection360Notification.Application.Ports;
using Connection360Notification.Application.Ports.Output;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Enums;
using Connection360Notification.Domain.Ports.Outbound;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Connection360Notification.Api.IntegrationTests.Infrastructure
{
    /// <summary>
    /// WebApplicationFactory que arranca la aplicación real (Program.cs) tal cual está compuesta
    /// (routing, versionado, [Authorize], ApiResponseFilter, ExceptionHandlingMiddleware,
    /// NotFoundMiddleware), mientras sustituye:
    ///   - La autenticación JwtBearer real (Auth0) por TestAuthHandler, controlable por headers.
    ///   - Los casos de uso de la capa de aplicación por mocks de Moq, para no depender de
    ///     Postgres real, Auth0 Management API ni las APIs externas (BPMS/SIM/OPENCOMEX/etc.).
    ///
    /// Esto permite que las pruebas de este proyecto sean auténticas pruebas de integración del
    /// pipeline HTTP + capa Api, sin convertirse en pruebas end-to-end contra servicios externos.
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        public Mock<IGetNotificationsUseCase> NotificationsUseCaseMock { get; } = new();
        /// <summary>Canales de notificación simulados (uno por NotificationChannelType) que sustituyen a los reales por defecto.</summary>
        public Mock<INotificationChannel> ApplicationChannelMock { get; } = CreateChannelMock(NotificationChannelType.Application);
        public Mock<INotificationChannel> EmailChannelMock { get; } = CreateChannelMock(NotificationChannelType.Email);
        public Mock<INotificationChannel> SmsChannelMock { get; } = CreateChannelMock(NotificationChannelType.TextMessages);

        public Mock<INotificationRepository> NotificationRepositoryMock { get; } = new();
        public Mock<IBasicUserDataRepository> BasicUserDataRepositoryMock { get; } = new();
        public Mock<IKafkaProducerService> KafkaProducerMock { get; } = new();

        /// <summary>Tipos de IHostedService que registraba la aplicación real antes de que la factory los retirara.</summary>
        public IReadOnlyList<Type> RegisteredHostedServiceTypes { get; private set; } = Array.Empty<Type>();

        /// <summary>
        /// Reinicia todos los mocks (setups + invocaciones registradas) entre pruebas
        /// para evitar fugas de estado, ya que la factory se comparte vía IClassFixture.
        /// </summary>
        public void ResetMocks()
        {
            NotificationsUseCaseMock.Reset();
            foreach (Mock<INotificationChannel> channel in ChannelMocks)
            {
                channel.Reset();
            }
            ConfigureChannelMocks();
            NotificationRepositoryMock.Reset();
            BasicUserDataRepositoryMock.Reset();
            KafkaProducerMock.Reset();
        }

        private IEnumerable<Mock<INotificationChannel>> ChannelMocks => new[] { ApplicationChannelMock, EmailChannelMock, SmsChannelMock };

        private static Mock<INotificationChannel> CreateChannelMock(NotificationChannelType type)
        {
            var mock = new Mock<INotificationChannel>();
            ConfigureChannelMock(mock, type);
            return mock;
        }

        private static void ConfigureChannelMock(Mock<INotificationChannel> mock, NotificationChannelType type)
        {
            mock.SetupGet(c => c.Channel).Returns(type);
            mock.Setup(c => c.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        }

        private void ConfigureChannelMocks()
        {
            ConfigureChannelMock(ApplicationChannelMock, NotificationChannelType.Application);
            ConfigureChannelMock(EmailChannelMock, NotificationChannelType.Email);
            ConfigureChannelMock(SmsChannelMock, NotificationChannelType.TextMessages);
        }

        /// <summary>Entorno de ASP.NET Core con el que se arranca el host (por defecto "Development").</summary>
        protected virtual String EnvironmentName => "Development";

        /// <summary>
        /// Si es true (por defecto) los canales de notificación (<see cref="INotificationChannel"/>: aplicación, correo y SMS)
        /// se sustituyen por mocks; el registro de canales y el despachador de Application siguen siendo los reales. Si es false
        /// se conservan los canales reales para poder probar el envío por SignalR extremo a extremo.
        /// </summary>
        protected virtual Boolean MockNotificationChannels => true;

        /// <summary>
        /// Si es true (por defecto) se sustituye <see cref="IGetNotificationsUseCase"/> por un mock; si es false se
        /// conserva el caso de uso real (con el repositorio simulado) para probar el flujo controlador -> aplicación -> mapeo.
        /// </summary>
        protected virtual Boolean MockGetNotificationsUseCase => true;

        /// <summary>
        /// Configuración adicional en memoria (clave "A:B" -> valor). Se aplica como configuración del host (UseSetting)
        /// para que también la lea el código de Program.cs que consulta la configuración al construir el contenedor.
        /// </summary>
        protected virtual IDictionary<String, String?> ExtraSettings => new Dictionary<String, String?>();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(EnvironmentName);

            foreach (KeyValuePair<String, String?> setting in ExtraSettings)
            {
                builder.UseSetting(setting.Key, setting.Value);
            }

            builder.ConfigureTestServices(services =>
            {
                // --- Autenticación de prueba, controlable por headers (ver TestAuthHandler) ---
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

                services.PostConfigure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    options.DefaultScheme = TestAuthHandler.SchemeName;
                });

                // --- Reemplazo de casos de uso por mocks (evita Postgres/Auth0/APIs externas reales) ---
                if (MockGetNotificationsUseCase)
                {
                    ReplaceWithMock(services, NotificationsUseCaseMock.Object);
                }

                // Puertos de salida de infraestructura (MongoDB y Kafka) simulados: nunca hay conexiones reales.
                ReplaceWithMock(services, NotificationRepositoryMock.Object);
                ReplaceWithMock(services, BasicUserDataRepositoryMock.Object);
                ReplaceWithMock(services, KafkaProducerMock.Object);
                if (MockNotificationChannels)
                {
                    services.RemoveAll<INotificationChannel>();
                    foreach (Mock<INotificationChannel> channel in ChannelMocks)
                    {
                        services.AddSingleton(channel.Object);
                    }
                }

                // --- Sin servicios hospedados que tocan infraestructura real (Kafka / MongoDB) ---
                RegisteredHostedServiceTypes = services
                    .Where(sd => sd.ServiceType == typeof(IHostedService))
                    .Select(sd => sd.ImplementationType)
                    .OfType<Type>()
                    .ToList();
                services.RemoveAll<IHostedService>();
            });
        }

        private static void ReplaceWithMock<TService>(IServiceCollection services, TService instance) where TService : class
        {
            services.RemoveAll<TService>();
            services.AddSingleton(instance);
        }
    }
}
