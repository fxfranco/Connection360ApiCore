using Connection360.Domain.Dtos;
using Connection360.Infrastructure.Adapters.Output;
using FluentAssertions;
using System.Collections;
using System.Text.Json;
using Xunit;

namespace Connection360.Infrastructure.Tests.Adapters
{
    /// <summary>Armado del cambio de user_metadata (company, document, fullName y phone.number) al actualizar (sin red).</summary>
    public class Auth0UserMetadataUpdaterTests
    {
        private static JsonElement Json(String raw) => JsonDocument.Parse(raw).RootElement.Clone();

        private static Dictionary<String, Object> MetadataActual() => new()
        {
            ["company"] = "Acme SAS",
            ["document"] = "900123456",
            ["fullName"] = "Ana Perez",
            ["phone"] = new Dictionary<String, Object> { ["number"] = "3113232323", ["countryCode"] = "57" },
            ["otro"] = "no se toca"
        };

        private static String Serializar(Object? value) => JsonSerializer.Serialize(value);

        [Fact]
        public void BuildPatch_SinCamposInformados_DevuelveNulo()
        {
            Auth0UserMetadataUpdater.BuildPatch(new Auth0UserDto(), MetadataActual()).Should().BeNull();
        }

        [Fact]
        public void BuildPatch_ConValoresEnBlanco_DevuelveNulo()
        {
            var requested = new Auth0UserDto { Company = " ", Document = "", FullName = "   ", Phone = "" };

            Auth0UserMetadataUpdater.BuildPatch(requested, MetadataActual()).Should().BeNull();
        }

        [Fact]
        public void BuildPatch_ConValoresIgualesALosActuales_DevuelveNulo()
        {
            var requested = new Auth0UserDto { Company = "Acme SAS", Document = "900123456", FullName = " Ana Perez ", Phone = "3113232323" };

            Auth0UserMetadataUpdater.BuildPatch(requested, MetadataActual()).Should().BeNull();
        }

        [Fact]
        public void BuildPatch_SoloCompany_IncluyeSoloCompany()
        {
            Dictionary<String, Object>? patch = Auth0UserMetadataUpdater.BuildPatch(new Auth0UserDto { Company = "Nueva SAS" }, MetadataActual());

            patch.Should().NotBeNull();
            patch!.Should().HaveCount(1);
            patch["company"].Should().Be("Nueva SAS");
        }

        [Fact]
        public void BuildPatch_CompanyDocumentYFullName_IncluyeSoloLosQueCambian()
        {
            var requested = new Auth0UserDto { Company = "Nueva SAS", Document = "900123456", FullName = "Carlos Franco" };

            Dictionary<String, Object>? patch = Auth0UserMetadataUpdater.BuildPatch(requested, MetadataActual());

            patch.Should().NotBeNull();
            patch!.Keys.Should().BeEquivalentTo(new[] { "company", "fullName" });
            patch["company"].Should().Be("Nueva SAS");
            patch["fullName"].Should().Be("Carlos Franco");
        }

        [Fact]
        public void BuildPatch_RecortaLosEspacios()
        {
            Dictionary<String, Object>? patch = Auth0UserMetadataUpdater.BuildPatch(new Auth0UserDto { Document = "  800456  " }, MetadataActual());

            patch!["document"].Should().Be("800456");
        }

        [Fact]
        public void BuildPatch_ClaveExistenteConOtraCapitalizacion_ReutilizaLaClave()
        {
            var current = new Dictionary<String, Object> { ["FullName"] = "Ana Perez" };

            Dictionary<String, Object>? patch = Auth0UserMetadataUpdater.BuildPatch(new Auth0UserDto { FullName = "Carlos Franco" }, current);

            patch!.Keys.Should().BeEquivalentTo(new[] { "FullName" });
            patch["FullName"].Should().Be("Carlos Franco");
        }

        [Fact]
        public void BuildPatch_MetadataNulo_CreaLasClavesCanonicas()
        {
            var requested = new Auth0UserDto { Company = "Acme", Document = "1", FullName = "Ana", Phone = "300" };

            Dictionary<String, Object>? patch = Auth0UserMetadataUpdater.BuildPatch(requested, null);

            patch!.Keys.Should().BeEquivalentTo(new[] { "company", "document", "fullName", "phone" });
            Serializar(patch["phone"]).Should().Be("{\"number\":\"300\"}");
        }

        [Fact]
        public void BuildPatch_TelefonoCambiado_ReenviaElObjetoPhoneConservandoLasDemasPropiedades()
        {
            Dictionary<String, Object>? patch = Auth0UserMetadataUpdater.BuildPatch(new Auth0UserDto { Phone = "3000000000" }, MetadataActual());

            patch!.Keys.Should().BeEquivalentTo(new[] { "phone" });
            using JsonDocument json = JsonDocument.Parse(Serializar(patch["phone"]));
            json.RootElement.GetProperty("number").GetString().Should().Be("3000000000");
            json.RootElement.GetProperty("countryCode").GetString().Should().Be("57");
        }

