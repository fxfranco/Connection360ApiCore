using System.Reflection;
using System.Text.Json;
using Confluent.Kafka;
using Connection360Notification.Application.Ports.Inbound;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Enums;
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
    public class KafkaConsumerHostedServiceTests : IDisposable
    {
        private static readonly DateTime Fecha = new(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc);

        private readonly Mock<IConsumer<String, String>> _consumerMock = new();
        private readonly Mock<IProcessIncomingNotificationUseCase> _useCaseMock = new();
        private readonly ServiceProvider _provider;
        private readonly CancellationTokenSource _cts = new();
        private readonly KafkaConsumerHostedService _sut;

        public KafkaConsumerHostedServiceTests()
        {
            var services = new ServiceCollection();
            services.AddScoped(_ => _useCaseMock.Object);
            _provider = services.BuildServiceProvider();

            _sut = new KafkaConsumerHostedService(
                Options.Create(new KafkaSettings { BootstrapServers = "localhost:9092", GroupId = "grupo-prueba", Topic = "topic-prueba" }),
                _provider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<KafkaConsumerHostedService>.Instance);

            // El constructor crea un consumidor real (no se conecta hasta consumir); se reemplaza por un mock.
            var campo = typeof(KafkaConsumerHostedService).GetField("_consumer", BindingFlags.NonPublic | BindingFlags.Instance)!;
            (campo.GetValue(_sut) as IDisposable)?.Dispose();
            campo.SetValue(_sut, _consumerMock.Object);
        }

        public void Dispose()
        {
            _sut.Dispose();
            _provider.Dispose();
            _cts.Dispose();
        }

        private static ConsumeResult<String, String> Resultado(String? valor) => new()
        {
            Message = new Message<String, String> { Key = "k", Value = valor! }
        };

        private static String Json(NotificationMessage n) => JsonSerializer.Serialize(n);

        private static NotificationMessage Notificacion(String id = "ID-1") =>
            new("CLI-1", NotificationType.Comment, "Mensaje", "HBL-1", "Titulo", Fecha, NotificationStatus.Unread, Fecha, 0, id);

        /// <summary>Entrega los resultados en orden y, al agotarse, cancela el servicio lanzando OperationCanceledException.</summary>
        private void ConfigurarConsumo(params Func<ConsumeResult<String, String>?>[] pasos)
        {
            var cola = new Queue<Func<ConsumeResult<String, String>?>>(pasos);
            _consumerMock.Setup(c => c.Consume(It.IsAny<CancellationToken>())).Returns(() =>
            {
                if (cola.Count > 0) return cola.Dequeue()();
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
        public async Task ExecuteAsync_SeSuscribeAlTopicConfiguradoYCierraElConsumidorAlTerminar()
        {
            ConfigurarConsumo();

            await EjecutarHastaTerminar();

            _consumerMock.Verify(c => c.Subscribe("topic-prueba"), Times.Once);
            _consumerMock.Verify(c => c.Close(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ConMensajeValido_DelegaEnElCasoDeUsoConLaNotificacionDeserializada()
        {
            NotificationMessage? recibida = null;
            _useCaseMock
                .Setup(u => u.ExecuteAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .Callback<NotificationMessage, CancellationToken>((n, _) => recibida = n)
                .Returns(Task.CompletedTask);
            var original = Notificacion("ID-77");
            ConfigurarConsumo(() => Resultado(Json(original)));

            await EjecutarHastaTerminar();

            recibida.Should().NotBeNull();
            recibida.Should().BeEquivalentTo(original);
            _useCaseMock.Verify(u => u.ExecuteAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ProcesaVariosMensajesEnOrden()
        {
            var ids = new List<String>();
            _useCaseMock
                .Setup(u => u.ExecuteAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .Callback<NotificationMessage, CancellationToken>((n, _) => ids.Add(n.Id))
                .Returns(Task.CompletedTask);
            ConfigurarConsumo(() => Resultado(Json(Notificacion("A"))), () => Resultado(Json(Notificacion("B"))));

            await EjecutarHastaTerminar();

            ids.Should().Equal("A", "B");
        }

        [Fact]
        public async Task ExecuteAsync_ConResultadoNulo_NoInvocaElCasoDeUso()
        {
            ConfigurarConsumo(() => null);

            await EjecutarHastaTerminar();

            _useCaseMock.Verify(u => u.ExecuteAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_ConMensajeSinValor_NoInvocaElCasoDeUso()
        {
            ConfigurarConsumo(() => Resultado(null), () => new ConsumeResult<String, String>());

            await EjecutarHastaTerminar();

            _useCaseMock.Verify(u => u.ExecuteAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_ConJsonLiteralNull_NoInvocaElCasoDeUso()
        {
            ConfigurarConsumo(() => Resultado("null"));

            await EjecutarHastaTerminar();

            _useCaseMock.Verify(u => u.ExecuteAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_ConJsonInvalido_ContinuaConElSiguienteMensaje()
        {
            var ids = new List<String>();
            _useCaseMock
                .Setup(u => u.ExecuteAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .Callback<NotificationMessage, CancellationToken>((n, _) => ids.Add(n.Id))
                .Returns(Task.CompletedTask);
            ConfigurarConsumo(() => Resultado("esto no es json"), () => Resultado(Json(Notificacion("OK"))));

            await EjecutarHastaTerminar();

            ids.Should().Equal("OK");
        }

        [Fact]
        public async Task ExecuteAsync_ConMensajeQueViolaElDominio_ContinuaConElSiguienteMensaje()
        {
            var ids = new List<String>();
            _useCaseMock
                .Setup(u => u.ExecuteAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .Callback<NotificationMessage, CancellationToken>((n, _) => ids.Add(n.Id))
                .Returns(Task.CompletedTask);
            ConfigurarConsumo(
                () => Resultado("{\"ClientId\":\"\",\"EventType\":0,\"Message\":\"x\"}"),
                () => Resultado(Json(Notificacion("OK"))));

            await EjecutarHastaTerminar();

            ids.Should().Equal("OK");
        }

        [Fact]
        public async Task ExecuteAsync_SiElCasoDeUsoFalla_LaExcepcionSeAbsorbeYSigueConsumiendo()
        {
            var llamadas = 0;
            _useCaseMock
                .Setup(u => u.ExecuteAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .Returns(() => ++llamadas == 1 ? Task.FromException(new InvalidOperationException("fallo")) : Task.CompletedTask);
            ConfigurarConsumo(() => Resultado(Json(Notificacion("A"))), () => Resultado(Json(Notificacion("B"))));

            await EjecutarHastaTerminar();

            llamadas.Should().Be(2);
            _consumerMock.Verify(c => c.Close(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_SiElConsumidorLanzaUnaExcepcionGenerica_ContinuaElCiclo()
        {
            var ids = new List<String>();
            _useCaseMock
                .Setup(u => u.ExecuteAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .Callback<NotificationMessage, CancellationToken>((n, _) => ids.Add(n.Id))
                .Returns(Task.CompletedTask);
            ConfigurarConsumo(
                () => throw new ConsumeException(new ConsumeResult<Byte[], Byte[]>(), new Error(ErrorCode.Local_Transport)),
                () => Resultado(Json(Notificacion("OK"))));

            await EjecutarHastaTerminar();

            ids.Should().Equal("OK");
        }

        [Fact]
        public async Task ExecuteAsync_ConTokenYaCancelado_NoConsumeYCierraElConsumidor()
        {
            _consumerMock.Setup(c => c.Consume(It.IsAny<CancellationToken>())).Throws(new InvalidOperationException("no debe consumir"));
            var metodo = typeof(KafkaConsumerHostedService).GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;

            await ((Task)metodo.Invoke(_sut, new Object[] { new CancellationToken(true) })!).WaitAsync(TimeSpan.FromSeconds(20));

            _consumerMock.Verify(c => c.Consume(It.IsAny<CancellationToken>()), Times.Never);
            _consumerMock.Verify(c => c.Close(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ConPayloadDelEtlConNotificationChannel_CapturaLosTresIndicadores()
        {
            NotificationMessage? recibida = null;
            _useCaseMock
                .Setup(u => u.ExecuteAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .Callback<NotificationMessage, CancellationToken>((n, _) => recibida = n)
                .Returns(Task.CompletedTask);
            // Forma exacta del Payload que escribe el ETL en outbox_messages (PascalCase).
            const String payload = "{\"ClientId\":\"900123\",\"EventType\":\"ChangeState\",\"DocumentNumber\":\"HBL-1\",\"Title\":\"Cambio\",\"Message\":\"Detalle\","
                + "\"MessageDate\":\"2024-05-06T07:08:09Z\",\"NotificationChannel\":{\"Application\":false,\"Email\":true,\"TextMessages\":true}}";
            ConfigurarConsumo(() => Resultado(payload));

            await EjecutarHastaTerminar();

            recibida.Should().NotBeNull();
            recibida!.ClientId.Should().Be("900123");
            recibida.EventType.Should().Be(NotificationType.ChangeState);
            recibida.NotificationChannel.Application.Should().BeFalse();
            recibida.NotificationChannel.Email.Should().BeTrue();
            recibida.NotificationChannel.TextMessages.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_ConLosNuevosCamposDelPayloadDelEtl_LosEnlazaSinErrores()
        {
            NotificationMessage? recibida = null;
            _useCaseMock
                .Setup(u => u.ExecuteAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .Callback<NotificationMessage, CancellationToken>((n, _) => recibida = n)
                .Returns(Task.CompletedTask);
            // Payload del ETL con los datos de contexto (Origen, Destino, Comentario, FechaComentario, EstadoAnterior, NuevoEstado).
            const String payload = "{\"ClientId\":\"900123\",\"EventType\":\"OtherInfo\",\"DocumentNumber\":\"HBL-1\",\"Title\":\"Actualización información de tu envío\","
                + "\"Message\":\"Se registró un cambio en la información general de tu envío\",\"MessageDate\":\"2024-05-06T07:08:09Z\","
                + "\"Origen\":\"Medellin\",\"Destino\":\"Miami\",\"Comentario\":\"Carga lista\",\"FechaComentario\":\"2024-05-05T00:00:00\","
                + "\"EstadoAnterior\":\"En transito\",\"NuevoEstado\":\"En puerto\","
                + "\"NotificationChannel\":{\"Application\":true,\"Email\":true,\"TextMessages\":false}}";
            ConfigurarConsumo(() => Resultado(payload));

            await EjecutarHastaTerminar();

            recibida.Should().NotBeNull();
            recibida!.EventType.Should().Be(NotificationType.OtherInfo);
            recibida.Origen.Should().Be("Medellin");
            recibida.Destino.Should().Be("Miami");
            recibida.Comentario.Should().Be("Carga lista");
            recibida.FechaComentario.Should().Be(new DateTime(2024, 5, 5));
            recibida.EstadoAnterior.Should().Be("En transito");
            recibida.NuevoEstado.Should().Be("En puerto");
        }

        [Theory]
        [InlineData("\"Comment\"", NotificationType.Comment)]
        [InlineData("\"ChangeState\"", NotificationType.ChangeState)]
        [InlineData("\"comment\"", NotificationType.Comment)]
        [InlineData("1", NotificationType.Comment)]
        [InlineData("0", NotificationType.ChangeState)]
        [InlineData("\"OtherInfo\"", NotificationType.OtherInfo)]
        [InlineData("2", NotificationType.OtherInfo)]
        public async Task ExecuteAsync_EnlazaElEventTypeDelPayloadComoTextoONumero(String valorJson, NotificationType esperado)
        {
            NotificationMessage? recibida = null;
            _useCaseMock
                .Setup(u => u.ExecuteAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .Callback<NotificationMessage, CancellationToken>((n, _) => recibida = n)
                .Returns(Task.CompletedTask);
            ConfigurarConsumo(() => Resultado($"{{\"ClientId\":\"900123\",\"EventType\":{valorJson},\"Message\":\"Detalle\"}}"));

            await EjecutarHastaTerminar();

            recibida.Should().NotBeNull();
            recibida!.EventType.Should().Be(esperado);
        }

        [Fact]
        public async Task ExecuteAsync_ConEventTypeDesconocido_DescartaElMensajeYContinua()
        {
            var ids = new List<String>();
            _useCaseMock
                .Setup(u => u.ExecuteAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .Callback<NotificationMessage, CancellationToken>((n, _) => ids.Add(n.Id))
                .Returns(Task.CompletedTask);
            ConfigurarConsumo(
                () => Resultado("{\"ClientId\":\"900123\",\"EventType\":\"NoExiste\",\"Message\":\"Detalle\"}"),
                () => Resultado(Json(Notificacion("OK"))));

            await EjecutarHastaTerminar();

            ids.Should().Equal("OK");
        }

        [Fact]
        public async Task ExecuteAsync_ConNotificationChannelEnCamelCase_TambienLoCaptura()
        {
            NotificationMessage? recibida = null;
            _useCaseMock
                .Setup(u => u.ExecuteAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .Callback<NotificationMessage, CancellationToken>((n, _) => recibida = n)
                .Returns(Task.CompletedTask);
            const String payload = "{\"clientId\":\"900123\",\"message\":\"Detalle\",\"notificationChannel\":{\"application\":true,\"email\":true,\"textMessages\":false}}";
            ConfigurarConsumo(() => Resultado(payload));

            await EjecutarHastaTerminar();

            recibida.Should().NotBeNull();
            recibida!.NotificationChannel.Application.Should().BeTrue();
            recibida.NotificationChannel.Email.Should().BeTrue();
            recibida.NotificationChannel.TextMessages.Should().BeFalse();
        }

        [Fact]
        public async Task ExecuteAsync_ConPayloadSinNotificationChannel_EntregaCanalesSinHabilitar()
        {
            NotificationMessage? recibida = null;
            _useCaseMock
                .Setup(u => u.ExecuteAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .Callback<NotificationMessage, CancellationToken>((n, _) => recibida = n)
                .Returns(Task.CompletedTask);
            ConfigurarConsumo(() => Resultado("{\"ClientId\":\"900123\",\"Message\":\"Detalle\"}"));

            await EjecutarHastaTerminar();

            recibida.Should().NotBeNull();
            recibida!.NotificationChannel.Should().NotBeNull();
            recibida.NotificationChannel.HasAnyEnabled.Should().BeFalse("sin configuración se aplicará el canal por defecto (aplicación)");
        }

        [Fact]
        public async Task ExecuteAsync_ConNotificationChannelEnElMensajeSerializado_ElCasoDeUsoRecibeLosCanales()
        {
            NotificationMessage? recibida = null;
            _useCaseMock
                .Setup(u => u.ExecuteAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .Callback<NotificationMessage, CancellationToken>((n, _) => recibida = n)
                .Returns(Task.CompletedTask);
            var original = new NotificationMessage("CLI-1", NotificationType.Comment, "Mensaje", "HBL-1", "Titulo", Fecha, NotificationStatus.Unread, Fecha, 0, "ID-1",
                new NotificationChannelPreferences { Application = true, TextMessages = true });
            ConfigurarConsumo(() => Resultado(Json(original)));

            await EjecutarHastaTerminar();

            recibida!.NotificationChannel.Should().BeEquivalentTo(original.NotificationChannel);
        }

        [Fact]
        public async Task StopAsync_CancelaElCicloDeConsumo()
        {
            var consumiendo = new TaskCompletionSource<Boolean>(TaskCreationOptions.RunContinuationsAsynchronously);
            _consumerMock.Setup(c => c.Consume(It.IsAny<CancellationToken>())).Returns<CancellationToken>(token =>
            {
                consumiendo.TrySetResult(true);
                token.WaitHandle.WaitOne(TimeSpan.FromSeconds(20));
                token.ThrowIfCancellationRequested();
                return null!;
            });

            await _sut.StartAsync(CancellationToken.None);
            await consumiendo.Task.WaitAsync(TimeSpan.FromSeconds(20));
            await _sut.StopAsync(CancellationToken.None);

            _sut.ExecuteTask!.IsCompleted.Should().BeTrue();
            _consumerMock.Verify(c => c.Close(), Times.Once);
        }
    }
}
