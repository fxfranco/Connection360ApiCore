using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Text.Json;
using Confluent.Kafka;
using Connection360.Observability.Domain.Telemetry;
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
    /// <summary>
    /// Telemetría del consumidor de Kafka: un span "notification.consume" (tipo Consumer) por mensaje y el contador
    /// "notifications.kafka.consumed" por resultado. Los oyentes de ActivitySource/Meter son globales al proceso y otras
    /// clases de prueba (que también consumen mensajes) corren en paralelo, por eso solo se toma lo que pertenece a
    /// esta prueba: el span del topic propio y las mediciones hechas dentro de ese span.
    /// </summary>
    public class KafkaConsumerTelemetryTests : IDisposable
    {
        private static readonly DateTime Fecha = new(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc);

        private readonly Mock<IConsumer<String, String>> _consumerMock = new();
        private readonly Mock<IProcessIncomingNotificationUseCase> _useCaseMock = new();
        private readonly ServiceProvider _provider;
        private readonly CancellationTokenSource _cts = new();
        private readonly KafkaConsumerHostedService _sut;
        private readonly ConcurrentQueue<Activity> _spans = new();
        private readonly ConcurrentQueue<(Int64 Value, Boolean? Success)> _consumed = new();
        private readonly ActivityListener _activityListener;
        private readonly MeterListener _meterListener;

        public KafkaConsumerTelemetryTests()
        {
            var services = new ServiceCollection();
            services.AddScoped(_ => _useCaseMock.Object);
            _provider = services.BuildServiceProvider();
            _sut = new KafkaConsumerHostedService(
                Options.Create(new KafkaSettings { BootstrapServers = "localhost:9092", GroupId = "g", Topic = Topic }),
                _provider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<KafkaConsumerHostedService>.Instance);
            var campo = typeof(KafkaConsumerHostedService).GetField("_consumer", BindingFlags.NonPublic | BindingFlags.Instance)!;
            (campo.GetValue(_sut) as IDisposable)?.Dispose();
            campo.SetValue(_sut, _consumerMock.Object);

            _activityListener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == Connection360Telemetry.Name,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = a => { if (EsDeEstaPrueba(a)) _spans.Enqueue(a); },
            };
            ActivitySource.AddActivityListener(_activityListener);

            _meterListener = new MeterListener
            {
                InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name == Connection360Telemetry.Name && instrument.Name == "notifications.kafka.consumed")
                        listener.EnableMeasurementEvents(instrument);
                },
            };
            _meterListener.SetMeasurementEventCallback<Int64>((_, value, tags, _) =>
            {
                // El contador se incrementa dentro del span del mensaje: si no es el de esta prueba, se ignora.
                if (!EsDeEstaPrueba(Activity.Current)) return;
                Boolean? success = null;
                foreach (var tag in tags) if (tag.Key == "success") success = tag.Value as Boolean?;
                _consumed.Enqueue((value, success));
            });
            _meterListener.Start();
        }

        private const String Topic = "topic-telemetria";

        private static Boolean EsDeEstaPrueba(Activity? activity)
            => activity is { OperationName: "notification.consume" } && Equals(activity.GetTagItem("messaging.destination.name"), Topic);

        public void Dispose()
        {
            _activityListener.Dispose();
            _meterListener.Dispose();
            _sut.Dispose();
            _provider.Dispose();
            _cts.Dispose();
        }

        private static ConsumeResult<String, String> Resultado(String valor) => new() { Message = new Message<String, String> { Key = "k", Value = valor } };

        private static String Json() => JsonSerializer.Serialize(
            new NotificationMessage("CLI-1", NotificationType.Comment, "Mensaje", "HBL-1", "Titulo", Fecha, NotificationStatus.Unread, Fecha, 0, "ID-1"));

        private async Task EjecutarConUnMensaje()
        {
            Boolean entregado = false;
            _consumerMock.Setup(c => c.Consume(It.IsAny<CancellationToken>())).Returns(() =>
            {
                if (!entregado) { entregado = true; return Resultado(Json()); }
                _cts.Cancel();
                throw new OperationCanceledException();
            });
            await _sut.StartAsync(_cts.Token);
            await _sut.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(20));
        }

        [Fact]
        public async Task MensajeProcesado_GeneraUnSpanConsumerYCuentaUnExito()
        {
            _useCaseMock.Setup(u => u.ExecuteAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            await EjecutarConUnMensaje();

            Activity span = _spans.Should().ContainSingle().Subject;
            span.Kind.Should().Be(ActivityKind.Consumer);
            span.GetTagItem("messaging.system").Should().Be("kafka");
            span.GetTagItem("messaging.destination.name").Should().Be(Topic);
            span.Status.Should().Be(ActivityStatusCode.Unset);
            _consumed.Should().ContainSingle().Which.Should().Be((1L, (Boolean?)true));
        }

        [Fact]
        public async Task MensajeQueFalla_MarcaElSpanEnErrorConLaExcepcionYCuentaUnFallo()
        {
            _useCaseMock.Setup(u => u.ExecuteAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("no se pudo guardar"));

            await EjecutarConUnMensaje();

            Activity span = _spans.Should().ContainSingle().Subject;
            span.Status.Should().Be(ActivityStatusCode.Error);
            span.StatusDescription.Should().Be("no se pudo guardar");
            span.Events.Should().ContainSingle(e => e.Name == "exception");
            _consumed.Should().ContainSingle().Which.Should().Be((1L, (Boolean?)false));
        }
    }
}
