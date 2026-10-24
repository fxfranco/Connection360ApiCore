using Connection360Notification.Domain.Enums;
using Connection360Notification.Infrastructure.Email;
using FluentAssertions;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Adapters
{
    /// <summary>Plantilla HTML del correo (tomada de "Plantilla notificación correo.docx"), sus tres variantes según el tipo de notificación y su logo incrustado.</summary>
    public class ShipmentStatusEmailTemplateTests
    {
        private static ShipmentStatusEmailValues Valores(
            String? cliente = "Ana Perez",
            String? guia = "GUIA-123",
            String? url = "https://conexion360.space",
            DateTime? fecha = null,
            NotificationType tipo = NotificationType.ChangeState,
            String? origen = "Medellin, Colombia",
            String? destino = "Miami, USA",
            String? estadoAnterior = "En transito",
            String? nuevoEstado = "En puerto",
            String? comentario = "Carga lista para despacho",
            DateTime? fechaComentario = null) =>
            new(cliente, guia, tipo, origen, destino, estadoAnterior, nuevoEstado, comentario,
                fechaComentario ?? new DateTime(2026, 10, 20),
                fecha ?? new DateTime(2026, 10, 21, 8, 5, 9, DateTimeKind.Utc),
                url);

        [Fact]
        public void Render_CambioDeEstado_ConservaElTextoFijoDeLaPlantilla()
        {
            String html = ShipmentStatusEmailTemplate.Render(Valores());

            html.Should().Contain("Estimado/a Ana Perez,")
                .And.Contain("Le informamos que el estado de su envío de carga internacional ha sido actualizado en <strong>Conexión360</strong>.")
                .And.Contain("Información del envío:")
                .And.Contain("Número de guía:")
                .And.Contain("Origen:")
                .And.Contain("Destino:")
                .And.Contain("Fecha y hora de actualización:")
                .And.Contain("Puede consultar el detalle y el historial de movimientos de su envío ingresando a Conexión360.")
                .And.Contain("Consultar mi envío:")
                .And.Contain("Esta notificación fue generada automáticamente por Conexión360 con el propósito de mantenerlo informado sobre el avance de su envío.")
                .And.Contain("Cordialmente,")
                .And.Contain("Equipo Operativo");
        }

        [Fact]
        public void Render_CambioDeEstado_MuestraEstadoAnteriorYNuevoEstado_YOcultaComentarioYFechaComentario()
        {
            String html = ShipmentStatusEmailTemplate.Render(Valores(tipo: NotificationType.ChangeState));

            html.Should().Contain("<strong>Estado anterior:</strong> En transito</li>")
                .And.Contain("<strong>Nuevo estado:</strong> En puerto</li>")
                .And.NotContain("<strong>Estado:</strong>")
                .And.NotContain("Comentario:")
                .And.NotContain("Fecha comentario:")
                .And.NotContain("Carga lista para despacho");
        }

        [Fact]
        public void Render_Comentario_MuestraEstadoUnico_Comentario_YFechaComentario_SinEstadoAnteriorNiNuevoEstado()
        {
            String html = ShipmentStatusEmailTemplate.Render(Valores(tipo: NotificationType.Comment));

            html.Should().Contain("Le informamos que un comentario ha sido ingresado en <strong>Conexión360</strong>.")
                .And.NotContain("el estado de su envío de carga internacional ha sido actualizado")
                .And.Contain("<strong>Estado:</strong> En puerto</li>")
                .And.NotContain("Estado anterior:")
                .And.NotContain("Nuevo estado:")
                .And.NotContain("En transito")
                .And.Contain("<strong>Comentario:</strong> Carga lista para despacho</li>")
                .And.Contain("<strong>Fecha comentario:</strong> 20/10/2026</li>");
            html.IndexOf("Fecha comentario:", StringComparison.Ordinal).Should().BeGreaterThan(html.IndexOf("Comentario:", StringComparison.Ordinal));
        }

        [Fact]
        public void Render_OtraInformacion_MuestraEstadoUnico_SinEstadoAnteriorNiNuevoEstadoNiComentario()
        {
            String html = ShipmentStatusEmailTemplate.Render(Valores(tipo: NotificationType.OtherInfo));

            html.Should().Contain("Le informamos que la información de su envío de carga internacional ha sido actualizada en <strong>Conexión360</strong>.")
                .And.NotContain("el estado de su envío de carga internacional ha sido actualizado")
                .And.Contain("<strong>Estado:</strong> En puerto</li>")
                .And.NotContain("Estado anterior:")
                .And.NotContain("Nuevo estado:")
                .And.NotContain("En transito")
                .And.NotContain("Comentario:")
                .And.NotContain("Fecha comentario:")
                .And.NotContain("Carga lista para despacho");
        }

        [Theory]
        [InlineData(NotificationType.ChangeState)]
        [InlineData(NotificationType.Comment)]
        [InlineData(NotificationType.OtherInfo)]
        public void Render_ReemplazaTodosLosCampos_YNoDejaMarcadoresSinResolver(NotificationType tipo)
        {
            String html = ShipmentStatusEmailTemplate.Render(Valores(tipo: tipo));

            html.Should().Contain("GUIA-123")
                .And.Contain("Medellin, Colombia")
                .And.Contain("Miami, USA")
                .And.Contain("21/10/2026 08:05:09 UTC");
            html.Should().NotContain("{{").And.NotContain("}}").And.NotContain("@");
        }

        [Fact]
        public void Render_ConValoresEnBlanco_MuestraUnGuion()
        {
            String html = ShipmentStatusEmailTemplate.Render(Valores(
                tipo: NotificationType.Comment, origen: null, destino: "", estadoAnterior: " ", nuevoEstado: null, comentario: "", fechaComentario: default(DateTime)));

            html.Should().Contain("<strong>Origen:</strong> -</li>")
                .And.Contain("<strong>Destino:</strong> -</li>")
                .And.Contain("<strong>Estado:</strong> -</li>")
                .And.Contain("<strong>Comentario:</strong> -</li>")
                .And.Contain("<strong>Fecha comentario:</strong> -</li>");
        }

        [Fact]
        public void Render_CodificaComoHtmlElComentario()
        {
            String html = ShipmentStatusEmailTemplate.Render(Valores(tipo: NotificationType.Comment, comentario: "<b>hola</b> {{Origen}}"));

            html.Should().Contain("&lt;b&gt;hola&lt;/b&gt; {{Origen}}").And.NotContain("<b>hola</b>");
        }

        [Fact]
        public void FormatCommentDate_SinFechaGuion_SinHoraSoloFecha_ConHoraFechaYHora()
        {
            ShipmentStatusEmailTemplate.FormatCommentDate(default).Should().Be("-");
            ShipmentStatusEmailTemplate.FormatCommentDate(new DateTime(2026, 10, 20)).Should().Be("20/10/2026");
            ShipmentStatusEmailTemplate.FormatCommentDate(new DateTime(2026, 10, 20, 14, 30, 0, DateTimeKind.Utc)).Should().Be("20/10/2026 14:30:00 UTC");
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
