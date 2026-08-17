using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Connection360.Api.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/reports")]
    [Authorize]
    [Produces("application/json")]
    public sealed class ReportsController : ControllerBase
    {
        private readonly IGetReportsUseCase _getReportsUseCase;
        public ReportsController(IGetReportsUseCase getReportsUseCase)
        {
            _getReportsUseCase = getReportsUseCase;
        }

        [HttpGet("home")]
        //[AllowAnonymous]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReportTotals([FromQuery] String idClient, CancellationToken cancellationToken)
        {
            ClientSummaryRequest request = new ClientSummaryRequest { IdClient = idClient, RoleName = String.Empty };
            ReportsSummaryResponse result = await _getReportsUseCase.ExecuteGetReportsTotalsAsync(request, cancellationToken);
            return Ok(result);
        }
    }
}
