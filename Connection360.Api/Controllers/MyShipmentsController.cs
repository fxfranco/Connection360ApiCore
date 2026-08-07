using Connection360.Api.Models;
using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Connection360.Api.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/myshipments")]
    [Authorize]
    [Produces("application/json")]
    public sealed class MyShipmentsController : ControllerBase
    {

        private readonly IGetMyShipmentsUseCase _getMyShipmentsUseCase;

        public MyShipmentsController(IGetMyShipmentsUseCase getMyShipmentsUseCase)
        {
            _getMyShipmentsUseCase = getMyShipmentsUseCase;
        }

        [HttpGet("allshipments")]
        //[AllowAnonymous]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllShipments([FromQuery] String idClient, [FromQuery] Int64 page, [FromQuery] Int64 size, CancellationToken cancellationToken)
        {
            MyShipmentsRequest request = new MyShipmentsRequest { IdClient = idClient, RoleName = String.Empty, Page = page, Size = size };
            MyShipmentsResponse result = await _getMyShipmentsUseCase.ExecuteGetAllShipmentsAsync(request, cancellationToken);

            PagedResult<Object> pagedResult = new PagedResult<Object>
            {
                Items = [result.ClientSummaryResponseData],
                TotalItems = result.ClientSummaryResponseData.TotalClientRecords,
                CurrentPage = page,
                Limit = size
            };

            return Ok(pagedResult);
        }

        [HttpGet("filterShipments")]
        //[AllowAnonymous]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> FilterShipments([FromQuery] String idClient, [FromQuery] Int64 page, [FromQuery] Int64 size, 
            [FromQuery] MyShipmentsFiltersRequest filters, CancellationToken cancellationToken)
        {
            MyShipmentsRequest request = new MyShipmentsRequest { IdClient = idClient, RoleName = String.Empty, Page = page, Size = size, Filters = filters };
            MyShipmentsResponse result = await _getMyShipmentsUseCase.ExecuteFilterShipmentsAsync(request, cancellationToken);

            PagedResult<Object> pagedResult = new PagedResult<Object>
            {
                Items = [result.ClientSummaryResponseData],
                TotalItems = result.ClientSummaryResponseData.TotalClientRecords,
                CurrentPage = page,
                Limit = size
            };

            return Ok(pagedResult);
        }
    }
}
