using Connection360.Domain.Dtos;
using Connection360.Domain.Ports.Persistence;
using Connection360.Infrastructure.Messaging;
using Connection360Notification.Domain.Settings;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Connection360.Infrastructure.Tests.Messaging
{
    /// <summary>
    /// Pruebas del ciclo del worker con IUnitOfWork/repositorio simulados. El productor de Kafka se
    /// construye pero nunca llega a conectarse: los escenarios con mensajes se detienen por
    /// cancelacion antes de que ProduceAsync pueda terminar. Intervalo de sondeo de 1 segundo.
    /// </summary>
    public class OutboxPublisherWorkerTests
    {
        private static readonly TimeSpan Guard = TimeSpan.FromSeconds(20);

        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IOutboxMessagesRepository> _repository = new();
        private readonly Mock<IOptionsMonitor<OutboxPublisherSettings>> _settings = new();
        private readonly ServiceProvider _provider;

        public OutboxPublisherWorkerTests()
        {
            _unitOfWork.Setup(u => u.GetRepository<IOutboxMessagesRepository>()).Returns(_repository.Object);
            _settings.SetupGet(s => s.CurrentValue).Returns(new OutboxPublisherSettings { PollingIntervalSeconds = 1 });

            var services = new ServiceCollection();
            services.AddSingleton(_unitOfWork.Object);
            _provider = services.BuildServiceProvider();
        }

        private OutboxPublisherWorker BuildWorker()
        {
            var kafka = Options.Create(new KafkaSettings { BootstrapServers = "localhost:9092", GroupId = "g", Topic = "outbox-topic" });
            return new OutboxPublisherWorker(_provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<OutboxPublisherWorker>.Instance, kafka, _settings.Object);
        }

        private static async Task RunUntil(OutboxPublisherWorker worker, Task signal)
        {
            await worker.StartAsync(CancellationToken.None);
            try
            {
                Task completed = await Task.WhenAny(signal, Task.Delay(Guard));
                completed.Should().BeSameAs(signal, "el ciclo del worker debe completarse dentro del tiempo limite");
            }
            finally
            {
                await worker.StopAsync(CancellationToken.None);
            }
        }

        [Fact]
        public void Dispose_DesechaElProductorDeKafka()
        {
            OutboxPublisherWorker worker = BuildWorker();
            var producer = new Mock<Confluent.Kafka.IProducer<String, String>>();
            var field = typeof(OutboxPublisherWorker).GetField("_producer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            (field.GetValue(worker) as IDisposable)?.Dispose();
            field.SetValue(worker, producer.Object);

            worker.Dispose();

            producer.Verify(p => p.Dispose(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_SinMensajesPendientes_AbreTransaccionYHaceCommit()
        {
            var committed = new TaskCompletionSource();
            _repository.Setup(r => r.GetListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<OutboxMessagesResultDto>());
            _unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Callback(() => committed.TrySetResult());
            using OutboxPublisherWorker worker = BuildWorker();

            await RunUntil(worker, committed.Task);

            _unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
            _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
            _repository.Verify(r => r.UpdateprocessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_SiLaConsultaFalla_HaceRollbackYNoCommit()
        {
            var rolledBack = new TaskCompletionSource();
            _repository.Setup(r => r.GetListAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("bd caida"));
            _unitOfWork.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Callback(() => rolledBack.TrySetResult());
            using OutboxPublisherWorker worker = BuildWorker();

            await RunUntil(worker, rolledBack.Task);

            _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_SiPublicarEnKafkaFalla_HaceRollbackYNoMarcaComoProcesado()
        {
            // El productor se reemplaza por un simulado que falla al publicar, de modo que la prueba no depende de si
            // hay (o no) un broker de Kafka disponible en localhost:9092 ni de temporizaciones.
            var rolledBack = new TaskCompletionSource();
            var producer = new Mock<Confluent.Kafka.IProducer<String, String>>();
            producer
                .Setup(p => p.ProduceAsync(It.IsAny<String>(), It.IsAny<Confluent.Kafka.Message<String, String>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Confluent.Kafka.KafkaException(new Confluent.Kafka.Error(Confluent.Kafka.ErrorCode.Local_Transport)));
            using OutboxPublisherWorker worker = BuildWorker();
            var field = typeof(OutboxPublisherWorker).GetField("_producer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            (field.GetValue(worker) as IDisposable)?.Dispose();
            field.SetValue(worker, producer.Object);
            _repository.Setup(r => r.GetListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OutboxMessagesResultDto> { new() { Id = Guid.NewGuid(), EventType = "E", Payload = "{}" } });
            _unitOfWork.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Callback(() => rolledBack.TrySetResult());

            await RunUntil(worker, rolledBack.Task);

            producer.Verify(p => p.ProduceAsync(It.IsAny<String>(), It.IsAny<Confluent.Kafka.Message<String, String>>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
            _repository.Verify(r => r.UpdateprocessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_SiElIntervaloConfiguradoCambia_ReasignaElPeriodoDelTimer()
        {
            // Primera lectura (timer inicial): 1 s; siguientes: 2 s -> se ejecuta la rama que reasigna timer.Period.
            var committed = new TaskCompletionSource();
            _settings.SetupSequence(s => s.CurrentValue)
                .Returns(new OutboxPublisherSettings { PollingIntervalSeconds = 1 })
                .Returns(new OutboxPublisherSettings { PollingIntervalSeconds = 2 })
                .Returns(new OutboxPublisherSettings { PollingIntervalSeconds = 2 });
            _repository.Setup(r => r.GetListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<OutboxMessagesResultDto>());
            _unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Callback(() => committed.TrySetResult());
            using OutboxPublisherWorker worker = BuildWorker();

            await RunUntil(worker, committed.Task);

            _settings.VerifyGet(s => s.CurrentValue, Times.AtLeast(2));
        }

        [Fact]
        public async Task ExecuteAsync_ConIntervaloInvalido_UsaElPorDefectoYNoEjecutaCiclosInmediatamente()
        {
            _settings.SetupGet(s => s.CurrentValue).Returns(new OutboxPublisherSettings { PollingIntervalSeconds = 0 });
            using OutboxPublisherWorker worker = BuildWorker();

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(200);
            await worker.StopAsync(CancellationToken.None);

            // El valor por defecto es 10 s, por lo que en 200 ms no ha habido ningun ciclo.
            _unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task StopAsync_AntesDelPrimerTick_TerminaSinEjecutarCiclos()
        {
            using OutboxPublisherWorker worker = BuildWorker();

            await worker.StartAsync(CancellationToken.None);
            Func<Task> act = () => worker.StopAsync(CancellationToken.None);

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public void Constructor_ConKafkaSettings_NoLanzaExcepcion()
        {
            Action act = () => BuildWorker().Dispose();

            act.Should().NotThrow();
        }
    }
}
