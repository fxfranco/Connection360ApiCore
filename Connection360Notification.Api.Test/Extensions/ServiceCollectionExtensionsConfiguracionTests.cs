using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Connection360Notification.Api.Extensions;
using Connection360Notification.Application.Ports;
using Connection360Notification.Application.Ports.Inbound;
using Connection360Notification.Application.Ports.Output;
using Connection360Notification.Application.UseCases;
using Connection360Notification.Domain.Ports.Outbound;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace Connection360Notification.Api.Tests.Extensions
{
    public class ServiceCollectionExtensionsConfiguracionTests
    {
        private const String Roles = "https://connection360/roles";
        private const String Issuer = "https://issuer.test/";
        private const String Audience = "https://audience.test";
        private const String Secret = "un-secreto-de-prueba-con-longitud-suficiente-32+";

        private static IConfiguration BuildConfig(Boolean includeSecret = true, Boolean includeRoles = true, Boolean includeIssuer = true)
        {
            var values = new Dictionary<String, String?>();
            if (includeSecret) values["Jwt:Secret"] = Secret;
            if (includeRoles) values["Jwt:Roles"] = Roles;
            if (includeIssuer)
            {
                values["Jwt:Issuer"] = Issuer;
                values["Jwt:Audience"] = Audience;
            }
            return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        }

        private static ServiceProvider BuildProviderWithFakes(Action<IServiceCollection> register)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(Mock.Of<INotificationRepository>());
            services.AddSingleton(Mock.Of<INotifierService>());
            services.AddSingleton(Mock.Of<IKafkaProducerService>());
            register(services);
            return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        }

        // ---------- AddApplicationServices / Dev ----------

        [Fact]
        public void AddApplicationServices_RegistraLosUseCasesConLasImplementacionesYCicloDeVidaScoped()
        {
            var services = new ServiceCollection();

            IServiceCollection result = services.AddApplicationServices();

            result.Should().BeSameAs(services);
            AssertDescriptor<INotificationUseCase, NotificationUseCase>(services);
            AssertDescriptor<IGetNotificationsUseCase, GetNotificationsUseCase>(services);
            AssertDescriptor<IProcessIncomingNotificationUseCase, ProcessIncomingNotificationUseCase>(services);
            services.Should().HaveCount(3);
        }

        [Fact]
        public void AddApplicationServices_ResuelveLosUseCasesDentroDeUnScope()
        {
            using ServiceProvider provider = BuildProviderWithFakes(s => s.AddApplicationServices());
            using IServiceScope scope = provider.CreateScope();

            scope.ServiceProvider.GetRequiredService<INotificationUseCase>().Should().BeOfType<NotificationUseCase>();
            scope.ServiceProvider.GetRequiredService<IGetNotificationsUseCase>().Should().BeOfType<GetNotificationsUseCase>();
            scope.ServiceProvider.GetRequiredService<IProcessIncomingNotificationUseCase>().Should().BeOfType<ProcessIncomingNotificationUseCase>();
        }

        [Fact]
        public void AddApplicationServices_ResuelveInstanciasDistintasEnScopesDistintosYLaMismaEnElMismoScope()
        {
            using ServiceProvider provider = BuildProviderWithFakes(s => s.AddApplicationServices());
            using IServiceScope scope1 = provider.CreateScope();
            using IServiceScope scope2 = provider.CreateScope();

            var a = scope1.ServiceProvider.GetRequiredService<IGetNotificationsUseCase>();
            var b = scope1.ServiceProvider.GetRequiredService<IGetNotificationsUseCase>();
            var c = scope2.ServiceProvider.GetRequiredService<IGetNotificationsUseCase>();

            a.Should().BeSameAs(b);
            a.Should().NotBeSameAs(c);
        }

        private static void AssertDescriptor<TService, TImplementation>(IServiceCollection services)
        {
            ServiceDescriptor descriptor = services.Single(sd => sd.ServiceType == typeof(TService));
            descriptor.ImplementationType.Should().Be(typeof(TImplementation));
            descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
        }

        // ---------- Versionado ----------

        [Fact]
        public void AddApiVersioningSetup_ConfiguraLasOpcionesDeVersionado()
        {
            var services = new ServiceCollection();
            services.AddApiVersioningSetup();
            using ServiceProvider provider = services.BuildServiceProvider();

            ApiVersioningOptions options = provider.GetRequiredService<IOptions<ApiVersioningOptions>>().Value;

            options.DefaultApiVersion.Should().Be(new ApiVersion(1, 0));
            options.AssumeDefaultVersionWhenUnspecified.Should().BeTrue();
            options.ReportApiVersions.Should().BeTrue();
            options.ApiVersionReader.Should().BeOfType<UrlSegmentApiVersionReader>();
        }

        [Fact]
        public void AddApiVersioningSetup_ConfiguraElApiExplorer()
        {
            var services = new ServiceCollection();
            services.AddApiVersioningSetup();
            using ServiceProvider provider = services.BuildServiceProvider();

            ApiExplorerOptions options = provider.GetRequiredService<IOptions<ApiExplorerOptions>>().Value;

            options.GroupNameFormat.Should().Be("'v'VVV");
            options.SubstituteApiVersionInUrl.Should().BeTrue();
        }

        // ---------- JWT (produccion: Auth0 / Authority) ----------

        private static JwtBearerOptions GetJwtOptions(Action<IServiceCollection> register)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            register(services);
            ServiceProvider provider = services.BuildServiceProvider();
            return provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);
        }

        [Fact]
        public void AddJwtAuthentication_RetornaLaMismaColeccion()
        {
            var services = new ServiceCollection();

            services.AddJwtAuthentication(BuildConfig()).Should().BeSameAs(services);
        }

        [Fact]
        public void AddJwtAuthentication_ConfiguraAutoridadAudienciaYClaimsDeRol()
        {
            JwtBearerOptions options = GetJwtOptions(s => s.AddJwtAuthentication(BuildConfig()));

            options.Authority.Should().Be(Issuer);
            options.Audience.Should().Be(Audience);
            options.TokenValidationParameters.RoleClaimType.Should().Be(Roles);
            options.TokenValidationParameters.NameClaimType.Should().Be(ClaimTypes.NameIdentifier);
        }

        [Fact]
        public void AddJwtAuthentication_RegistraElEsquemaBearerComoPorDefecto()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddJwtAuthentication(BuildConfig());
            using ServiceProvider provider = services.BuildServiceProvider();

            AuthenticationOptions options = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;

            options.DefaultAuthenticateScheme.Should().Be(JwtBearerDefaults.AuthenticationScheme);
            options.DefaultChallengeScheme.Should().Be(JwtBearerDefaults.AuthenticationScheme);
            options.SchemeMap.Should().ContainKey(JwtBearerDefaults.AuthenticationScheme);
        }

        [Fact]
        public void AddJwtAuthentication_SinIssuerNiAudience_NoLanzaExcepcionYDejaLosValoresNulos()
        {
            JwtBearerOptions options = GetJwtOptions(s => s.AddJwtAuthentication(BuildConfig(includeIssuer: false)));

            options.Authority.Should().BeNull();
            options.Audience.Should().BeNull();
        }

        [Fact]
        public void AddJwtAuthentication_SinSecretYSinRoles_LanzaPrimeroLaExcepcionDelSecret()
        {
            var services = new ServiceCollection();
            IConfiguration config = BuildConfig(includeSecret: false, includeRoles: false);

            Action act = () => services.AddJwtAuthentication(config);

            act.Should().Throw<InvalidOperationException>().WithMessage("*Jwt:Secret*");
        }

        [Fact]
        public async Task AddJwtAuthentication_OnMessageReceived_ConTokenEnQueryYRutaDelHub_AsignaElToken()
        {
            JwtBearerOptions options = GetJwtOptions(s => s.AddJwtAuthentication(BuildConfig()));
            MessageReceivedContext context = BuildMessageContext(options, "/api/v1/hubs/notifications", "?access_token=abc.def.ghi");

            await options.Events.MessageReceived(context);

            context.Token.Should().Be("abc.def.ghi");
        }

        [Fact]
        public async Task AddJwtAuthentication_OnMessageReceived_ConTokenEnQueryPeroOtraRuta_NoAsignaElToken()
        {
            JwtBearerOptions options = GetJwtOptions(s => s.AddJwtAuthentication(BuildConfig()));
            MessageReceivedContext context = BuildMessageContext(options, "/api/v1/notifications/allnotifications", "?access_token=abc");

            await options.Events.MessageReceived(context);

            context.Token.Should().BeNull();
        }

        [Fact]
        public async Task AddJwtAuthentication_OnMessageReceived_SinTokenEnQueryEnRutaDelHub_NoAsignaElToken()
        {
            JwtBearerOptions options = GetJwtOptions(s => s.AddJwtAuthentication(BuildConfig()));
            MessageReceivedContext context = BuildMessageContext(options, "/api/v1/hubs/notifications", String.Empty);

            await options.Events.MessageReceived(context);

            context.Token.Should().BeNull();
        }

        [Fact]
        public async Task AddJwtAuthentication_OnMessageReceived_ConTokenVacioEnRutaDelHub_NoAsignaElToken()
        {
            JwtBearerOptions options = GetJwtOptions(s => s.AddJwtAuthentication(BuildConfig()));
            MessageReceivedContext context = BuildMessageContext(options, "/api/v1/hubs/notifications", "?access_token=");

            await options.Events.MessageReceived(context);

            context.Token.Should().BeNull();
        }        

        private static MessageReceivedContext BuildMessageContext(JwtBearerOptions options, String path, String query)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = path;
            httpContext.Request.QueryString = new QueryString(query);
            var scheme = new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler));
            return new MessageReceivedContext(httpContext, scheme, options);
        }
    }
}
