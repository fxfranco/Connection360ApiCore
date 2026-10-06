using Connection360.Etl.Application.UseCases;
using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Domain.Enums;
using Connection360.Etl.Domain.Interfaces;
using Connection360.Etl.Domain.Ports.Persistence;
using FluentAssertions;
using Moq;
using System.Reflection;
using Xunit;

namespace Connection360.Etl.Application.Tests.UseCases
{
    /// <summary>
    /// EtlTransactionHelper y EtlJobControlTracker son internos del ensamblado de producción (sin
    /// InternalsVisibleTo); sus ramas inalcanzables por los casos de uso públicos se ejercitan por reflexión.
    /// </summary>
    public class InternalHelpersTests
    {
        private static readonly Assembly ApplicationAssembly = typeof(RunEtlProcessUseCase).Assembly;

        private static Type TrackerType => ApplicationAssembly.GetType("Connection360.Etl.Application.UseCases.EtlJobControlTracker", throwOnError: true)!;
        private static Type HelperType => ApplicationAssembly.GetType("Connection360.Etl.Application.UseCases.EtlTransactionHelper", throwOnError: true)!;

        private static object CreateTracker(IEtlJobControlRepository repository, EtlJobName job)
            => Activator.CreateInstance(TrackerType, repository, job)!;

        private static async Task InvokeAsync(object target, String method, params Object?[] args)
        {
            var task = (Task)TrackerType.GetMethod(method)!.Invoke(target, args)!;
            await task;
        }

        private static async Task<T> InvokeTransactionHelperAsync<T>(IUnitOfWork unitOfWork, Func<IUnitOfWork, Task<T>> action, CancellationToken ct = default)
        {
            MethodInfo method = HelperType.GetMethod("RunInOwnTransactionAsync")!.MakeGenericMethod(typeof(T));
            try
            {
                return await (Task<T>)method.Invoke(null, new Object?[] { unitOfWork, action, ct })!;
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                throw ex.InnerException;
            }
        }

        // ------------------------------------------------------------ EtlJobControlTracker

