using Connection360Notification.Domain.Enums;
using Connection360Notification.Infrastructure.Sms;
using FluentAssertions;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Adapters
{
    /// <summary>Texto del SMS para alertas del sistema (NotificationType.Alert).</summary>
    public class ShipmentStatusSmsTemplateAlertTests
    {
        private const String Cierre = "“Visibilidad total. Decisiones inteligentes”";

        [Fact]
        public void Render_Alerta_UsaElTextoDeAlertaConElMensajeYSinEnlace()
        {
            String text = ShipmentStatusSmsTemplate.Render(NotificationType.Alert, "", "", "Se generó un error 'api caida' en el proceso de ETL");

            text.Should().Be(
                "Conexión360\n"
                + "Alerta en el proceso de sincronización de la ETL: Se generó un error 'api caida' en el proceso de ETL\n"
                + Cierre);
        }

        [Fact]
        public void Render_Alerta_NoIncluyeElEnlaceNiLosDatosDelEnvio()
        {
            String text = ShipmentStatusSmsTemplate.Render(NotificationType.Alert, "HBL-1", "Entregado", "falla");

            text.Should().NotContain("nuestro sitio web").And.NotContain("HBL-1").And.NotContain("Entregado").And.NotContain("Consulta los detalles");
        }

        [Fact]
        public void Render_Alerta_ConservaLaEstructuraDeTresLineas()
        {
            String[] lines = ShipmentStatusSmsTemplate.Render(NotificationType.Alert, null, null, "falla").Split('\n');

            lines.Should().HaveCount(3);
            lines[0].Should().Be(ShipmentStatusSmsTemplate.Brand);
            lines[2].Should().Be(ShipmentStatusSmsTemplate.Slogan);
        }

        [Fact]
        public void Render_AlertaConSaltosDeLinea_LosQuitaParaNoPartirElTexto()
        {
            String[] lines = ShipmentStatusSmsTemplate.Render(NotificationType.Alert, null, null, "linea uno\r\n  linea dos\n").Split('\n');

            lines.Should().HaveCount(3);
            lines[1].Should().Be("Alerta en el proceso de sincronización de la ETL: linea uno linea dos");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Render_AlertaSinMensaje_MuestraUnGuion(String? mensaje)
        {
            ShipmentStatusSmsTemplate.Render(NotificationType.Alert, null, null, mensaje).Should().Contain("Alerta en el proceso de sincronización de la ETL: -");
        }

        [Theory]
        [InlineData(NotificationType.ChangeState)]
        [InlineData(NotificationType.OtherInfo)]
        [InlineData(NotificationType.Comment)]
        public void Render_NotificacionDeCliente_IgnoraElMensajeYSigueUsandoElEnlace(NotificationType tipo)
        {
            String text = ShipmentStatusSmsTemplate.Render(tipo, "HBL-1", "Entregado", "texto que no debe aparecer");

            text.Should().Contain("nuestro sitio web.").And.NotContain("texto que no debe aparecer").And.NotContain("Alerta en el proceso");
        }
    }
}
