using Connection360.Etl.Domain.Enums;
using Connection360.Etl.Infrastructure.Configuration;
using Connection360.Etl.Infrastructure.Persistence.Repositories;
using Connection360.Etl.Infrastructure.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using System.Data;
using Xunit;

namespace Connection360.Etl.Infrastructure.Tests.Persistence.Repositories
{
    public class MaterializedViewRepositoryTests
    {
        private static MaterializedViewRepository Create(FakeSessionScope scope, Int32 timeoutSeconds = 600)
            => new(scope.Session, Options.Create(new MaterializedViewSettings { CommandTimeoutSeconds = timeoutSeconds }));

        [Theory]
        [InlineData(MaterializedViewName.LogStatusTracking, "REFRESH MATERIALIZED VIEW CONCURRENTLY connection360read.vw_log_status_tracking;")]
        [InlineData(MaterializedViewName.ApplicationDataSheetEntregados, "REFRESH MATERIALIZED VIEW CONCURRENTLY connection360read.vw_application_data_sheet_entregados;")]
        [InlineData(MaterializedViewName.ApplicationDataSheetNoEntregados, "REFRESH MATERIALIZED VIEW CONCURRENTLY connection360read.vw_application_data_sheet_no_entregados;")]
        public async Task RefreshConcurrentlyAsync_EjecutaElRefreshConcurrentlyDeLaVistaIndicada(MaterializedViewName view, String expected)
        {
            await using var scope = new FakeSessionScope();
            var sut = Create(scope);

            await sut.RefreshConcurrentlyAsync(view);

            FakeExecution execution = scope.Connection.Executions.Should().ContainSingle().Subject;
            execution.Kind.Should().Be("NonQuery");
            execution.CommandText.Should().Be(expected);
            execution.Parameters.Should().BeEmpty();
        }

        [Fact]
        public async Task RefreshConcurrentlyAsync_SiempreUsaConcurrentlyParaNoBloquearLecturas()
        {
            await using var scope = new FakeSessionScope();
            var sut = Create(scope);

            foreach (MaterializedViewName view in Enum.GetValues<MaterializedViewName>())
                await sut.RefreshConcurrentlyAsync(view);

            scope.Connection.Executions.Should().HaveCount(3)
                .And.OnlyContain(e => e.CommandText.StartsWith("REFRESH MATERIALIZED VIEW CONCURRENTLY connection360read.vw_"));
        }

        [Fact]
        public async Task RefreshConcurrentlyAsync_UsaElTimeoutConfigurado()
        {
            await using var scope = new FakeSessionScope();
            var sut = Create(scope, timeoutSeconds: 123);

            await sut.RefreshConcurrentlyAsync(MaterializedViewName.LogStatusTracking);

            scope.Connection.Executions.Single().CommandTimeout.Should().Be(123);
        }

        [Fact]
        public async Task RefreshConcurrentlyAsync_TimeoutNegativo_SeNormalizaACeroSinLimite()
        {
            await using var scope = new FakeSessionScope();
            var sut = Create(scope, timeoutSeconds: -5);

            await sut.RefreshConcurrentlyAsync(MaterializedViewName.LogStatusTracking);

            scope.Connection.Executions.Single().CommandTimeout.Should().Be(0);
        }

        [Fact]
        public async Task RefreshConcurrentlyAsync_ConexionCerrada_LaAbreAntesDeEjecutar()
        {
            await using var scope = new FakeSessionScope(ConnectionState.Closed);
            var sut = Create(scope);

            await sut.RefreshConcurrentlyAsync(MaterializedViewName.ApplicationDataSheetEntregados);

            scope.Connection.OpenCalls.Should().Be(1);
            scope.Connection.Executions.Should().ContainSingle();
        }

        [Fact]
        public async Task RefreshConcurrentlyAsync_SinTransaccionActiva_SeEjecutaEnAutocommit()
        {
            await using var scope = new FakeSessionScope();
            var sut = Create(scope);

            await sut.RefreshConcurrentlyAsync(MaterializedViewName.LogStatusTracking);

            scope.Connection.Executions.Single().Transaction.Should().BeNull();
        }

        [Fact]
        public async Task RefreshConcurrentlyAsync_ElMotorFalla_PropagaLaExcepcion()
        {
            await using var scope = new FakeSessionScope();
            scope.Connection.ThrowOnExecute = new InvalidOperationException("no hay índice único");
            var sut = Create(scope);

            Func<Task> act = () => sut.RefreshConcurrentlyAsync(MaterializedViewName.LogStatusTracking);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("no hay índice único");
        }

        [Fact]
        public async Task RefreshConcurrentlyAsync_ValorDeEnumNoMapeado_LanzaArgumentOutOfRangeSinTocarLaBaseDeDatos()
        {
            await using var scope = new FakeSessionScope();
            var sut = Create(scope);

            Func<Task> act = () => sut.RefreshConcurrentlyAsync((MaterializedViewName)999);

            await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
            scope.Connection.Executions.Should().BeEmpty();
        }
    }
}