        [Fact]
        public void BuildPatch_TelefonoActualComoJsonElement_ConservaLasDemasPropiedades()
        {
            var current = new Dictionary<String, Object>
            {
                ["phone"] = Json("{\"number\":\"3113232323\",\"countryCode\":\"57\",\"verified\":true}")
            };

            Dictionary<String, Object>? patch = Auth0UserMetadataUpdater.BuildPatch(new Auth0UserDto { Phone = "3000000000" }, current);

            using JsonDocument json = JsonDocument.Parse(Serializar(patch!["phone"]));
            json.RootElement.GetProperty("number").GetString().Should().Be("3000000000");
            json.RootElement.GetProperty("countryCode").GetString().Should().Be("57");
            json.RootElement.GetProperty("verified").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public void BuildPatch_TelefonoIgualAlActualEnJsonElement_DevuelveNulo()
        {
            var current = new Dictionary<String, Object> { ["phone"] = Json("{\"number\":\"3113232323\"}") };

            Auth0UserMetadataUpdater.BuildPatch(new Auth0UserDto { Phone = "3113232323" }, current).Should().BeNull();
        }

        [Fact]
        public void BuildPatch_SinPhoneEnElMetadata_CreaElObjetoConNumber()
        {
            var current = new Dictionary<String, Object> { ["company"] = "Acme" };

            Dictionary<String, Object>? patch = Auth0UserMetadataUpdater.BuildPatch(new Auth0UserDto { Phone = "3000000000" }, current);

            patch!.Keys.Should().BeEquivalentTo(new[] { "phone" });
            Serializar(patch["phone"]).Should().Be("{\"number\":\"3000000000\"}");
        }

        [Fact]
        public void BuildPatch_PhoneActualComoTextoSimple_LoReemplazaPorUnObjeto()
        {
            var current = new Dictionary<String, Object> { ["phone"] = "3113232323" };

            Dictionary<String, Object>? patch = Auth0UserMetadataUpdater.BuildPatch(new Auth0UserDto { Phone = "3000000000" }, current);

            Serializar(patch!["phone"]).Should().Be("{\"number\":\"3000000000\"}");
        }

        [Fact]
        public void BuildPatch_PhoneActualComoHashtable_ConservaLasDemasPropiedades()
        {
            var phone = new Hashtable { ["number"] = "3113232323", ["countryCode"] = "57" };
            var current = new Dictionary<String, Object> { ["phone"] = phone };

            Dictionary<String, Object>? patch = Auth0UserMetadataUpdater.BuildPatch(new Auth0UserDto { Phone = "3000000000" }, current);

            using JsonDocument json = JsonDocument.Parse(Serializar(patch!["phone"]));
            json.RootElement.GetProperty("number").GetString().Should().Be("3000000000");
            json.RootElement.GetProperty("countryCode").GetString().Should().Be("57");
        }

        [Fact]
        public void BuildPatch_NumberConOtraCapitalizacion_ReutilizaLaClave()
        {
            var current = new Dictionary<String, Object>
            {
                ["Phone"] = new Dictionary<String, Object> { ["Number"] = "3113232323" }
            };

            Dictionary<String, Object>? patch = Auth0UserMetadataUpdater.BuildPatch(new Auth0UserDto { Phone = "3000000000" }, current);

            patch!.Keys.Should().BeEquivalentTo(new[] { "Phone" });
            Serializar(patch["Phone"]).Should().Be("{\"Number\":\"3000000000\"}");
        }

        [Fact]
        public void BuildPatch_NoModificaElMetadataActual()
        {
            Dictionary<String, Object> current = MetadataActual();

            Auth0UserMetadataUpdater.BuildPatch(new Auth0UserDto { Company = "Otra", Phone = "3000000000" }, current);

            current["company"].Should().Be("Acme SAS");
            ((Dictionary<String, Object>)current["phone"])["number"].Should().Be("3113232323");
        }

        [Fact]
        public void BuildPatch_TodosLosCamposCambian_IncluyeLosCuatro()
        {
            var requested = new Auth0UserDto { Company = "Nueva", Document = "777", FullName = "Carlos Franco", Phone = "3009998888" };

            Dictionary<String, Object>? patch = Auth0UserMetadataUpdater.BuildPatch(requested, MetadataActual());

            patch!.Keys.Should().BeEquivalentTo(new[] { "company", "document", "fullName", "phone" });
            patch.Should().NotContainKey("otro");
        }
    }
}
