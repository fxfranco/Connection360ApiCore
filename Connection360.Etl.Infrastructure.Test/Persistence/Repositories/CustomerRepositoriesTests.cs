using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Infrastructure.Persistence.Repositories;
using Connection360.Etl.Infrastructure.Tests.TestHelpers;
using FluentAssertions;
using System.Data;
using Xunit;

namespace Connection360.Etl.Infrastructure.Tests.Persistence.Repositories
{
    public class CustomerRepositoryTests
    {
        private static List<String> IdentificationParameters(FakeExecution execution)
            => execution.Parameters.Where(p => p.Key.StartsWith("Identifications", StringComparison.OrdinalIgnoreCase))
                .Select(p => (String)p.Value!).ToList();

        [Fact]
        public async Task InsertMissingAsync_ListaVacia_DevuelveCeroSinTocarLaBaseDeDatos()
        {
            await using var scope = new FakeSessionScope(ConnectionState.Closed);
            var sut = new CustomerRepository(scope.Session);

            Int32 result = await sut.InsertMissingAsync(new List<String>());

            result.Should().Be(0);
            scope.Connection.Executions.Should().BeEmpty();
            scope.Connection.OpenCalls.Should().Be(0);
        }

        [Fact]
        public async Task InsertMissingAsync_Nulo_DevuelveCero()
        {
            await using var scope = new FakeSessionScope();
            var sut = new CustomerRepository(scope.Session);

            (await sut.InsertMissingAsync(null!)).Should().Be(0);
            scope.Connection.Executions.Should().BeEmpty();
        }

        [Fact]
        public async Task InsertMissingAsync_SoloVaciosOEspacios_NoEjecutaNada()
        {
            await using var scope = new FakeSessionScope(ConnectionState.Closed);
            var sut = new CustomerRepository(scope.Session);

            (await sut.InsertMissingAsync(new[] { "", "   ", null! })).Should().Be(0);

            scope.Connection.Executions.Should().BeEmpty();
            scope.Connection.OpenCalls.Should().Be(0);
        }

        [Fact]
        public async Task InsertMissingAsync_FiltraVaciosYDuplicadosYEjecutaUnaSolaSentenciaSinDuplicarClientes()
        {
            await using var scope = new FakeSessionScope(ConnectionState.Closed);
            scope.Connection.NonQueryResult = _ => 2;
            var sut = new CustomerRepository(scope.Session);

            Int32 result = await sut.InsertMissingAsync(new[] { "900", "900", " ", "901", "" });

            result.Should().Be(2);
            scope.Connection.OpenCalls.Should().Be(1);
            FakeExecution execution = scope.Connection.Executions.Single();
            execution.CommandText.Should().Contain("INSERT INTO connection360write.customers (identificacion)")
                .And.Contain("unnest(")
                .And.Contain("NOT EXISTS")
                .And.Contain("c.identificacion = t.identificacion");
            IdentificationParameters(execution).Should().BeEquivalentTo(new[] { "900", "901" });
        }

        [Fact]
        public async Task InsertMissingAsync_UsaLaTransaccionActivaDeLaSesion()
        {
            await using var scope = new FakeSessionScope();
            var transaction = new FakeDbTransaction(scope.Connection, IsolationLevel.ReadCommitted);
            scope.Session.Transaction = transaction;
            var sut = new CustomerRepository(scope.Session);

            await sut.InsertMissingAsync(new[] { "900" });

            scope.Connection.Executions.Single().Transaction.Should().BeSameAs(transaction);
        }

        [Fact]
        public async Task InsertMissingAsync_FallaLaBaseDeDatos_PropagaLaExcepcion()
        {
            await using var scope = new FakeSessionScope();
            scope.Connection.ThrowOnExecute = new TimeoutException("lento");
            var sut = new CustomerRepository(scope.Session);

            Func<Task> act = () => sut.InsertMissingAsync(new[] { "900" });

            await act.Should().ThrowAsync<TimeoutException>();
        }
    }