        [Fact]
        public async Task Tracker_RegisterPageProgressSinStart_LanzaInvalidOperationException()
        {
            var repo = new Mock<IEtlJobControlRepository>();
            object tracker = CreateTracker(repo.Object, EtlJobName.ApplicationDataSheet);

            Func<Task> act = async () =>
            {
                try { await InvokeAsync(tracker, "RegisterPageProgressAsync", 1, 1, CancellationToken.None); }
                catch (TargetInvocationException ex) { throw ex.InnerException!; }
            };

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*StartAsync*");
            repo.Verify(r => r.RegisterPageProgressAsync(It.IsAny<Int64>(), It.IsAny<Int32>(), It.IsAny<Int32>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Tracker_CompleteSinStart_LanzaInvalidOperationException()
        {
            var repo = new Mock<IEtlJobControlRepository>();
            object tracker = CreateTracker(repo.Object, EtlJobName.LogStatusTracking);

            Func<Task> act = async () =>
            {
                try { await InvokeAsync(tracker, "CompleteAsync", CancellationToken.None); }
                catch (TargetInvocationException ex) { throw ex.InnerException!; }
            };

            await act.Should().ThrowAsync<InvalidOperationException>();
            repo.Verify(r => r.CompleteRunAsync(It.IsAny<Int64>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Tracker_FailSinStart_NoHaceNada()
        {
            var repo = new Mock<IEtlJobControlRepository>();
            object tracker = CreateTracker(repo.Object, EtlJobName.LogStatusTracking);

            await InvokeAsync(tracker, "FailAsync", CancellationToken.None);

            repo.Verify(r => r.FailRunAsync(It.IsAny<Int64>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Tracker_ConStart_PasaElIdRecordadoEnProgresoCompleteYFail()
        {
            var repo = new Mock<IEtlJobControlRepository>();
            repo.Setup(r => r.StartRunAsync(EtlJobName.ApplicationDataSheet, 50, It.IsAny<CancellationToken>())).ReturnsAsync(42);
            object tracker = CreateTracker(repo.Object, EtlJobName.ApplicationDataSheet);

            await InvokeAsync(tracker, "StartAsync", (Int32?)50, CancellationToken.None);
            await InvokeAsync(tracker, "RegisterPageProgressAsync", 3, 20, CancellationToken.None);
            await InvokeAsync(tracker, "CompleteAsync", CancellationToken.None);
            await InvokeAsync(tracker, "FailAsync", CancellationToken.None);

            repo.Verify(r => r.RegisterPageProgressAsync(42, 3, 20, It.IsAny<CancellationToken>()), Times.Once);
            repo.Verify(r => r.CompleteRunAsync(42, It.IsAny<CancellationToken>()), Times.Once);
            repo.Verify(r => r.FailRunAsync(42, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Tracker_HasCompletedRun_ConsultaElRepositorioConElNombreDelJob()
        {
            var repo = new Mock<IEtlJobControlRepository>();
            repo.Setup(r => r.HasCompletedRunAsync(EtlJobName.ApplicationDataSheetMigration, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            object tracker = CreateTracker(repo.Object, EtlJobName.ApplicationDataSheetMigration);

            var task = (Task<Boolean>)TrackerType.GetMethod("HasCompletedRunAsync")!.Invoke(tracker, new Object?[] { CancellationToken.None })!;

            (await task).Should().BeTrue();
        }

        // ------------------------------------------------------------ EtlTransactionHelper

        [Fact]
        public async Task TransactionHelper_AccionExitosa_HaceBeginCommitYDevuelveElValor()
        {
            var calls = new List<String>();
            var uow = new Mock<IUnitOfWork>();
            uow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Callback(() => calls.Add("Begin")).Returns(Task.CompletedTask);
            uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Callback(() => calls.Add("Commit")).Returns(Task.CompletedTask);
            uow.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Callback(() => calls.Add("Rollback")).Returns(Task.CompletedTask);

            Int32 value = await InvokeTransactionHelperAsync(uow.Object, u =>
            {
                calls.Add("Action");
                u.Should().BeSameAs(uow.Object);
                return Task.FromResult(99);
            });

            value.Should().Be(99);
            calls.Should().Equal("Begin", "Action", "Commit");
        }

        [Fact]
        public async Task TransactionHelper_AccionLanza_HaceRollbackYRelanzaLaMismaExcepcion()
        {
            var calls = new List<String>();
            var uow = new Mock<IUnitOfWork>();
            uow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Callback(() => calls.Add("Begin")).Returns(Task.CompletedTask);
            uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Callback(() => calls.Add("Commit")).Returns(Task.CompletedTask);
            uow.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Callback(() => calls.Add("Rollback")).Returns(Task.CompletedTask);
            var error = new InvalidOperationException("falla");

            Func<Task> act = () => InvokeTransactionHelperAsync<Int32>(uow.Object, _ => throw error);

            (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(error);
            calls.Should().Equal("Begin", "Rollback");
        }

        [Fact]
        public async Task TransactionHelper_FallaElCommit_HaceRollback()
        {
            var uow = new Mock<IUnitOfWork>();
            uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new TimeoutException("commit"));

            Func<Task> act = () => InvokeTransactionHelperAsync(uow.Object, _ => Task.FromResult(1));

            await act.Should().ThrowAsync<TimeoutException>();
            uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task TransactionHelper_ReenviaElTokenAlUnitOfWork()
        {
            using var cts = new CancellationTokenSource();
            var uow = new Mock<IUnitOfWork>();

            await InvokeTransactionHelperAsync(uow.Object, _ => Task.FromResult(1), cts.Token);

            uow.Verify(u => u.BeginTransactionAsync(cts.Token), Times.Once);
            uow.Verify(u => u.CommitAsync(cts.Token), Times.Once);
        }

        // ------------------------------------------------------------ EtlChangeNotifier

        private static Task InvokeNotifier(IReadOnlyList<ApplicationDataSheetChange>? changes, Mock<ILogStatusTrackingRepository> logRepo, Mock<IOutboxMessageRepository> outbox)
        {
            Type type = ApplicationAssembly.GetType("Connection360.Etl.Application.UseCases.EtlChangeNotifier", throwOnError: true)!;
            var catalog = new Mock<IEtlChangeMessageCatalog>();
            object notifier = Activator.CreateInstance(type, logRepo.Object, outbox.Object, catalog.Object, "SYS")!;
            return (Task)type.GetMethod("NotifyChangesAsync")!.Invoke(notifier, new Object?[] { changes, CancellationToken.None })!;
        }

        [Fact]
        public async Task Notifier_ListaNula_NoInsertaNada()
        {
            var logRepo = new Mock<ILogStatusTrackingRepository>();
            var outbox = new Mock<IOutboxMessageRepository>();

            await InvokeNotifier(null, logRepo, outbox);

            logRepo.VerifyNoOtherCalls();
            outbox.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Notifier_ListaVacia_NoInsertaNada()
        {
            var logRepo = new Mock<ILogStatusTrackingRepository>();
            var outbox = new Mock<IOutboxMessageRepository>();

            await InvokeNotifier(new List<ApplicationDataSheetChange>(), logRepo, outbox);

            logRepo.VerifyNoOtherCalls();
            outbox.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Notifier_CambioSinEstadoNiComentario_NoInsertaNada()
        {
            var logRepo = new Mock<ILogStatusTrackingRepository>();
            var outbox = new Mock<IOutboxMessageRepository>();

            await InvokeNotifier(new List<ApplicationDataSheetChange> { new() { DocumentoTransporteHbl = "H" } }, logRepo, outbox);

            logRepo.VerifyNoOtherCalls();
            outbox.VerifyNoOtherCalls();
        }
    }
}
