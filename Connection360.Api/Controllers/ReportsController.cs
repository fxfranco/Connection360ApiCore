using Asp.Versioning;
using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using Connection360.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Connection360.Api.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/reports")]
    [Authorize]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    public sealed class ReportsController : ControllerBase
    {
        private readonly IGetReportsUseCase _getReportsUseCase;
        public ReportsController(IGetReportsUseCase getReportsUseCase)
        {
            _getReportsUseCase = getReportsUseCase;
        }

        [HttpGet("home")]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReportTotals([FromQuery] String idClient, [FromQuery] String? idQueryClient, CancellationToken cancellationToken)
        {
            ClientSummaryRequest request = new ClientSummaryRequest
            {
                IdClient = idClient,
                AllClient = String.IsNullOrEmpty(idQueryClient) ? true : false,
                IdQueryClient = !String.IsNullOrEmpty(idQueryClient) ? idQueryClient : String.Empty
            };

            UserRoleApplication role = Enum.GetValues<UserRoleApplication>().FirstOrDefault(r => User.IsInRole(r.ToString()));
            request.RoleName = role.ToString();

            List<ReportsSummaryResponse> result = await _getReportsUseCase.ExecuteGetReportsTotalsAsync(request, cancellationToken);
            return Ok(result);
        }
    }
}
