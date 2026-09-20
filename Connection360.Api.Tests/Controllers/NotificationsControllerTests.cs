using Connection360.Api.Controllers;
using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using Connection360.Application.Ports.Output;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Connection360.Api.Tests.Controllers
{
    public class NotificationsControllerTests
    {
        private readonly Mock<IGetNotificationsUseCase> _useCaseMock = new();
        private readonly Mock<INotifierService> _notifierMock = new();
        private readonly NotificationsController _sut;

        public NotificationsControllerTests()
        {
            _sut = new NotificationsController(_useCaseMock.Object, _notifierMock.Object);
        }

        [Fact]
        public async Task GetAllNotifications_RetornaOkConLaListaDelUseCase()
        {
            var expected = new List<NotificationsListResponse> { new() { IdNotification = 1 } };
            _useCaseMock.Setup(u => u.ExecuteGetNotificationsAllAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>())).Returns(expected);

            IActionResult result = await _sut.GetAllNotifications("123", CancellationToken.None);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeSameAs(expected);
        }

        [Fact]
        public async Task UpdateReadNotification_RetornaNoContent()
        {
            IActionResult result = await _sut.UpdateReadNotification("123", 1, CancellationToken.None);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task GenerateNotifications_EnviaLaNotificacionAlServicio()
        {
            IActionResult result = await _sut.GenerateNotifications("123", "Mensaje de prueba", CancellationToken.None);

            _notifierMock.Verify(n => n.SendNotificationToUserAsync("123", "Mensaje de prueba", It.IsAny<Object>()), Times.Once);
            result.Should().BeOfType<OkObjectResult>();
        }
    }
}
