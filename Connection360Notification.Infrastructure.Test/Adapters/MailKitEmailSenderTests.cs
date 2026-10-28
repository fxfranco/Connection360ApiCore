using Connection360Notification.Infrastructure.Email;
using FluentAssertions;
using Microsoft.Extensions.Options;
using MimeKit;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Adapters
{
    /// <summary>
    /// El envío real requiere un servidor SMTP, por eso aquí solo se cubre el contrato y las validaciones previas
    /// a la conexión; la lógica del canal se prueba con IEmailSender simulado (EmailNotificationChannelTests).
    /// </summary>
    public class MailKitEmailSenderTests
    {
        [Fact]
        public void EsUnIEmailSender()
        {
            new MailKitEmailSender(Options.Create(new SmtpSettings())).Should().BeAssignableTo<IEmailSender>();
        }

        [Fact]
        public async Task SendAsync_ConMensajeNulo_LanzaArgumentNullException()
        {
            var sut = new MailKitEmailSender(Options.Create(new SmtpSettings()));

            Func<Task> act = () => sut.SendAsync(null!, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public void EmailMessage_ConservaSusValores()
        {
            var sut = new EmailMessage("a@b.com", "Asunto", "Cuerpo");

            sut.To.Should().Be("a@b.com");
            sut.Subject.Should().Be("Asunto");
            sut.Body.Should().Be("Cuerpo");
        }

        private static MailKitEmailSender Crear() => new(Options.Create(new SmtpSettings { SenderName = "Conexion360App", SenderEmail = "remitente@gmail.com" }));

        [Fact]
        public void EmailMessage_SinHtml_NoTraeHtmlNiImagen()
        {
            var sut = new EmailMessage("a@b.com", "Asunto", "Cuerpo");

            sut.HtmlBody.Should().BeNull();
            sut.InlineImage.Should().BeNull();
        }

        [Fact]
        public void CreateMimeMessage_SinHtml_EsTextoPlano()
        {
            MimeMessage mime = Crear().CreateMimeMessage(new EmailMessage("destino@ejemplo.com", "Asunto", "Cuerpo <b>x</b>"));

            mime.Subject.Should().Be("Asunto");
            mime.To.Mailboxes.Should().ContainSingle().Which.Address.Should().Be("destino@ejemplo.com");
            mime.From.Mailboxes.Should().ContainSingle().Which.Address.Should().Be("remitente@gmail.com");
            mime.Body.Should().BeAssignableTo<TextPart>().Which.Text.Should().Be("Cuerpo <b>x</b>");
            mime.HtmlBody.Should().BeNull();
        }

        [Fact]
        public void CreateMimeMessage_ConHtml_EnviaTextoYHtml()
        {
            MimeMessage mime = Crear().CreateMimeMessage(new EmailMessage("destino@ejemplo.com", "Asunto", "Texto", "<p>Hola</p>"));

            mime.TextBody.Should().Be("Texto");
            mime.HtmlBody.Should().Be("<p>Hola</p>");
            mime.Body.ContentType.MimeType.Should().Be("multipart/alternative");
        }

        [Fact]
        public void CreateMimeMessage_ConImagenIncrustada_LaEnlazaPorContentId()
        {
            Byte[] png = { 0x89, 0x50, 0x4E, 0x47 };
            var correo = new EmailMessage("destino@ejemplo.com", "Asunto", "Texto", "<img src=\"cid:logo\" />", new EmailInlineImage("logo", png, "image/png"));

            MimeMessage mime = Crear().CreateMimeMessage(correo);

            mime.HtmlBody.Should().Contain("cid:logo");
            MimePart imagen = mime.BodyParts.OfType<MimePart>().Single(p => p.ContentType.MimeType == "image/png");
            imagen.ContentId.Should().Be("logo");
            using var memoria = new MemoryStream();
            imagen.Content.DecodeTo(memoria);
            memoria.ToArray().Should().Equal(png);
        }

        [Fact]
        public void CreateMimeMessage_ConMensajeNulo_LanzaArgumentNullException()
        {
            Action act = () => Crear().CreateMimeMessage(null!);

            act.Should().Throw<ArgumentNullException>();
        }
    }
}
