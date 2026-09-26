using Asp.Versioning;
using Connection360Notification.Application.DTOs;
using Connection360Notification.Application.Ports;
using Connection360Notification.Application.Ports.Inbound;
using Connection360Notification.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Connection360Notification.Api.Controllers
{
    /// <summary>
    /// Expone las operaciones de consulta y gestión de notificaciones para los clientes de Connection360.
    /// </summary>
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

        /// <summary>
        /// Crea una nueva instancia del controlador de notificaciones.
        /// </summary>
        /// <param name="getNotificationsUseCase">Caso de uso para consultar y actualizar el estado de las notificaciones.</param>
        /// <param name="notificationUseCase">Caso de uso para enviar notificaciones (producción de eventos en Kafka).</param>
        public NotificationsController(IGetNotificationsUseCase getNotificationsUseCase, INotificationUseCase notificationUseCase)
        {
            _getNotificationsUseCase = getNotificationsUseCase;
            _notificationUseCase = notificationUseCase;
        }

        /// <summary>
        /// Obtiene todas las notificaciones asociadas a un cliente.
        /// </summary>
        /// <param name="idClient">Identificador del cliente cuyas notificaciones se desean consultar.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>La lista de notificaciones del cliente indicado.</returns>
        /// <response code="200">Lista de notificaciones del cliente (puede estar vacía).</response>
        [HttpGet("allnotifications")]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(typeof(List<NotificationsListResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllNotifications([FromQuery] String idClient, CancellationToken cancellationToken)
        {
            ClientSummaryRequest request = new ClientSummaryRequest { IdClient = idClient, RoleName = String.Empty };
            List<NotificationsListResponse> result = await _getNotificationsUseCase.ExecuteGetNotificationsByClientAsync(request, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Marca una notificación puntual de un cliente como leída.
        /// </summary>
        /// <param name="idClient">Identificador del cliente propietario de la notificación.</param>
        /// <param name="idNotification">Identificador secuencial de la notificación a marcar como leída.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Sin contenido si la actualización fue exitosa.</returns>
        /// <response code="204">La notificación fue marcada como leída correctamente.</response>
        /// <response code="404">No existe una notificación con el identificador indicado para ese cliente.</response>
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

        /// <summary>
        /// Obtiene todas las notificaciones almacenadas, sin filtrar por cliente.
        /// </summary>
        /// <remarks>
        /// Endpoint de prueba (nombre "allnotificationslistTest") habilitado con <c>[AllowAnonymous]</c>, es decir,
        /// no requiere autenticación. Devuelve directamente las entidades de dominio <see cref="NotificationMessage"/>
        /// tal como están almacenadas.
        /// </remarks>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>La lista completa de notificaciones almacenadas.</returns>
        /// <response code="200">Lista completa de notificaciones.</response>
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
