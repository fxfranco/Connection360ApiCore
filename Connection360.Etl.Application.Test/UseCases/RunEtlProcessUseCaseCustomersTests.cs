using Connection360.Etl.Application.DTOs;
using Connection360.Etl.Application.Tests.Support;
using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Domain.Ports.Persistence;
using FluentAssertions;
using Moq;
using System.Text.Json;
using Xunit;

namespace Connection360.Etl.Application.Tests.UseCases
{
    /// <summary>
    /// Inserción de clientes (NitCliente) en connection360write.customers por ronda y canales de
    /// notificación (customer_notification_channels) incluidos en el payload de outbox_messages.
    /// </summary>
    public class RunEtlProcessUseCaseCustomersTests
    {
        private static ApplicationDataSheetChange Change(String document, Boolean stateChanged = false, Boolean commentChanged = false, String? nit = null)
            => new()
            {
                DocumentoTransporteHbl = document,
                NitCliente = nit ?? "NIT-" + document,
                StateChanged = stateChanged,
                CommentChanged = commentChanged,
                NuevoEstado = "ENTREGADO",
            };

        private static void WithChanges(RunEtlHarness h, params ApplicationDataSheetChange[] changes)
            => h.Detector.Setup(d => d.DetectChanges(It.IsAny<IReadOnlyList<ApplicationDataSheet>>(), It.IsAny<IReadOnlyDictionary<String, ApplicationDataSheetChangeSnapshot>>()))
                .Returns(changes.ToList());

        private static EtlOutboxPayload Payload(OutboxMessage message) => JsonSerializer.Deserialize<EtlOutboxPayload>(message.Payload)!;

        // ------------------------------------------------------------ customers

        [Fact]
        public async Task ExecuteAsync_InsertaLosNitClienteDeCadaRondaEnCustomers()
        {
            var h = new RunEtlHarness(true).WithPages("BPMS", DataSets.WithDocuments("H1", "H2"), DataSets.WithDocuments("H3"));

            var result = await h.CreateUseCase().ExecuteAsync();

            result.Success.Should().BeTrue();
            h.InsertedCustomerBatches.Should().HaveCount(2);
            h.InsertedCustomerBatches[0].Should().BeEquivalentTo(new[] { "NIT-H1", "NIT-H2" });
            h.InsertedCustomerBatches[1].Should().BeEquivalentTo(new[] { "NIT-H3" });
        }

        [Fact]
        public async Task ExecuteAsync_EnLaMigracionInicialTambienInsertaLosClientes()
        {
            var h = new RunEtlHarness(migrationAlreadyCompleted: false).WithPages("BPMS", DataSets.WithDocuments("H1"));

            var result = await h.CreateUseCase().ExecuteAsync();

            result.IsInitialMigrationRun.Should().BeTrue();
            h.InsertedCustomerBatches.Should().ContainSingle().Which.Should().BeEquivalentTo(new[] { "NIT-H1" });
        }

        [Fact]
        public async Task ExecuteAsync_InsertaLosClientesDentroDeLaTransaccionDespuesDelUpsertYAntesDeNotificar()
        {
            var h = new RunEtlHarness(true).WithPages("BPMS", DataSets.WithDocuments("H1"));
            WithChanges(h, Change("H1", stateChanged: true));
            h.CustomerRepository.Setup(r => r.InsertMissingAsync(It.IsAny<IEnumerable<String>>(), It.IsAny<CancellationToken>()))
                .Callback(() => h.Calls.Add("InsertCustomers")).ReturnsAsync(1);

            await h.CreateUseCase().ExecuteAsync();

            Int32 begin = h.Calls.IndexOf("Begin");
            Int32 upsert = h.Calls.IndexOf("Upsert");
            Int32 customers = h.Calls.IndexOf("InsertCustomers");
            Int32 outbox = h.Calls.IndexOf("InsertOutbox");
            Int32 commit = h.Calls.IndexOf("Commit");
            begin.Should().BeLessThan(upsert);
            upsert.Should().BeLessThan(customers);
            customers.Should().BeLessThan(outbox);
            outbox.Should().BeLessThan(commit);
        }

