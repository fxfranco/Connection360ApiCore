using Connection360Notification.Application.DTOs;
using Connection360Notification.Application.Enum;
using FluentAssertions;
using Xunit;

namespace Connection360Notification.Application.Tests.DTOs
{
    /// <summary>
    /// Pruebas de contrato (get/set) para los DTOs de la capa Application.
    /// No contienen lógica propia; se valida asignación/lectura y valores por defecto.
    /// </summary>
    public class ApplicationDtosTests
    {
        [Fact]
        public void ClientSummaryRequest_DebeAsignarTodasLasPropiedades()
        {
            var dto = new ClientSummaryRequest { IdClient = "1", RoleName = "admin", FilterValue = "HBL-1" };

            dto.IdClient.Should().Be("1");
            dto.RoleName.Should().Be("admin");
            dto.FilterValue.Should().Be("HBL-1");
        }

        [Fact]
        public void NotificationsListResponse_DebeAsignarEnumsYFechas()
        {
            var fecha = new DateTime(2024, 1, 1);
            var dto = new NotificationsListResponse
            {
                IdNotification = 1,
                NotificationType = NotificationType.Comment,
                DocumentNumber = "HBL-1",
                Title = "Titulo",
                Message = "Mensaje",
                MessageDate = fecha,
                NotificationStatus = NotificationStatus.Unread,
                NotificationDate = fecha
            };

            dto.NotificationType.Should().Be(NotificationType.Comment);
            dto.NotificationStatus.Should().Be(NotificationStatus.Unread);
        }

        [Fact]
        public void ClientSummaryRequest_DebeAsignarLosDatosDeAccesoDeClientes()
        {
            var dto = new ClientSummaryRequest { IdClient = "1", RoleName = "ANALISTAOPE", IdQueryClient = "CUS-1", AllClient = true };

            dto.IdQueryClient.Should().Be("CUS-1");
            dto.AllClient.Should().BeTrue();
        }

        [Fact]
        public void NotificationsRequest_DebeAsignarTodasLasPropiedades()
        {
            var dto = new NotificationsRequest { IdClient = "1", RoleName = "admin", IdNotification = 1 };

            dto.IdClient.Should().Be("1");
            dto.RoleName.Should().Be("admin");
            dto.IdNotification.Should().Be(1);
        }

        [Fact]
        public void CreateNotificationRequest_EsUnRecordConIgualdadPorValor()
        {
            var dto1 = new CreateNotificationRequest("Prueba", "Prueba", "Prueba");
            var dto2 = new CreateNotificationRequest("Prueba", "Prueba", "Prueba");

            dto1.Should().Be(dto2);
        }
    }
}
