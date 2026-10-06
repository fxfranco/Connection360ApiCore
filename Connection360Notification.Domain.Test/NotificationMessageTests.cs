using System.Globalization;
using System.Text.Json;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Connection360Notification.Domain.Tests
{
    public class NotificationMessageTests
    {
        private static readonly DateTime FechaFija = new(2024, 5, 17, 10, 30, 0, DateTimeKind.Utc);

        [Fact]
        public void Constructor_ConTodosLosDatos_AsignaCadaPropiedad()
        {
            var sut = new NotificationMessage(
                clientId: "CLI-1",
                type: NotificationType.Comment,
                message: "Mensaje",
                documentNumber: "HBL-1",
                title: "Titulo",
                messageDate: FechaFija,
                status: NotificationStatus.Read,
                notificationDate: FechaFija.AddHours(1),
                idNotification: 42,
                id: "ID-FIJO");

            sut.Id.Should().Be("ID-FIJO");
            sut.IdNotification.Should().Be(42);
            sut.ClientId.Should().Be("CLI-1");
            sut.Type.Should().Be(NotificationType.Comment);
            sut.Message.Should().Be("Mensaje");
            sut.DocumentNumber.Should().Be("HBL-1");
            sut.Title.Should().Be("Titulo");
            sut.MessageDate.Should().Be(FechaFija);
            sut.Status.Should().Be(NotificationStatus.Read);
            sut.NotificationDate.Should().Be(FechaFija.AddHours(1));
        }

        [Fact]
        public void Constructor_SoloConDatosObligatorios_AplicaValoresPorDefecto()
        {
            var antes = DateTime.UtcNow;

            var sut = new NotificationMessage("CLI-1", NotificationType.ChangeState, "Mensaje");

            var despues = DateTime.UtcNow;
            sut.DocumentNumber.Should().BeEmpty();
            sut.Title.Should().BeEmpty();
            sut.Status.Should().Be(NotificationStatus.Unread);
            sut.IdNotification.Should().Be(0);
            sut.MessageDate.Should().BeOnOrAfter(antes).And.BeOnOrBefore(despues);
            sut.NotificationDate.Should().BeOnOrAfter(antes).And.BeOnOrBefore(despues);
        }

        [Fact]
        public void Constructor_SinId_GeneraUnGuidValido()
        {
            var sut = new NotificationMessage("CLI-1", NotificationType.Comment, "Mensaje");

            Guid.TryParse(sut.Id, out var guid).Should().BeTrue();
            guid.Should().NotBe(Guid.Empty);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_ConIdNuloOVacio_GeneraUnIdNuevo(String? id)
        {
            var sut = new NotificationMessage("CLI-1", NotificationType.Comment, "Mensaje", id: id);

            sut.Id.Should().NotBeNullOrWhiteSpace();
            Guid.TryParse(sut.Id, out _).Should().BeTrue();
        }

        [Fact]
        public void Constructor_DosInstanciasSinId_GeneranIdsDistintos()
        {
            var a = new NotificationMessage("CLI-1", NotificationType.Comment, "Mensaje");
            var b = new NotificationMessage("CLI-1", NotificationType.Comment, "Mensaje");

            a.Id.Should().NotBe(b.Id);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_ConClientIdInvalido_LanzaArgumentException(String? clientId)
        {
            Action act = () => new NotificationMessage(clientId!, NotificationType.Comment, "Mensaje");

            act.Should().Throw<ArgumentException>().WithParameterName("clientId");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_ConMensajeInvalido_LanzaArgumentException(String? message)
        {
            Action act = () => new NotificationMessage("CLI-1", NotificationType.Comment, message!);

            act.Should().Throw<ArgumentException>().WithParameterName("message");
        }

        [Fact]
        public void Constructor_ConDocumentNumberYTitleNulos_UsaCadenaVacia()
        {
            var sut = new NotificationMessage("CLI-1", NotificationType.Comment, "Mensaje", documentNumber: null!, title: null!);

            sut.DocumentNumber.Should().BeEmpty();
            sut.Title.Should().BeEmpty();
        }

        [Fact]
        public void Constructor_ConFechasPorDefecto_UsaLaFechaActual()
        {
            var sut = new NotificationMessage("CLI-1", NotificationType.Comment, "Mensaje", messageDate: default, notificationDate: default);

            sut.MessageDate.Should().NotBe(default);
            sut.NotificationDate.Should().NotBe(default);
        }

        [Fact]
        public void AssignSequentialId_AsignaElIdentificadorDeNegocio()
        {
            var sut = new NotificationMessage("CLI-1", NotificationType.Comment, "Mensaje");

            sut.AssignSequentialId(99);

            sut.IdNotification.Should().Be(99);
        }

        [Fact]
        public void MarkAsRead_CambiaElEstadoALeida()
        {
            var sut = new NotificationMessage("CLI-1", NotificationType.Comment, "Mensaje");

            sut.MarkAsRead();

            sut.Status.Should().Be(NotificationStatus.Read);
        }

        [Fact]
        public void MarkAsRead_EsIdempotente()
        {
            var sut = new NotificationMessage("CLI-1", NotificationType.Comment, "Mensaje", status: NotificationStatus.Read);

            sut.MarkAsRead();

            sut.Status.Should().Be(NotificationStatus.Read);
        }

        [Fact]
        public void Serializacion_IdaYVuelta_PreservaTodosLosDatos()
        {
            var original = new NotificationMessage(
                "CLI-1", NotificationType.ChangeState, "Mensaje", "HBL-9", "Titulo",
                FechaFija, NotificationStatus.Read, FechaFija.AddMinutes(5), 7, "ID-7");

            var json = JsonSerializer.Serialize(original);
            var copia = JsonSerializer.Deserialize<NotificationMessage>(json);

            copia.Should().NotBeNull();
            copia!.Should().BeEquivalentTo(original);
        }

        [Fact]
        public void Deserializacion_ConJsonMinimo_AplicaValoresPorDefecto()
        {
            var json = "{\"ClientId\":\"CLI-2\",\"Type\":1,\"Message\":\"Hola\"}";

            var sut = JsonSerializer.Deserialize<NotificationMessage>(json);

            sut.Should().NotBeNull();
            sut!.ClientId.Should().Be("CLI-2");
            sut.Type.Should().Be(NotificationType.Comment);
            sut.Message.Should().Be("Hola");
            sut.Status.Should().Be(NotificationStatus.Unread);
            Guid.TryParse(sut.Id, out _).Should().BeTrue();
        }

        [Fact]
        public void Deserializacion_ConFechasEnFormatoInvariante_LasConserva()
        {
            var fecha = FechaFija.ToString("O", CultureInfo.InvariantCulture);
            var json = $"{{\"ClientId\":\"CLI-2\",\"Type\":0,\"Message\":\"Hola\",\"MessageDate\":\"{fecha}\",\"NotificationDate\":\"{fecha}\"}}";

            var sut = JsonSerializer.Deserialize<NotificationMessage>(json);

            sut!.MessageDate.Should().Be(FechaFija);
            sut.NotificationDate.Should().Be(FechaFija);
        }

        [Fact]
        public void Deserializacion_SinClientId_LanzaArgumentException()
        {
            var json = "{\"Type\":0,\"Message\":\"Hola\"}";

            Action act = () => JsonSerializer.Deserialize<NotificationMessage>(json);

            act.Should().Throw<ArgumentException>();
        }
    }
}
