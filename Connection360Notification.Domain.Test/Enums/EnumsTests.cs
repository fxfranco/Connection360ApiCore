using Connection360Notification.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Connection360Notification.Domain.Tests.Enums
{
    public class EnumsTests
    {
        [Fact]
        public void NotificationStatus_TieneLosValoresEsperados()
        {
            ((Int32)NotificationStatus.Unread).Should().Be(0);
            ((Int32)NotificationStatus.Read).Should().Be(1);
            Enum.GetValues<NotificationStatus>().Should().HaveCount(2);
        }

        [Fact]
        public void NotificationType_TieneLosValoresEsperados()
        {
            ((Int32)NotificationType.ChangeState).Should().Be(0);
            ((Int32)NotificationType.Comment).Should().Be(1);
            Enum.GetValues<NotificationType>().Should().HaveCount(2);
        }

        [Theory]
        [InlineData("ChangeState", NotificationType.ChangeState)]
        [InlineData("Comment", NotificationType.Comment)]
        public void NotificationType_SeParseaPorNombre(String nombre, NotificationType esperado)
        {
            Enum.Parse<NotificationType>(nombre).Should().Be(esperado);
        }

        [Theory]
        [InlineData("Unread", NotificationStatus.Unread)]
        [InlineData("Read", NotificationStatus.Read)]
        public void NotificationStatus_SeParseaPorNombre(String nombre, NotificationStatus esperado)
        {
            Enum.Parse<NotificationStatus>(nombre).Should().Be(esperado);
        }
    }
}
