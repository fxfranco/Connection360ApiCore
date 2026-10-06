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
    public class PurgeEtlJobControlUseCaseTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IEtlJobControlRepository> _repository = new();
        private readonly ListLogger<PurgeEtlJobControlUseCase> _logger = new();

        public PurgeEtlJobControlUseCaseTests()
        {
            _unitOfWork.Setup(u => u.GetRepository<IEtlJobControlRepository>()).Returns(_repository.Object);
        }

        private PurgeEtlJobControlUseCase Create(Int32 days = 30) => new(_unitOfWork.Object, days, _logger);

        [Fact]
        public async Task ExecuteAsync_ConRegistrosViejos_DevuelveLaCantidadEliminada()
        {
            _repository.Setup(r => r.DeleteOlderThanAsync(It.IsAny<EtlJobName>(), It.IsAny<Int32>(), It.IsAny<CancellationToken>())).ReturnsAsync(12);

            Int32 deleted = await Create().ExecuteAsync();

            deleted.Should().Be(12);
        }

        [Fact]
        public async Task ExecuteAsync_DepuraUnicamenteElJobApplicationDataSheetConLosDiasConfigurados()
        {
            _repository.Setup(r => r.DeleteOlderThanAsync(It.IsAny<EtlJobName>(), It.IsAny<Int32>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

            await Create(days: 45).ExecuteAsync();

            _repository.Verify(r => r.DeleteOlderThanAsync(EtlJobName.ApplicationDataSheet, 45, It.IsAny<CancellationToken>()), Times.Once);
            _repository.Verify(r => r.DeleteOlderThanAsync(EtlJobName.LogStatusTracking, It.IsAny<Int32>(), It.IsAny<CancellationToken>()), Times.Never);
            _repository.Verify(r => r.DeleteOlderThanAsync(EtlJobName.ApplicationDataSheetMigration, It.IsAny<Int32>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_SinRegistrosViejos_DevuelveCeroYRegistraInformacion()
        {
            _repository.Setup(r => r.DeleteOlderThanAsync(It.IsAny<EtlJobName>(), It.IsAny<Int32>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

            Int32 deleted = await Create().ExecuteAsync();

            deleted.Should().Be(0);
            _logger.Messages(LogLevel.Information).Should().ContainSingle().Which.Should().Contain("application_data_sheet");
            _logger.Entries.Should().NotContain(e => e.Level == LogLevel.Error);
        }

        [Fact]
        public async Task ExecuteAsync_ReenviaElTokenDeCancelacion()
        {
            using var cts = new CancellationTokenSource();
            _repository.Setup(r => r.DeleteOlderThanAsync(It.IsAny<EtlJobName>(), It.IsAny<Int32>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);

            await Create().ExecuteAsync(cts.Token);

            _repository.Verify(r => r.DeleteOlderThanAsync(EtlJobName.ApplicationDataSheet, 30, cts.Token), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_FallaElRepositorio_NoLanzaDevuelveCeroYRegistraElError()
        {
            var error = new InvalidOperationException("bd caida");
            _repository.Setup(r => r.DeleteOlderThanAsync(It.IsAny<EtlJobName>(), It.IsAny<Int32>(), It.IsAny<CancellationToken>())).ThrowsAsync(error);

            Int32 deleted = await Create().ExecuteAsync();

            deleted.Should().Be(0);
            var entry = _logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Error).Subject;
            entry.Exception.Should().BeSameAs(error);
        }

        [Fact]
        public async Task ExecuteAsync_FallaLaResolucionDelRepositorio_DevuelveCero()
        {
            _unitOfWork.Setup(u => u.GetRepository<IEtlJobControlRepository>()).Throws(new InvalidOperationException("sin servicio"));

            Int32 deleted = await Create().ExecuteAsync();

            deleted.Should().Be(0);
            _logger.Messages(LogLevel.Error).Should().ContainSingle();
        }

        [Fact]
        public async Task ExecuteAsync_TokenCancelado_NoLanzaYDevuelveCero()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            _repository.Setup(r => r.DeleteOlderThanAsync(It.IsAny<EtlJobName>(), It.IsAny<Int32>(), It.IsAny<CancellationToken>()))
                .Returns((EtlJobName _, Int32 _, CancellationToken ct) => Task.FromCanceled<Int32>(ct));

            Int32 deleted = await Create().ExecuteAsync(cts.Token);

            deleted.Should().Be(0);
        }
    }
}