    public class CustomerNotificationChannelsRepositoryTests
    {
        private static DataTable ChannelsTable(params (String Id, Boolean App, Boolean Email, Boolean Sms)[] rows)
        {
            var table = new DataTable();
            table.Columns.Add("Identification", typeof(String));
            table.Columns.Add("Application", typeof(Boolean));
            table.Columns.Add("Email", typeof(Boolean));
            table.Columns.Add("TextMessages", typeof(Boolean));
            foreach (var row in rows)
                table.Rows.Add(row.Id, row.App, row.Email, row.Sms);
            return table;
        }

        private static List<String> IdentificationParameters(FakeExecution execution)
            => execution.Parameters.Where(p => p.Key.StartsWith("Identifications", StringComparison.OrdinalIgnoreCase))
                .Select(p => (String)p.Value!).ToList();

        [Fact]
        public async Task GetByCustomerIdentificationsAsync_SinIdentificaciones_DevuelveVacioSinConsultar()
        {
            await using var scope = new FakeSessionScope(ConnectionState.Closed);
            var sut = new CustomerNotificationChannelsRepository(scope.Session);

            (await sut.GetByCustomerIdentificationsAsync(new List<String>())).Should().BeEmpty();
            (await sut.GetByCustomerIdentificationsAsync(null!)).Should().BeEmpty();
            (await sut.GetByCustomerIdentificationsAsync(new[] { "", "  ", null! })).Should().BeEmpty();

            scope.Connection.Executions.Should().BeEmpty();
            scope.Connection.OpenCalls.Should().Be(0);
        }

        [Fact]
        public async Task GetByCustomerIdentificationsAsync_ConsultaCustomersUnidaAChannelsEnUnSoloRoundTrip()
        {
            await using var scope = new FakeSessionScope();
            scope.Connection.ReaderResult = _ => ChannelsTable();
            var sut = new CustomerNotificationChannelsRepository(scope.Session);

            await sut.GetByCustomerIdentificationsAsync(new[] { "900", "900", " ", "901" });

            FakeExecution execution = scope.Connection.Executions.Single();
            execution.Kind.Should().Be("Reader");
            execution.CommandText.Should().Contain("FROM connection360write.customers c")
                .And.Contain("INNER JOIN connection360write.customer_notification_channels n ON n.id_customer = c.id_customer")
                .And.Contain("c.identificacion = ANY(") // con la conexión falsa Dapper expande la lista; con Npgsql viaja como arreglo nativo
                .And.Contain("n.text_messages");
            IdentificationParameters(execution).Should().BeEquivalentTo(new[] { "900", "901" });
        }

        [Fact]
        public async Task GetByCustomerIdentificationsAsync_MapeaLosCanalesPorIdentificacion()
        {
            await using var scope = new FakeSessionScope();
            scope.Connection.ReaderResult = _ => ChannelsTable(("900", true, false, true), ("901", false, true, false));
            var sut = new CustomerNotificationChannelsRepository(scope.Session);

            var result = await sut.GetByCustomerIdentificationsAsync(new[] { "900", "901", "902" });

            result.Should().HaveCount(2);
            result["900"].Should().Be(new CustomerNotificationChannelSettings(true, false, true));
            result["901"].Should().Be(new CustomerNotificationChannelSettings(false, true, false));
            result.ContainsKey("902").Should().BeFalse(); // sin configuración: el llamador usa None
        }

        [Fact]
        public async Task GetByCustomerIdentificationsAsync_UsaLaTransaccionActivaDeLaSesion()
        {
            await using var scope = new FakeSessionScope();
            scope.Connection.ReaderResult = _ => ChannelsTable();
            var transaction = new FakeDbTransaction(scope.Connection, IsolationLevel.ReadCommitted);
            scope.Session.Transaction = transaction;
            var sut = new CustomerNotificationChannelsRepository(scope.Session);

            await sut.GetByCustomerIdentificationsAsync(new[] { "900" });

            scope.Connection.Executions.Single().Transaction.Should().BeSameAs(transaction);
        }

        [Fact]
        public void CustomerNotificationChannelSettings_None_TieneTodosLosCanalesEnFalse()
        {
            CustomerNotificationChannelSettings.None.Should().Be(new CustomerNotificationChannelSettings(false, false, false));
        }
    }
}
