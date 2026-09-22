using Connection360Notification.Application.Ports;
using Connection360Notification.Application.Ports.Output;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        /// <summary>
        /// Reinicia todos los mocks (setups + invocaciones registradas) entre pruebas
        /// para evitar fugas de estado, ya que la factory se comparte vía IClassFixture.
        /// </summary>
        public void ResetMocks()
        {
            NotificationsUseCaseMock.Reset();
            NotifierServiceMock.Reset();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

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
                ReplaceWithMock(services, NotificationsUseCaseMock.Object);
                ReplaceWithMock(services, NotifierServiceMock.Object);
            });
        }

        private static void ReplaceWithMock<TService>(IServiceCollection services, TService instance) where TService : class
        {
            services.RemoveAll<TService>();
            services.AddSingleton(instance);
        }
    }
}
