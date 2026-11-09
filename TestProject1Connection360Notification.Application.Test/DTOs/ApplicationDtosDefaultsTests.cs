using Connection360Notification.Application.DTOs;
using FluentAssertions;
using Xunit;

namespace Connection360Notification.Application.Tests.DTOs
{
    public class ApplicationDtosDefaultsTests
    {
        [Fact]
        public void NotificationsListResponse_PorDefecto_TieneValoresIniciales()
        {
            var dto = new NotificationsListResponse();

            dto.Id.Should().BeEmpty();
            dto.IdNotification.Should().Be(0);
            dto.ClientId.Should().BeEmpty();
            dto.DocumentNumber.Should().BeEmpty();
            dto.Title.Should().BeEmpty();
            dto.Message.Should().BeEmpty();
            dto.MessageDate.Should().Be(default);
            dto.NotificationDate.Should().Be(default);
        }

        [Fact]
        public void NotificationsListResponse_AsignaIdentificadoresYTextos()
        {
            var dto = new NotificationsListResponse { Id = "X", ClientId = "C", IdNotification = 4 };

            dto.Id.Should().Be("X");
            dto.ClientId.Should().Be("C");
            dto.IdNotification.Should().Be(4);
        }

        [Fact]
        public void CreateNotificationRequest_ConSoloObligatorios_LosOpcionalesSonNulos()
        {
            var dto = new CreateNotificationRequest("R", "C", "T");

            dto.Recipient.Should().Be("R");
            dto.Content.Should().Be("C");
            dto.Type.Should().Be("T");
            dto.DocumentNumber.Should().BeNull();
            dto.Title.Should().BeNull();
            dto.MessageDate.Should().BeNull();
        }

        [Fact]
        public void CreateNotificationRequest_ConTodosLosDatos_ExponeCadaPropiedad()
        {
            var fecha = new DateTime(2024, 2, 3, 4, 5, 6, DateTimeKind.Utc);

            var dto = new CreateNotificationRequest("R", "C", "T", "DOC", "Titulo", fecha);

            dto.DocumentNumber.Should().Be("DOC");
            dto.Title.Should().Be("Titulo");
            dto.MessageDate.Should().Be(fecha);
        }

        [Fact]
        public void CreateNotificationRequest_ConWith_CreaCopiaModificada()
        {
            var original = new CreateNotificationRequest("R", "C", "T");

            var copia = original with { Content = "Otro" };

            copia.Content.Should().Be("Otro");
            copia.Should().NotBe(original);
            original.Content.Should().Be("C");
        }

        [Fact]
        public void CreateNotificationRequest_ConDatosDistintos_NoSonIguales()
        {
            new CreateNotificationRequest("R", "C", "T").Should().NotBe(new CreateNotificationRequest("R2", "C", "T"));
        }

        [Fact]
        public void ClientSummaryRequest_AllClientPorDefecto_EsFalso()
        {
            new ClientSummaryRequest().AllClient.Should().BeFalse();
        }
    }
}
