using Connection360Notification.Infrastructure.Persistence.Mongo;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Persistence
{
    /// <summary>
    /// Forma en que el documento de la colección basicUserData se guarda y se lee en MongoDB, en particular el
    /// campo RoleName agregado después de que ya existieran documentos.
    /// </summary>
    public class BasicUserDataDocumentTests
    {
        [Fact]
        public void RoleName_PorDefecto_EsVacio()
        {
            new BasicUserDataDocument().RoleName.Should().BeEmpty();
        }

        [Fact]
        public void AlSerializarABson_IncluyeElCampoRoleName()
        {
            var document = new BasicUserDataDocument { Id = "auth0|1", Document = "900", FullName = "Ana", Phone = "3", Email = "a@b.c", RoleName = "ADMIN", SyncId = "s" };

            BsonDocument bson = document.ToBsonDocument();

            bson["_id"].AsString.Should().Be("auth0|1");
            bson["RoleName"].AsString.Should().Be("ADMIN");
        }

        [Fact]
        public void AlDeserializarDesdeBson_LeeElRoleName()
        {
            var bson = new BsonDocument
            {
                { "_id", "auth0|1" }, { "Document", "900" }, { "FullName", "Ana" }, { "Phone", "3" }, { "Email", "a@b.c" },
                { "RoleName", "CLIENT" }, { "SyncId", "s" }, { "UpdatedAtUtc", new BsonDateTime(DateTime.UtcNow) }
            };

            BasicUserDataDocument document = BsonSerializer.Deserialize<BasicUserDataDocument>(bson);

            document.RoleName.Should().Be("CLIENT");
        }

        [Fact]
        public void DocumentoGuardadoAntesDelCampoRoleName_SeLeeConElRolVacio()
        {
            var legacy = new BsonDocument
            {
                { "_id", "auth0|1" }, { "Document", "900" }, { "FullName", "Ana" }, { "Phone", "3" }, { "Email", "a@b.c" },
                { "SyncId", "s" }, { "UpdatedAtUtc", new BsonDateTime(DateTime.UtcNow) }
            };

            BasicUserDataDocument document = BsonSerializer.Deserialize<BasicUserDataDocument>(legacy);

            document.Id.Should().Be("auth0|1");
            document.RoleName.Should().BeEmpty();
        }
    }
}
