using Connection360Notification.Domain.Ports.Outbound;
using FluentAssertions;
using Moq;
using Xunit;

namespace Connection360Notification.Domain.Tests
{
    /// <summary>
    /// Verifica que los puertos de salida del dominio son interfaces simulables y que su
    /// contrato (firmas) permite a la aplicacion usarlos sin conocer la infraestructura.
    /// </summary>
    public class PortsContractTests
    {
        [Fact]
        public async Task INotificationIdGenerator_PermiteGenerarIdsSecuenciales()
        {
            var mock = new Mock<INotificationIdGenerator>();
            mock.Setup(g => g.NextIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(5);

            var id = await mock.Object.NextIdAsync(CancellationToken.None);

            id.Should().Be(5);
        }

        [Fact]
        public async Task INotificationRepository_PermiteMarcarComoLeida()
        {
            var mock = new Mock<INotificationRepository>();
            mock.Setup(r => r.MarkAsReadAsync("C", 1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            (await mock.Object.MarkAsReadAsync("C", 1, CancellationToken.None)).Should().BeTrue();
        }

        [Fact]
        public async Task IKafkaProducerService_PermiteProducirNotificaciones()
        {
            var mock = new Mock<IKafkaProducerService>();
            var n = new NotificationMessage("C", Connection360Notification.Domain.Enums.NotificationType.Comment, "m");

            await mock.Object.ProduceNotificationAsync(n, CancellationToken.None);

            mock.Verify(k => k.ProduceNotificationAsync(n, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
