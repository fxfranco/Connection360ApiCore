using Connection360.Etl.Application.DTOs;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace Connection360.Etl.Application.Tests.DTOs
{
    public class EtlOutboxPayloadTests
    {
        [Fact]
        public void NuevaInstancia_InicializaLasCadenasVacias()
        {
            var payload = new EtlOutboxPayload();

            payload.ClientId.Should().BeEmpty();
            payload.EventType.Should().BeEmpty();
            payload.DocumentNumber.Should().BeEmpty();
            payload.Title.Should().BeEmpty();
            payload.Message.Should().BeEmpty();
            payload.MessageDate.Should().Be(default);
        }

        [Fact]
        public void Serializar_UsaExactamenteLosNombresDelContratoEnPascalCase()
        {
            var payload = new EtlOutboxPayload
            {
                ClientId = "900123",
                EventType = "ChangeState",
                DocumentNumber = "HBL-1",
                Title = "T",
                Message = "M",
                MessageDate = new DateTime(2025, 3, 4, 5, 6, 7, DateTimeKind.Utc),
            };

            using JsonDocument doc = JsonDocument.Parse(JsonSerializer.Serialize(payload));

            doc.RootElement.EnumerateObject().Select(p => p.Name)
                .Should().Equal("ClientId", "EventType", "DocumentNumber", "Title", "Message", "MessageDate");
            doc.RootElement.GetProperty("ClientId").GetString().Should().Be("900123");
            doc.RootElement.GetProperty("MessageDate").GetDateTime().Should().Be(payload.MessageDate);
        }

        [Fact]
        public void Deserializar_RecuperaTodosLosValores()
        {
            const String json = "{\"ClientId\":\"1\",\"EventType\":\"Comment\",\"DocumentNumber\":\"D\",\"Title\":\"T\",\"Message\":\"M\",\"MessageDate\":\"2025-01-02T03:04:05Z\"}";

            EtlOutboxPayload payload = JsonSerializer.Deserialize<EtlOutboxPayload>(json)!;

            payload.ClientId.Should().Be("1");
            payload.EventType.Should().Be("Comment");
            payload.DocumentNumber.Should().Be("D");
            payload.Title.Should().Be("T");
            payload.Message.Should().Be("M");
            payload.MessageDate.ToUniversalTime().Should().Be(new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        }
    }
}
