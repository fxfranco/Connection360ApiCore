using Connection360.Domain.Dtos;
using Connection360.Domain.Enums;
using Connection360.Domain.Ports.Persistence;
using Connection360.Infrastructure.Persistence.Repositories;
using Connection360.Infrastructure.Tests.TestHelpers;
using FluentAssertions;
using Xunit;

namespace Connection360.Infrastructure.Tests.Persistence.Repositories
{
    /// <summary>
    /// Las dos vistas (entregados / no entregados) comparten la misma logica: se prueban ambas con el mismo conjunto de casos.
    /// </summary>
    public class ApplicationDataSheetRepositoriesTests
    {
        public static IEnumerable<Object[]> Variantes()
        {
            yield return new Object[] { "entregados" };
            yield return new Object[] { "no_entregados" };
        }

        private static (IApplicationDataSheetViewRepositoryAdapter Sut, FakeDbConnection Connection, String ViewName) Build(String variant)
        {
            var connection = new FakeDbConnection();
            DbSession session = TestDbSession.Create(connection);
            return variant == "entregados"
                ? (new Adapter<ApplicationDataSheetEntregadosRepository>(new ApplicationDataSheetEntregadosRepository(session),
                    r => r.GetAllAsync(default), (r, s) => r.GetAllAsync(s), (r, n) => r.GetByNitClienteAsync(n), (r, n, s) => r.GetByNitClienteAsync(n, s)),
                    connection, "connection360read.vw_application_data_sheet_entregados")
                : (new Adapter<ApplicationDataSheetNoEntregadosRepository>(new ApplicationDataSheetNoEntregadosRepository(session),
                    r => r.GetAllAsync(default), (r, s) => r.GetAllAsync(s), (r, n) => r.GetByNitClienteAsync(n), (r, n, s) => r.GetByNitClienteAsync(n, s)),
                    connection, "connection360read.vw_application_data_sheet_no_entregados");
        }

        private interface IApplicationDataSheetViewRepositoryAdapter
        {
            Task<List<ApplicationDataSheetViewResultDto>> GetAll();
            Task<List<ApplicationDataSheetViewResultDto>> GetAll(ApplicationDataSheetViewFieldsSelectionDto selection);
            Task<List<ApplicationDataSheetViewResultDto>> GetByNit(String nit);
            Task<List<ApplicationDataSheetViewResultDto>> GetByNit(String nit, ApplicationDataSheetViewFieldsSelectionDto selection);
        }

        private sealed class Adapter<TRepo> : IApplicationDataSheetViewRepositoryAdapter
        {
            private readonly TRepo _repo;
            private readonly Func<TRepo, Task<List<ApplicationDataSheetViewResultDto>>> _all;
            private readonly Func<TRepo, ApplicationDataSheetViewFieldsSelectionDto, Task<List<ApplicationDataSheetViewResultDto>>> _allSel;
            private readonly Func<TRepo, String, Task<List<ApplicationDataSheetViewResultDto>>> _byNit;
            private readonly Func<TRepo, String, ApplicationDataSheetViewFieldsSelectionDto, Task<List<ApplicationDataSheetViewResultDto>>> _byNitSel;

            public Adapter(TRepo repo,
                Func<TRepo, Task<List<ApplicationDataSheetViewResultDto>>> all,
                Func<TRepo, ApplicationDataSheetViewFieldsSelectionDto, Task<List<ApplicationDataSheetViewResultDto>>> allSel,
                Func<TRepo, String, Task<List<ApplicationDataSheetViewResultDto>>> byNit,
                Func<TRepo, String, ApplicationDataSheetViewFieldsSelectionDto, Task<List<ApplicationDataSheetViewResultDto>>> byNitSel)
            {
                _repo = repo; _all = all; _allSel = allSel; _byNit = byNit; _byNitSel = byNitSel;
            }

            public Task<List<ApplicationDataSheetViewResultDto>> GetAll() => _all(_repo);
            public Task<List<ApplicationDataSheetViewResultDto>> GetAll(ApplicationDataSheetViewFieldsSelectionDto selection) => _allSel(_repo, selection);
            public Task<List<ApplicationDataSheetViewResultDto>> GetByNit(String nit) => _byNit(_repo, nit);
            public Task<List<ApplicationDataSheetViewResultDto>> GetByNit(String nit, ApplicationDataSheetViewFieldsSelectionDto selection) => _byNitSel(_repo, nit, selection);
        }

        [Theory]
        [MemberData(nameof(Variantes))]
        public async Task GetAllAsync_ConsultaLaVistaConTodasLasColumnasYMapeaFilas(String variant)
        {
            var (sut, connection, view) = Build(variant);
            connection.ReaderResult = FakeDbConnection.Table(
                new[] { "id", "fecha_creacion", "tipo_operacion", "nit_cliente", "peso_kg" },
                new Object?[] { 1L, new DateTime(2026, 3, 4), "IMPORTACION", "900", 12.5m },
                new Object?[] { 2L, new DateTime(2026, 3, 5), "EXPORTACION", "901", 7m });

            List<ApplicationDataSheetViewResultDto> result = await sut.GetAll();

            result.Should().HaveCount(2);
            result[0].Id.Should().Be(1);
            result[0].FechaCreacion.Should().Be(new DateTime(2026, 3, 4));
            result[0].TipoOperacion.Should().Be("IMPORTACION");
            result[0].NitCliente.Should().Be("900");
            result[1].Id.Should().Be(2);
            String sql = connection.Commands.Single().CommandText;
            sql.Should().StartWith("SELECT id, fecha_creacion::timestamp, tipo_operacion").And.EndWith($"FROM {view};");
        }

