using Connection360.Domain.Dtos;
using Connection360.Infrastructure.Adapters.Output;
using FluentAssertions;
using System.Collections;
using System.Text.Json;
using Xunit;

namespace Connection360.Infrastructure.Tests.Adapters
{
    /// <summary>Lectura de company, document, fullName y phone.number desde user_metadata (sin red).</summary>
    public class Auth0UserMetadataReaderTests
    {
        private static JsonElement Json(String raw) => JsonDocument.Parse(raw).RootElement.Clone();

        [Fact]
        public void Apply_LlenaCompanyDocumentFullNameYPhone()
        {
            var user = new Auth0UserDto { PhoneNumber = "+570000000000" };
            var metadata = new Dictionary<String, Object>
            {
                ["company"] = "Acme SAS",
                ["document"] = "900123456",
                ["fullName"] = "Ana Perez",
                ["phone"] = new Dictionary<String, Object> { ["number"] = "+573113232323" }
            };

            Auth0UserMetadataReader.Apply(user, metadata);

            user.Company.Should().Be("Acme SAS");
            user.Document.Should().Be("900123456");
            user.FullName.Should().Be("Ana Perez");
            user.Phone.Should().Be("+573113232323");
            user.PhoneNumber.Should().Be("+570000000000", "phone_number de Auth0 no se toca");
        }

        [Fact]
        public void Apply_ConMetadataNula_DejaTodoEnNulo()
        {
            var user = new Auth0UserDto();

            Auth0UserMetadataReader.Apply(user, null);

            user.Company.Should().BeNull();
            user.Document.Should().BeNull();
            user.FullName.Should().BeNull();
            user.Phone.Should().BeNull();
        }

        [Fact]
        public void Apply_ConMetadataVacia_DejaTodoEnNulo()
        {
            var user = new Auth0UserDto();

            Auth0UserMetadataReader.Apply(user, new Dictionary<String, Object>());

            user.Company.Should().BeNull();
            user.Phone.Should().BeNull();
        }

        [Theory]
        [InlineData("fullName")]
        [InlineData("FULLNAME")]
        [InlineData("fullname")]
        public void GetString_NoDistingueMayusculasEnLaClave(String key)
        {
            var metadata = new Dictionary<String, Object> { [key] = "Ana" };

            Auth0UserMetadataReader.GetString(metadata, "fullName").Should().Be("Ana");
        }

        [Fact]
        public void GetString_ConJsonElement_LeeTextoNumeroYBooleano()
        {
            var metadata = new Dictionary<String, Object>
            {
                ["texto"] = Json("\"  abc  \""),
                ["numero"] = Json("800555"),
                ["verdadero"] = Json("true"),
                ["nulo"] = Json("null")
            };

            Auth0UserMetadataReader.GetString(metadata, "texto").Should().Be("abc");
            Auth0UserMetadataReader.GetString(metadata, "numero").Should().Be("800555");
            Auth0UserMetadataReader.GetString(metadata, "verdadero").Should().Be("True");
            Auth0UserMetadataReader.GetString(metadata, "nulo").Should().BeNull();
        }

