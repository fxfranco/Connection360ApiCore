using Connection360.Observability.Application.Initialization;
using Connection360.Observability.Application.Pipeline;
using Connection360.Observability.Domain.Ports;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Connection360.Observability.Application.Test.Initialization
{
    public class TelemetryStoreInitializationServiceTests
    {
        [Fact]
        public async Task StartAsync_EjecutaTodosLosInicializadores()
        {
            var a = new Mock<ITelemetryStoreInitializer>();
            var b = new Mock<ITelemetryStoreInitializer>();
            var service = new TelemetryStoreInitializationService(new[] { a.Object, b.Object }, NullLogger.Instance);

            await service.StartAsync(CancellationToken.None);
            await service.Completion;

            a.Verify(i => i.InitializeAsync(It.IsAny<CancellationToken>()), Times.Once);
            b.Verify(i => i.InitializeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task StartAsync_NoEsperaALaInicializacion()
        {
            var gate = new TaskCompletionSource();
            var slow = new Mock<ITelemetryStoreInitializer>();
            slow.Setup(i => i.InitializeAsync(It.IsAny<CancellationToken>())).Returns(gate.Task);
            var service = new TelemetryStoreInitializationService(new[] { slow.Object }, NullLogger.Instance);

            Task start = service.StartAsync(CancellationToken.None);

            start.IsCompletedSuccessfully.Should().BeTrue();
            service.Completion.IsCompleted.Should().BeFalse();
            gate.SetResult();
            await service.Completion;
        }

        [Fact]
        public async Task StartAsync_SiUnoFalla_ContinuaConLosDemasYNoLanza()
        {
            var broken = new Mock<ITelemetryStoreInitializer>();
            broken.Setup(i => i.InitializeAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("sin conexión"));
            var healthy = new Mock<ITelemetryStoreInitializer>();
            var service = new TelemetryStoreInitializationService(new[] { broken.Object, healthy.Object }, NullLogger.Instance);

            await service.StartAsync(CancellationToken.None);
            Func<Task> wait = () => service.Completion;

            await wait.Should().NotThrowAsync();
            healthy.Verify(i => i.InitializeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task StartAsync_SinInicializadores_NoHaceNada()
        {
            var service = new TelemetryStoreInitializationService(Array.Empty<ITelemetryStoreInitializer>(), NullLogger.Instance);

            await service.StartAsync(CancellationToken.None);

            service.Completion.IsCompletedSuccessfully.Should().BeTrue();
        }

        [Fact]
        public async Task StartAsync_LosInicializadoresCorrenSuprimiendoLaTelemetria()
        {
            Boolean? suppressed = null;
            var initializer = new Mock<ITelemetryStoreInitializer>();
            initializer.Setup(i => i.InitializeAsync(It.IsAny<CancellationToken>()))
                .Returns(() => { suppressed = TelemetrySuppression.IsSuppressed; return Task.CompletedTask; });
            var service = new TelemetryStoreInitializationService(new[] { initializer.Object }, NullLogger.Instance);

            await service.StartAsync(CancellationToken.None);
            await service.Completion;

            suppressed.Should().BeTrue();
        }

        [Fact]
        public async Task StopAsync_NoLanza()
        {
            var service = new TelemetryStoreInitializationService(Array.Empty<ITelemetryStoreInitializer>(), NullLogger.Instance);

            Func<Task> act = () => service.StopAsync(CancellationToken.None);

            await act.Should().NotThrowAsync();
        }
    }
}
