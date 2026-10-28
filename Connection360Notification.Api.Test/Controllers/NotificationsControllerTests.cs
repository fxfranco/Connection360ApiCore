using Connection360Notification.Api.Controllers;
using Connection360Notification.Application.DTOs;
using Connection360Notification.Application.Ports;
using Connection360Notification.Application.Ports.Inbound;
using Connection360Notification.Application.Ports.Output;
using Connection360Notification.Domain.Ports.Outbound;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Connection360Notification.Api.Tests.Controllers
{
    public class NotificationsControllerTests
    {
        private readonly Mock<IGetNotificationsUseCase> _useCaseMock = new();
        private readonly Mock<INotificationUseCase> _notificationUseCaseMock = new();
        private readonly NotificationsController _sut;

        public NotificationsControllerTests()
        {
            _sut = new NotificationsController(_useCaseMock.Object, _notificationUseCaseMock.Object);
        }

        [Fact]
        public async Task GetAllNotifications_RetornaOkConLaListaDelUseCase()
        {
            var expected = new List<NotificationsListResponse> { new() { IdNotification = 1 } };
            _useCaseMock.Setup(u => u.ExecuteGetNotificationsAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);

            IActionResult result = await _sut.GetAllNotificationsDB(CancellationToken.None);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeSameAs(expected);
        }

        [Fact]
        public async Task UpdateReadNotification_CuandoElUseCaseMarcaComoLeida_RetornaNoContent()
        {
            _useCaseMock.Setup(u => u.ExecuteMarkAsReadAsync(It.IsAny<NotificationsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            IActionResult result = await _sut.UpdateReadNotification("123", 1, CancellationToken.None);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task UpdateReadNotification_CuandoElUseCaseNoEncuentraLaNotificacion_RetornaNotFound()
        {
            _useCaseMock.Setup(u => u.ExecuteMarkAsReadAsync(It.IsAny<NotificationsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

            IActionResult result = await _sut.UpdateReadNotification("123", 1, CancellationToken.None);

            result.Should().BeOfType<NotFoundResult>();
        }
    }
}
