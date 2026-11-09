using Connection360Notification.Domain.Enums;
using Connection360Notification.Infrastructure.Persistence.Mongo;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Persistence
{
    public class NotificationDocumentTests
    {
        private static readonly DateTime Fecha = new(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc);

        [Fact]
        public void PorDefecto_TieneCadenasVaciasYValoresIniciales()
        {
            var sut = new NotificationDocument();

            sut.DocumentNumber.Should().BeEmpty();
            sut.Title.Should().BeEmpty();
            sut.IdNotification.Should().Be(0);
            sut.Type.Should().Be(NotificationType.ChangeState);
            sut.Status.Should().Be(NotificationStatus.Unread);
            sut.NotificationChannel.Should().NotBeNull();
            sut.NotificationChannel.Application.Should().BeFalse();
            sut.NotificationChannel.Email.Should().BeFalse();
            sut.NotificationChannel.TextMessages.Should().BeFalse();
        }

        [Fact]
        public void ABson_ElTipoOtherInfoSeGuardaComoTextoYSeLeeDeVuelta()
        {
            var sut = new NotificationDocument { Id = "GUID-3", ClientId = "CLI-1", Type = NotificationType.OtherInfo, Message = "M" };

            var bson = sut.ToBsonDocument();
            var leido = BsonSerializer.Deserialize<NotificationDocument>(bson);

            bson["Type"].AsString.Should().Be("OtherInfo");
            leido.Type.Should().Be(NotificationType.OtherInfo);
        }

        [Fact]
        public void ABson_UsaIdComoClaveYEnumsComoTexto()
        {
            var sut = new NotificationDocument
            {
                Id = "GUID-1", IdNotification = 3, ClientId = "CLI-1", Type = NotificationType.Comment,
                DocumentNumber = "HBL-1", Title = "T", Message = "M", MessageDate = Fecha,
                Status = NotificationStatus.Read, NotificationDate = Fecha
            };

            var bson = sut.ToBsonDocument();

            bson["_id"].AsString.Should().Be("GUID-1");
            bson.Contains("Id").Should().BeFalse();
            bson["Type"].AsString.Should().Be("Comment");
            bson["Status"].AsString.Should().Be("Read");
            bson["IdNotification"].ToInt64().Should().Be(3);
            bson["ClientId"].AsString.Should().Be("CLI-1");
        }

        [Fact]
        public void ABson_GuardaNotificationChannelComoSubdocumentoConLosTresIndicadores()
        {
            var sut = new NotificationDocument
            {
                Id = "GUID-3", ClientId = "CLI-3", Message = "M",
                NotificationChannel = new NotificationChannelDocument { Application = true, Email = false, TextMessages = true }
            };

            var bson = sut.ToBsonDocument();

            var canales = bson["NotificationChannel"].AsBsonDocument;
            canales["Application"].AsBoolean.Should().BeTrue();
            canales["Email"].AsBoolean.Should().BeFalse();
            canales["TextMessages"].AsBoolean.Should().BeTrue();
        }

        [Fact]
        public void DesdeBson_DocumentoAntiguoSinNotificationChannel_QuedaConTodosLosCanalesEnFalse()
        {
            var antiguo = new BsonDocument
            {
                { "_id", "GUID-OLD" }, { "IdNotification", 1L }, { "ClientId", "CLI-1" }, { "Type", "Comment" },
                { "DocumentNumber", "" }, { "Title", "" }, { "Message", "M" },
                { "MessageDate", Fecha }, { "Status", "Unread" }, { "NotificationDate", Fecha }
            };

            var sut = BsonSerializer.Deserialize<NotificationDocument>(antiguo);

            sut.NotificationChannel.Should().NotBeNull();
            sut.NotificationChannel.Application.Should().BeFalse();
            sut.NotificationChannel.Email.Should().BeFalse();
            sut.NotificationChannel.TextMessages.Should().BeFalse();
        }

        [Fact]
        public void BsonIdaYVuelta_PreservaTodosLosCampos()
        {
            var original = new NotificationDocument
            {
                Id = "GUID-2", IdNotification = 8, ClientId = "CLI-2", Type = NotificationType.ChangeState,
                DocumentNumber = "HBL-2", Title = "Titulo", Message = "Mensaje", MessageDate = Fecha,
                Status = NotificationStatus.Unread, NotificationDate = Fecha.AddDays(1),
                NotificationChannel = new NotificationChannelDocument { Email = true }
            };

            var copia = BsonSerializer.Deserialize<NotificationDocument>(original.ToBson());

            copia.Should().BeEquivalentTo(original);
        }
    }
}
