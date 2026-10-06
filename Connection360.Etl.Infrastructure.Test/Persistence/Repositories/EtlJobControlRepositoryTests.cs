using Connection360.Etl.Domain.Enums;
using Connection360.Etl.Infrastructure.Persistence.Repositories;
using Connection360.Etl.Infrastructure.Tests.TestHelpers;
using FluentAssertions;
using System.Data;
using Xunit;

namespace Connection360.Etl.Infrastructure.Tests.Persistence.Repositories
{
    public class EtlJobControlRepositoryTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task HasCompletedRunAsync_DevuelveElValorEscalarDeLaConsulta(Boolean exists)
        {
            await using var scope = new FakeSessionScope();
            scope.Connection.ScalarResult = _ => exists;
            var sut = new EtlJobControlRepository(scope.Session);

            Boolean result = await sut.HasCompletedRunAsync(EtlJobName.LogStatusTracking);

            result.Should().Be(exists);
            FakeExecution execution = scope.Connection.Executions.Should().ContainSingle().Subject;
            execution.Kind.Should().Be("Scalar");
            execution.CommandText.Should().Contain("connection360write.etl_job_control").And.Contain("EXISTS");
            execution.Parameters["JobName"].Should().Be("log_status_tracking");
            execution.Parameters["Status"].Should().Be("COMPLETED");
        }

        [Fact]
        public async Task HasCompletedRunAsync_ConexionCerrada_LaAbreAntesDeConsultar()
        {
            await using var scope = new FakeSessionScope(ConnectionState.Closed);
            scope.Connection.ScalarResult = _ => true;
            var sut = new EtlJobControlRepository(scope.Session);

            await sut.HasCompletedRunAsync(EtlJobName.ApplicationDataSheet);

            scope.Connection.OpenCalls.Should().Be(1);
        }

        [Fact]
        public async Task HasCompletedRunAsync_UsaLaTransaccionActivaDeLaSesion()
        {
            await using var scope = new FakeSessionScope();
            scope.Connection.ScalarResult = _ => false;
            var transaction = new FakeDbTransaction(scope.Connection, IsolationLevel.ReadCommitted);
            scope.Session.Transaction = transaction;
            var sut = new EtlJobControlRepository(scope.Session);

            await sut.HasCompletedRunAsync(EtlJobName.ApplicationDataSheet);

            scope.Connection.Executions.Single().Transaction.Should().BeSameAs(transaction);
        }

        [Theory]
        [InlineData(EtlJobName.ApplicationDataSheet, "application_data_sheet", 500)]
        [InlineData(EtlJobName.LogStatusTracking, "log_status_tracking", null)]
        [InlineData(EtlJobName.ApplicationDataSheetMigration, "application_data_sheet_migration", 10)]
        public async Task StartRunAsync_InsertaElRegistroEnProcessingYDevuelveElIdGenerado(EtlJobName job, String dbName, Int32? pageSize)
        {
            await using var scope = new FakeSessionScope();
            scope.Connection.ScalarResult = _ => 77L;
            var sut = new EtlJobControlRepository(scope.Session);
            DateTime before = DateTime.UtcNow;

            Int64 id = await sut.StartRunAsync(job, pageSize);

            id.Should().Be(77);
            FakeExecution execution = scope.Connection.Executions.Single();
            execution.CommandText.Should().Contain("INSERT INTO connection360write.etl_job_control").And.Contain("RETURNING id");
            execution.Parameters["JobName"].Should().Be(dbName);
            execution.Parameters["Status"].Should().Be("PROCESSING");
            execution.Parameters["PageSize"].Should().Be(pageSize);
            ((DateTime)execution.Parameters["UpdatedAt"]!).Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
        }