        [Theory]
        [MemberData(nameof(Variantes))]
        public async Task GetAllAsync_ConSeleccion_SoloConsultaLasColumnasPedidas(String variant)
        {
            var (sut, connection, view) = Build(variant);
            connection.ReaderResult = FakeDbConnection.Table(new[] { "id", "estado" }, new Object?[] { 3L, "ENTREGADO" });
            var selection = new ApplicationDataSheetViewFieldsSelectionDto
            {
                Fields = { ApplicationDataSheetViewField.Estado, ApplicationDataSheetViewField.Id }
            };

            List<ApplicationDataSheetViewResultDto> result = await sut.GetAll(selection);

            connection.Commands.Single().CommandText.Should().Be($"SELECT id, estado FROM {view};");
            result.Single().Estado.Should().Be("ENTREGADO");
        }

        [Theory]
        [MemberData(nameof(Variantes))]
        public async Task GetAllAsync_ConSeleccionNulaOVacia_Lanza(String variant)
        {
            var (sut, connection, _) = Build(variant);

            await ((Func<Task>)(() => sut.GetAll(null!))).Should().ThrowAsync<ArgumentNullException>();
            await ((Func<Task>)(() => sut.GetAll(new ApplicationDataSheetViewFieldsSelectionDto()))).Should().ThrowAsync<ArgumentException>();
            connection.Commands.Should().BeEmpty();
        }

        [Theory]
        [MemberData(nameof(Variantes))]
        public async Task GetByNitClienteAsync_FiltraPorNitConParametro(String variant)
        {
            var (sut, connection, view) = Build(variant);
            connection.ReaderResult = FakeDbConnection.Table(new[] { "id", "nit_cliente" }, new Object?[] { 4L, "900123" });

            List<ApplicationDataSheetViewResultDto> result = await sut.GetByNit("900123");

            result.Single().NitCliente.Should().Be("900123");
            RecordedCommand command = connection.Commands.Single();
            command.CommandText.Should().EndWith($"FROM {view} WHERE nit_cliente = @NitCliente;");
            command.Parameters["NitCliente"].Should().Be("900123");
        }

        [Theory]
        [MemberData(nameof(Variantes))]
        public async Task GetByNitClienteAsync_ConSeleccion_UsaSoloLasColumnasPedidas(String variant)
        {
            var (sut, connection, view) = Build(variant);
            connection.ReaderResult = FakeDbConnection.Table(new[] { "cliente", "fecha_eta" }, new Object?[] { "ACME", new DateTime(2026, 1, 1) });
            var selection = new ApplicationDataSheetViewFieldsSelectionDto
            {
                Fields = { ApplicationDataSheetViewField.FechaEta, ApplicationDataSheetViewField.Cliente }
            };

            List<ApplicationDataSheetViewResultDto> result = await sut.GetByNit("900", selection);

            RecordedCommand command = connection.Commands.Single();
            command.CommandText.Should().Be($"SELECT cliente, fecha_eta::timestamp FROM {view} WHERE nit_cliente = @NitCliente;");
            command.Parameters["NitCliente"].Should().Be("900");
            result.Single().Cliente.Should().Be("ACME");
            result.Single().FechaEta.Should().Be(new DateTime(2026, 1, 1));
        }

        [Theory]
        [MemberData(nameof(Variantes))]
        public async Task GetByNitClienteAsync_ConSeleccionNulaOVacia_Lanza(String variant)
        {
            var (sut, connection, _) = Build(variant);

            await ((Func<Task>)(() => sut.GetByNit("900", null!))).Should().ThrowAsync<ArgumentNullException>();
            await ((Func<Task>)(() => sut.GetByNit("900", new ApplicationDataSheetViewFieldsSelectionDto()))).Should().ThrowAsync<ArgumentException>();
            connection.Commands.Should().BeEmpty();
        }

        [Theory]
        [MemberData(nameof(Variantes))]
        public async Task GetAllAsync_SinFilas_RetornaListaVacia(String variant)
        {
            var (sut, connection, _) = Build(variant);
            connection.ReaderResult = FakeDbConnection.Table(new[] { "id" });

            (await sut.GetAll()).Should().BeEmpty();
        }

        [Fact]
        public void Repositorios_ImplementanSusPuertosDeDominio()
        {
            DbSession session = TestDbSession.Create(new FakeDbConnection());

            new ApplicationDataSheetEntregadosRepository(session).Should().BeAssignableTo<IApplicationDataSheetEntregadosRepository>();
            new ApplicationDataSheetNoEntregadosRepository(session).Should().BeAssignableTo<IApplicationDataSheetNoEntregadosRepository>();
            new LogStatusTrackingViewRepository(session).Should().BeAssignableTo<ILogStatusTrackingViewRepository>();
        }
    }
}
