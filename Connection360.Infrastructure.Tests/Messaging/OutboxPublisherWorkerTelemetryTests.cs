using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Connection360.Domain.Dtos;
using Connection360.Domain.Ports.Persistence;
using Connection360.Infrastructure.Messaging;
using Connection360.Observability.Domain.Telemetry;
using Connection360Notification.Domain.Settings;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Connection360.Infrastructure.Tests.Messaging
{
    /// <summary>
    /// Telemetría del publicador del outbox: un span "outbox.publish" (tipo Producer) por ciclo y el contador
    /// "outbox.publish.cycles" por resultado.
    /// </summary>
    [Collection("TelemetriaOutbox")]
    public class OutboxPublisherWorkerTelemetryTests : IDisposable
    {
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IOutboxMessagesRepository> _repository = new();
        private readonly ServiceProvider _provider;
        private readonly ConcurrentQueue<Activity> _spans = new();
        private readonly ConcurrentQueue<Boolean?> _cycles = new();
        private readonly ActivityListener _activityListener;
        private readonly MeterListener _meterListener;

        public OutboxPublisherWorkerTelemetryTests()
        {
            _unitOfWork.Setup(u => u.GetRepository<IOutboxMessagesRepository>()).Returns(_repository.Object);
            var services = new ServiceCollection();
            services.AddSingleton(_unitOfWork.Object);
            _provider = services.BuildServiceProvider();

            _activityListener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == Connection360Telemetry.Name,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = a => { if (a.OperationName == "outbox.publish") _spans.Enqueue(a); },
            };
            ActivitySource.AddActivityListener(_activityListener);

            _meterListener = new MeterListener
            {
                InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name == Connection360Telemetry.Name && instrument.Name == "outbox.publish.cycles")
                        listener.EnableMeasurementEvents(instrument);
                },
            };
            _meterListener.SetMeasurementEventCallback<Int64>((_, _, tags, _) =>
            {
                Boolean? success = null;
                foreach (var tag in tags) if (tag.Key == "success") success = tag.Value as Boolean?;
                _cycles.Enqueue(success);
            });
            _meterListener.Start();
        }

        public void Dispose()
        {
            _activityListener.Dispose();
            _meterListener.Dispose();
            _provider.Dispose();
        }

        private OutboxPublisherWorker BuildWorker()
        {
            var settings = new Mock<IOptionsMonitor<OutboxPublisherSettings>>();
            settings.SetupGet(s => s.CurrentValue).Returns(new OutboxPublisherSettings { PollingIntervalSeconds = 1 });
            var kafka = Options.Create(new KafkaSettings { BootstrapServers = "localhost:9092", GroupId = "g", Topic = "outbox-topic" });
            return new OutboxPublisherWorker(_provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<OutboxPublisherWorker>.Instance, kafka, settings.Object);
        }

        private static async Task RunOneCycle(OutboxPublisherWorker worker, Task signal)
        {
            await worker.StartAsync(CancellationToken.None);
            try
            {
                (await Task.WhenAny(signal, Task.Delay(TimeSpan.FromSeconds(20)))).Should().BeSameAs(signal);
                await Task.Delay(100); // deja que el ciclo cierre su span y registre la métrica
            }
            finally
            {
                await worker.StopAsync(CancellationToken.None);
            }
        }

        [Fact]
        public async Task CicloExitoso_GeneraUnSpanProducerConElConteoDeMensajesYCuentaUnExito()
        {
            var committed = new TaskCompletionSource();
            _repository.Setup(r => r.GetListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<OutboxMessagesResultDto>());
            _unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Callback(() => committed.TrySetResult());
            using OutboxPublisherWorker worker = BuildWorker();

            await RunOneCycle(worker, committed.Task);

            Activity span = _spans.First();
            span.Kind.Should().Be(ActivityKind.Producer);
            span.GetTagItem("outbox.messages").Should().Be(0);
            span.Status.Should().Be(ActivityStatusCode.Unset);
            _cycles.Should().Contain(true).And.NotContain(false);
        }

        [Fact]
        public async Task CicloConFallo_MarcaElSpanEnErrorConLaExcepcionYCuentaUnFallo()
        {
            var rolledBack = new TaskCompletionSource();
            _repository.Setup(r => r.GetListAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("bd caida"));
            _unitOfWork.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Callback(() => rolledBack.TrySetResult());
            using OutboxPublisherWorker worker = BuildWorker();

            await RunOneCycle(worker, rolledBack.Task);

            Activity span = _spans.First();
            span.Status.Should().Be(ActivityStatusCode.Error);
            span.StatusDescription.Should().Be("bd caida");
            span.Events.Should().Contain(e => e.Name == "exception");
            _cycles.Should().Contain(false).And.NotContain(true);
        }
    }
}