        [Fact]
        public async Task ExecuteAsync_SiFallaLaInsercionDeClientes_RevierteLaRondaYMarcaLaCorridaComoFallida()
        {
            var h = new RunEtlHarness(true).WithPages("BPMS", DataSets.WithDocuments("H1"));
            h.CustomerRepository.Setup(r => r.InsertMissingAsync(It.IsAny<IEnumerable<String>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("customers"));

            var result = await h.CreateUseCase().ExecuteAsync();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("customers");
            h.Calls.Should().Contain("Rollback").And.NotContain("Commit");
        }

        // ------------------------------------------------------------ canales en el payload

        [Fact]
        public async Task ExecuteAsync_IncluyeLosCanalesDelClienteEnElPayloadDelOutbox()
        {
            var h = new RunEtlHarness(true).WithPages("BPMS", DataSets.WithDocuments("H1", "H2"));
            h.ChannelsByClient["NIT-H1"] = new CustomerNotificationChannelSettings(true, false, true);
            h.ChannelsByClient["NIT-H2"] = new CustomerNotificationChannelSettings(false, true, false);
            WithChanges(h, Change("H1", stateChanged: true), Change("H2", commentChanged: true));

            await h.CreateUseCase().ExecuteAsync();

            EtlOutboxPayload first = Payload(h.InsertedOutboxMessages.Single(m => m.EventType == "ChangeState"));
            first.ClientId.Should().Be("NIT-H1");
            first.NotificationChannel.Should().BeEquivalentTo(new EtlNotificationChannelPayload { Application = true, Email = false, TextMessages = true });
            EtlOutboxPayload second = Payload(h.InsertedOutboxMessages.Single(m => m.EventType == "Comment"));
            second.ClientId.Should().Be("NIT-H2");
            second.NotificationChannel.Should().BeEquivalentTo(new EtlNotificationChannelPayload { Application = false, Email = true, TextMessages = false });
        }

        [Fact]
        public async Task ExecuteAsync_SiElClienteNoTieneCanales_ElPayloadLlevaTodoEnFalse()
        {
            var h = new RunEtlHarness(true).WithPages("BPMS", DataSets.WithDocuments("H1"));
            WithChanges(h, Change("H1", stateChanged: true));

            await h.CreateUseCase().ExecuteAsync();

            OutboxMessage message = h.InsertedOutboxMessages.Should().ContainSingle().Subject;
            EtlOutboxPayload payload = Payload(message);
            payload.NotificationChannel.Application.Should().BeFalse();
            payload.NotificationChannel.Email.Should().BeFalse();
            payload.NotificationChannel.TextMessages.Should().BeFalse();
            // Las propiedades quedan serializadas explícitamente (no se omiten) aunque sean false.
            using JsonDocument doc = JsonDocument.Parse(message.Payload);
            doc.RootElement.GetProperty("NotificationChannel").GetProperty("TextMessages").GetBoolean().Should().BeFalse();
        }

        [Fact]
        public async Task ExecuteAsync_ConsultaLosCanalesUnaSolaVezPorRondaConLosClientesDistintosConCambios()
        {
            var h = new RunEtlHarness(true).WithPages("BPMS", DataSets.WithDocuments("H1", "H2", "H3"));
            // H3 no tiene cambios relevantes (no se notifica); H1 y H2 comparten el mismo cliente.
            WithChanges(h, Change("H1", stateChanged: true, nit: "NIT-A"), Change("H2", commentChanged: true, nit: "NIT-A"), Change("H3"));
            List<String>? requested = null;
            h.ChannelsRepository.Setup(r => r.GetByCustomerIdentificationsAsync(It.IsAny<IEnumerable<String>>(), It.IsAny<CancellationToken>()))
                .Callback((IEnumerable<String> ids, CancellationToken _) => requested = ids.ToList())
                .ReturnsAsync(new Dictionary<String, CustomerNotificationChannelSettings>());

            await h.CreateUseCase().ExecuteAsync();

            h.ChannelsRepository.Verify(r => r.GetByCustomerIdentificationsAsync(It.IsAny<IEnumerable<String>>(), It.IsAny<CancellationToken>()), Times.Once);
            requested.Should().Equal("NIT-A");
        }

        [Fact]
        public async Task ExecuteAsync_SinCambiosNoConsultaLosCanales()
        {
            var h = new RunEtlHarness(true).WithPages("BPMS", DataSets.WithDocuments("H1"));

            await h.CreateUseCase().ExecuteAsync();

            h.ChannelsRepository.Verify(r => r.GetByCustomerIdentificationsAsync(It.IsAny<IEnumerable<String>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_EnLaMigracionInicialNoConsultaLosCanales()
        {
            var h = new RunEtlHarness(migrationAlreadyCompleted: false).WithPages("BPMS", DataSets.WithDocuments("H1"));

            await h.CreateUseCase().ExecuteAsync();

            h.ChannelsRepository.Verify(r => r.GetByCustomerIdentificationsAsync(It.IsAny<IEnumerable<String>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_SiFallaLaConsultaDeCanales_RevierteLaRonda()
        {
            var h = new RunEtlHarness(true).WithPages("BPMS", DataSets.WithDocuments("H1"));
            WithChanges(h, Change("H1", stateChanged: true));
            h.ChannelsRepository.Setup(r => r.GetByCustomerIdentificationsAsync(It.IsAny<IEnumerable<String>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutException("canales"));

            var result = await h.CreateUseCase().ExecuteAsync();

            result.Success.Should().BeFalse();
            h.Calls.Should().Contain("Rollback").And.NotContain("Commit");
            h.InsertedOutboxMessages.Should().BeEmpty();
        }
    }
}
