using Connection360.Application.DTOs;
using Connection360.Application.Ports;
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

        public NotificationsController(IGetNotificationsUseCase getNotificationsUseCase)
        {
            _getNotificationsUseCase = getNotificationsUseCase;
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
    }
}