        [Fact]
        public async Task RegisterPageProgressAsync_ActualizaPaginaYAcumulaTotal()
        {
            await using var scope = new FakeSessionScope();
            var sut = new EtlJobControlRepository(scope.Session);

            await sut.RegisterPageProgressAsync(jobControlId: 9, lastProcessedPage: 4, recordsProcessedInPage: 250);

            FakeExecution execution = scope.Connection.Executions.Single();
            execution.Kind.Should().Be("NonQuery");
            execution.CommandText.Should().Contain("UPDATE connection360write.etl_job_control").And.Contain("COALESCE(total_records_processed, 0)");
            execution.Parameters["Id"].Should().Be(9L);
            execution.Parameters["LastProcessedPage"].Should().Be(4);
            execution.Parameters["RecordsProcessedInPage"].Should().Be(250);
            execution.Parameters.Should().ContainKey("UpdatedAt");
        }

        [Fact]
        public async Task CompleteRunAsync_MarcaElRegistroComoCompleted()
        {
            await using var scope = new FakeSessionScope();
            var sut = new EtlJobControlRepository(scope.Session);

            await sut.CompleteRunAsync(15);

            FakeExecution execution = scope.Connection.Executions.Single();
            execution.CommandText.Should().Contain("UPDATE connection360write.etl_job_control").And.Contain("SET status");
            execution.Parameters["Id"].Should().Be(15L);
            execution.Parameters["Status"].Should().Be("COMPLETED");
        }

        [Fact]
        public async Task FailRunAsync_MarcaElRegistroComoFailed()
        {
            await using var scope = new FakeSessionScope();
            var sut = new EtlJobControlRepository(scope.Session);

            await sut.FailRunAsync(16);

            FakeExecution execution = scope.Connection.Executions.Single();
            execution.Parameters["Id"].Should().Be(16L);
            execution.Parameters["Status"].Should().Be("FAILED");
        }

        [Fact]
        public async Task DeleteOlderThanAsync_EliminaPorJobYAntiguedadYDevuelveLasFilasAfectadas()
        {
            await using var scope = new FakeSessionScope();
            scope.Connection.NonQueryResult = _ => 8;
            var sut = new EtlJobControlRepository(scope.Session);

            Int32 deleted = await sut.DeleteOlderThanAsync(EtlJobName.ApplicationDataSheet, 30);

            deleted.Should().Be(8);
            FakeExecution execution = scope.Connection.Executions.Single();
            execution.CommandText.Should().Contain("DELETE FROM connection360write.etl_job_control");
            execution.Parameters["JobName"].Should().Be("application_data_sheet");
            execution.Parameters["OlderThanDays"].Should().Be(30);
        }

        [Fact]
        public async Task TodasLasOperaciones_ConConexionCerrada_LaAbrenYUsanLaTransaccionDeLaSesion()
        {
            await using var scope = new FakeSessionScope(ConnectionState.Closed);
            scope.Connection.ScalarResult = _ => 1L;
            var transaction = new FakeDbTransaction(scope.Connection, IsolationLevel.ReadCommitted);
            scope.Session.Transaction = transaction;
            var sut = new EtlJobControlRepository(scope.Session);

            await sut.StartRunAsync(EtlJobName.ApplicationDataSheet, null);
            await sut.RegisterPageProgressAsync(1, 1, 1);
            await sut.CompleteRunAsync(1);
            await sut.FailRunAsync(1);
            await sut.DeleteOlderThanAsync(EtlJobName.ApplicationDataSheet, 1);

            scope.Connection.OpenCalls.Should().Be(1);
            scope.Connection.Executions.Should().HaveCount(5).And.OnlyContain(e => ReferenceEquals(e.Transaction, transaction));
        }

        [Fact]
        public async Task OperacionesAsincronas_PropaganLaExcepcionDeLaBaseDeDatos()
        {
            await using var scope = new FakeSessionScope();
            scope.Connection.ThrowOnExecute = new InvalidOperationException("bd caida");
            var sut = new EtlJobControlRepository(scope.Session);

            Func<Task> act = () => sut.CompleteRunAsync(1);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("bd caida");
        }
    }
}
