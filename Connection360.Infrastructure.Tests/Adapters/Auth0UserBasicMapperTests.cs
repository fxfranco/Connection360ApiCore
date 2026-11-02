using Auth0.ManagementApi;
using Connection360.Domain.Dtos;
using Connection360.Infrastructure.Adapters.Output;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace Connection360.Infrastructure.Tests.Adapters
{
    /// <summary>
    /// Extracción de id, documento, nombre completo, teléfono y correo de un usuario de Auth0, y asignación del rol (sin red).
    /// </summary>
    public class Auth0UserBasicMapperTests
    {
        private static readonly String[] ValuesUserIdEmailUserMetadat = { "user_id", "email", "user_metadata" };

        private static JsonElement Json(String raw) => JsonDocument.Parse(raw).RootElement.Clone();

        private static UserResponseSchema User(Dictionary<String, Object>? metadata = null)
        {
            return new UserResponseSchema
            {
                UserId = "auth0|abc",
                Email = "ana@correo.com",
                PhoneNumber = "+570000000000",
                Nickname = "ana",
                UserMetadata = metadata
            };
        }

        [Fact]
        public void ToBasicDto_MapeaIdCorreoYLosDatosDeUserMetadata()
        {
            UserResponseSchema user = User(new Dictionary<String, Object>
            {
                ["document"] = "900123456",
                ["fullName"] = "Ana Perez",
                ["phone"] = new Dictionary<String, Object> { ["number"] = "+573113232323", ["country"] = "CO" },
                ["company"] = "Acme"
            });

            Auth0UserBasicDto result = Auth0UserBasicMapper.ToBasicDto(user);

            result.Id.Should().Be("auth0|abc");
            result.Document.Should().Be("900123456");
            result.FullName.Should().Be("Ana Perez");
            result.Phone.Should().Be("+573113232323");
            result.Email.Should().Be("ana@correo.com");
        }

        [Fact]
        public void ToBasicDto_ConMetadataComoJsonElement_LeeLosTresDatos()
        {
            UserResponseSchema user = User(new Dictionary<String, Object>
            {
                ["document"] = Json("\"900123456\""),
                ["fullName"] = Json("\"Ana Perez\""),
                ["phone"] = Json("{\"countryCode\":\"+57\",\"number\":\"3113232323\"}")
            });

            Auth0UserBasicDto result = Auth0UserBasicMapper.ToBasicDto(user);

            result.Document.Should().Be("900123456");
            result.FullName.Should().Be("Ana Perez");
            result.Phone.Should().Be("3113232323");
        }

        [Fact]
        public void ToBasicDto_NoUsaElTelefonoRaizDelUsuario()
        {
            Auth0UserBasicDto result = Auth0UserBasicMapper.ToBasicDto(User(new Dictionary<String, Object> { ["document"] = "1" }));

            result.Phone.Should().BeEmpty("el teléfono sale de user_metadata.phone.number, no de phone_number");
            result.Email.Should().Be("ana@correo.com", "el correo sí es el atributo email del usuario");
        }

        [Fact]
        public void ToBasicDto_SinUserMetadata_DevuelveCadenasVacias()
        {
            Auth0UserBasicDto result = Auth0UserBasicMapper.ToBasicDto(new UserResponseSchema());

            result.Id.Should().BeEmpty();
            result.Document.Should().BeEmpty();
            result.FullName.Should().BeEmpty();
            result.Phone.Should().BeEmpty();
            result.Email.Should().BeEmpty();
        }

        [Fact]
        public void ToBasicDto_ElRolArrancaVacio_PorqueNoVieneEnLaRespuestaDeUsers()
        {
            Auth0UserBasicDto result = Auth0UserBasicMapper.ToBasicDto(User(new Dictionary<String, Object> { ["document"] = "1" }));

            result.RoleName.Should().BeEmpty();
        }

        [Fact]
        public void ApplyRoleName_AsignaElNombreDelRol()
        {
            var user = new Auth0UserBasicDto { Id = "auth0|1" };

            Auth0UserBasicMapper.ApplyRoleName(user, "ADMIN");

            user.RoleName.Should().Be("ADMIN");
        }

        [Fact]
        public void ApplyRoleName_RecortaLosEspacios()
        {
            var user = new Auth0UserBasicDto { Id = "auth0|1" };

            Auth0UserBasicMapper.ApplyRoleName(user, "  CLIENT  ");

            user.RoleName.Should().Be("CLIENT");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ApplyRoleName_SinNombre_DejaElRolVacio(String? roleName)
        {
            var user = new Auth0UserBasicDto { Id = "auth0|1", RoleName = "ANTERIOR" };

            Auth0UserBasicMapper.ApplyRoleName(user, roleName);

            user.RoleName.Should().BeEmpty();
        }

        [Fact]
        public void ApplyRoleName_UsuarioNulo_LanzaArgumentNullException()
        {
            System.Action act = () => Auth0UserBasicMapper.ApplyRoleName(null!, "ADMIN");

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ApplyRoleNames_AsignaACadaUsuarioSuRolPorId()
        {
            var ana = new Auth0UserBasicDto { Id = "auth0|ana" };
            var beto = new Auth0UserBasicDto { Id = "auth0|beto" };
            var carla = new Auth0UserBasicDto { Id = "auth0|carla" };
            var roles = new Dictionary<String, String?>
            {
                ["auth0|ana"] = "ADMIN",
                ["auth0|beto"] = "CLIENT",
                ["auth0|carla"] = null
            };

            Auth0UserBasicMapper.ApplyRoleNames(new[] { ana, beto, carla }, roles);

            ana.RoleName.Should().Be("ADMIN");
            beto.RoleName.Should().Be("CLIENT");
            carla.RoleName.Should().BeEmpty("el usuario no tiene rol");
        }

        [Fact]
        public void ApplyRoleNames_UsuarioSinConsultaDeRol_ConservaSuRolActual()
        {
            var user = new Auth0UserBasicDto { Id = "auth0|1", RoleName = "ANALISTAOPE" };

            Auth0UserBasicMapper.ApplyRoleNames(new[] { user }, new Dictionary<String, String?> { ["auth0|otro"] = "ADMIN" });

            user.RoleName.Should().Be("ANALISTAOPE");
        }

        [Fact]
        public void ApplyRoleNames_ConArgumentosNulos_LanzaArgumentNullException()
        {
            System.Action sinUsuarios = () => Auth0UserBasicMapper.ApplyRoleNames(null!, new Dictionary<String, String?>());
            System.Action sinRoles = () => Auth0UserBasicMapper.ApplyRoleNames(Array.Empty<Auth0UserBasicDto>(), null!);

            sinUsuarios.Should().Throw<ArgumentNullException>();
            sinRoles.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void RequestedFields_SoloPideElIdElCorreoYElUserMetadata()
        {
            Auth0UserBasicMapper.RequestedFields.Split(',').Should().BeEquivalentTo(ValuesUserIdEmailUserMetadat);
        }
    }
}
