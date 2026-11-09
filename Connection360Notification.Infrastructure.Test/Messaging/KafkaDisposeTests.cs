using System.Reflection;
using Confluent.Kafka;
using Connection360Notification.Application.Ports.Inbound;
using Connection360Notification.Domain.Settings;
using Connection360Notification.Infrastructure.Messaging;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Messaging
{
    /// <summary>Liberación de los clientes nativos de Kafka y registro de errores del consumidor.</summary>
    public class KafkaDisposeTests
    {
        private static readonly IOptions<KafkaSettings> Settings =
            Options.Create(new KafkaSettings { BootstrapServers = "localhost:9092", GroupId = "g", Topic = "t" });

        private static void ReplaceField<T>(Object target, String field, T value)
        {
            FieldInfo info = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)!;
            (info.GetValue(target) as IDisposable)?.Dispose(); // cliente real creado por el constructor
            info.SetValue(target, value);
        }

        [Fact]
        public void KafkaProducerService_Dispose_VaciaLosPendientesYDesechaElProductor()
        {
            var producer = new Mock<IProducer<String, String>>();
            var sut = new KafkaProducerService(Settings);
            ReplaceField(sut, "_producer", producer.Object);

            sut.Dispose();

            producer.Verify(p => p.Flush(It.IsAny<TimeSpan>()), Times.Once);
            producer.Verify(p => p.Dispose(), Times.Once);
        }

        [Fact]
        public void KafkaProducerService_ImplementaIDisposable_ParaQueElContenedorLoLibere()
        {
            typeof(IDisposable).IsAssignableFrom(typeof(KafkaProducerService)).Should().BeTrue();
        }

        [Fact]
        public void KafkaConsumerHostedService_Dispose_DesechaElConsumidor()
        {
            var consumer = new Mock<IConsumer<String, String>>();
            var sut = new KafkaConsumerHostedService(Settings, new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
                Mock.Of<ILogger<KafkaConsumerHostedService>>());
            ReplaceField(sut, "_consumer", consumer.Object);

            sut.Dispose();

            consumer.Verify(c => c.Dispose(), Times.Once);
        }

        [Fact]
        public async Task KafkaConsumerHostedService_SiElProcesamientoFalla_RegistraElErrorEnElLog()
        {
            using var cts = new CancellationTokenSource();
            var consumer = new Mock<IConsumer<String, String>>();
            var llamadas = 0;
            consumer.Setup(c => c.Consume(It.IsAny<CancellationToken>())).Returns(() =>
            {
                if (++llamadas == 1) throw new ConsumeException(new ConsumeResult<Byte[], Byte[]>(), new Error(ErrorCode.Local_Fail));
                cts.Cancel();
                throw new OperationCanceledException();
            });
            var logger = new Mock<ILogger<KafkaConsumerHostedService>>();
            logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            using var provider = new ServiceCollection().BuildServiceProvider();
            var sut = new KafkaConsumerHostedService(Settings, provider.GetRequiredService<IServiceScopeFactory>(), logger.Object);
            ReplaceField(sut, "_consumer", consumer.Object);

            await sut.StartAsync(cts.Token);
            await sut.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(20));
            sut.Dispose();

            logger.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, String>>()), Times.Once);
        }
    }
}
