using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

        /// https://localhost:44369/api/v1/home/totals?idClient=123&rol=cliente
        [HttpGet("totals")]
        //[AllowAnonymous]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHomeTotals([FromQuery] String idClient, [FromQuery] String rol, CancellationToken cancellationToken)
        {
            ClientSummaryRequest request = new ClientSummaryRequest { IdClient = idClient, RolName = rol };
            var result = await _getClientSummaryUseCase.ExecuteAsync(request, cancellationToken);
            return Ok(result); // El ApiResponseFilter lo envuelve automáticamente
        }

        /// https://localhost:44369/api/v1/home/filters?idClient=123&rol=cliente&filterValue=HR12354
        [HttpGet("filters")]
        //[AllowAnonymous]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHomeFilters([FromQuery] String idClient, [FromQuery] String rol, [FromQuery] String filterValue, CancellationToken cancellationToken)
        {
            ClientSummaryRequest request = new ClientSummaryRequest { IdClient = idClient, RolName = rol, FilterValue = filterValue };
            var result = await _getClientSummaryUseCase.ExecuteFilterAsync(request, cancellationToken);
            return Ok(result); // El ApiResponseFilter lo envuelve automáticamente
        }

        [HttpGet("totalsRol")]
        //[AllowAnonymous]
        //[Authorize]
        [Authorize(Roles = "Client,Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHomeTotalsV2(CancellationToken cancellationToken)
        {
            // 1. Verificar rol mediante helper IsInRole
            bool esAdmin = User.IsInRole("Admin");
            
            // 2. Extraer todos los roles del usuario activo
            var roles = User.Claims
                .Where(c => c.Type == "https://mi-app.com/claims/roles" || c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            // 3. Extraer el ID del usuario de Auth0
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Ok(new
            {
                UserId = userId,
                IsAdministrator = esAdmin,
                AssignedRoles = roles
            });
        }

        [HttpGet("totalsRolNoValid")]
        //[AllowAnonymous]
        //[Authorize]
        [Authorize(Roles = "Editor")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHomeTotalsV3(CancellationToken cancellationToken)
        {
            // 1. Verificar rol mediante helper IsInRole
            bool esAdmin = User.IsInRole("Editor");

            // 2. Extraer todos los roles del usuario activo
            var roles = User.Claims
                .Where(c => c.Type == "https://mi-app.com/claims/roles" || c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            // 3. Extraer el ID del usuario de Auth0
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Ok(new
            {
                UserId = userId,
                IsAdministrator = esAdmin,
                AssignedRoles = roles
            });
        }
    }
}
