using System.Reflection;
using System.Text.Json;
using Confluent.Kafka;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Enums;
using Connection360Notification.Domain.Settings;
using Connection360Notification.Infrastructure.Messaging;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Messaging
{
    public class KafkaProducerServiceTests
    {
        private static readonly DateTime Fecha = new(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc);

        /// <summary>
        /// Crea el servicio (el constructor construye un productor real de Confluent, que no se conecta
        /// hasta enviar) y reemplaza el productor interno por un mock para no tocar la red.
        /// </summary>
        private static (KafkaProducerService Sut, Mock<IProducer<String, String>> Producer) Crear(String topic = "notificaciones")
        {
            var sut = new KafkaProducerService(Options.Create(new KafkaSettings { BootstrapServers = "localhost:9092", Topic = topic }));
            var campo = typeof(KafkaProducerService).GetField("_producer", BindingFlags.NonPublic | BindingFlags.Instance)!;
            (campo.GetValue(sut) as IDisposable)?.Dispose();
            var mock = new Mock<IProducer<String, String>>();
            campo.SetValue(sut, mock.Object);
            return (sut, mock);
        }

        private static NotificationMessage Notificacion() =>
            new("CLI-1", NotificationType.Comment, "Mensaje", "HBL-1", "Titulo", Fecha, NotificationStatus.Unread, Fecha, 3, "ID-3");

        [Fact]
        public void Constructor_ConConfiguracionValida_CreaElServicio()
        {
            Action act = () => _ = new KafkaProducerService(Options.Create(new KafkaSettings { BootstrapServers = "localhost:9092", Topic = "t" }));

            act.Should().NotThrow();
        }

        [Fact]
        public async Task ProduceNotificationAsync_PublicaEnElTopicConfiguradoUsandoElIdComoClave()
        {
            var (sut, producer) = Crear("mi-topic");
            String? topic = null;
            Message<String, String>? mensaje = null;
            producer
                .Setup(p => p.ProduceAsync(It.IsAny<String>(), It.IsAny<Message<String, String>>(), It.IsAny<CancellationToken>()))
                .Callback<String, Message<String, String>, CancellationToken>((t, m, _) => { topic = t; mensaje = m; })
                .ReturnsAsync(new DeliveryResult<String, String>());

            await sut.ProduceNotificationAsync(Notificacion(), CancellationToken.None);

            topic.Should().Be("mi-topic");
            mensaje!.Key.Should().Be("ID-3");
        }

        [Fact]
        public async Task ProduceNotificationAsync_SerializaLaNotificacionComoJsonDeserializable()
        {
            var (sut, producer) = Crear();
            Message<String, String>? mensaje = null;
            producer
                .Setup(p => p.ProduceAsync(It.IsAny<String>(), It.IsAny<Message<String, String>>(), It.IsAny<CancellationToken>()))
                .Callback<String, Message<String, String>, CancellationToken>((_, m, _) => mensaje = m)
                .ReturnsAsync(new DeliveryResult<String, String>());
            var original = Notificacion();

            await sut.ProduceNotificationAsync(original, CancellationToken.None);

            var copia = JsonSerializer.Deserialize<NotificationMessage>(mensaje!.Value);
            copia.Should().BeEquivalentTo(original);
        }

        [Fact]
        public async Task ProduceNotificationAsync_PropagaElTokenDeCancelacion()
        {
            var (sut, producer) = Crear();
            using var cts = new CancellationTokenSource();
            producer
                .Setup(p => p.ProduceAsync(It.IsAny<String>(), It.IsAny<Message<String, String>>(), cts.Token))
                .ReturnsAsync(new DeliveryResult<String, String>());

            await sut.ProduceNotificationAsync(Notificacion(), cts.Token);

            producer.Verify(p => p.ProduceAsync(It.IsAny<String>(), It.IsAny<Message<String, String>>(), cts.Token), Times.Once);
        }

        [Fact]
        public async Task ProduceNotificationAsync_SiElProductorFalla_PropagaLaExcepcion()
        {
            var (sut, producer) = Crear();
            producer
                .Setup(p => p.ProduceAsync(It.IsAny<String>(), It.IsAny<Message<String, String>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ProduceException<String, String>(new Error(ErrorCode.Local_Transport), new DeliveryResult<String, String>()));

            Func<Task> act = () => sut.ProduceNotificationAsync(Notificacion(), CancellationToken.None);

            await act.Should().ThrowAsync<ProduceException<String, String>>();
        }

        [Fact]
        public async Task ProduceNotificationAsync_ConTokenCancelado_PropagaOperationCanceledException()
        {
            var (sut, producer) = Crear();
            producer
                .Setup(p => p.ProduceAsync(It.IsAny<String>(), It.IsAny<Message<String, String>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            Func<Task> act = () => sut.ProduceNotificationAsync(Notificacion(), new CancellationToken(true));

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}
