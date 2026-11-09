using Connection360Notification.Application.Ports.Output;
using Connection360Notification.Application.Services;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Enums;
using Connection360Notification.Domain.Ports.Outbound;
using Connection360Notification.Domain.Settings;
using Connection360Notification.Infrastructure.Adapters.Input;
using Connection360Notification.Infrastructure.Adapters.Output;
using Connection360Notification.Infrastructure.DependencyInjection;
using Connection360Notification.Infrastructure.Email;
using Connection360Notification.Infrastructure.Messaging;
using Connection360Notification.Infrastructure.Persistence.Mongo;
using Connection360Notification.Infrastructure.Sms;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.DependencyInjection
{
    /// <summary>Pruebas de AddInfrastructure con un contenedor real (sin conectarse a Mongo ni Kafka).</summary>
    public class InfrastructureServiceCollectionExtensionsResolucionTests
    {
        private static IConfiguration Config(params (String Key, String Value)[] valores) =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(valores.Select(v => new KeyValuePair<String, String?>(v.Key, v.Value)))
                .Build();

        private static readonly (String, String)[] ConfigBase =
        {
            ("MongoDbSettings:ConnectionString", "mongodb://127.0.0.1:27017"),
            ("MongoDbSettings:DatabaseName", "base_prueba"),
            ("MongoDbSettings:CollectionName", "coleccion_prueba")
        };

        private static ServiceProvider Construir(IConfiguration configuration)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.Configure<KafkaSettings>(o => { o.BootstrapServers = "localhost:9092"; o.GroupId = "g"; o.Topic = "t"; });
            services.AddInfrastructure(configuration);
            return services.BuildServiceProvider();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("MongoDB")]
        public void AddInfrastructure_ConProveedorMongoOPorDefecto_RegistraLaPersistenciaDeMongo(String? proveedor)
        {
            var valores = proveedor is null ? ConfigBase : ConfigBase.Append(("Persistence:Provider", proveedor)).ToArray();
            var services = new ServiceCollection();

            services.AddInfrastructure(Config(valores));

            services.Should().Contain(sd => sd.ServiceType == typeof(IMongoDbContext));
            services.Should().Contain(sd => sd.ServiceType == typeof(INotificationIdGenerator));
        }

        [Theory]
        [InlineData("DynamoDB")]
        [InlineData("mongodb")]
        [InlineData("SqlServer")]
        public void AddInfrastructure_ConProveedorNoSoportado_LanzaNotSupportedException(String proveedor)
        {
            var services = new ServiceCollection();

            Action act = () => services.AddInfrastructure(Config(("Persistence:Provider", proveedor)));

            act.Should().Throw<NotSupportedException>().WithMessage($"*'{proveedor}'*");
        }

        [Fact]
        public void AddInfrastructure_RegistraLosServiciosHospedados()
        {
            var services = new ServiceCollection();

            services.AddInfrastructure(Config(ConfigBase));

            services.Should().Contain(sd => sd.ServiceType == typeof(IHostedService) && sd.ImplementationType == typeof(KafkaConsumerHostedService));
            services.Should().Contain(sd => sd.ServiceType == typeof(IHostedService) && sd.ImplementationType == typeof(MongoIndexInitializer));
            services.Should().Contain(sd => sd.ServiceType == typeof(IHostedService) && sd.ImplementationType == typeof(BasicUserDataConsumerHostedService));
            services.Should().Contain(sd => sd.ServiceType == typeof(IHostedService) && sd.ImplementationType == typeof(BasicUserDataIndexInitializer));
        }

        [Fact]
        public void AddInfrastructure_EnlazaLaSeccionBasicUserSyncConSusValoresPorDefecto()
        {
            using var provider = Construir(Config(ConfigBase));

            var settings = provider.GetRequiredService<IOptions<BasicUserSyncSettings>>().Value;

            settings.Topic.Should().Be("notification-users");
            settings.CollectionName.Should().Be("basicUserData");
            settings.GroupId.Should().BeEmpty();
            settings.ErrorBackoffMilliseconds.Should().Be(BasicUserSyncSettings.DefaultErrorBackoffMilliseconds);
        }

        [Fact]
        public void AddInfrastructure_EnlazaLaSeccionBasicUserSyncDesdeLaConfiguracion()
        {
            var valores = ConfigBase.Concat(new[]
            {
                ("BasicUserSync:Topic", "usuarios"),
                ("BasicUserSync:GroupId", "grupo-usuarios"),
                ("BasicUserSync:CollectionName", "mis_usuarios"),
                ("BasicUserSync:ErrorBackoffMilliseconds", "250")
            }).ToArray();
            using var provider = Construir(Config(valores));

            var settings = provider.GetRequiredService<IOptions<BasicUserSyncSettings>>().Value;

            settings.Topic.Should().Be("usuarios");
            settings.GroupId.Should().Be("grupo-usuarios");
            settings.CollectionName.Should().Be("mis_usuarios");
            settings.ErrorBackoffMilliseconds.Should().Be(250);
        }

        [Fact]
        public void AddInfrastructure_ConContenedorReal_ResuelveElRepositorioDeUsuariosBasicos()
        {
            using var provider = Construir(Config(ConfigBase));
            using var scope = provider.CreateScope();

            scope.ServiceProvider.GetRequiredService<IBasicUserDataRepository>().Should().BeOfType<MongoBasicUserDataRepository>();
        }

        [Fact]
        public void AddInfrastructure_RegistraElContextoDelDestinatarioComoScoped_YCadaAmbitoTieneElSuyo()
        {
            using var provider = Construir(Config(ConfigBase));
            using var scopeUno = provider.CreateScope();
            using var scopeDos = provider.CreateScope();

            var uno = scopeUno.ServiceProvider.GetRequiredService<INotificationRecipientContext>();
            var dos = scopeDos.ServiceProvider.GetRequiredService<INotificationRecipientContext>();

            uno.Should().BeOfType<NotificationRecipientContext>();
            uno.Should().BeSameAs(scopeUno.ServiceProvider.GetRequiredService<INotificationRecipientContext>());
            uno.Should().NotBeSameAs(dos);
        }

        [Fact]
        public void AddInfrastructure_ElCanalDeCorreoYElContextoCompartenElMismoAmbito()
        {
            using var provider = Construir(Config(ConfigBase));
            using var scope = provider.CreateScope();
            var usuario = new BasicUserData("auth0|1", "900123", "Ana", "300", "ana@correo.com");

            scope.ServiceProvider.GetRequiredService<INotificationRecipientContext>().Set(usuario);
            var canal = scope.ServiceProvider.GetServices<INotificationChannel>().OfType<EmailNotificationChannel>().Single();

            canal.Should().NotBeNull();
            scope.ServiceProvider.GetRequiredService<INotificationRecipientContext>().Recipient.Should().Be(usuario);
        }

        [Fact]
        public void AddInfrastructure_RegistraLosTresCanalesDeNotificacionComoScoped()
        {
            var services = new ServiceCollection();

            services.AddInfrastructure(Config(ConfigBase));

            foreach (Type implementation in new[] { typeof(SignalRNotificationChannel), typeof(EmailNotificationChannel), typeof(SmsNotificationChannel) })
            {
                services.Should().Contain(sd => sd.ServiceType == typeof(INotificationChannel)
                    && sd.ImplementationType == implementation
                    && sd.Lifetime == ServiceLifetime.Scoped);
            }
        }

        [Fact]
        public void AddInfrastructure_ConContenedorReal_ResuelveElEnviadorDeCorreoYLaConfiguracionSmtp()
        {
            var valores = ConfigBase.Concat(new[]
            {
                ("SmtpSettings:Server", "smtp.gmail.com"),
                ("SmtpSettings:Port", "465"),
                ("SmtpSettings:SenderName", "Mi Aplicación"),
                ("SmtpSettings:SenderEmail", "tucorreo@gmail.com"),
                ("SmtpSettings:Password", "clave"),
                ("SmtpSettings:RecipientEmail", "destino@ejemplo.com")
            }).ToArray();
            using var provider = Construir(Config(valores));
            using var scope = provider.CreateScope();

            scope.ServiceProvider.GetRequiredService<IEmailSender>().Should().BeOfType<MailKitEmailSender>();
            scope.ServiceProvider.GetRequiredService<IEmailSender>().Should().BeSameAs(provider.GetRequiredService<IEmailSender>());
            var smtp = scope.ServiceProvider.GetRequiredService<IOptions<SmtpSettings>>().Value;
            smtp.Server.Should().Be("smtp.gmail.com");
            smtp.Port.Should().Be(465);
            smtp.SenderName.Should().Be("Mi Aplicación");
            smtp.SenderEmail.Should().Be("tucorreo@gmail.com");
            smtp.Password.Should().Be("clave");
            smtp.RecipientEmail.Should().Be("destino@ejemplo.com");
        }

        [Fact]
        public void AddInfrastructure_ConContenedorReal_ResuelveElEnviadorDeSmsDeBrevoYSuConfiguracion()
        {
            var valores = ConfigBase.Concat(new[]
            {
                ("BrevoSettingsSMS:ApiKey", "clave-brevo"),
                ("BrevoSettingsSMS:Sender", "Conexion360"),
                ("BrevoSettingsSMS:Conexion360Url", "https://conexion360.space"),
                ("BrevoSettingsSMS:DefaultCountryCode", "57")
            }).ToArray();
            using var provider = Construir(Config(valores));
            using var scope = provider.CreateScope();

            scope.ServiceProvider.GetRequiredService<ISmsSender>().Should().BeOfType<BrevoSmsSender>();
            var brevo = scope.ServiceProvider.GetRequiredService<IOptions<BrevoSmsSettings>>().Value;
            brevo.ApiKey.Should().Be("clave-brevo");
            brevo.Sender.Should().Be("Conexion360");
            brevo.Conexion360Url.Should().Be("https://conexion360.space");
            brevo.DefaultCountryCode.Should().Be("57");
            scope.ServiceProvider.GetServices<INotificationChannel>().OfType<SmsNotificationChannel>().Should().ContainSingle();
        }

        [Fact]
        public void AddInfrastructure_SinSeccionBrevo_ResuelveElCanalDeSmsConLaConfiguracionPorDefecto()
        {
            using var provider = Construir(Config(ConfigBase));
            using var scope = provider.CreateScope();

            scope.ServiceProvider.GetRequiredService<IOptions<BrevoSmsSettings>>().Value.ApiKey.Should().BeEmpty();
            scope.ServiceProvider.GetRequiredService<IOptions<BrevoSmsSettings>>().Value.BaseUrl.Should().Be(BrevoSmsSettings.DefaultBaseUrl);
            scope.ServiceProvider.GetServices<INotificationChannel>().OfType<SmsNotificationChannel>().Should().ContainSingle();
        }

        [Fact]
        public void AddInfrastructure_SinSeccionSmtp_ResuelveElCanalDeCorreoConLaConfiguracionPorDefecto()
        {
            using var provider = Construir(Config(ConfigBase));
            using var scope = provider.CreateScope();

            scope.ServiceProvider.GetRequiredService<IOptions<SmtpSettings>>().Value.Port.Should().Be(587);
            scope.ServiceProvider.GetServices<INotificationChannel>().OfType<EmailNotificationChannel>().Should().ContainSingle();
        }

        [Fact]
        public void AddInfrastructure_ConContenedorReal_ResuelveLosServiciosDeUnScope()
        {
            using var provider = Construir(Config(ConfigBase));
            using var scope = provider.CreateScope();

            scope.ServiceProvider.GetRequiredService<INotificationRepository>().Should().BeOfType<MongoNotificationRepository>();
            scope.ServiceProvider.GetRequiredService<INotificationIdGenerator>().Should().BeOfType<MongoNotificationIdGenerator>();
            scope.ServiceProvider.GetServices<INotificationChannel>().Select(c => c.GetType())
                .Should().BeEquivalentTo(new[] { typeof(SignalRNotificationChannel), typeof(EmailNotificationChannel), typeof(SmsNotificationChannel) });
            scope.ServiceProvider.GetServices<INotificationChannel>().Select(c => c.Channel)
                .Should().BeEquivalentTo(Enum.GetValues<NotificationChannelType>(), "hay un canal registrado por cada NotificationChannelType");
            scope.ServiceProvider.GetRequiredService<IUserIdProvider>().Should().BeOfType<CustomUserIdProvider>();
            scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>().Should().NotBeNull();
        }

        [Fact]
        public void AddInfrastructure_ConContenedorReal_ResuelveElProductorDeKafkaComoSingleton()
        {
            using var provider = Construir(Config(ConfigBase));

            var a = provider.GetRequiredService<IKafkaProducerService>();
            var b = provider.GetRequiredService<IKafkaProducerService>();

            a.Should().BeOfType<KafkaProducerService>();
            a.Should().BeSameAs(b);
        }

        [Fact]
        public void AddInfrastructure_ConContenedorReal_ResuelveLosServiciosHospedados()
        {
            using var provider = Construir(Config(ConfigBase));

            var hospedados = provider.GetServices<IHostedService>().ToList();

            hospedados.Should().Contain(h => h is KafkaConsumerHostedService);
            hospedados.Should().Contain(h => h is MongoIndexInitializer);
            hospedados.Should().Contain(h => h is BasicUserDataConsumerHostedService);
            hospedados.Should().Contain(h => h is BasicUserDataIndexInitializer);
            hospedados.OfType<IDisposable>().ToList().ForEach(h => h.Dispose());
        }
    }
}
