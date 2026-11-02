using Connection360.Domain.Dtos;
using Connection360.Infrastructure.Messaging;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace Connection360.Infrastructure.Tests.Messaging
{
    /// <summary>
    /// Fija el contrato JSON (camelCase) del mensaje de sincronización de usuarios, que consume
    /// Connection360Notification (BasicUsersSyncMessage / BasicUserItem). Si se renombra o quita una propiedad aquí,
    /// el consumidor dejaría de recibirla sin ningún error de compilación; estas pruebas lo detectan.
    /// </summary>
    public class Auth0UsersSyncMessageContractTests
    {
        private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

        private static Auth0UsersSyncMessage Message(params Auth0UserBasicDto[] users) => new()
        {
            SyncId = Guid.NewGuid(),
            BatchIndex = 0,
            BatchCount = 1,
            TotalUsers = users.Length,
            GeneratedAtUtc = new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc),
            Users = users.ToList()
        };

        [Fact]
        public void Usuario_SeSerializaConExactamenteLasPropiedadesQueConsumeNotification()
        {
            var user = new Auth0UserBasicDto
            {
                Id = "auth0|1",
                Document = "900123",
                FullName = "Ana Perez",
                Phone = "+573001112233",
                Email = "ana@correo.com",
                RoleName = "ADMIN"
            };

            using JsonDocument json = JsonDocument.Parse(JsonSerializer.Serialize(Message(user), Web));
            JsonElement serializedUser = json.RootElement.GetProperty("users")[0];

            serializedUser.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(new[] { "id", "document", "fullName", "phone", "email", "roleName" });
            serializedUser.GetProperty("id").GetString().Should().Be("auth0|1");
            serializedUser.GetProperty("document").GetString().Should().Be("900123");
            serializedUser.GetProperty("fullName").GetString().Should().Be("Ana Perez");
            serializedUser.GetProperty("phone").GetString().Should().Be("+573001112233");
            serializedUser.GetProperty("email").GetString().Should().Be("ana@correo.com");
            serializedUser.GetProperty("roleName").GetString().Should().Be("ADMIN");
        }

        [Fact]
        public void Lote_SeSerializaConLasPropiedadesDeSincronizacionEnCamelCase()
        {
            using JsonDocument json = JsonDocument.Parse(JsonSerializer.Serialize(Message(new Auth0UserBasicDto { Id = "auth0|1" }), Web));

            json.RootElement.EnumerateObject().Select(p => p.Name)
                .Should().BeEquivalentTo(new[] { "syncId", "batchIndex", "batchCount", "totalUsers", "generatedAtUtc", "users" });
        }

        [Fact]
        public void Usuario_ConRol_VuelveAIgualDespuesDeSerializarYDeserializar()
        {
            var user = new Auth0UserBasicDto { Id = "auth0|1", Document = "9", FullName = "Ana", Phone = "3", Email = "a@b.c", RoleName = "ANALISTAOPE" };

            Auth0UsersSyncMessage roundTrip = JsonSerializer.Deserialize<Auth0UsersSyncMessage>(JsonSerializer.Serialize(Message(user), Web), Web)!;

            roundTrip.Users.Should().ContainSingle().Which.Should().BeEquivalentTo(user);
        }

        [Fact]
        public void Usuario_PorDefecto_TieneElRolVacio()
        {
            new Auth0UserBasicDto().RoleName.Should().BeEmpty();
        }
    }
}
