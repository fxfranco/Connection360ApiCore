using Connection360.Domain.Dtos;
using Connection360.Domain.Entities.Persistence;
using Connection360.Infrastructure.Persistence.Repositories;
using Connection360.Infrastructure.Tests.TestHelpers;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace Connection360.Infrastructure.Tests.Persistence.Repositories
{
    /// <summary>
    /// Repositorios Dapper de escritura/lectura sobre una DbSession con conexion en memoria:
    /// se verifica el SQL, los parametros y el mapeo de resultados.
    /// </summary>
    public class WriteRepositoriesTests
    {
        private readonly FakeDbConnection _connection = new();
        private readonly DbSession _session;

        public WriteRepositoriesTests()
        {
            _session = TestDbSession.Create(_connection);
        }

        // ---------- CustomerRepository ----------

        [Fact]
        public async Task CustomerRepository_CrearAsync_InsertaYRetornaElIdGenerado()
        {
            _connection.ScalarResult = 33L;

            Int64 id = await new CustomerRepository(_session).CrearAsync("900123");

            id.Should().Be(33);
            RecordedCommand command = _connection.Commands.Single();
            command.CommandText.Should().Contain("INSERT INTO connection360write.customers").And.Contain("RETURNING id_customer");
            command.Parameters["Id"].Should().Be("900123");
            _connection.OpenCount.Should().Be(1);
        }

        [Fact]
        public async Task CustomerRepository_GetCustomerByIdAsync_RetornaElId()
        {
            _connection.ReaderResult = FakeDbConnection.Table(new[] { "id_customer" }, new Object?[] { 8L });

            Int64? id = await new CustomerRepository(_session).GetCustomerByIdAsync("900123");

            id.Should().Be(8);
            _connection.Commands.Single().CommandText.Should().Contain("FROM connection360write.customers WHERE identificacion = @Id");
        }

        [Fact]
        public async Task CustomerRepository_GetCustomerByIdAsync_SinFilas_RetornaCeroPorElTipoDeLaConsulta()
        {
            // Comportamiento actual: QueryFirstOrDefaultAsync<Int64> devuelve 0 (no null) cuando no hay fila.
            _connection.ReaderResult = FakeDbConnection.Table(new[] { "id_customer" });

            Int64? id = await new CustomerRepository(_session).GetCustomerByIdAsync("NO-EXISTE");

            id.Should().Be(0);
        }

        // ---------- CollaboratorRepository ----------

        [Fact]
        public async Task CollaboratorRepository_CrearAsync_InsertaYRetornaElIdGenerado()
        {
            _connection.ScalarResult = 5L;

            Int64 id = await new CollaboratorRepository(_session).CrearAsync("COL-1");

            id.Should().Be(5);
            RecordedCommand command = _connection.Commands.Single();
            command.CommandText.Should().Contain("INSERT INTO connection360write.collaborators").And.Contain("RETURNING id_collaborator");
            command.Parameters["Id"].Should().Be("COL-1");
        }

        [Fact]
        public async Task CollaboratorRepository_GetCollaboratorByIdAsync_RetornaElId()
        {
            _connection.ReaderResult = FakeDbConnection.Table(new[] { "id_collaborator" }, new Object?[] { 21L });

            Int64? id = await new CollaboratorRepository(_session).GetCollaboratorByIdAsync("COL-1");

            id.Should().Be(21);
            _connection.Commands.Single().Parameters["Id"].Should().Be("COL-1");
        }

        [Fact]
        public async Task CollaboratorRepository_GetCollaboratorByIdAsync_SinFilas_RetornaCero()
        {
            _connection.ReaderResult = FakeDbConnection.Table(new[] { "id_collaborator" });

            Int64? id = await new CollaboratorRepository(_session).GetCollaboratorByIdAsync("X");

            id.Should().Be(0);
        }

        // ---------- CustomersOfCollaboratorsRepository ----------

        [Fact]
        public async Task CustomersOfCollaboratorsRepository_CrearAsync_PropagaLosDosIdsYRetornaElGenerado()
        {
            _connection.ScalarResult = 90L;

            Int64 id = await new CustomersOfCollaboratorsRepository(_session).CrearAsync(3, 4);

            id.Should().Be(90);
            RecordedCommand command = _connection.Commands.Single();
            command.CommandText.Should().Contain("customers_of_collaborators").And.Contain("RETURNING id_customer_of_collaborator");
            command.Parameters["IdCustomer"].Should().Be(3L);
            command.Parameters["IdCollaborator"].Should().Be(4L);
        }

        [Fact]
        public async Task CustomersOfCollaboratorsRepository_ListCustomersByCollaboratorAsync_MapeaLasFilas()
        {
            _connection.ReaderResult = FakeDbConnection.Table(
                new[] { "id_collaborator", "identificacion_collaborator", "id_customer", "identificacion_customer" },
                new Object?[] { 1L, "COL-1", 10L, "CUS-10" },
                new Object?[] { 1L, "COL-1", 11L, "CUS-11" });

            List<CustomersOfCollaboratorDtoResult> result = await new CustomersOfCollaboratorsRepository(_session).ListCustomersByCollaboratorAsync("COL-1");

            result.Should().HaveCount(2);
            result[0].IdCollaborator.Should().Be(1);
            result[0].IdentificacionCollaborator.Should().Be("COL-1");
            result[1].IdCustomer.Should().Be(11);
            result[1].IdentificacionCustomer.Should().Be("CUS-11");
            _connection.Commands.Single().Parameters["IdCollaborator"].Should().Be("COL-1");
        }

        [Fact]
        public async Task CustomersOfCollaboratorsRepository_ListCustomersByCollaboratorAsync_SinFilas_RetornaListaVacia()
        {
            _connection.ReaderResult = FakeDbConnection.Table(new[] { "id_collaborator" });

            List<CustomersOfCollaboratorDtoResult> result = await new CustomersOfCollaboratorsRepository(_session).ListCustomersByCollaboratorAsync("COL-X");

            result.Should().BeEmpty();
        }

        // ---------- MasterSettingsRepository ----------

        private static MasterSettings SampleMasterSettings() => new(7, true, false, true, "COP", "es", "America/Bogota", 30);

        [Fact]
        public async Task MasterSettingsRepository_CrearAsync_PropagaTodosLosCamposYRetornaElId()
        {
            _connection.ScalarResult = 4L;

            Int64 id = await new MasterSettingsRepository(_session).CrearAsync(SampleMasterSettings());

            id.Should().Be(4);
            RecordedCommand command = _connection.Commands.Single();
            command.CommandText.Should().Contain("INSERT INTO connection360write.master_settings").And.Contain("RETURNING id_master_settings");
            command.Parameters["AutomaticTrackingUpdate"].Should().Be(true);
            command.Parameters["RequireDocumentUpload"].Should().Be(false);
            command.Parameters["PublicMonitoring"].Should().Be(true);
            command.Parameters["CurrencyType"].Should().Be("COP");
            command.Parameters["Language"].Should().Be("es");
            command.Parameters["TimeZone"].Should().Be("America/Bogota");
            command.Parameters["DataRetentionDays"].Should().Be((Int16)30);
        }

        [Fact]
        public async Task MasterSettingsRepository_GetAsync_MapeaLaFilaALaEntidad()
        {
            _connection.ReaderResult = FakeDbConnection.Table(
                new[] { "id_master_settings", "automatic_tracking_update", "require_document_upload", "public_monitoring", "currency_type", "language", "time_zone", "data_retention_days" },
                new Object?[] { 2L, true, false, true, "USD", "en", "UTC", (Int16)90 });

            MasterSettings result = await new MasterSettingsRepository(_session).GetAsync();

            result.IdMasterSettings.Should().Be(2);
            result.AutomaticTrackingUpdate.Should().BeTrue();
            result.RequireDocumentUpload.Should().BeFalse();
            result.PublicMonitoring.Should().BeTrue();
            result.CurrencyType.Should().Be("USD");
            result.Language.Should().Be("en");
            result.TimeZone.Should().Be("UTC");
            result.DataRetentionDays.Should().Be(90);
        }

        [Fact]
        public async Task MasterSettingsRepository_GetAsync_SinFilas_RetornaNull()
        {
            _connection.ReaderResult = FakeDbConnection.Table(new[] { "id_master_settings" });

            MasterSettings? result = await new MasterSettingsRepository(_session).GetAsync();

            result.Should().BeNull();
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(3, true)]
        [InlineData(0, false)]
        public async Task MasterSettingsRepository_UpdateAsync_RetornaTrueSoloSiAfectoFilas(Int32 rows, Boolean expected)
        {
            _connection.NonQueryResult = rows;

            Boolean result = await new MasterSettingsRepository(_session).UpdateAsync(SampleMasterSettings());

            result.Should().Be(expected);
            RecordedCommand command = _connection.Commands.Single();
            command.CommandText.Should().Contain("UPDATE connection360write.master_settings").And.Contain("WHERE id_master_settings = @IdMasterSettings");
            command.Parameters["IdMasterSettings"].Should().Be(7L);
        }

        // ---------- CustomerNotificationChannelsRepository ----------

        private static readonly String[] ChannelColumns = { "id_notification_channel", "id_customer", "application", "email", "text_messages" };

        [Fact]
        public async Task ChannelsRepository_ListAllAsync_MapeaTodasLasFilas()
        {
            _connection.ReaderResult = FakeDbConnection.Table(ChannelColumns,
                new Object?[] { 1L, 10L, true, false, true },
                new Object?[] { 2L, 11L, false, true, false });

            IEnumerable<CustomerNotificationChannels> result = await new CustomerNotificationChannelsRepository(_session).ListAllAsync();

            List<CustomerNotificationChannels> list = result.ToList();
            list.Should().HaveCount(2);
            list[0].IdNotificationChannel.Should().Be(1);
            list[0].IdCustomer.Should().Be(10);
            list[0].Application.Should().BeTrue();
            list[0].Email.Should().BeFalse();
            list[0].TextMessages.Should().BeTrue();
            list[1].Email.Should().BeTrue();
        }

        [Fact]
        public async Task ChannelsRepository_GetByIdAsync_RetornaLaFilaYPropagaElParametro()
        {
            _connection.ReaderResult = FakeDbConnection.Table(ChannelColumns, new Object?[] { 5L, 10L, true, true, true });

            CustomerNotificationChannels? result = await new CustomerNotificationChannelsRepository(_session).GetByIdAsync(5);

            result.Should().NotBeNull();
            result!.IdNotificationChannel.Should().Be(5);
            _connection.Commands.Single().Parameters["Id"].Should().Be(5L);
        }

        [Fact]
        public async Task ChannelsRepository_GetByIdAsync_SinFilas_RetornaNull()
        {
            _connection.ReaderResult = FakeDbConnection.Table(ChannelColumns);

            (await new CustomerNotificationChannelsRepository(_session).GetByIdAsync(99)).Should().BeNull();
        }

        [Fact]
        public async Task ChannelsRepository_GetByCustomerIdAsync_RetornaLaFilaYPropagaElParametro()
        {
            _connection.ReaderResult = FakeDbConnection.Table(ChannelColumns, new Object?[] { 5L, 10L, true, true, true });

            CustomerNotificationChannels? result = await new CustomerNotificationChannelsRepository(_session).GetByCustomerIdAsync(10);

            result!.IdCustomer.Should().Be(10);
            _connection.Commands.Single().Parameters["IdCustomer"].Should().Be(10L);
        }

        [Fact]
        public async Task ChannelsRepository_GetByCustomerIdAsync_SinFilas_RetornaNull()
        {
            _connection.ReaderResult = FakeDbConnection.Table(ChannelColumns);

            (await new CustomerNotificationChannelsRepository(_session).GetByCustomerIdAsync(1)).Should().BeNull();
        }

        [Fact]
        public async Task ChannelsRepository_CrearAsync_PropagaCamposYRetornaElId()
        {
            _connection.ScalarResult = 6L;
            var channels = new CustomerNotificationChannels(0, 10, true, false, true);

            Int64 id = await new CustomerNotificationChannelsRepository(_session).CrearAsync(channels);

            id.Should().Be(6);
            RecordedCommand command = _connection.Commands.Single();
            command.CommandText.Should().Contain("INSERT INTO connection360write.customer_notification_channels");
            command.Parameters["IdCustomer"].Should().Be(10L);
            command.Parameters["Application"].Should().Be(true);
            command.Parameters["Email"].Should().Be(false);
            command.Parameters["TextMessages"].Should().Be(true);
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(0, false)]
        public async Task ChannelsRepository_UpdateAsync_RetornaTrueSoloSiAfectoFilas(Int32 rows, Boolean expected)
        {
            _connection.NonQueryResult = rows;
            var channels = new CustomerNotificationChannels(15, 10, true, false, true);

            Boolean result = await new CustomerNotificationChannelsRepository(_session).UpdateAsync(channels);

            result.Should().Be(expected);
            _connection.Commands.Single().Parameters["IdNotificationChannel"].Should().Be(15L);
        }

        // ---------- CustomerNotificationEventRepository ----------

        private static readonly String[] EventColumns =
            { "id_notification_event", "id_customer", "change_state", "successful_delivery", "with_issues", "shipment_transit", "delivery_reminder" };

        [Fact]
        public async Task EventRepository_CrearAsync_PropagaCamposYRetornaElId()
        {
            _connection.ScalarResult = 14L;
            var events = new CustomerNotificationEvents(0, 10, true, false, true, false, true);

            Int64 id = await new CustomerNotificationEventRepository(_session).CrearAsync(events);

            id.Should().Be(14);
            RecordedCommand command = _connection.Commands.Single();
            command.CommandText.Should().Contain("INSERT INTO connection360write.customer_notification_events").And.Contain("RETURNING id_notification_event");
            command.Parameters["IdCustomer"].Should().Be(10L);
            command.Parameters["ChangeState"].Should().Be(true);
            command.Parameters["SuccessfulDelivery"].Should().Be(false);
            command.Parameters["WithIssues"].Should().Be(true);
            command.Parameters["ShipmentTransit"].Should().Be(false);
            command.Parameters["DeliveryReminder"].Should().Be(true);
        }

        [Fact]
        public async Task EventRepository_GetByCustomerIdAsync_MapeaLaFila()
        {
            _connection.ReaderResult = FakeDbConnection.Table(EventColumns, new Object?[] { 3L, 10L, true, false, true, false, true });

            CustomerNotificationEvents? result = await new CustomerNotificationEventRepository(_session).GetByCustomerIdAsync(10);

            result.Should().NotBeNull();
            result!.IdNotificationEvent.Should().Be(3);
            result.IdCustomer.Should().Be(10);
            result.ChangeState.Should().BeTrue();
            result.SuccessfulDelivery.Should().BeFalse();
            result.WithIssues.Should().BeTrue();
            result.ShipmentTransit.Should().BeFalse();
            result.DeliveryReminder.Should().BeTrue();
            _connection.Commands.Single().Parameters["IdCustomer"].Should().Be(10L);
        }

        [Fact]
        public async Task EventRepository_GetByCustomerIdAsync_SinFilas_RetornaNull()
        {
            _connection.ReaderResult = FakeDbConnection.Table(EventColumns);

            (await new CustomerNotificationEventRepository(_session).GetByCustomerIdAsync(1)).Should().BeNull();
        }

        [Fact]
        public async Task EventRepository_GetByIdAsync_MapeaLaFilaYPropagaElParametro()
        {
            _connection.ReaderResult = FakeDbConnection.Table(EventColumns, new Object?[] { 3L, 10L, false, false, false, false, false });

            CustomerNotificationEvents? result = await new CustomerNotificationEventRepository(_session).GetByIdAsync(3);

            result!.IdNotificationEvent.Should().Be(3);
            _connection.Commands.Single().Parameters["Id"].Should().Be(3L);
        }

        [Fact]
        public async Task EventRepository_GetByIdAsync_SinFilas_RetornaNull()
        {
            _connection.ReaderResult = FakeDbConnection.Table(EventColumns);

            (await new CustomerNotificationEventRepository(_session).GetByIdAsync(3)).Should().BeNull();
        }

        [Fact]
        public async Task EventRepository_ListAllAsync_MapeaTodasLasFilas()
        {
            _connection.ReaderResult = FakeDbConnection.Table(EventColumns,
                new Object?[] { 1L, 10L, true, true, true, true, true },
                new Object?[] { 2L, 11L, false, false, false, false, false });

            IEnumerable<CustomerNotificationEvents> result = await new CustomerNotificationEventRepository(_session).ListAllAsync();

            result.Select(e => e.IdNotificationEvent).Should().Equal(1, 2);
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(0, false)]
        public async Task EventRepository_UpdateAsync_RetornaTrueSoloSiAfectoFilas(Int32 rows, Boolean expected)
        {
            _connection.NonQueryResult = rows;
            var events = new CustomerNotificationEvents(77, 10, true, false, true, false, true);

            Boolean result = await new CustomerNotificationEventRepository(_session).UpdateAsync(events);

            result.Should().Be(expected);
            _connection.Commands.Single().Parameters["IdNotificationEvent"].Should().Be(77L);
        }

        // ---------- OutboxMessagesRepository ----------

        [Fact]
        public async Task OutboxRepository_CrearAsync_SerializaElPayloadYRetornaElGuid()
        {
            Guid generated = Guid.NewGuid();
            _connection.ScalarResult = generated;
            var request = new OutboxMessagesRequestDto { ClientId = "C1", EventType = "ESTADO", DocumentNumber = "D1", Title = "T", Message = "M" };
            DateTime before = DateTime.UtcNow.AddSeconds(-5);

            Guid id = await new OutboxMessagesRepository(_session).CrearAsync(request);

            id.Should().Be(generated);
            RecordedCommand command = _connection.Commands.Single();
            command.CommandText.Should().Contain("INSERT INTO connection360write.outbox_messages");
            command.Parameters["EventType"].Should().Be("ESTADO");
            command.Parameters["Id"].Should().BeOfType<Guid>().Which.Should().NotBe(Guid.Empty);
            command.Parameters["CreatedAt"].Should().BeOfType<DateTime>().Which.Should().BeOnOrAfter(before);
            OutboxMessagesRequestDto payload = JsonSerializer.Deserialize<OutboxMessagesRequestDto>((String)command.Parameters["Payload"]!)!;
            payload.ClientId.Should().Be("C1");
            payload.DocumentNumber.Should().Be("D1");
            payload.Title.Should().Be("T");
        }

        [Fact]
        public async Task OutboxRepository_GetListAsync_MapeaLosMensajesPendientes()
        {
            Guid first = Guid.NewGuid();
            Guid second = Guid.NewGuid();
            _connection.ReaderResult = FakeDbConnection.Table(new[] { "id", "event_type", "payload" },
                new Object?[] { first, "A", "{}" },
                new Object?[] { second, "B", "{\"x\":1}" });

            List<OutboxMessagesResultDto> result = await new OutboxMessagesRepository(_session).GetListAsync();

            result.Should().HaveCount(2);
            result[0].Id.Should().Be(first);
            result[0].EventType.Should().Be("A");
            result[1].Payload.Should().Be("{\"x\":1}");
            _connection.Commands.Single().CommandText.Should().Contain("processed_at IS NULL").And.Contain("FOR UPDATE SKIP LOCKED");
        }

        [Fact]
        public async Task OutboxRepository_GetListAsync_SinFilas_RetornaListaVacia()
        {
            _connection.ReaderResult = FakeDbConnection.Table(new[] { "id", "event_type", "payload" });

            (await new OutboxMessagesRepository(_session).GetListAsync()).Should().BeEmpty();
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(0, false)]
        public async Task OutboxRepository_UpdateprocessedAsync_RetornaTrueSoloSiAfectoFilas(Int32 rows, Boolean expected)
        {
            _connection.NonQueryResult = rows;
            Guid id = Guid.NewGuid();

            Boolean result = await new OutboxMessagesRepository(_session).UpdateprocessedAsync(id);

            result.Should().Be(expected);
            RecordedCommand command = _connection.Commands.Single();
            command.CommandText.Should().Contain("UPDATE connection360write.outbox_messages SET processed_at = @Now WHERE id = @Id");
            command.Parameters["Id"].Should().Be(id);
            command.Parameters["Now"].Should().BeOfType<DateTime>();
        }

        // ---------- Errores y cancelacion (comunes) ----------

        [Fact]
        public async Task Repositorios_SiLaBaseDeDatosFalla_PropaganLaExcepcion()
        {
            _connection.ExceptionToThrow = new InvalidOperationException("bd caida");

            await ((Func<Task>)(() => new CustomerRepository(_session).CrearAsync("1"))).Should().ThrowAsync<InvalidOperationException>();
            await ((Func<Task>)(() => new CollaboratorRepository(_session).GetCollaboratorByIdAsync("1"))).Should().ThrowAsync<InvalidOperationException>();
            await ((Func<Task>)(() => new MasterSettingsRepository(_session).GetAsync())).Should().ThrowAsync<InvalidOperationException>();
            await ((Func<Task>)(() => new OutboxMessagesRepository(_session).GetListAsync())).Should().ThrowAsync<InvalidOperationException>();
            await ((Func<Task>)(() => new CustomerNotificationEventRepository(_session).ListAllAsync())).Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task Repositorios_ConTokenCancelado_LanzanOperationCanceledException()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await ((Func<Task>)(() => new CustomerRepository(_session).CrearAsync("1", cts.Token))).Should().ThrowAsync<OperationCanceledException>();
            await ((Func<Task>)(() => new CustomersOfCollaboratorsRepository(_session).CrearAsync(1, 1, cts.Token))).Should().ThrowAsync<OperationCanceledException>();
        }
    }
}
