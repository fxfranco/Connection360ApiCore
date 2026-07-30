using Connection360.Api.Models;
using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Connection360.Api.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/home")]
    [Authorize]
    [Produces("application/json")]
    public sealed class HomeController : ControllerBase
    {
        private readonly IGetClientSummaryUseCase _getClientSummaryUseCase;

        public HomeController(IGetClientSummaryUseCase getClientSummaryUseCase)
        {
            _getClientSummaryUseCase = getClientSummaryUseCase;
        }

        /*
         https://localhost:44369/api/v1/home/totalsv2?idClient=asdf&rol=asdfas
         */

        [HttpGet("totals")]
        //[AllowAnonymous]
        //[Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHomeTotals([FromQuery] String idClient, [FromQuery] String rol, CancellationToken cancellationToken)
        {
            ClientSummaryRequest request = new ClientSummaryRequest { IdClient = idClient, RolName = rol };
            var result = await _getClientSummaryUseCase.ExecuteAsync(request, cancellationToken);
            return Ok(result); // El ApiResponseFilter lo envuelve automáticamente
        }

        [HttpGet("filters")]
        //[AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHomeFilters([FromQuery] String idClient, [FromQuery] String rol, [FromQuery] String filterValue, CancellationToken cancellationToken)
        {
            ClientSummaryRequest request = new ClientSummaryRequest { IdClient = idClient, RolName = rol, FilterValue = filterValue };
            var result = await _getClientSummaryUseCase.ExecuteFilterAsync(request, cancellationToken);
            return Ok(result); // El ApiResponseFilter lo envuelve automáticamente
        }
    }
}
