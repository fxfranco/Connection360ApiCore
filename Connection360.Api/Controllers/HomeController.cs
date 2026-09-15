using Asp.Versioning;
using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using Connection360.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Connection360.Api.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/home")]
    [Authorize]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    public sealed class HomeController : ControllerBase
    {
        private readonly IGetClientSummaryUseCase _getClientSummaryUseCase;

        public HomeController(IGetClientSummaryUseCase getClientSummaryUseCase)
        {
            _getClientSummaryUseCase = getClientSummaryUseCase;
        }

        /// https://localhost:44369/api/v1/home/totals?idClient=123&rol=cliente
        [HttpGet("totals")]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHomeTotals([FromQuery] String idClient, [FromQuery] String? idQueryClient, CancellationToken cancellationToken)
        {
            ClientSummaryRequest clientSummaryRequest = new ClientSummaryRequest 
            { 
                IdClient = idClient, 
                AllClient = String.IsNullOrEmpty(idQueryClient) ? true : false, 
                IdQueryClient = !String.IsNullOrEmpty(idQueryClient) ? idQueryClient : String.Empty 
            };

            UserRoleApplication role = Enum.GetValues<UserRoleApplication>().FirstOrDefault(r => User.IsInRole(r.ToString()));
            clientSummaryRequest.RoleName = role.ToString();

            ClientSummaryRequest request = clientSummaryRequest;
            ClientSummaryResponse result = await _getClientSummaryUseCase.ExecuteTotalsAsync(request, cancellationToken);
            return Ok(result); // El ApiResponseFilter lo envuelve automáticamente
        }

        /// https://localhost:44369/api/v1/home/filters?idClient=123&rol=cliente&filterValue=HR12354
        [HttpGet("filters")]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHomeFilters([FromQuery] String idClient, [FromQuery] String? idQueryClient, [FromQuery] String filterValue, CancellationToken cancellationToken)
        {
            ClientSummaryRequest clientSummaryRequest = new ClientSummaryRequest
            {
                IdClient = idClient,
                FilterValue = filterValue,
                AllClient = String.IsNullOrEmpty(idQueryClient) ? true : false,
                IdQueryClient = !String.IsNullOrEmpty(idQueryClient) ? idQueryClient : String.Empty
            };

            UserRoleApplication role = Enum.GetValues<UserRoleApplication>().FirstOrDefault(r => User.IsInRole(r.ToString()));
            clientSummaryRequest.RoleName = role.ToString();

            ResumenClienteResponse result = await _getClientSummaryUseCase.ExecuteFilterAsync(clientSummaryRequest, cancellationToken);
            return Ok(result); // El ApiResponseFilter lo envuelve automáticamente
        }

        //[HttpGet("totalsRol")]
        ////[AllowAnonymous]
        ////[Authorize]
        //[Authorize(Roles = "Client,Admin")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //public async Task<IActionResult> GetHomeTotalsV2(CancellationToken cancellationToken)
        //{
        //    // 1. Verificar rol mediante helper IsInRole
        //    bool esAdmin = User.IsInRole("Admin");

        //    // 2. Extraer todos los roles del usuario activo
        //    var roles = User.Claims
        //        .Where(c => c.Type == "https://mi-app.com/claims/roles" || c.Type == ClaimTypes.Role)
        //        .Select(c => c.Value)
        //        .ToList();

        //    // 3. Extraer el ID del usuario de Auth0
        //    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        //    return Ok(new
        //    {
        //        UserId = userId,
        //        IsAdministrator = esAdmin,
        //        AssignedRoles = roles
        //    });
        //}

        //[HttpGet("totalsRolNoValid")]
        ////[AllowAnonymous]
        ////[Authorize]
        //[Authorize(Roles = "Editor")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //public async Task<IActionResult> GetHomeTotalsV3(CancellationToken cancellationToken)
        //{
        //    // 1. Verificar rol mediante helper IsInRole
        //    bool esAdmin = User.IsInRole("Editor");

        //    // 2. Extraer todos los roles del usuario activo
        //    var roles = User.Claims
        //        .Where(c => c.Type == "https://mi-app.com/claims/roles" || c.Type == ClaimTypes.Role)
        //        .Select(c => c.Value)
        //        .ToList();

        //    // 3. Extraer el ID del usuario de Auth0
        //    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        //    return Ok(new
        //    {
        //        UserId = userId,
        //        IsAdministrator = esAdmin,
        //        AssignedRoles = roles
        //    });
        //}
    }
}
