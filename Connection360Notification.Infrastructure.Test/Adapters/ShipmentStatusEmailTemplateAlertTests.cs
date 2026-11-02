using Connection360Notification.Domain.Enums;
using Connection360Notification.Infrastructure.Email;
using FluentAssertions;
using System.Net;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Adapters
{
    /// <summary>Variante de la plantilla del correo para alertas del sistema (NotificationType.Alert).</summary>
    public class ShipmentStatusEmailTemplateAlertTests
    {
        private const String Mensaje = "Se generó un error 'api caida' en el proceso de ETL";

        private static ShipmentStatusEmailValues Alerta(String? cliente = "Ana Admin", String? mensaje = Mensaje, NotificationType tipo = NotificationType.Alert) =>
            new(cliente, null, tipo, null, null, null, null, null, default,
                new DateTime(2026, 10, 21, 8, 5, 9, DateTimeKind.Utc), "https://conexion360.space", mensaje);

        [Fact]
        public void Render_Alerta_MantieneElSaludoConElNombreDelUsuario()
        {
            ShipmentStatusEmailTemplate.Render(Alerta()).Should().Contain("Estimado/a Ana Admin,");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Render_AlertaSinUsuario_DejaElSaludoSinNombre(String? cliente)
        {
            String html = ShipmentStatusEmailTemplate.Render(Alerta(cliente));

            html.Should().Contain("Estimado/a,").And.NotContain("Estimado/a cliente");
        }

        [Fact]
        public void Render_Alerta_InformaLaAlertaDeLaEtlConElMensaje()
        {
            String html = ShipmentStatusEmailTemplate.Render(Alerta());

            // El mensaje se codifica como HTML (comillas y letras acentuadas se vuelven entidades); el texto fijo conserva sus tildes.
            html.Should().Contain($"Le informamos que se ha generado una alerta en el proceso de sincronización de la ETL con el error: {WebUtility.HtmlEncode(Mensaje)}.");
            html.Should().Contain("&#39;api caida&#39;");
        }

        [Fact]
        public void Render_Alerta_NoMuestraLaMarcaDeConexion360EnLaIntroduccion()
        {
            String html = ShipmentStatusEmailTemplate.Render(Alerta());

            html.Should().NotContain("<strong>Conexión360</strong>");
        }

        [Fact]
        public void Render_Alerta_OcultaLaInformacionDelEnvioYElEnlace()
        {
            String html = ShipmentStatusEmailTemplate.Render(Alerta());

            html.Should().NotContain("Información del envío")
                .And.NotContain("Número de guía")
                .And.NotContain("Origen:")
                .And.NotContain("Destino:")
                .And.NotContain("Estado anterior")
                .And.NotContain("Nuevo estado")
                .And.NotContain("Estado:")
                .And.NotContain("Comentario:")
                .And.NotContain("Fecha y hora de actualización")
                .And.NotContain("Puede consultar el detalle")
                .And.NotContain("Consultar mi envío")
                .And.NotContain("https://conexion360.space");
        }

        [Fact]
        public void Render_Alerta_MuestraSoloLaFraseDeNotificacionAutomatica()
        {
            String html = ShipmentStatusEmailTemplate.Render(Alerta());

            html.Should().Contain("Esta notificación fue generada automáticamente por Conexión360.</p>")
                .And.NotContain("con el propósito de mantenerlo informado");
        }

        [Fact]
        public void Render_Alerta_ConservaLaDespedidaYElLogo()
        {
            String html = ShipmentStatusEmailTemplate.Render(Alerta());

            html.Should().Contain("Cordialmente,")
                .And.Contain("Equipo Operativo")
                .And.Contain($"src=\"cid:{ShipmentStatusEmailTemplate.LogoContentId}\"");
        }

        [Fact]
        public void Render_Alerta_CodificaElMensajeComoHtml()
        {
            String html = ShipmentStatusEmailTemplate.Render(Alerta(mensaje: "<script>alert(1)</script> & más"));

            html.Should().Contain("&lt;script&gt;alert(1)&lt;/script&gt; &amp; m&#225;s").And.NotContain("<script>");
        }

        [Fact]
        public void Render_Alerta_CodificaElNombreDelUsuario()
        {
            ShipmentStatusEmailTemplate.Render(Alerta(cliente: "<b>Ana</b>")).Should().Contain("Estimado/a &lt;b&gt;Ana&lt;/b&gt;,");
        }

        [Fact]
        public void Render_Alerta_ElMensajeConLlavesNoSeVuelveAExpandir()
        {
            String html = ShipmentStatusEmailTemplate.Render(Alerta(mensaje: "error {{NumeroGuia}} y {{UrlConexion360}}"));

            html.Should().Contain("error {{NumeroGuia}} y {{UrlConexion360}}.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("   ")]
        public void Render_AlertaSinMensaje_MuestraUnGuion(String? mensaje)
        {
            ShipmentStatusEmailTemplate.Render(Alerta(mensaje: mensaje)).Should().Contain("con el error: -.");
        }

        [Fact]
        public void Render_Alerta_NoDejaMarcadoresSinReemplazar()
        {
            ShipmentStatusEmailTemplate.Render(Alerta()).Should().NotContain("{{").And.NotContain("}}");
        }

        [Theory]
        [InlineData(NotificationType.ChangeState)]
        [InlineData(NotificationType.Comment)]
        [InlineData(NotificationType.OtherInfo)]
        public void Render_NotificacionDeCliente_IgnoraElMensajeYConservaSuFormato(NotificationType tipo)
        {
            String html = ShipmentStatusEmailTemplate.Render(Alerta(tipo: tipo));

            html.Should().Contain("Estimado/a Ana Admin,")
                .And.Contain("<strong>Conexión360</strong>.")
                .And.Contain("Información del envío")
                .And.Contain("Consultar mi envío:")
                .And.Contain("con el propósito de mantenerlo informado sobre el avance de su envío.")
                .And.NotContain("alerta en el proceso");
        }
    }
}
