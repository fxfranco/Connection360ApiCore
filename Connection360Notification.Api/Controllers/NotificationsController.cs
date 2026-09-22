using Asp.Versioning;
using Connection360Notification.Application.DTOs;
using Connection360Notification.Application.Enum;
using Connection360Notification.Application.Ports;
using Connection360Notification.Application.Ports.Inbound;
using Connection360Notification.Application.Ports.Output;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Ports.Outbound;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Connection360Notification.Api.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/notifications")]
    [Authorize]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    public class NotificationsController : ControllerBase
    {
        private readonly IGetNotificationsUseCase _getNotificationsUseCase;
        private readonly INotificationUseCase _notificationUseCase;

        //ToDo: Este se debe pasar al caso de uso
        private readonly INotificationRepository _notificationRepository;

        private readonly INotifierService _notifierService;

        public NotificationsController(IGetNotificationsUseCase getNotificationsUseCase, INotificationUseCase notificationUseCase, INotificationRepository notificationRepository, INotifierService notifierService)
        {
            _getNotificationsUseCase = getNotificationsUseCase;
            _notificationUseCase = notificationUseCase;
            _notificationRepository = notificationRepository;
            _notifierService = notifierService;
        }

        [HttpGet("allnotifications")]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllNotifications([FromQuery] String idClient, CancellationToken cancellationToken)
        {
            ClientSummaryRequest request = new ClientSummaryRequest { IdClient = idClient, RoleName = String.Empty };
            List<NotificationsListResponse> result = _getNotificationsUseCase.ExecuteGetNotificationsAllAsync(request, cancellationToken);
            return Ok(result);
        }

        [HttpGet("allnotificationsdb")]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IEnumerable<NotificationMessage>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllNotificationsDB([FromQuery] String idClient, CancellationToken cancellationToken)
        {
            var notifications = await _notificationRepository.GetAllAsync(cancellationToken);
            return Ok(notifications);
        }

        [HttpPatch("readnotification/{idClient}/{idNotification}")]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> UpdateReadNotification([FromRoute] String idClient, [FromRoute] Int64 idNotification, CancellationToken cancellationToken)
        {
            //Todo: Falta implementar lògica de actualizaciòón real
            NotificationsRequest request = new NotificationsRequest { IdClient = idClient, IdNotification = idNotification, RoleName = String.Empty };
            //List<NotificationsListResponse> result = _getNotificationsUseCase.ExecuteGetNotificationsAllAsync(request, cancellationToken);
            return NoContent();
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

        /// <summary>
        /// Envía una notificación produciendo un evento en Kafka.
        /// </summary>
        [HttpPost("generatenotificationsdb")]
        [AllowAnonymous]
        //[Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SendNotification([FromBody] CreateNotificationRequest request, CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Recipient))
            {
                return BadRequest("Solicitud inválida.");
            }

            await _notificationUseCase.ExecuteSendAsync(request, cancellationToken);

            return Accepted(new { Message = "Notificación enviada a cola de procesamiento." });
        }
    }
}
