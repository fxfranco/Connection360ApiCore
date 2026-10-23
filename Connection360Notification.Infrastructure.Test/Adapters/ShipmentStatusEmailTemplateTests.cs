using Connection360Notification.Infrastructure.Email;
using FluentAssertions;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Adapters
{
    /// <summary>Plantilla HTML del correo de cambio de estado (tomada de "Plantilla notificación correo.docx") y su logo incrustado.</summary>
    public class ShipmentStatusEmailTemplateTests
    {
        private static ShipmentStatusEmailValues Valores(
            String? cliente = "Ana Perez",
            String? guia = "GUIA-123",
            String? url = "https://conexion360.space",
            DateTime? fecha = null) =>
            new(cliente, guia,
                ShipmentStatusEmailTemplate.PendingOrigin,
                ShipmentStatusEmailTemplate.PendingDestination,
                ShipmentStatusEmailTemplate.PendingPreviousStatus,
                ShipmentStatusEmailTemplate.PendingNewStatus,
                ShipmentStatusEmailTemplate.PendingComment,
                fecha ?? new DateTime(2026, 10, 21, 8, 5, 9, DateTimeKind.Utc),
                url);

        [Fact]
        public void Render_ConservaElTextoFijoDeLaPlantilla()
        {
            String html = ShipmentStatusEmailTemplate.Render(Valores());

            html.Should().Contain("Estimado/a Ana Perez,")
                .And.Contain("Le informamos que el estado de su envío de carga internacional ha sido actualizado en <strong>Conexion360</strong>.")
                .And.Contain("Información del envío:")
                .And.Contain("Número de guía:")
                .And.Contain("Origen:")
                .And.Contain("Destino:")
                .And.Contain("Estado anterior:")
                .And.Contain("Nuevo estado:")
                .And.Contain("Comentario:")
                .And.Contain("Fecha y hora de actualización:")
                .And.Contain("Puede consultar el detalle y el historial de movimientos de su envío ingresando a Conexion360.")
                .And.Contain("Consultar mi envío:")
                .And.Contain("Esta notificación fue generada automáticamente por Conexion360 con el propósito de mantenerlo informado sobre el avance de su envío.")
                .And.Contain("Cordialmente,")
                .And.Contain("Equipo Operativo");
        }

        [Fact]
        public void Render_ReemplazaTodosLosCampos_YNoDejaMarcadoresSinResolver()
        {
            String html = ShipmentStatusEmailTemplate.Render(Valores());

            html.Should().Contain("GUIA-123")
                .And.Contain("ORIGENPENDIENTE")
                .And.Contain("DESTINOPENDIENTE")
                .And.Contain("ESTADOANTERIORPENDIENTE")
                .And.Contain("ESTADONUEVOPENDIENTE")
                .And.Contain("COMENTARIOPENDIENTE")
                .And.Contain("21/10/2026 08:05:09 UTC");
            html.Should().NotContain("{{").And.NotContain("}}").And.NotContain("@");
        }

        [Fact]
        public void Render_ElLogoSeReferenciaPorContentId()
        {
            ShipmentStatusEmailTemplate.Render(Valores()).Should().Contain("src=\"cid:logoConexion360\"");
            ShipmentStatusEmailTemplate.LogoContentId.Should().Be("logoConexion360");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Render_SinNombre_SaludaComoCliente(String? nombre)
        {
            ShipmentStatusEmailTemplate.Render(Valores(cliente: nombre)).Should().Contain("Estimado/a cliente,");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Render_SinNumeroDeGuia_MuestraUnGuion(String? guia)
        {
            ShipmentStatusEmailTemplate.Render(Valores(guia: guia)).Should().Contain("<strong>Número de guía:</strong> -</li>");
        }

        [Fact]
        public void Render_CodificaComoHtmlLosValoresVariables()
        {
            String html = ShipmentStatusEmailTemplate.Render(Valores(cliente: "<img src=x onerror=alert(1)>", guia: "A&B \"q\""));

            html.Should().NotContain("<img src=x").And.Contain("&lt;img src=x onerror=alert(1)&gt;");
            html.Should().Contain("A&amp;B &quot;q&quot;");
        }

        [Fact]
        public void Render_UnValorConMarcadoresNoSeVuelveAExpandir()
        {
            String html = ShipmentStatusEmailTemplate.Render(Valores(cliente: "{{NumeroGuia}}", guia: "GUIA-123"));

            html.Should().Contain("Estimado/a {{NumeroGuia}},");
        }

        [Fact]
        public void Render_ConUrlHttp_GeneraElEnlaceConLaPropiaUrlComoTexto()
        {
            String html = ShipmentStatusEmailTemplate.Render(Valores(url: " https://conexion360.space/envios?a=1&b=2 "));

            html.Should().Contain("<a href=\"https://conexion360.space/envios?a=1&amp;b=2\"")
                .And.Contain(">https://conexion360.space/envios?a=1&amp;b=2</a>");
        }

        [Theory]
        [InlineData("javascript:alert(1)")]
        [InlineData("conexion360.space")]
        [InlineData("ftp://servidor/archivo")]
        public void Render_ConUrlQueNoEsHttp_NoGeneraEnlace(String url)
        {
            String html = ShipmentStatusEmailTemplate.Render(Valores(url: url));

            html.Should().NotContain("<a href");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Render_SinUrl_NoGeneraEnlaceNiFalla(String? url)
        {
            String html = ShipmentStatusEmailTemplate.Render(Valores(url: url));

            html.Should().NotContain("<a href").And.Contain("Consultar mi envío:");
        }

        [Fact]
        public void Render_ConValoresNulos_LanzaArgumentNullException()
        {
            Action act = () => ShipmentStatusEmailTemplate.Render(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void FormatDate_UtcAgregaElSufijoYLasDemasNo()
        {
            ShipmentStatusEmailTemplate.FormatDate(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)).Should().Be("02/01/2026 03:04:05 UTC");
            ShipmentStatusEmailTemplate.FormatDate(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Unspecified)).Should().Be("02/01/2026 03:04:05");
        }

        [Fact]
        public void GetLogo_DevuelveElPngIncrustado()
        {
            Byte[] logo = ShipmentStatusEmailTemplate.GetLogo();

            logo.Length.Should().BeGreaterThan(1000);
            logo.Take(8).Should().Equal(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A);
            ShipmentStatusEmailTemplate.LogoMediaType.Should().Be("image/png");
        }
    }
}
