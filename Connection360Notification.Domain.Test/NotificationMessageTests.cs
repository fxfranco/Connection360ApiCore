using System.Globalization;
using System.Text.Json;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Enums;
using Connection360Notification.Domain.Time;
using FluentAssertions;
using Xunit;

namespace Connection360Notification.Domain.Tests
{
    public class NotificationMessageTests
    {
        private static readonly JsonSerializerOptions Opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };

        private static readonly DateTime FechaFija = new(2024, 5, 17, 10, 30, 0, DateTimeKind.Utc);

        [Fact]
        public void Constructor_ConTodosLosDatos_AsignaCadaPropiedad()
        {
            var sut = new NotificationMessage(
                clientId: "CLI-1",
                eventType: NotificationType.Comment,
                message: "Mensaje",
                documentNumber: "HBL-1",
                title: "Titulo",
                messageDate: FechaFija,
                status: NotificationStatus.Read,
                notificationDate: FechaFija.AddHours(1),
                idNotification: 42,
                id: "ID-FIJO",
                origen: "Medellin, Colombia",
                destino: "Miami, USA",
                comentario: "Carga lista",
                fechaComentario: FechaFija.Date,
                estadoAnterior: "En transito",
                nuevoEstado: "En puerto");

            sut.Id.Should().Be("ID-FIJO");
            sut.IdNotification.Should().Be(42);
            sut.ClientId.Should().Be("CLI-1");
            sut.EventType.Should().Be(NotificationType.Comment);
            sut.Message.Should().Be("Mensaje");
            sut.DocumentNumber.Should().Be("HBL-1");
            sut.Title.Should().Be("Titulo");
            sut.MessageDate.Should().Be(FechaFija);
            sut.Status.Should().Be(NotificationStatus.Read);
            sut.NotificationDate.Should().Be(FechaFija.AddHours(1));
            sut.Origen.Should().Be("Medellin, Colombia");
            sut.Destino.Should().Be("Miami, USA");
            sut.Comentario.Should().Be("Carga lista");
            sut.FechaComentario.Should().Be(FechaFija.Date);
            sut.EstadoAnterior.Should().Be("En transito");
            sut.NuevoEstado.Should().Be("En puerto");
        }

