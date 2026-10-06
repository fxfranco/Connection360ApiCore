using Connection360Notification.Application.Enum;
using FluentAssertions;
using Xunit;

namespace Connection360Notification.Application.Tests.Enums
{
    public class ApplicationEnumsTests
    {
        [Fact]
        public void NotificationType_TieneLosValoresDelContrato()
        {
            ((Int32)NotificationType.ChangeState).Should().Be(0);
            ((Int32)NotificationType.Comment).Should().Be(1);
            System.Enum.GetValues<NotificationType>().Should().HaveCount(2);
        }

        [Fact]
        public void NotificationStatus_TieneLosValoresDelContrato()
        {
            ((Int32)NotificationStatus.Unread).Should().Be(0);
            ((Int32)NotificationStatus.Read).Should().Be(1);
            System.Enum.GetValues<NotificationStatus>().Should().HaveCount(2);
        }

        [Fact]
        public void Enums_CoincidenEnNombresConLosDeDominio()
        {
            System.Enum.GetNames<NotificationType>()
                .Should().BeEquivalentTo(System.Enum.GetNames<Connection360Notification.Domain.Enums.NotificationType>());
            System.Enum.GetNames<NotificationStatus>()
                .Should().BeEquivalentTo(System.Enum.GetNames<Connection360Notification.Domain.Enums.NotificationStatus>());
        }
    }
}
