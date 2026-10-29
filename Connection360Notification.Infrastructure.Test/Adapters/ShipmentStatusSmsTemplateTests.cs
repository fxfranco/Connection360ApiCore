using Connection360Notification.Domain.Enums;
using Connection360Notification.Infrastructure.Sms;
using FluentAssertions;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Adapters
{
    /// <summary>Textos del SMS por tipo de notificación (archivo "TextosSMS.txt").</summary>
    public class ShipmentStatusSmsTemplateTests
    {
        private const String Cierre = "“Visibilidad total. Decisiones inteligentes”";

        [Fact]
        public void Render_CambioDeEstado_ReemplazaGuiaYNuevoEstadoYUrl()
        {
            String text = ShipmentStatusSmsTemplate.Render(NotificationType.ChangeState, "HBL-1", "Entregado");

            text.Should().Be(
                "Conexión360\n"
                + "Tu envío internacional con número de guía HBL-1 ha actualizado su estado a Entregado. Consulta los detalles y seguimiento en nuestro sitio web.\n"
                + Cierre);
        }

        [Fact]
        public void Render_OtraInformacion_NoIncluyeElEstado()
        {
            String text = ShipmentStatusSmsTemplate.Render(NotificationType.OtherInfo, "HBL-1", "Entregado");

            text.Should().Be(
                "Conexión360\n"
                + "Tu envío internacional con número de guía HBL-1 ha actualizado su información. Consulta los detalles y seguimiento en nuestro sitio web.\n"
                + Cierre);
            text.Should().NotContain("Entregado");
        }

        [Fact]
        public void Render_Comentario_NoIncluyeElEstado()
        {
            String text = ShipmentStatusSmsTemplate.Render(NotificationType.Comment, "HBL-1", "Entregado");

            text.Should().Be(
                "Conexión360\n"
                + "Tu envío internacional con número de guía HBL-1 se le ha registrado un comentario. Consulta los detalles y seguimiento en nuestro sitio web.\n"
                + Cierre);
            text.Should().NotContain("Entregado");
        }

        [Fact]
        public void Render_ConTipoDesconocido_UsaElTextoDeCambioDeEstado()
        {
            String text = ShipmentStatusSmsTemplate.Render((NotificationType)99, "HBL-1", "En puerto");

            text.Should().Contain("ha actualizado su estado a En puerto");
        }

        [Fact]
        public void Render_TieneTresLineas_MarcaCuerpoYLema()
        {
            String[] lines = ShipmentStatusSmsTemplate.Render(NotificationType.ChangeState, "HBL-1", "X").Split('\n');

            lines.Should().HaveCount(3);
            lines[0].Should().Be(ShipmentStatusSmsTemplate.Brand).And.Be("Conexión360");
            lines[2].Should().Be(ShipmentStatusSmsTemplate.Slogan).And.Be(Cierre);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Render_SinGuiaNiEstado_MuestraUnGuion(String? valor)
        {
            String text = ShipmentStatusSmsTemplate.Render(NotificationType.ChangeState, valor, valor);

            text.Should().Contain("número de guía - ha actualizado su estado a -.");
        }

        [Fact]
        public void Render_QuitaSaltosDeLineaDeLosValoresExternos()
        {
            String text = ShipmentStatusSmsTemplate.Render(NotificationType.ChangeState, "HBL\r\n1", "En\npuerto ");

            text.Should().Contain("guía HBL 1 ha actualizado su estado a En puerto.");
            text.Split('\n').Should().HaveCount(3);
        }
    }
}