        [Fact]
        public void GetString_ConNumeroNativo_LoConvierteConCulturaInvariante()
        {
            var metadata = new Dictionary<String, Object> { ["document"] = 900123456 };

            Auth0UserMetadataReader.GetString(metadata, "document").Should().Be("900123456");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void GetString_ConTextoVacio_DevuelveNulo(String value)
        {
            var metadata = new Dictionary<String, Object> { ["company"] = value };

            Auth0UserMetadataReader.GetString(metadata, "company").Should().BeNull();
        }

        [Fact]
        public void GetString_ConClaveInexistenteOMetadataNula_DevuelveNulo()
        {
            Auth0UserMetadataReader.GetString(new Dictionary<String, Object> { ["otra"] = "x" }, "company").Should().BeNull();
            Auth0UserMetadataReader.GetString(null, "company").Should().BeNull();
        }

        [Fact]
        public void GetPhoneNumber_ConObjetoJson_LeeLaPropiedadNumber()
        {
            var metadata = new Dictionary<String, Object> { ["phone"] = Json("{\"countryCode\":\"+57\",\"number\":\"3113232323\"}") };

            Auth0UserMetadataReader.GetPhoneNumber(metadata).Should().Be("3113232323");
        }

        [Fact]
        public void GetPhoneNumber_ConObjetoJsonYNumberNumerico_LoDevuelveComoTexto()
        {
            var metadata = new Dictionary<String, Object> { ["phone"] = Json("{\"number\":3113232323}") };

            Auth0UserMetadataReader.GetPhoneNumber(metadata).Should().Be("3113232323");
        }

        [Fact]
        public void GetPhoneNumber_NoDistingueMayusculasEnPhoneNiEnNumber()
        {
            var metadata = new Dictionary<String, Object> { ["Phone"] = Json("{\"Number\":\"3113232323\"}") };

            Auth0UserMetadataReader.GetPhoneNumber(metadata).Should().Be("3113232323");
        }

        [Fact]
        public void GetPhoneNumber_ConDiccionarioGenerico_LeeLaClaveNumber()
        {
            var metadata = new Dictionary<String, Object> { ["phone"] = new Dictionary<String, Object> { ["NUMBER"] = "+573001112233" } };

            Auth0UserMetadataReader.GetPhoneNumber(metadata).Should().Be("+573001112233");
        }

        [Fact]
        public void GetPhoneNumber_ConDiccionarioNoGenerico_LeeLaClaveNumber()
        {
            var metadata = new Dictionary<String, Object> { ["phone"] = new Hashtable { ["number"] = "+573001112233", [1] = "ignorada" } };

            Auth0UserMetadataReader.GetPhoneNumber(metadata).Should().Be("+573001112233");
        }

        [Fact]
        public void GetPhoneNumber_ConPhoneComoTextoSimple_LoDevuelveTalCual()
        {
            var metadata = new Dictionary<String, Object> { ["phone"] = "+573001112233" };

            Auth0UserMetadataReader.GetPhoneNumber(metadata).Should().Be("+573001112233");
        }

        [Fact]
        public void GetPhoneNumber_SinNumberEnElObjeto_DevuelveNulo()
        {
            Auth0UserMetadataReader.GetPhoneNumber(new Dictionary<String, Object> { ["phone"] = Json("{\"countryCode\":\"+57\"}") }).Should().BeNull();
            Auth0UserMetadataReader.GetPhoneNumber(new Dictionary<String, Object> { ["phone"] = new Dictionary<String, Object>() }).Should().BeNull();
            Auth0UserMetadataReader.GetPhoneNumber(new Dictionary<String, Object> { ["phone"] = Json("{\"number\":null}") }).Should().BeNull();
        }

        [Fact]
        public void GetPhoneNumber_SinPhoneOMetadataNula_DevuelveNulo()
        {
            Auth0UserMetadataReader.GetPhoneNumber(new Dictionary<String, Object> { ["otra"] = "x" }).Should().BeNull();
            Auth0UserMetadataReader.GetPhoneNumber(null).Should().BeNull();
        }

        [Fact]
        public void GetPhoneNumber_ConPhoneNuloEnJson_DevuelveNulo()
        {
            Auth0UserMetadataReader.GetPhoneNumber(new Dictionary<String, Object> { ["phone"] = Json("null") }).Should().BeNull();
        }

        [Fact]
        public void Claves_SonLasEsperadas()
        {
            Auth0UserMetadataReader.CompanyKey.Should().Be("company");
            Auth0UserMetadataReader.DocumentKey.Should().Be("document");
            Auth0UserMetadataReader.FullNameKey.Should().Be("fullName");
            Auth0UserMetadataReader.PhoneKey.Should().Be("phone");
            Auth0UserMetadataReader.PhoneNumberKey.Should().Be("number");
        }
    }
}
