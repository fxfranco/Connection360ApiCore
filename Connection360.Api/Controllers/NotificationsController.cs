using Connection360.Application.DTOs;
using Connection360.Application.Enum;
using Connection360.Application.Ports;
using Connection360.Application.Ports.Output;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Connection360.Api.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/notifications")]
    [Authorize]
    [Produces("application/json")]
    public class NotificationsController : ControllerBase
    {
        private readonly IGetNotificationsUseCase _getNotificationsUseCase;
        private readonly INotifierService _notifierService;

        public NotificationsController(IGetNotificationsUseCase getNotificationsUseCase, INotifierService notifierService)
        {
            _getNotificationsUseCase = getNotificationsUseCase;
            _notifierService = notifierService;
        }

        [HttpGet("allnotifications")]
        //[AllowAnonymous]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReportTotals([FromQuery] String idClient, CancellationToken cancellationToken)
        {
            ClientSummaryRequest request = new ClientSummaryRequest { IdClient = idClient, RoleName = String.Empty };
            List<NotificationsListResponse> result = _getNotificationsUseCase.ExecuteGetNotificationsAllAsync(request, cancellationToken);
            return Ok(result);
        }

        [HttpGet("generatenotifications")]
        [AllowAnonymous]
        //[Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GenerateNotifications([FromQuery] String idClient, String Message, CancellationToken cancellationToken)
        {
            ClientSummaryRequest request = new ClientSummaryRequest { IdClient = idClient, RoleName = String.Empty };

            NotificationsListResponse notificationsListResponse = new NotificationsListResponse
            {
                IdNotification = 10,
                NotificationType = NotificationType.Comment,
                DocumentNumber = "HBL-5U6HC36K",
                Title = "Notificacion generada de prueba",
                Message = Message,
                MessageDate = DateTime.Now.AddDays(-3),
                NotificationStatus = NotificationStatus.Unread,
                NotificationDate = DateTime.Now
            };

            await _notifierService.SendNotificationToUserAsync(idClient, Message, notificationsListResponse);
            return Ok("result");
        }
    }
}
