using Connection360Notification.Application.Mapping;
using Connection360Notification.Domain;
using FluentAssertions;
using Xunit;
using AppNotificationStatus = Connection360Notification.Application.Enum.NotificationStatus;
using AppNotificationType = Connection360Notification.Application.Enum.NotificationType;
using DomainNotificationStatus = Connection360Notification.Domain.Enums.NotificationStatus;
using DomainNotificationType = Connection360Notification.Domain.Enums.NotificationType;

namespace Connection360Notification.Application.Tests.Mapping
{
    public class NotificationMappingExtensionsTests
    {
        [Theory]
        [InlineData(AppNotificationType.ChangeState, DomainNotificationType.ChangeState)]
        [InlineData(AppNotificationType.Comment, DomainNotificationType.Comment)]
        public void ToDomain_TipoDeDto_MapeaAlTipoDeDominio(AppNotificationType origen, DomainNotificationType esperado)
        {
            origen.ToDomain().Should().Be(esperado);
        }

        [Theory]
        [InlineData(DomainNotificationType.ChangeState, AppNotificationType.ChangeState)]
        [InlineData(DomainNotificationType.Comment, AppNotificationType.Comment)]
        public void ToDto_TipoDeDominio_MapeaAlTipoDeDto(DomainNotificationType origen, AppNotificationType esperado)
        {
            origen.ToDto().Should().Be(esperado);
        }

        [Theory]
        [InlineData(AppNotificationStatus.Unread, DomainNotificationStatus.Unread)]
        [InlineData(AppNotificationStatus.Read, DomainNotificationStatus.Read)]
        public void ToDomain_EstadoDeDto_MapeaAlEstadoDeDominio(AppNotificationStatus origen, DomainNotificationStatus esperado)
        {
            origen.ToDomain().Should().Be(esperado);
        }

        [Theory]
        [InlineData(DomainNotificationStatus.Unread, AppNotificationStatus.Unread)]
        [InlineData(DomainNotificationStatus.Read, AppNotificationStatus.Read)]
        public void ToDto_EstadoDeDominio_MapeaAlEstadoDeDto(DomainNotificationStatus origen, AppNotificationStatus esperado)
        {
            origen.ToDto().Should().Be(esperado);
        }

        [Fact]
        public void ToDomain_TipoDeDtoFueraDeRango_LanzaArgumentOutOfRangeException()
        {
            Action act = () => ((AppNotificationType)99).ToDomain();

            act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("type");
        }

        [Fact]
        public void ToDto_TipoDeDominioFueraDeRango_LanzaArgumentOutOfRangeException()
        {
            Action act = () => ((DomainNotificationType)99).ToDto();

            act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("type");
        }

        [Fact]
        public void ToDomain_EstadoDeDtoFueraDeRango_LanzaArgumentOutOfRangeException()
        {
            Action act = () => ((AppNotificationStatus)99).ToDomain();

            act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("status");
        }

        [Fact]
        public void ToDto_EstadoDeDominioFueraDeRango_LanzaArgumentOutOfRangeException()
        {
            Action act = () => ((DomainNotificationStatus)99).ToDto();

            act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("status");
        }

        [Fact]
        public void ToListResponse_MapeaTodosLosCamposDeLaEntidad()
        {
            var fechaMensaje = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            var fechaNotificacion = new DateTime(2024, 6, 7, 8, 9, 10, DateTimeKind.Utc);
            var notificacion = new NotificationMessage(
                "CLI-1", DomainNotificationType.Comment, "Mensaje", "HBL-1", "Titulo",
                fechaMensaje, DomainNotificationStatus.Read, fechaNotificacion, 15, "ID-15");

            var respuesta = notificacion.ToListResponse();

            respuesta.Id.Should().Be("ID-15");
            respuesta.IdNotification.Should().Be(15);
            respuesta.ClientId.Should().Be("CLI-1");
            respuesta.NotificationType.Should().Be(AppNotificationType.Comment);
            respuesta.DocumentNumber.Should().Be("HBL-1");
            respuesta.Title.Should().Be("Titulo");
            respuesta.Message.Should().Be("Mensaje");
            respuesta.MessageDate.Should().Be(fechaMensaje);
            respuesta.NotificationStatus.Should().Be(AppNotificationStatus.Read);
            respuesta.NotificationDate.Should().Be(fechaNotificacion);
        }

        [Fact]
        public void ToListResponse_ConValoresPorDefecto_MapeaNoLeidaYCadenasVacias()
        {
            var notificacion = new NotificationMessage("CLI-1", DomainNotificationType.ChangeState, "Mensaje");

            var respuesta = notificacion.ToListResponse();

            respuesta.NotificationStatus.Should().Be(AppNotificationStatus.Unread);
            respuesta.NotificationType.Should().Be(AppNotificationType.ChangeState);
            respuesta.DocumentNumber.Should().BeEmpty();
            respuesta.Title.Should().BeEmpty();
        }
    }
}
