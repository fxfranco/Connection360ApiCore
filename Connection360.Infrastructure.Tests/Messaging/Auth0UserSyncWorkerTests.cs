using Confluent.Kafka;
using Connection360.Domain.Dtos;
using Connection360.Domain.Interfaces;
using Connection360.Infrastructure.Messaging;
using Connection360Notification.Domain.Settings;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Connection360.Infrastructure.Tests.Messaging
{
    /// <summary>
    /// Pruebas del ciclo del <see cref="Auth0UserSyncWorker"/> con IAuth0UserService y el productor de Kafka
    /// simulados (no hay red). La sincronización inicial corre al arrancar; la periodicidad mínima es de un
    /// minuto, por eso el tick periódico se prueba solo a través de la lectura del intervalo.
    /// </summary>
    public class Auth0UserSyncWorkerTests
    {
        private static readonly TimeSpan Guard = TimeSpan.FromSeconds(20);
        private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

        private readonly Mock<IAuth0UserService> _auth0 = new();
        private readonly Mock<IProducer<String, String>> _producer = new();
        private readonly Mock<IOptionsMonitor<Auth0UserSyncSettings>> _settings = new();
        private readonly List<(String Topic, Message<String, String> Message)> _published = new();
        private readonly ServiceProvider _provider;
        private TaskCompletionSource _expectedPublished = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private Int32 _expectedCount = 1;

        public Auth0UserSyncWorkerTests()
        {
            _settings.SetupGet(s => s.CurrentValue).Returns(new Auth0UserSyncSettings { StartupRetryDelaySeconds = 0 });

            _producer
                .Setup(p => p.ProduceAsync(It.IsAny<String>(), It.IsAny<Message<String, String>>(), It.IsAny<CancellationToken>()))
                .Callback<String, Message<String, String>, CancellationToken>((topic, message, _) =>
                {
                    lock (_published)
                    {
                        _published.Add((topic, message));
                        if (_published.Count >= _expectedCount)
                        {
                            _expectedPublished.TrySetResult();
                        }
                    }
                })
                .ReturnsAsync(new DeliveryResult<String, String>());

            var services = new ServiceCollection();
            services.AddSingleton(_auth0.Object);
            _provider = services.BuildServiceProvider();
        }

        private Auth0UserSyncWorker BuildWorker()
        {
            var kafka = Options.Create(new KafkaSettings { BootstrapServers = "localhost:9092", GroupId = "g", Topic = "outbox-topic" });
            var worker = new Auth0UserSyncWorker(_provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<Auth0UserSyncWorker>.Instance, kafka, _settings.Object);

            // El constructor crea un productor real (no se conecta hasta publicar); se reemplaza por el simulado.
            FieldInfo field = typeof(Auth0UserSyncWorker).GetField("_producer", BindingFlags.NonPublic | BindingFlags.Instance)!;
            (field.GetValue(worker) as IDisposable)?.Dispose();
            field.SetValue(worker, _producer.Object);
            return worker;
        }

        private void ExpectPublished(Int32 count)
        {
            _expectedCount = count;
            _expectedPublished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private static List<Auth0UserBasicDto> Users(Int32 count) =>
            Enumerable.Range(1, count).Select(i => new Auth0UserBasicDto { Id = $"auth0|{i}", Document = $"900{i}", FullName = $"Usuario {i}", Phone = $"30000000{i:00}", Email = $"u{i}@correo.com", RoleName = i % 2 == 1 ? "ADMIN" : "CLIENT" }).ToList();

        private static async Task RunUntil(Auth0UserSyncWorker worker, Task signal)
        {
            await worker.StartAsync(CancellationToken.None);
            try
            {
                Task completed = await Task.WhenAny(signal, Task.Delay(Guard));
                completed.Should().BeSameAs(signal, "la sincronización debe completarse dentro del tiempo límite");
            }
            finally
            {
                await worker.StopAsync(CancellationToken.None);
            }
        }

        private static async Task RunBriefly(Auth0UserSyncWorker worker, TimeSpan duration)
        {
            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(duration);
            await worker.StopAsync(CancellationToken.None);
        }

        private static Auth0UsersSyncMessage Parse(Message<String, String> message) => JsonSerializer.Deserialize<Auth0UsersSyncMessage>(message.Value, Web)!;

        [Fact]
        public async Task ExecuteAsync_AlArrancar_SincronizaSinEsperarElIntervalo()
        {
            _auth0.Setup(s => s.GetBasicUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Users(3));
            using Auth0UserSyncWorker worker = BuildWorker();

            await RunUntil(worker, _expectedPublished.Task);

            _auth0.Verify(s => s.GetBasicUsersAsync(It.IsAny<CancellationToken>()), Times.Once);
            _published.Should().ContainSingle();
        }

        [Fact]
        public async Task ExecuteAsync_PublicaEnElTopicoPorDefectoConJsonEnCamelCase()
        {
            _auth0.Setup(s => s.GetBasicUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Users(2));
            using Auth0UserSyncWorker worker = BuildWorker();

            await RunUntil(worker, _expectedPublished.Task);

            (String topic, Message<String, String> message) = _published.Single();
            topic.Should().Be("notification-users");
            message.Value.Should().Contain("\"syncId\"").And.Contain("\"batchIndex\":0").And.Contain("\"document\":\"9001\"").And.Contain("\"fullName\":\"Usuario 1\"").And.Contain("\"phone\":\"3000000001\"").And.Contain("\"email\":\"u1@correo.com\"").And.Contain("\"roleName\":\"ADMIN\"");
            message.Value.Should().NotContain("identificacion");
        }

        [Fact]
        public async Task ExecuteAsync_PublicaElNombreDelRolDeCadaUsuario()
        {
            _auth0.Setup(s => s.GetBasicUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Users(3));
            using Auth0UserSyncWorker worker = BuildWorker();

            await RunUntil(worker, _expectedPublished.Task);

            Auth0UsersSyncMessage message = Parse(_published.Single().Message);
            message.Users.Select(u => (u.Id, u.RoleName)).Should().Equal(("auth0|1", "ADMIN"), ("auth0|2", "CLIENT"), ("auth0|3", "ADMIN"));
        }

        [Fact]
        public async Task ExecuteAsync_ConUsuarioSinRol_PublicaElRolVacioSinOmitirElCampo()
        {
            _auth0.Setup(s => s.GetBasicUsersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Auth0UserBasicDto> { new() { Id = "auth0|1", Document = "900", FullName = "Ana", Phone = "300", Email = "a@b.com" } });
            using Auth0UserSyncWorker worker = BuildWorker();

            await RunUntil(worker, _expectedPublished.Task);

            Message<String, String> message = _published.Single().Message;
            message.Value.Should().Contain("\"roleName\":\"\"");
            Parse(message).Users.Single().RoleName.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteAsync_DivideLosUsuariosEnLotesQueComparteLaMismaSincronizacionYClave()
        {
            _settings.SetupGet(s => s.CurrentValue).Returns(new Auth0UserSyncSettings { BatchSize = 2, Topic = "mi-topico", StartupRetryDelaySeconds = 0 });
            _auth0.Setup(s => s.GetBasicUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Users(5));
            ExpectPublished(3);
            using Auth0UserSyncWorker worker = BuildWorker();

            await RunUntil(worker, _expectedPublished.Task);

            List<Auth0UsersSyncMessage> messages = _published.Select(p => Parse(p.Message)).ToList();
            _published.Should().HaveCount(3).And.OnlyContain(p => p.Topic == "mi-topico");
            messages.Select(m => m.BatchIndex).Should().Equal(0, 1, 2);
            messages.Should().OnlyContain(m => m.BatchCount == 3 && m.TotalUsers == 5);
            messages.Select(m => m.Users.Count).Should().Equal(2, 2, 1);
            messages.Select(m => m.SyncId).Distinct().Should().ContainSingle();
            _published.Select(p => p.Message.Key).Distinct().Should().ContainSingle().Which.Should().Be(messages[0].SyncId.ToString());
            messages.SelectMany(m => m.Users).Select(u => u.Id).Should().Equal(Users(5).Select(u => u.Id));
        }

        [Fact]
        public async Task ExecuteAsync_ConTopicoVacioYLoteInvalido_UsaLosValoresPorDefecto()
        {
            _settings.SetupGet(s => s.CurrentValue).Returns(new Auth0UserSyncSettings { Topic = "  ", BatchSize = 0, StartupRetryDelaySeconds = 0 });
            _auth0.Setup(s => s.GetBasicUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Users(3));
            using Auth0UserSyncWorker worker = BuildWorker();

            await RunUntil(worker, _expectedPublished.Task);

            _published.Should().ContainSingle().Which.Topic.Should().Be("notification-users");
            Parse(_published.Single().Message).Users.Should().HaveCount(3);
        }

        [Fact]
        public async Task ExecuteAsync_ConSincronizacionDeshabilitada_NoConsultaAuth0NiPublica()
        {
            _settings.SetupGet(s => s.CurrentValue).Returns(new Auth0UserSyncSettings { Enabled = false });
            using Auth0UserSyncWorker worker = BuildWorker();

            await RunBriefly(worker, TimeSpan.FromMilliseconds(500));

            _auth0.Verify(s => s.GetBasicUsersAsync(It.IsAny<CancellationToken>()), Times.Never);
            _producer.Verify(p => p.ProduceAsync(It.IsAny<String>(), It.IsAny<Message<String, String>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_SiAuth0NoDevuelveUsuarios_NoPublicaParaNoBorrarLoYaSincronizado()
        {
            var consulted = new TaskCompletionSource();
            _auth0.Setup(s => s.GetBasicUsersAsync(It.IsAny<CancellationToken>()))
                .Callback(() => consulted.TrySetResult())
                .ReturnsAsync(new List<Auth0UserBasicDto>());
            using Auth0UserSyncWorker worker = BuildWorker();

            await RunUntil(worker, consulted.Task);
            await Task.Delay(200);

            _producer.Verify(p => p.ProduceAsync(It.IsAny<String>(), It.IsAny<Message<String, String>>(), It.IsAny<CancellationToken>()), Times.Never);
            _auth0.Verify(s => s.GetBasicUsersAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_SiLaSincronizacionInicialFalla_ReintentaYTerminaPublicando()
        {
            _auth0.SetupSequence(s => s.GetBasicUsersAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("auth0 caído"))
                .ReturnsAsync(Users(2));
            using Auth0UserSyncWorker worker = BuildWorker();

            await RunUntil(worker, _expectedPublished.Task);

            _auth0.Verify(s => s.GetBasicUsersAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
            _published.Should().ContainSingle();
        }

        [Fact]
        public async Task ExecuteAsync_SiTodosLosIntentosIniciales_Fallan_NoTumbaElWorkerYSeIntentaTresVeces()
        {
            var thirdAttempt = new TaskCompletionSource();
            Int32 calls = 0;
            _auth0.Setup(s => s.GetBasicUsersAsync(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    if (Interlocked.Increment(ref calls) >= 3) thirdAttempt.TrySetResult();
                    return Task.FromException<IList<Auth0UserBasicDto>>(new InvalidOperationException("auth0 caído"));
                });
            using Auth0UserSyncWorker worker = BuildWorker();

            await RunUntil(worker, thirdAttempt.Task);
            await Task.Delay(200);

            calls.Should().Be(3);
            worker.ExecuteTask!.IsFaulted.Should().BeFalse();
            _producer.Verify(p => p.ProduceAsync(It.IsAny<String>(), It.IsAny<Message<String, String>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_SiKafkaFalla_ElCicloSeTrataComoErrorYSeReintenta()
        {
            var secondAttempt = new TaskCompletionSource();
            Int32 calls = 0;
            _auth0.Setup(s => s.GetBasicUsersAsync(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    if (Interlocked.Increment(ref calls) >= 2) secondAttempt.TrySetResult();
                    return Task.FromResult<IList<Auth0UserBasicDto>>(Users(1));
                });
            _producer
                .Setup(p => p.ProduceAsync(It.IsAny<String>(), It.IsAny<Message<String, String>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new KafkaException(new Error(ErrorCode.Local_Transport)));
            using Auth0UserSyncWorker worker = BuildWorker();

            await RunUntil(worker, secondAttempt.Task);

            calls.Should().BeGreaterThanOrEqualTo(2);
        }

        [Theory]
        [InlineData(15, 15)]
        [InlineData(1, 1)]
        [InlineData(0, 60)]
        [InlineData(-5, 60)]
        public void GetPollingInterval_UsaLaPeriodicidadConfiguradaOLaPorDefectoSiEsInvalida(Int32 configured, Int32 expectedMinutes)
        {
            _settings.SetupGet(s => s.CurrentValue).Returns(new Auth0UserSyncSettings { PollingIntervalMinutes = configured });
            using Auth0UserSyncWorker worker = BuildWorker();
            MethodInfo method = typeof(Auth0UserSyncWorker).GetMethod("GetPollingInterval", BindingFlags.NonPublic | BindingFlags.Instance)!;

            var interval = (TimeSpan)method.Invoke(worker, null)!;

            interval.Should().Be(TimeSpan.FromMinutes(expectedMinutes));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(5, 5)]
        [InlineData(-1, 30)]
        public void GetStartupRetryDelay_UsaLaEsperaConfiguradaOLaPorDefectoSiEsNegativa(Int32 configured, Int32 expectedSeconds)
        {
            _settings.SetupGet(s => s.CurrentValue).Returns(new Auth0UserSyncSettings { StartupRetryDelaySeconds = configured });
            using Auth0UserSyncWorker worker = BuildWorker();
            MethodInfo method = typeof(Auth0UserSyncWorker).GetMethod("GetStartupRetryDelay", BindingFlags.NonPublic | BindingFlags.Instance)!;

            var delay = (TimeSpan)method.Invoke(worker, null)!;

            delay.Should().Be(TimeSpan.FromSeconds(expectedSeconds));
        }

        [Fact]
        public void Dispose_DesechaElProductorDeKafka()
        {
            Auth0UserSyncWorker worker = BuildWorker();

            worker.Dispose();

            _producer.Verify(p => p.Dispose(), Times.Once);
        }
    }
}
