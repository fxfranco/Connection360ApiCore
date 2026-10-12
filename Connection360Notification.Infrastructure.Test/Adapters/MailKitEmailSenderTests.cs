using Connection360Notification.Infrastructure.Email;
using FluentAssertions;
using Microsoft.Extensions.Options;
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
    }
}
