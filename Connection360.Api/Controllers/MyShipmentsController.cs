using Asp.Versioning;
using Connection360.Api.Models;
using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using Connection360.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Connection360.Api.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/myshipments")]
    [Authorize]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    public sealed class MyShipmentsController : ControllerBase
    {

        private readonly IGetMyShipmentsUseCase _getMyShipmentsUseCase;

        public MyShipmentsController(IGetMyShipmentsUseCase getMyShipmentsUseCase)
        {
            _getMyShipmentsUseCase = getMyShipmentsUseCase;
        }

        [HttpGet("allshipments")]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllShipments([FromQuery] String idClient, [FromQuery] String? idQueryClient, [FromQuery] Int64 page, [FromQuery] Int64 size, CancellationToken cancellationToken)
        {
            MyShipmentsRequest request = new MyShipmentsRequest 
            { 
                IdClient = idClient,
                AllClient = String.IsNullOrEmpty(idQueryClient) ? true : false,
                IdQueryClient = !String.IsNullOrEmpty(idQueryClient) ? idQueryClient : String.Empty,
                Page = page, 
                Size = size 
            };

            UserRoleApplication role = Enum.GetValues<UserRoleApplication>().FirstOrDefault(r => User.IsInRole(r.ToString()));
            request.RoleName = role.ToString();
            
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
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> FilterShipments([FromQuery] String idClient, [FromQuery] String? idQueryClient, [FromQuery] Int64 page, [FromQuery] Int64 size,
            [FromQuery] MyShipmentsFiltersRequest filters, CancellationToken cancellationToken)
        {

            MyShipmentsRequest request = new MyShipmentsRequest
            {
                IdClient = idClient,
                AllClient = String.IsNullOrEmpty(idQueryClient) ? true : false,
                IdQueryClient = !String.IsNullOrEmpty(idQueryClient) ? idQueryClient : String.Empty,
                Page = page,
                Size = size
            };

            UserRoleApplication role = Enum.GetValues<UserRoleApplication>().FirstOrDefault(r => User.IsInRole(r.ToString()));
            request.RoleName = role.ToString();

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

        [HttpGet("allhistory")]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHistoryAllShipments([FromQuery] String idClient, [FromQuery] String? idQueryClient, [FromQuery] Int64 page, [FromQuery] Int64 size, CancellationToken cancellationToken)
        {
            MyShipmentsRequest request = new MyShipmentsRequest
            {
                IdClient = idClient,
                AllClient = String.IsNullOrEmpty(idQueryClient) ? true : false,
                IdQueryClient = !String.IsNullOrEmpty(idQueryClient) ? idQueryClient : String.Empty,
                Page = page,
                Size = size
            };

            UserRoleApplication role = Enum.GetValues<UserRoleApplication>().FirstOrDefault(r => User.IsInRole(r.ToString()));
            request.RoleName = role.ToString();

            MyShipmentsResponse result = await _getMyShipmentsUseCase.ExecuteGetHistoryAllShipmentsAsync(request, cancellationToken);

            PagedResult<Object> pagedResult = new PagedResult<Object>
            {
                Items = [result.ClientSummaryResponseData],
                TotalItems = result.ClientSummaryResponseData.TotalClientRecords,
                CurrentPage = page,
                Limit = size
            };

            return Ok(pagedResult);
        }

        [HttpGet("filterhistory")]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> FilterHistoryShipments([FromQuery] String idClient, [FromQuery] String? idQueryClient, [FromQuery] Int64 page, [FromQuery] Int64 size, 
            [FromQuery] MyShipmentsFiltersRequest filters, CancellationToken cancellationToken)
        {
            MyShipmentsRequest request = new MyShipmentsRequest
            {
                IdClient = idClient,
                AllClient = String.IsNullOrEmpty(idQueryClient) ? true : false,
                IdQueryClient = !String.IsNullOrEmpty(idQueryClient) ? idQueryClient : String.Empty,
                Page = page,
                Size = size,
                Filters = filters
            };

            UserRoleApplication role = Enum.GetValues<UserRoleApplication>().FirstOrDefault(r => User.IsInRole(r.ToString()));
            request.RoleName = role.ToString();

            MyShipmentsResponse result = await _getMyShipmentsUseCase.ExecuteFilterHistoryShipmentsAsync(request, cancellationToken);

            PagedResult<Object> pagedResult = new PagedResult<Object>
            {
                Items = [result.ClientSummaryResponseData],
                TotalItems = result.ClientSummaryResponseData.TotalClientRecords,
                CurrentPage = page,
                Limit = size
            };

            return Ok(pagedResult);
        }

        [HttpGet("detailsshipments")]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDetailsShipments([FromQuery] String idClient, [FromQuery] String? idQueryClient, [FromQuery] String documentNumber, CancellationToken cancellationToken)
        {
            MyShipmentsRequest request = new MyShipmentsRequest
            {
                IdClient = idClient,
                AllClient = String.IsNullOrEmpty(idQueryClient) ? true : false,
                IdQueryClient = !String.IsNullOrEmpty(idQueryClient) ? idQueryClient : String.Empty,
                DocumentNumber = documentNumber
            };

            UserRoleApplication role = Enum.GetValues<UserRoleApplication>().FirstOrDefault(r => User.IsInRole(r.ToString()));
            request.RoleName = role.ToString();

            DetailsShipmentsResponse result = await _getMyShipmentsUseCase.ExecuteDetailsShipmentsAsync(request, cancellationToken);
            return Ok(result);
        }
    }
}