        [Fact]
        public void Constructor_SoloConDatosObligatorios_AplicaValoresPorDefecto()
        {
            var antes = ColombiaClock.Now;

            var sut = new NotificationMessage("CLI-1", NotificationType.ChangeState, "Mensaje");

            var despues = ColombiaClock.Now;
            sut.DocumentNumber.Should().BeEmpty();
            sut.Title.Should().BeEmpty();
            sut.Origen.Should().BeEmpty();
            sut.Destino.Should().BeEmpty();
            sut.Comentario.Should().BeEmpty();
            sut.FechaComentario.Should().Be(default(DateTime));
            sut.EstadoAnterior.Should().BeEmpty();
            sut.NuevoEstado.Should().BeEmpty();
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
            Action act = () => _ = new NotificationMessage(clientId!, NotificationType.Comment, "Mensaje");

            act.Should().Throw<ArgumentException>().WithParameterName(nameof(clientId));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_ConMensajeInvalido_LanzaArgumentException(String? message)
        {
            Action act = () => _ = new NotificationMessage("CLI-1", NotificationType.Comment, message!);

            act.Should().Throw<ArgumentException>().WithParameterName(nameof(message));
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
        public void Constructor_SinCanales_NotificationChannelNuncaEsNulaYNoTieneCanalesHabilitados()
        {
            var sut = new NotificationMessage("CLI-1", NotificationType.Comment, "Mensaje");

            sut.NotificationChannel.Should().NotBeNull();
            sut.NotificationChannel.HasAnyEnabled.Should().BeFalse();
        }

        [Fact]
        public void Constructor_ConCanales_ConservaLaConfiguracionRecibida()
        {
            var canales = new NotificationChannelPreferences { Email = true, TextMessages = true };

            var sut = new NotificationMessage("CLI-1", NotificationType.Comment, "Mensaje", notificationChannel: canales);

            sut.NotificationChannel.Should().BeSameAs(canales);
        }

        [Fact]
        public void Serializacion_IdaYVuelta_PreservaLosCanales()
        {
            var original = new NotificationMessage(
                "CLI-1", NotificationType.ChangeState, "Mensaje",
                notificationChannel: new NotificationChannelPreferences { Application = true, Email = true });

            var copia = JsonSerializer.Deserialize<NotificationMessage>(JsonSerializer.Serialize(original));

            copia!.NotificationChannel.Application.Should().BeTrue();
            copia.NotificationChannel.Email.Should().BeTrue();
            copia.NotificationChannel.TextMessages.Should().BeFalse();
        }

        [Fact]
        public void Deserializacion_ConNotificationChannelDelPayload_CapturaLosTresIndicadores()
        {
            var json = "{\"ClientId\":\"CLI-2\",\"Message\":\"Hola\",\"NotificationChannel\":{\"Application\":false,\"Email\":true,\"TextMessages\":true}}";

            var sut = JsonSerializer.Deserialize<NotificationMessage>(json);

            sut!.NotificationChannel.Application.Should().BeFalse();
            sut.NotificationChannel.Email.Should().BeTrue();
            sut.NotificationChannel.TextMessages.Should().BeTrue();
        }

        [Fact]
        public void Deserializacion_SinNotificationChannel_QuedaSinCanalesHabilitadosYNoEsNula()
        {
            var sut = JsonSerializer.Deserialize<NotificationMessage>("{\"ClientId\":\"CLI-2\",\"Message\":\"Hola\"}");

            sut!.NotificationChannel.Should().NotBeNull();
            sut.NotificationChannel.HasAnyEnabled.Should().BeFalse();
        }

        [Fact]
        public void Deserializacion_ConNotificationChannelNulo_QuedaSinCanalesHabilitadosYNoEsNula()
        {
            var sut = JsonSerializer.Deserialize<NotificationMessage>("{\"ClientId\":\"CLI-2\",\"Message\":\"Hola\",\"NotificationChannel\":null}");

            sut!.NotificationChannel.Should().NotBeNull();
            sut.NotificationChannel.HasAnyEnabled.Should().BeFalse();
        }

        [Fact]
        public void Deserializacion_ConNotificationChannelVacio_QuedaSinCanalesHabilitados()
        {
            var sut = JsonSerializer.Deserialize<NotificationMessage>("{\"ClientId\":\"CLI-2\",\"Message\":\"Hola\",\"NotificationChannel\":{}}");

            sut!.NotificationChannel.HasAnyEnabled.Should().BeFalse();
        }

        [Fact]
        public void Serializacion_ExponeElTipoDeEventoComoEventType()
        {
            var json = JsonSerializer.Serialize(new NotificationMessage("CLI-1", NotificationType.Comment, "Mensaje"));

            using var doc = JsonDocument.Parse(json);
            doc.RootElement.TryGetProperty("EventType", out var valor).Should().BeTrue();
            valor.GetInt32().Should().Be((Int32)NotificationType.Comment);
            doc.RootElement.TryGetProperty("Type", out _).Should().BeFalse();
        }

        [Fact]
        public void Deserializacion_ConLosNuevosCamposDelPayloadDelEtl_LosCapturaSinErrores()
        {
            var json = "{\"ClientId\":\"CLI-2\",\"EventType\":\"OtherInfo\",\"DocumentNumber\":\"HBL-9\",\"Title\":\"t\",\"Message\":\"Hola\","
                + "\"MessageDate\":\"2026-10-21T15:30:05Z\",\"Origen\":\"Medellin\",\"Destino\":\"Miami\",\"Comentario\":\"Listo\","
                + "\"FechaComentario\":\"2026-10-20T00:00:00\",\"EstadoAnterior\":\"En transito\",\"NuevoEstado\":\"En puerto\"}";
            var opciones = Opciones;

            var sut = JsonSerializer.Deserialize<NotificationMessage>(json, opciones);

            sut!.EventType.Should().Be(NotificationType.OtherInfo);
            sut.Origen.Should().Be("Medellin");
            sut.Destino.Should().Be("Miami");
            sut.Comentario.Should().Be("Listo");
            sut.FechaComentario.Should().Be(new DateTime(2026, 10, 20));
            sut.EstadoAnterior.Should().Be("En transito");
            sut.NuevoEstado.Should().Be("En puerto");
        }

        [Fact]
        public void Deserializacion_SinLosNuevosCampos_QuedanVacios()
        {
            var sut = JsonSerializer.Deserialize<NotificationMessage>("{\"ClientId\":\"CLI-2\",\"Message\":\"Hola\"}");

            sut!.Origen.Should().BeEmpty();
            sut.Destino.Should().BeEmpty();
            sut.Comentario.Should().BeEmpty();
            sut.FechaComentario.Should().Be(default(DateTime));
            sut.EstadoAnterior.Should().BeEmpty();
            sut.NuevoEstado.Should().BeEmpty();
        }

        [Fact]
        public void Serializacion_IdaYVuelta_PreservaLosNuevosCampos()
        {
            var original = new NotificationMessage("CLI-1", NotificationType.Comment, "Mensaje",
                origen: "Medellin", destino: "Miami", comentario: "Listo", fechaComentario: FechaFija.Date,
                estadoAnterior: "A", nuevoEstado: "B");

            var copia = JsonSerializer.Deserialize<NotificationMessage>(JsonSerializer.Serialize(original));

            copia!.Origen.Should().Be("Medellin");
            copia.Destino.Should().Be("Miami");
            copia.Comentario.Should().Be("Listo");
            copia.FechaComentario.Should().Be(FechaFija.Date);
            copia.EstadoAnterior.Should().Be("A");
            copia.NuevoEstado.Should().Be("B");
        }

        [Fact]
        public void Deserializacion_ConJsonMinimo_AplicaValoresPorDefecto()
        {
            var json = "{\"ClientId\":\"CLI-2\",\"EventType\":1,\"Message\":\"Hola\"}";

            var sut = JsonSerializer.Deserialize<NotificationMessage>(json);

            sut.Should().NotBeNull();
            sut!.ClientId.Should().Be("CLI-2");
            sut.EventType.Should().Be(NotificationType.Comment);
            sut.Message.Should().Be("Hola");
            sut.Status.Should().Be(NotificationStatus.Unread);
            Guid.TryParse(sut.Id, out _).Should().BeTrue();
        }

        [Fact]
        public void Deserializacion_ConFechasEnFormatoInvariante_LasConserva()
        {
            var fecha = FechaFija.ToString("O", CultureInfo.InvariantCulture);
            var json = $"{{\"ClientId\":\"CLI-2\",\"EventType\":0,\"Message\":\"Hola\",\"MessageDate\":\"{fecha}\",\"NotificationDate\":\"{fecha}\"}}";

            var sut = JsonSerializer.Deserialize<NotificationMessage>(json);

            sut!.MessageDate.Should().Be(FechaFija);
            sut.NotificationDate.Should().Be(FechaFija);
        }

        [Fact]
        public void Deserializacion_SinClientId_LanzaArgumentException()
        {
            var json = "{\"EventType\":0,\"Message\":\"Hola\"}";

            Action act = () => JsonSerializer.Deserialize<NotificationMessage>(json);

            act.Should().Throw<ArgumentException>();
        }
    }
}
