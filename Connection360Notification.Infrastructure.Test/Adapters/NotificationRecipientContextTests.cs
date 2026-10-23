using Connection360Notification.Application.Services;
using Connection360Notification.Domain;
using Connection360Notification.Infrastructure.Adapters.Output;
using FluentAssertions;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Adapters
{
    public class NotificationRecipientContextTests
    {
        [Fact]
        public void AlInicio_NoHayDestinatario()
        {
            new NotificationRecipientContext().Recipient.Should().BeNull();
        }

        [Fact]
        public void Set_GuardaElDestinatario_YConNuloLoLimpia()
        {
            INotificationRecipientContext sut = new NotificationRecipientContext();
            var usuario = new BasicUserData("auth0|1", "900123", "Ana Perez", "3001112233", "ana@correo.com");

            sut.Set(usuario);
            sut.Recipient.Should().Be(usuario);

            sut.Set(null);
            sut.Recipient.Should().BeNull();
        }
    }
}
