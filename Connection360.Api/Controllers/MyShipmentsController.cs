using Asp.Versioning;
using Connection360.Api.Models;
using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using Connection360.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Connection360.Api.Controllers
{
    /// <summary>
    /// Expone la sección "Mis envíos" de un cliente: el listado completo y filtrado de sus envíos,
    /// su historial de cambios y el detalle de un envío puntual.
    /// </summary>
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

        /// <summary>Obtiene el listado paginado (sin filtros) de todos los envíos de un cliente.</summary>
        /// <param name="idClient">Identificador del cliente sobre el que se consulta.</param>
        /// <param name="idQueryClient">
        /// Identificador de un cliente específico a consultar; si se omite, se incluyen todos los
        /// clientes asociados al usuario autenticado.
        /// </param>
        /// <param name="page">Número de página solicitada (base 0).</param>
        /// <param name="size">Cantidad máxima de elementos por página.</param>
        /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
        /// <returns>El listado paginado de envíos, envuelto en la respuesta estándar de la API.</returns>
        /// <response code="200">Listado de envíos obtenido correctamente.</response>
        [HttpGet("allshipments")]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(typeof(PagedResult<ClientSummaryResponse>), StatusCodes.Status200OK)]
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

        /// <summary>Obtiene el listado paginado de envíos de un cliente, aplicando filtros adicionales.</summary>
        /// <param name="idClient">Identificador del cliente sobre el que se consulta.</param>
        /// <param name="idQueryClient">
        /// Identificador de un cliente específico a consultar; si se omite, se incluyen todos los
        /// clientes asociados al usuario autenticado.
        /// </param>
        /// <param name="page">Número de página solicitada (base 0).</param>
        /// <param name="size">Cantidad máxima de elementos por página.</param>
        /// <param name="filters">Filtros adicionales a aplicar sobre el listado (texto libre, tipo de operación, modalidad, estado).</param>
        /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
        /// <returns>El listado paginado y filtrado de envíos, envuelto en la respuesta estándar de la API.</returns>
        /// <response code="200">Listado de envíos filtrado correctamente.</response>
        [HttpGet("filterShipments")]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(typeof(PagedResult<ClientSummaryResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> FilterShipments([FromQuery] String idClient, [FromQuery] String? idQueryClient, [FromQuery] Int64 page, [FromQuery] Int64 size,
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

        /// <summary>Obtiene el listado paginado (sin filtros) del historial de cambios de todos los envíos de un cliente.</summary>
        /// <param name="idClient">Identificador del cliente sobre el que se consulta.</param>
        /// <param name="idQueryClient">
        /// Identificador de un cliente específico a consultar; si se omite, se incluyen todos los
        /// clientes asociados al usuario autenticado.
        /// </param>
        /// <param name="page">Número de página solicitada (base 0).</param>
        /// <param name="size">Cantidad máxima de elementos por página.</param>
        /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
        /// <returns>El historial paginado de envíos, envuelto en la respuesta estándar de la API.</returns>
        /// <response code="200">Historial de envíos obtenido correctamente.</response>
        [HttpGet("allhistory")]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(typeof(PagedResult<ClientSummaryResponse>), StatusCodes.Status200OK)]
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

        /// <summary>Obtiene el listado paginado del historial de cambios de los envíos de un cliente, aplicando filtros adicionales.</summary>
        /// <param name="idClient">Identificador del cliente sobre el que se consulta.</param>
        /// <param name="idQueryClient">
        /// Identificador de un cliente específico a consultar; si se omite, se incluyen todos los
        /// clientes asociados al usuario autenticado.
        /// </param>
        /// <param name="page">Número de página solicitada (base 0).</param>
        /// <param name="size">Cantidad máxima de elementos por página.</param>
        /// <param name="filters">Filtros adicionales a aplicar sobre el historial (texto libre, tipo de operación, modalidad, estado).</param>
        /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
        /// <returns>El historial paginado y filtrado de envíos, envuelto en la respuesta estándar de la API.</returns>
        /// <response code="200">Historial de envíos filtrado correctamente.</response>
        [HttpGet("filterhistory")]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(typeof(PagedResult<ClientSummaryResponse>), StatusCodes.Status200OK)]
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

        /// <summary>Obtiene el detalle completo de un envío puntual (resumen, seguimiento, fechas logísticas, contenedores, información financiera e historial).</summary>
        /// <param name="idClient">Identificador del cliente dueño del envío.</param>
        /// <param name="idQueryClient">
        /// Identificador de un cliente específico a consultar; si se omite, se incluyen todos los
        /// clientes asociados al usuario autenticado.
        /// </param>
        /// <param name="documentNumber">Número de documento del envío a consultar.</param>
        /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
        /// <returns>El detalle del envío, envuelto en la respuesta estándar de la API.</returns>
        /// <response code="200">Detalle del envío obtenido correctamente.</response>
        [HttpGet("detailsshipments")]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(typeof(DetailsShipmentsResponse), StatusCodes.Status200OK)]
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
