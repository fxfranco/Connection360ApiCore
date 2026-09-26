using Asp.Versioning;
using Connection360Notification.Application.DTOs;
using Connection360Notification.Application.Ports;
using Connection360Notification.Application.Ports.Inbound;
using Connection360Notification.Domain;
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
        
        //Productor Kafka enviar notificaciones
        private readonly INotificationUseCase _notificationUseCase;

        public NotificationsController(IGetNotificationsUseCase getNotificationsUseCase, INotificationUseCase notificationUseCase)
        {
            _getNotificationsUseCase = getNotificationsUseCase;
            _notificationUseCase = notificationUseCase;
        }

        [HttpGet("allnotifications")]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllNotifications([FromQuery] String idClient, CancellationToken cancellationToken)
        {
            ClientSummaryRequest request = new ClientSummaryRequest { IdClient = idClient, RoleName = String.Empty };
            List<NotificationsListResponse> result = await _getNotificationsUseCase.ExecuteGetNotificationsByClientAsync(request, cancellationToken);
            return Ok(result);
        }

        [HttpPatch("readnotification/{idClient}/{idNotification}")]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateReadNotification([FromRoute] String idClient, [FromRoute] Int64 idNotification, CancellationToken cancellationToken)
        {
            NotificationsRequest request = new NotificationsRequest { IdClient = idClient, IdNotification = idNotification, RoleName = String.Empty };
            Boolean updated = await _getNotificationsUseCase.ExecuteMarkAsReadAsync(request, cancellationToken);

            if (!updated)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpGet("allnotificationslistTest")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IEnumerable<NotificationMessage>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllNotificationsDB(CancellationToken cancellationToken)
        {
            var notifications = await _getNotificationsUseCase.ExecuteGetNotificationsAllAsync(cancellationToken);
            return Ok(notifications);
        }

        /// <summary>
        /// Envía una notificación produciendo un evento en Kafka.
        /// </summary>
        //[HttpPost("simulatenotifications")]
        //[AllowAnonymous]
        ////[Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        //[ProducesResponseType(StatusCodes.Status202Accepted)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //public async Task<IActionResult> SendNotification([FromBody] CreateNotificationRequest request, CancellationToken cancellationToken)
        //{
        //    if (request == null || string.IsNullOrWhiteSpace(request.Recipient))
        //    {
        //        return BadRequest("Solicitud inválida.");
        //    }

        //    await _notificationUseCase.ExecuteSendAsync(request, cancellationToken);

        //    return Accepted(new { Message = "Notificación enviada a cola de procesamiento." });
        //}
    }
}
