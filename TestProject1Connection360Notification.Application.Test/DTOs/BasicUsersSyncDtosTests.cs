using Connection360Notification.Application.DTOs;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace Connection360Notification.Application.Tests.DTOs
{
    public class BasicUsersSyncDtosTests
    {
        private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

        [Fact]
        public void BasicUsersSyncMessage_PorDefecto_TieneListaDeUsuariosVacia()
        {
            var sut = new BasicUsersSyncMessage();

            sut.SyncId.Should().Be(Guid.Empty);
            sut.BatchIndex.Should().Be(0);
            sut.BatchCount.Should().Be(0);
            sut.TotalUsers.Should().Be(0);
            sut.Users.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void BasicUserItem_PorDefecto_TieneCadenasVacias()
        {
            var sut = new BasicUserItem();

            sut.Id.Should().BeEmpty();
            sut.Document.Should().BeEmpty();
            sut.FullName.Should().BeEmpty();
            sut.Phone.Should().BeEmpty();
            sut.Email.Should().BeEmpty();
            sut.RoleName.Should().BeEmpty();
        }

        [Fact]
        public void BasicUsersSyncMessage_SeDeserializaDesdeElJsonEnCamelCaseDelApi()
        {
            Guid syncId = Guid.NewGuid();
            String json = "{\"syncId\":\"" + syncId + "\",\"batchIndex\":1,\"batchCount\":3,\"totalUsers\":5,\"generatedAtUtc\":\"2024-05-06T07:08:09Z\","
                + "\"users\":[{\"id\":\"auth0|1\",\"document\":\"900\",\"fullName\":\"Ana Perez\",\"phone\":\"+57300\",\"email\":\"a@b.com\"}]}";

            BasicUsersSyncMessage? message = JsonSerializer.Deserialize<BasicUsersSyncMessage>(json, WebOptions);

            message.Should().NotBeNull();
            message!.SyncId.Should().Be(syncId);
            message.BatchIndex.Should().Be(1);
            message.BatchCount.Should().Be(3);
            message.TotalUsers.Should().Be(5);
            message.GeneratedAtUtc.Should().Be(new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc));
            message.Users.Should().ContainSingle().Which.Phone.Should().Be("+57300");
            message.Users[0].Document.Should().Be("900");
            message.Users[0].FullName.Should().Be("Ana Perez");
            message.Users[0].Email.Should().Be("a@b.com");
        }

        [Fact]
        public void BasicUsersSyncMessage_ConRoleName_SeDeserializaElRolDelUsuario()
        {
            String json = "{\"syncId\":\"" + Guid.NewGuid() + "\",\"batchIndex\":0,\"batchCount\":1,\"totalUsers\":2,\"generatedAtUtc\":\"2024-05-06T07:08:09Z\","
                + "\"users\":[{\"id\":\"auth0|1\",\"document\":\"900\",\"fullName\":\"Ana Perez\",\"phone\":\"+57300\",\"email\":\"a@b.com\",\"roleName\":\"ADMIN\"},"
                + "{\"id\":\"auth0|2\",\"document\":\"901\",\"fullName\":\"Beto\",\"phone\":\"+57301\",\"email\":\"b@b.com\",\"roleName\":\"\"}]}";

            BasicUsersSyncMessage? message = JsonSerializer.Deserialize<BasicUsersSyncMessage>(json, WebOptions);

            message!.Users.Select(u => u.RoleName).Should().Equal("ADMIN", String.Empty);
        }

        [Fact]
        public void BasicUsersSyncMessage_DeUnaVersionAnteriorSinRoleName_DejaElRolVacio()
        {
            String json = "{\"syncId\":\"" + Guid.NewGuid() + "\",\"batchIndex\":0,\"batchCount\":1,\"totalUsers\":1,\"generatedAtUtc\":\"2024-05-06T07:08:09Z\","
                + "\"users\":[{\"id\":\"auth0|1\",\"document\":\"900\",\"fullName\":\"Ana Perez\",\"phone\":\"+57300\",\"email\":\"a@b.com\"}]}";

            BasicUsersSyncMessage? message = JsonSerializer.Deserialize<BasicUsersSyncMessage>(json, WebOptions);

            message!.Users.Should().ContainSingle().Which.RoleName.Should().BeEmpty();
        }

        [Fact]
        public void BasicUsersSyncResult_EsUnRecordConIgualdadPorValor()
        {
            var a = new BasicUsersSyncResult(1, 2, true, true, 3, 4);
            var b = new BasicUsersSyncResult(1, 2, true, true, 3, 4);

            a.Should().Be(b);
            a.Upserted.Should().Be(1);
            a.Discarded.Should().Be(2);
            a.IsLastBatch.Should().BeTrue();
            a.CleanupPerformed.Should().BeTrue();
            a.Deleted.Should().Be(3);
            a.StoredInSync.Should().Be(4);
        }
    }
}
