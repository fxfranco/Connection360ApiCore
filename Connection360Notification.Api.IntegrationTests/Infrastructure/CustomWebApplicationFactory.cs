using Connection360Notification.Application.Ports;
using Connection360Notification.Application.Ports.Output;
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
        public Mock<INotifierService> NotifierServiceMock { get; } = new();

        public Mock<INotificationRepository> NotificationRepositoryMock { get; } = new();
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
            NotifierServiceMock.Reset();
            NotificationRepositoryMock.Reset();
            KafkaProducerMock.Reset();
        }

        /// <summary>Entorno de ASP.NET Core con el que se arranca el host (por defecto "Development").</summary>
        protected virtual String EnvironmentName => "Development";

        /// <summary>
        /// Si es true (por defecto) se sustituye <see cref="INotifierService"/> por un mock; si es false se
        /// conserva el <c>SignalRNotifierService</c> real para poder probar el envío por SignalR extremo a extremo.
        /// </summary>
        protected virtual Boolean MockNotifierService => true;

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
                ReplaceWithMock(services, KafkaProducerMock.Object);
                if (MockNotifierService)
                {
                    ReplaceWithMock(services, NotifierServiceMock.Object);
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
