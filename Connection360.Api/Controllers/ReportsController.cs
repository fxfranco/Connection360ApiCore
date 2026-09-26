using Asp.Versioning;
using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using Connection360.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Connection360.Api.Controllers
{
    /// <summary>
    /// Expone los reportes estadísticos de envíos de un cliente (totales por estado, rutas
    /// frecuentes, valores facturados, entre otros).
    /// </summary>
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

        /// <summary>
        /// Obtiene el resumen de reportes de envíos (totales por estado, rutas frecuentes y valores
        /// financieros) de un cliente.
        /// </summary>
        /// <param name="idClient">Identificador del cliente sobre el que se consulta.</param>
        /// <param name="idQueryClient">
        /// Identificador de un cliente específico a consultar; si se omite, se incluyen todos los
        /// clientes asociados al usuario autenticado.
        /// </param>
        /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
        /// <returns>El listado de resúmenes de reportes, envuelto en la respuesta estándar de la API.</returns>
        /// <response code="200">Resumen de reportes calculado correctamente.</response>
        [HttpGet("home")]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(typeof(List<ReportsSummaryResponse>), StatusCodes.Status200OK)]
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
