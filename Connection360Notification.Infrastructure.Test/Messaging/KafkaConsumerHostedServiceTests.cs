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
                () => Resultado("{\"ClientId\":\"\",\"Type\":0,\"Message\":\"x\"}"),
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
