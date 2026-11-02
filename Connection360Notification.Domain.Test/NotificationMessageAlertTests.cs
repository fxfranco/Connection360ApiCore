using System.Text.Json;
using System.Text.Json.Serialization;
using Connection360Notification.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Connection360Notification.Domain.Tests
{
    /// <summary>Las alertas del sistema (NotificationType.Alert) no pertenecen a ningún cliente.</summary>
    public class NotificationMessageAlertTests
    {
        private static readonly JsonSerializerOptions Opciones = new() { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Constructor_AlertaSinClientId_EsValida(String? clientId)
        {
            var sut = new NotificationMessage(clientId!, NotificationType.Alert, "Se generó un error");

            sut.ClientId.Should().BeEmpty();
            sut.EventType.Should().Be(NotificationType.Alert);
        }

        [Theory]
        [InlineData(NotificationType.ChangeState)]
        [InlineData(NotificationType.Comment)]
        [InlineData(NotificationType.OtherInfo)]
        public void Constructor_TiposDeClienteSinClientId_SiguenLanzandoArgumentException(NotificationType tipo)
        {
            Action act = () => _ = new NotificationMessage("", tipo, "Mensaje");

            act.Should().Throw<ArgumentException>().WithParameterName("clientId");
        }

        [Fact]
        public void Constructor_AlertaSinMensaje_SigueLanzandoArgumentException()
        {
            Action act = () => _ = new NotificationMessage("", NotificationType.Alert, "  ");

            act.Should().Throw<ArgumentException>().WithParameterName("message");
        }

        [Fact]
        public void Constructor_AlertaConClientId_ConservaElClientId()
        {
            new NotificationMessage("CLI-1", NotificationType.Alert, "m").ClientId.Should().Be("CLI-1");
        }

        private static NotificationMessage AlertaCompleta() => new(
            "", NotificationType.Alert, "Se generó un error", documentNumber: "DOC-1", title: "Alerta en proceso de ETL",
            messageDate: new DateTime(2026, 10, 4, 8, 0, 0), status: NotificationStatus.Unread,
            notificationDate: new DateTime(2026, 10, 4, 8, 0, 1), idNotification: 77, id: "ORIGINAL",
            notificationChannel: new NotificationChannelPreferences { Application = true, Email = true, TextMessages = true },
            origen: "o", destino: "d", comentario: "c", fechaComentario: new DateTime(2026, 10, 3), estadoAnterior: "ea", nuevoEstado: "ne");

        [Fact]
        public void CopyForClient_EnUnaAlerta_CreaUnaNotificacionNuevaConElClienteRecortado()
        {
            NotificationMessage original = AlertaCompleta();

            NotificationMessage copia = original.CopyForClient("  900123 ");

            copia.Should().NotBeSameAs(original);
            copia.ClientId.Should().Be("900123");
            copia.Id.Should().NotBe(original.Id).And.NotBeNullOrWhiteSpace();
            copia.IdNotification.Should().Be(0, "el repositorio le asigna su propio consecutivo");
        }

        [Fact]
        public void CopyForClient_ConservaElContenidoLasFechasElEstadoYLosCanales()
        {
            NotificationMessage original = AlertaCompleta();

            NotificationMessage copia = original.CopyForClient("900123");

            copia.EventType.Should().Be(NotificationType.Alert);
            copia.Message.Should().Be(original.Message);
            copia.Title.Should().Be(original.Title);
            copia.DocumentNumber.Should().Be("DOC-1");
            copia.MessageDate.Should().Be(original.MessageDate);
            copia.NotificationDate.Should().Be(original.NotificationDate);
            copia.Status.Should().Be(NotificationStatus.Unread);
            copia.Origen.Should().Be("o");
            copia.Destino.Should().Be("d");
            copia.Comentario.Should().Be("c");
            copia.FechaComentario.Should().Be(new DateTime(2026, 10, 3));
            copia.EstadoAnterior.Should().Be("ea");
            copia.NuevoEstado.Should().Be("ne");
            copia.NotificationChannel.Application.Should().BeTrue();
            copia.NotificationChannel.Email.Should().BeTrue();
            copia.NotificationChannel.TextMessages.Should().BeTrue();
        }

        [Fact]
        public void CopyForClient_NoComparteLosCanalesNiModificaLaOriginal()
        {
            NotificationMessage original = AlertaCompleta();

            NotificationMessage copia = original.CopyForClient("900123");
            copia.NotificationChannel.Email = false;

            original.NotificationChannel.Email.Should().BeTrue();
            original.ClientId.Should().BeEmpty();
            original.Id.Should().Be("ORIGINAL");
            original.IdNotification.Should().Be(77);
        }

        [Fact]
        public void CopyForClient_CadaLlamadaGeneraUnIdDistinto()
        {
            NotificationMessage original = AlertaCompleta();

            original.CopyForClient("1").Id.Should().NotBe(original.CopyForClient("2").Id);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void CopyForClient_ConValorEnBlanco_DejaElClienteVacio(String? valor)
        {
            AlertaCompleta().CopyForClient(valor).ClientId.Should().BeEmpty();
        }

        [Theory]
        [InlineData(NotificationType.ChangeState)]
        [InlineData(NotificationType.Comment)]
        [InlineData(NotificationType.OtherInfo)]
        public void CopyForClient_EnUnaNotificacionDeCliente_LanzaInvalidOperationException(NotificationType tipo)
        {
            var sut = new NotificationMessage("CLI-1", tipo, "m");

            Action act = () => sut.CopyForClient("OTRO");

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Deserializar_PayloadDeAlertaDelEtl_SeEnlazaConCamposVacios()
        {
            // Forma exacta que escribe el ETL (EtlOutboxPayload): solo Title, Message y MessageDate llenos y los tres canales en true.
            var json = "{\"ClientId\":\"\",\"EventType\":\"Alert\",\"DocumentNumber\":\"\",\"Title\":\"Alerta en proceso de ETL\","
                     + "\"Message\":\"Se generó un error 'api caida' en el proceso de ETL\",\"MessageDate\":\"2026-10-04T08:00:00\","
                     + "\"Origen\":\"\",\"Destino\":\"\",\"Comentario\":\"\",\"FechaComentario\":\"0001-01-01T00:00:00\","
                     + "\"EstadoAnterior\":\"\",\"NuevoEstado\":\"\","
                     + "\"NotificationChannel\":{\"Application\":true,\"Email\":true,\"TextMessages\":true}}";

            var sut = JsonSerializer.Deserialize<NotificationMessage>(json, Opciones);

            sut.Should().NotBeNull();
            sut!.EventType.Should().Be(NotificationType.Alert);
            sut.ClientId.Should().BeEmpty();
            sut.Title.Should().Be("Alerta en proceso de ETL");
            sut.Message.Should().Be("Se generó un error 'api caida' en el proceso de ETL");
            sut.MessageDate.Should().Be(new DateTime(2026, 10, 4, 8, 0, 0));
            sut.DocumentNumber.Should().BeEmpty();
            sut.NotificationChannel.Application.Should().BeTrue();
            sut.NotificationChannel.Email.Should().BeTrue();
            sut.NotificationChannel.TextMessages.Should().BeTrue();
            sut.NotificationChannel.GetEnabledChannels().Should().HaveCount(3);
        }
    }
}
