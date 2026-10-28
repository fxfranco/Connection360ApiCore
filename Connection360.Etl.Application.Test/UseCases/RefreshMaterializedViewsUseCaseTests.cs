using Connection360.Etl.Application.Tests.Support;
using Connection360.Etl.Application.UseCases;
using Connection360.Etl.Domain.Enums;
using Connection360.Etl.Domain.Ports.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Connection360.Etl.Application.Tests.UseCases
{
    public class RefreshMaterializedViewsUseCaseTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IMaterializedViewRepository> _repository = new();
        private readonly ListLogger<RefreshMaterializedViewsUseCase> _logger = new();

        public RefreshMaterializedViewsUseCaseTests()
        {
            _unitOfWork.Setup(u => u.GetRepository<IMaterializedViewRepository>()).Returns(_repository.Object);
            _repository
                .Setup(r => r.RefreshConcurrentlyAsync(It.IsAny<MaterializedViewName>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        private RefreshMaterializedViewsUseCase Create(Boolean enabled = true) => new(_unitOfWork.Object, enabled, _logger);

        [Fact]
        public async Task ExecuteAsync_Habilitado_RefrescaLasTresVistasUnaVezCadaUna()
        {
            var result = await Create().ExecuteAsync();

            result.Success.Should().BeTrue();
            result.Skipped.Should().BeFalse();
            result.Views.Select(v => v.View).Should().BeEquivalentTo(new[]
            {
                MaterializedViewName.LogStatusTracking,
                MaterializedViewName.ApplicationDataSheetEntregados,
                MaterializedViewName.ApplicationDataSheetNoEntregados,
            });
            result.Views.Should().OnlyContain(v => v.Success && v.ErrorMessage == null);
            foreach (MaterializedViewName view in Enum.GetValues<MaterializedViewName>())
                _repository.Verify(r => r.RefreshConcurrentlyAsync(view, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_Habilitado_RefrescaUnaVistaALaVezEnElOrdenDelEnum()
        {
            var order = new List<MaterializedViewName>();
            Int32 concurrent = 0, maxConcurrent = 0;
            _repository
                .Setup(r => r.RefreshConcurrentlyAsync(It.IsAny<MaterializedViewName>(), It.IsAny<CancellationToken>()))
                .Returns(async (MaterializedViewName view, CancellationToken _) =>
                {
                    maxConcurrent = Math.Max(maxConcurrent, Interlocked.Increment(ref concurrent));
                    order.Add(view);
                    await Task.Delay(5, CancellationToken.None);
                    Interlocked.Decrement(ref concurrent);
                });

            await Create().ExecuteAsync();

            order.Should().Equal(Enum.GetValues<MaterializedViewName>());
            maxConcurrent.Should().Be(1, "el refresco es secuencial para limitar la carga sobre PostgreSQL");
        }

        [Fact]
        public async Task ExecuteAsync_Deshabilitado_NoRefrescaNadaNiResuelveElRepositorio()
        {
            var result = await Create(enabled: false).ExecuteAsync();

            result.Skipped.Should().BeTrue();
            result.Success.Should().BeTrue("omitir por configuración no es un error");
            result.Views.Should().BeEmpty();
            _unitOfWork.Verify(u => u.GetRepository<IMaterializedViewRepository>(), Times.Never);
            _repository.Verify(r => r.RefreshConcurrentlyAsync(It.IsAny<MaterializedViewName>(), It.IsAny<CancellationToken>()), Times.Never);
            _logger.Messages(LogLevel.Information).Should().ContainSingle().Which.Should().Contain("deshabilitada");
        }

        [Fact]
        public async Task ExecuteAsync_UnaVistaFalla_NoLanzaYContinuaConLasDemas()
        {
            var error = new InvalidOperationException("bloqueo de refresco");
            _repository
                .Setup(r => r.RefreshConcurrentlyAsync(MaterializedViewName.ApplicationDataSheetEntregados, It.IsAny<CancellationToken>()))
                .ThrowsAsync(error);

            var result = await Create().ExecuteAsync();

            result.Success.Should().BeFalse();
            result.Views.Should().HaveCount(3);
            var failed = result.Views.Should().ContainSingle(v => !v.Success).Subject;
            failed.View.Should().Be(MaterializedViewName.ApplicationDataSheetEntregados);
            failed.ErrorMessage.Should().Be("bloqueo de refresco");
            _repository.Verify(r => r.RefreshConcurrentlyAsync(MaterializedViewName.ApplicationDataSheetNoEntregados, It.IsAny<CancellationToken>()), Times.Once);
            _logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Error).Which.Exception.Should().BeSameAs(error);
        }

        [Fact]
        public async Task ExecuteAsync_TodasLasVistasFallan_ReportaTresFallos()
        {
            _repository
                .Setup(r => r.RefreshConcurrentlyAsync(It.IsAny<MaterializedViewName>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutException("tiempo agotado"));

            var result = await Create().ExecuteAsync();

            result.Success.Should().BeFalse();
            result.Views.Should().HaveCount(3).And.OnlyContain(v => !v.Success && v.ErrorMessage == "tiempo agotado");
        }

        [Fact]
        public async Task ExecuteAsync_NoSePuedeResolverElRepositorio_NoLanzaYMarcaTodasLasVistasComoFallidas()
        {
            _unitOfWork.Setup(u => u.GetRepository<IMaterializedViewRepository>()).Throws(new InvalidOperationException("sin repositorio"));

            var result = await Create().ExecuteAsync();

            result.Success.Should().BeFalse();
            result.Views.Should().HaveCount(3).And.OnlyContain(v => !v.Success && v.ErrorMessage == "sin repositorio");
            _logger.Entries.Should().Contain(e => e.Level == LogLevel.Error);
        }

        [Fact]
        public async Task ExecuteAsync_CancelacionPrevia_NoRefrescaYMarcaLasVistasComoFallidas()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var result = await Create().ExecuteAsync(cts.Token);

            result.Success.Should().BeFalse();
            result.Views.Should().HaveCount(3).And.OnlyContain(v => !v.Success && v.ErrorMessage!.Contains("Cancelado"));
            _repository.Verify(r => r.RefreshConcurrentlyAsync(It.IsAny<MaterializedViewName>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_CancelacionDuranteElRefresco_LasVistasRestantesQuedanFallidas()
        {
            using var cts = new CancellationTokenSource();
            _repository
                .Setup(r => r.RefreshConcurrentlyAsync(MaterializedViewName.LogStatusTracking, It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    cts.Cancel();
                    return Task.CompletedTask;
                });

            var result = await Create().ExecuteAsync(cts.Token);

            result.Views.Single(v => v.View == MaterializedViewName.LogStatusTracking).Success.Should().BeTrue();
            result.Views.Where(v => v.View != MaterializedViewName.LogStatusTracking).Should().OnlyContain(v => !v.Success);
            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task ExecuteAsync_ReenviaElTokenDeCancelacion()
        {
            using var cts = new CancellationTokenSource();

            await Create().ExecuteAsync(cts.Token);

            foreach (MaterializedViewName view in Enum.GetValues<MaterializedViewName>())
                _repository.Verify(r => r.RefreshConcurrentlyAsync(view, cts.Token), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_RegistraInicioYFinDeLaCorrida()
        {
            DateTime before = DateTime.UtcNow;

            var result = await Create().ExecuteAsync();

            result.StartedAtUtc.Should().BeOnOrAfter(before);
            result.FinishedAtUtc.Should().BeOnOrAfter(result.StartedAtUtc);
            result.Duration.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
        }

        [Fact]
        public async Task ExecuteAsync_Exitoso_RegistraUnMensajeInformativoPorVistaSinErrores()
        {
            await Create().ExecuteAsync();

            _logger.Messages(LogLevel.Information).Should().HaveCount(3);
            _logger.Entries.Should().NotContain(e => e.Level == LogLevel.Error);
        }
    }
}
