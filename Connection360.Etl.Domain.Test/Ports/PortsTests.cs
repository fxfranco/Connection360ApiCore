using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Domain.Enums;
using Connection360.Etl.Domain.Ports.Persistence;
using FluentAssertions;
using Moq;
using Xunit;

namespace Connection360.Etl.Domain.Test.Ports
{
    /// <summary>
    /// Verifica que los contratos (puertos) del dominio sean simulables y mantengan su firma esperada.
    /// </summary>
    public class PortsTests
    {
        [Fact]
        public async Task IApplicationDataSheetRepository_Mock_RespetaFirmasYValoresPorDefectoDeCancelacion()
        {
            var repo = new Mock<IApplicationDataSheetRepository>();
            var snapshots = new Dictionary<String, ApplicationDataSheetChangeSnapshot> { ["H"] = new() { Id = 1 } };
            repo.Setup(r => r.UpsertBatchAsync(It.IsAny<IEnumerable<ApplicationDataSheet>>(), It.IsAny<CancellationToken>())).ReturnsAsync(2);
            repo.Setup(r => r.GetChangeSnapshotsAsync(It.IsAny<IEnumerable<String>>(), It.IsAny<CancellationToken>())).ReturnsAsync(snapshots);
            repo.Setup(r => r.GetIdsByDocumentAsync(It.IsAny<IEnumerable<String>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<String, Int64> { ["H"] = 7 });

            (await repo.Object.UpsertBatchAsync(new[] { new ApplicationDataSheet(), new ApplicationDataSheet() })).Should().Be(2);
            (await repo.Object.GetChangeSnapshotsAsync(new[] { "H" })).Should().ContainKey("H");
            (await repo.Object.GetIdsByDocumentAsync(new[] { "H" }))["H"].Should().Be(7);
        }

        [Fact]
        public async Task IEtlJobControlRepository_Mock_TodasLasOperacionesSonInvocables()
        {
            var repo = new Mock<IEtlJobControlRepository>();
            repo.Setup(r => r.HasCompletedRunAsync(EtlJobName.LogStatusTracking, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            repo.Setup(r => r.StartRunAsync(EtlJobName.ApplicationDataSheet, 50, It.IsAny<CancellationToken>())).ReturnsAsync(11L);
            repo.Setup(r => r.DeleteOlderThanAsync(EtlJobName.ApplicationDataSheet, 30, It.IsAny<CancellationToken>())).ReturnsAsync(3);

            (await repo.Object.HasCompletedRunAsync(EtlJobName.LogStatusTracking)).Should().BeTrue();
            (await repo.Object.StartRunAsync(EtlJobName.ApplicationDataSheet, 50)).Should().Be(11);
            await repo.Object.RegisterPageProgressAsync(11, 2, 50);
            await repo.Object.CompleteRunAsync(11);
            await repo.Object.FailRunAsync(11);
            (await repo.Object.DeleteOlderThanAsync(EtlJobName.ApplicationDataSheet, 30)).Should().Be(3);

            repo.Verify(r => r.RegisterPageProgressAsync(11, 2, 50, It.IsAny<CancellationToken>()), Times.Once);
            repo.Verify(r => r.CompleteRunAsync(11, It.IsAny<CancellationToken>()), Times.Once);
            repo.Verify(r => r.FailRunAsync(11, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ILogStatusTrackingRepositoryYOutbox_Mock_InsertBatchRetornaCantidad()
        {
            var logs = new Mock<ILogStatusTrackingRepository>();
            var outbox = new Mock<IOutboxMessageRepository>();
            logs.Setup(r => r.InsertBatchAsync(It.IsAny<IEnumerable<LogStatusTracking>>(), It.IsAny<CancellationToken>())).ReturnsAsync(4);
            outbox.Setup(r => r.InsertBatchAsync(It.IsAny<IEnumerable<OutboxMessage>>(), It.IsAny<CancellationToken>())).ReturnsAsync(5);

            (await logs.Object.InsertBatchAsync(Array.Empty<LogStatusTracking>())).Should().Be(4);
            (await outbox.Object.InsertBatchAsync(Array.Empty<OutboxMessage>())).Should().Be(5);
        }

        [Fact]
        public async Task IUnitOfWork_Mock_ResuelveRepositorioYControlaTransaccion()
        {
            var logs = new Mock<ILogStatusTrackingRepository>();
            var uow = new Mock<IUnitOfWork>();
            uow.Setup(u => u.GetRepository<ILogStatusTrackingRepository>()).Returns(logs.Object);

            uow.Object.GetRepository<ILogStatusTrackingRepository>().Should().BeSameAs(logs.Object);
            await uow.Object.BeginTransactionAsync();
            await uow.Object.CommitAsync();
            await uow.Object.RollbackAsync();
            await uow.Object.DisposeAsync();

            uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
            uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            uow.Verify(u => u.DisposeAsync(), Times.Once);
        }

        [Fact]
        public void IUnitOfWork_HeredaDeIAsyncDisposable()
        {
            typeof(IAsyncDisposable).IsAssignableFrom(typeof(IUnitOfWork)).Should().BeTrue();
        }
    }
}
