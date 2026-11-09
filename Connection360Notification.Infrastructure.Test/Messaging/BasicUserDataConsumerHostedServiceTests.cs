using System.Reflection;
using System.Text.Json;
using Confluent.Kafka;
using Connection360Notification.Application.DTOs;
using Connection360Notification.Application.Ports.Inbound;
using Connection360Notification.Domain.Settings;
using Connection360Notification.Infrastructure.Messaging;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Messaging
{
    public class BasicUserDataConsumerHostedServiceTests : IDisposable
    {
        private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

        private readonly Mock<IConsumer<String, String>> _consumerMock = new();
        private readonly Mock<ISyncBasicUsersUseCase> _useCaseMock = new();
        private readonly ServiceProvider _provider;
        private readonly CancellationTokenSource _cts = new();
        private readonly BasicUserDataConsumerHostedService _sut;

        public BasicUserDataConsumerHostedServiceTests()
        {
            var services = new ServiceCollection();
            services.AddScoped(_ => _useCaseMock.Object);
            _provider = services.BuildServiceProvider();

            _useCaseMock
                .Setup(u => u.ExecuteAsync(It.IsAny<BasicUsersSyncMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BasicUsersSyncResult(0, 0, false, false, 0, 0));

            _sut = Crear(new BasicUserSyncSettings { Topic = "usuarios-prueba", ErrorBackoffMilliseconds = 0 });
        }

        private BasicUserDataConsumerHostedService Crear(BasicUserSyncSettings sync)
        {
            var servicio = new BasicUserDataConsumerHostedService(
                Options.Create(new KafkaSettings { BootstrapServers = "localhost:9092", GroupId = "grupo-prueba", Topic = "topic-prueba" }),
                Options.Create(sync),
                _provider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<BasicUserDataConsumerHostedService>.Instance);

            // El constructor crea un consumidor real (no se conecta hasta consumir); se reemplaza por un mock.
            FieldInfo campo = typeof(BasicUserDataConsumerHostedService).GetField("_consumer", BindingFlags.NonPublic | BindingFlags.Instance)!;
            (campo.GetValue(servicio) as IDisposable)?.Dispose();
            campo.SetValue(servicio, _consumerMock.Object);
            return servicio;
        }

        public void Dispose()
        {
            _sut.Dispose();
            _provider.Dispose();
            _cts.Dispose();
            GC.SuppressFinalize(this);
        }

        private static ConsumeResult<String, String> Resultado(String? valor) => new()
        {
            Message = new Message<String, String> { Key = "k", Value = valor! }
        };

        private static BasicUsersSyncMessage Mensaje(Int32 batchIndex = 0, Int32 batchCount = 1, params String[] ids) => new()
        {
            SyncId = Guid.NewGuid(),
            BatchIndex = batchIndex,
            BatchCount = batchCount,
            TotalUsers = ids.Length,
            GeneratedAtUtc = new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc),
            Users = ids.Select(id => new BasicUserItem { Id = id, Document = "900", FullName = "Ana Perez", Phone = "+573001112233", Email = "a@b.com", RoleName = "ADMIN" }).ToList()
        };

        private static String Json(BasicUsersSyncMessage message) => JsonSerializer.Serialize(message, Web);

        /// <summary>Entrega los resultados en orden y, al agotarse, cancela el servicio lanzando OperationCanceledException.</summary>
        private void ConfigurarConsumo(params Func<ConsumeResult<String, String>?>[] pasos)
        {
            var cola = new Queue<Func<ConsumeResult<String, String>?>>(pasos);
            _consumerMock.Setup(c => c.Consume(It.IsAny<CancellationToken>())).Returns(() =>
            {
                if (cola.Count > 0) return cola.Dequeue()()!;
                _cts.Cancel();
                throw new OperationCanceledException();
            });
        }

        private async Task EjecutarHastaTerminar()
        {
            await _sut.StartAsync(_cts.Token);
            await _sut.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(20));
        }

        [Fact]
        public async Task ExecuteAsync_SeSuscribeAlTopicDeUsuariosYCierraElConsumidorAlTerminar()
        {
            ConfigurarConsumo();

            await EjecutarHastaTerminar();

            _consumerMock.Verify(c => c.Subscribe("usuarios-prueba"), Times.Once);
            _consumerMock.Verify(c => c.Close(), Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ExecuteAsync_SinTopicConfigurado_UsaNotificationUsers(String topic)
        {
            using BasicUserDataConsumerHostedService servicio = Crear(new BasicUserSyncSettings { Topic = topic, ErrorBackoffMilliseconds = 0 });
            ConfigurarConsumo();

            await servicio.StartAsync(_cts.Token);
            await servicio.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(20));

            _consumerMock.Verify(c => c.Subscribe("notification-users"), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ConMensajeValido_DelegaEnElCasoDeUsoConElLoteDeserializado()
        {
            BasicUsersSyncMessage? recibido = null;
            _useCaseMock
                .Setup(u => u.ExecuteAsync(It.IsAny<BasicUsersSyncMessage>(), It.IsAny<CancellationToken>()))
                .Callback<BasicUsersSyncMessage, CancellationToken>((m, _) => recibido = m)
                .ReturnsAsync(new BasicUsersSyncResult(2, 0, false, false, 0, 0));
            BasicUsersSyncMessage original = Mensaje(0, 2, "auth0|1", "auth0|2");
            ConfigurarConsumo(() => Resultado(Json(original)));

            await EjecutarHastaTerminar();

            recibido.Should().NotBeNull();
            recibido.Should().BeEquivalentTo(original);
            _useCaseMock.Verify(u => u.ExecuteAsync(It.IsAny<BasicUsersSyncMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_LeeElJsonEnCamelCaseQuePublicaElApi()
        {
            BasicUsersSyncMessage? recibido = null;
            _useCaseMock
                .Setup(u => u.ExecuteAsync(It.IsAny<BasicUsersSyncMessage>(), It.IsAny<CancellationToken>()))
                .Callback<BasicUsersSyncMessage, CancellationToken>((m, _) => recibido = m)
                .ReturnsAsync(new BasicUsersSyncResult(1, 0, true, true, 0, 1));
            Guid syncId = Guid.NewGuid();
            String json = "{\"syncId\":\"" + syncId + "\",\"batchIndex\":0,\"batchCount\":1,\"totalUsers\":1,\"generatedAtUtc\":\"2024-05-06T07:08:09Z\","
                + "\"users\":[{\"id\":\"auth0|1\",\"document\":\"900123\",\"fullName\":\"Ana Perez\",\"phone\":\"+573001112233\",\"email\":\"a@b.com\",\"roleName\":\"ADMIN\"}]}";
            ConfigurarConsumo(() => Resultado(json));

            await EjecutarHastaTerminar();

            recibido.Should().NotBeNull();
            recibido!.SyncId.Should().Be(syncId);
            recibido.TotalUsers.Should().Be(1);
            recibido.Users.Should().ContainSingle();
            recibido.Users[0].Id.Should().Be("auth0|1");
            recibido.Users[0].Document.Should().Be("900123");
            recibido.Users[0].FullName.Should().Be("Ana Perez");
            recibido.Users[0].Phone.Should().Be("+573001112233");
            recibido.Users[0].Email.Should().Be("a@b.com");
            recibido.Users[0].RoleName.Should().Be("ADMIN");
        }

        [Fact]
        public async Task ExecuteAsync_ConMensajeDeUnaVersionAnteriorSinRoleName_LoProcesaConElRolVacio()
        {
            BasicUsersSyncMessage? recibido = null;
            _useCaseMock
                .Setup(u => u.ExecuteAsync(It.IsAny<BasicUsersSyncMessage>(), It.IsAny<CancellationToken>()))
                .Callback<BasicUsersSyncMessage, CancellationToken>((m, _) => recibido = m)
                .ReturnsAsync(new BasicUsersSyncResult(1, 0, true, true, 0, 1));
            String json = "{\"syncId\":\"" + Guid.NewGuid() + "\",\"batchIndex\":0,\"batchCount\":1,\"totalUsers\":1,\"generatedAtUtc\":\"2024-05-06T07:08:09Z\","
                + "\"users\":[{\"id\":\"auth0|1\",\"document\":\"900123\",\"fullName\":\"Ana Perez\",\"phone\":\"+573001112233\",\"email\":\"a@b.com\"}]}";
            ConfigurarConsumo(() => Resultado(json));

            await EjecutarHastaTerminar();

            recibido.Should().NotBeNull();
            recibido!.Users.Should().ContainSingle().Which.RoleName.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteAsync_ProcesaVariosLotesEnOrden()
        {
            var indices = new List<Int32>();
            _useCaseMock
                .Setup(u => u.ExecuteAsync(It.IsAny<BasicUsersSyncMessage>(), It.IsAny<CancellationToken>()))
                .Callback<BasicUsersSyncMessage, CancellationToken>((m, _) => indices.Add(m.BatchIndex))
                .ReturnsAsync(new BasicUsersSyncResult(1, 0, false, false, 0, 0));
            ConfigurarConsumo(() => Resultado(Json(Mensaje(0, 3, "a"))), () => Resultado(Json(Mensaje(1, 3, "b"))), () => Resultado(Json(Mensaje(2, 3, "c"))));

            await EjecutarHastaTerminar();

            indices.Should().Equal(0, 1, 2);
        }

        [Fact]
        public async Task ExecuteAsync_UltimoLoteCompleto_Procesa()
        {
            _useCaseMock
                .Setup(u => u.ExecuteAsync(It.IsAny<BasicUsersSyncMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BasicUsersSyncResult(1, 0, true, true, 3, 1));
            ConfigurarConsumo(() => Resultado(Json(Mensaje(0, 1, "a"))));

            await EjecutarHastaTerminar();

            _useCaseMock.Verify(u => u.ExecuteAsync(It.IsAny<BasicUsersSyncMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_UltimoLoteIncompleto_NoFalla()
        {
            _useCaseMock
                .Setup(u => u.ExecuteAsync(It.IsAny<BasicUsersSyncMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BasicUsersSyncResult(1, 0, true, false, 0, 1));
            ConfigurarConsumo(() => Resultado(Json(Mensaje(1, 2, "a"))));

            Func<Task> act = EjecutarHastaTerminar;

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task ExecuteAsync_ConResultadoNulo_NoInvocaElCasoDeUso()
        {
            ConfigurarConsumo(() => null);

            await EjecutarHastaTerminar();

            _useCaseMock.Verify(u => u.ExecuteAsync(It.IsAny<BasicUsersSyncMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_ConMensajeSinValor_NoInvocaElCasoDeUso()
        {
            ConfigurarConsumo(() => Resultado(null), () => new ConsumeResult<String, String>());

            await EjecutarHastaTerminar();

            _useCaseMock.Verify(u => u.ExecuteAsync(It.IsAny<BasicUsersSyncMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_ConJsonNull_NoInvocaElCasoDeUso()
        {
            ConfigurarConsumo(() => Resultado("null"));

            await EjecutarHastaTerminar();

            _useCaseMock.Verify(u => u.ExecuteAsync(It.IsAny<BasicUsersSyncMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_ConJsonInvalido_SeRegistraYContinuaConElSiguienteMensaje()
        {
            var ids = new List<String>();
            _useCaseMock
                .Setup(u => u.ExecuteAsync(It.IsAny<BasicUsersSyncMessage>(), It.IsAny<CancellationToken>()))
                .Callback<BasicUsersSyncMessage, CancellationToken>((m, _) => ids.Add(m.Users[0].Id))
                .ReturnsAsync(new BasicUsersSyncResult(1, 0, false, false, 0, 0));
            ConfigurarConsumo(() => Resultado("{no es json"), () => Resultado(Json(Mensaje(0, 2, "valido"))));

            await EjecutarHastaTerminar();

            ids.Should().Equal("valido");
        }

        [Fact]
        public async Task ExecuteAsync_SiElCasoDeUsoFalla_SeguraYContinuaConElSiguienteMensaje()
        {
            Int32 llamadas = 0;
            _useCaseMock
                .Setup(u => u.ExecuteAsync(It.IsAny<BasicUsersSyncMessage>(), It.IsAny<CancellationToken>()))
                .Returns<BasicUsersSyncMessage, CancellationToken>((_, _) =>
                {
                    llamadas++;
                    if (llamadas == 1) throw new InvalidOperationException("mongo caído");
                    return Task.FromResult(new BasicUsersSyncResult(1, 0, false, false, 0, 0));
                });
            ConfigurarConsumo(() => Resultado(Json(Mensaje(0, 2, "a"))), () => Resultado(Json(Mensaje(1, 2, "b"))));

            await EjecutarHastaTerminar();

            llamadas.Should().Be(2);
            _consumerMock.Verify(c => c.Close(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_SiElConsumidorLanzaUnError_SeguraYReintenta()
        {
            var llamado = false;
            _useCaseMock
                .Setup(u => u.ExecuteAsync(It.IsAny<BasicUsersSyncMessage>(), It.IsAny<CancellationToken>()))
                .Callback(() => llamado = true)
                .ReturnsAsync(new BasicUsersSyncResult(1, 0, false, false, 0, 0));
            ConfigurarConsumo(() => throw new ConsumeException(new ConsumeResult<Byte[], Byte[]>(), new Error(ErrorCode.Local_Transport)), () => Resultado(Json(Mensaje(0, 2, "a"))));

            await EjecutarHastaTerminar();

            llamado.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_CadaMensajeSeProcesaEnUnScopeNuevo()
        {
            var scopesCreados = 0;
            var servicios = new ServiceCollection();
            servicios.AddScoped(_ =>
            {
                scopesCreados++;
                return _useCaseMock.Object;
            });
            using ServiceProvider proveedor = servicios.BuildServiceProvider();
            using var servicio = new BasicUserDataConsumerHostedService(
                Options.Create(new KafkaSettings { BootstrapServers = "localhost:9092" }),
                Options.Create(new BasicUserSyncSettings { ErrorBackoffMilliseconds = 0 }),
                proveedor.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<BasicUserDataConsumerHostedService>.Instance);
            FieldInfo campo = typeof(BasicUserDataConsumerHostedService).GetField("_consumer", BindingFlags.NonPublic | BindingFlags.Instance)!;
            (campo.GetValue(servicio) as IDisposable)?.Dispose();
            campo.SetValue(servicio, _consumerMock.Object);
            ConfigurarConsumo(() => Resultado(Json(Mensaje(0, 2, "a"))), () => Resultado(Json(Mensaje(1, 2, "b"))));

            await servicio.StartAsync(_cts.Token);
            await servicio.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(20));

            scopesCreados.Should().Be(2);
        }

        [Fact]
        public async Task ExecuteAsync_ConBackoffNegativo_UsaElValorPorDefectoSinFallar()
        {
            using BasicUserDataConsumerHostedService servicio = Crear(new BasicUserSyncSettings { ErrorBackoffMilliseconds = -5 });
            ConfigurarConsumo();

            await servicio.StartAsync(_cts.Token);
            await servicio.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(20));

            _consumerMock.Verify(c => c.Close(), Times.Once);
        }

        [Fact]
        public void Dispose_LiberaElConsumidor()
        {
            _sut.Dispose();

            _consumerMock.Verify(c => c.Dispose(), Times.AtLeastOnce);
        }

        [Fact]
        public void ResolveGroupId_ConGrupoPropio_LoUsaRecortado()
        {
            String grupo = BasicUserDataConsumerHostedService.ResolveGroupId(
                new KafkaSettings { GroupId = "notificaciones" }, new BasicUserSyncSettings { GroupId = "  mi-grupo  " });

            grupo.Should().Be("mi-grupo");
        }

        [Fact]
        public void ResolveGroupId_SinGrupoPropio_UsaElDeNotificacionesMasSufijo()
        {
            String grupo = BasicUserDataConsumerHostedService.ResolveGroupId(
                new KafkaSettings { GroupId = " notificaciones " }, new BasicUserSyncSettings());

            grupo.Should().Be("notificaciones-users");
        }

        [Fact]
        public void ResolveGroupId_SinNingunGrupo_UsaElValorPorDefecto()
        {
            String grupo = BasicUserDataConsumerHostedService.ResolveGroupId(new KafkaSettings(), new BasicUserSyncSettings());

            grupo.Should().Be(BasicUserSyncSettings.DefaultGroupId);
        }

        [Fact]
        public void ResolveGroupId_NuncaCoincideConElGrupoDeNotificaciones()
        {
            var kafka = new KafkaSettings { GroupId = "connection360" };

            BasicUserDataConsumerHostedService.ResolveGroupId(kafka, new BasicUserSyncSettings()).Should().NotBe(kafka.GroupId);
        }
    }
}
