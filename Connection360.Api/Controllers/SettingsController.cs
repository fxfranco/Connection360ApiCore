using Asp.Versioning;
using Connection360.Api.Models;
using Connection360.Application.DTOs;
using Connection360.Application.DTOs.Persistence;
using Connection360.Application.Ports;
using Connection360.Application.Ports.Persistence;
using Connection360.Domain.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Connection360.Api.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/settings")]
    [Authorize]
    //[AllowAnonymous]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    public sealed class SettingsController : ControllerBase
    {
        private readonly IGetUserManagementUseCase _getUserManagementUseCase;
        private readonly ICustomerNotificationChannelsUseCase _customerNotificationChannelsUseCase;
        private readonly ICustomerNotificationEventsUseCase _customerNotificationEventsUseCase;
        public SettingsController(IGetUserManagementUseCase getUserManagementUseCase, ICustomerNotificationChannelsUseCase customerNotificationChannelsUseCase, ICustomerNotificationEventsUseCase customerNotificationEventsUseCase)
        {
            _getUserManagementUseCase = getUserManagementUseCase;
            _customerNotificationChannelsUseCase = customerNotificationChannelsUseCase;
            _customerNotificationEventsUseCase = customerNotificationEventsUseCase;
        }

        [HttpGet("viewnotifications")]
        //[AllowAnonymous]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNotificationsSettings([FromQuery] String idClient, CancellationToken cancellationToken)
        {
            ClientSummaryRequest request = new ClientSummaryRequest { IdClient = idClient, RoleName = String.Empty };

            NotificationsSettingsResponse notificationsSettings = new NotificationsSettingsResponse
            {
                NotificationChannels = new NotificationChannelsResponse
                {
                    Application = true,
                    Email = true,
                    TextMessages = false
                },
                NotificationEvents = new NotificationEventsResponse
                {
                    ChangeState = true,
                    SuccessfulDelivery = true,
                    WithIssues = true,
                    ShipmentTransit = false,
                    DeliveryReminder = false
                  }
            };

            //ToDo: Pendiente hacer logica 
            //MyShipmentsResponse result = await _getMyShipmentsUseCase.ExecuteGetAllShipmentsAsync(request, cancellationToken);

            return Ok(notificationsSettings);
        }

        [HttpGet("viewmaster")]
        //[AllowAnonymous]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMasterSettings([FromQuery] String idClient, CancellationToken cancellationToken)
        {
            ClientSummaryRequest request = new ClientSummaryRequest { IdClient = idClient, RoleName = String.Empty };


            MasterSettingsResponse masterSettings = new MasterSettingsResponse
            {
                GeneralParameters = new GeneralParametersResponse
                {
                    AutomaticTrackingUpdate = true,
                    RequireDocumentUpload = false,
                    PublicMonitoring = true
                },
                Location = new LocationResponse
                {
                    CurrencyType = "USD - Dólar",
                    Language = "Español",
                },
                System = new SystemResponse
                {
                    TimeZone = "America/Bogota(UTC-5)",
                    DataRetentionDays = 365,
                }
            };

            //ToDo: Pendiente hacer logica 
            //MyShipmentsResponse result = await _getMyShipmentsUseCase.ExecuteGetAllShipmentsAsync(request, cancellationToken);

            return Ok(masterSettings);
        }

        [HttpGet("listusers")]
        //[AllowAnonymous]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUsersListSettings([FromQuery] Int32 page, [FromQuery] Int32 size, CancellationToken cancellationToken)
        {
            try
            {
                if (page > 0)
                {
                    page--;
                }

                UsersManagementRequest request = new UsersManagementRequest { RoleName = String.Empty, Page = page, Size = size };
                IList<Auth0UserDto> result = await _getUserManagementUseCase.GetAllUsersAsync(request);

                if (result != null)
                {
                    PagedResult<Object> pagedResult = new PagedResult<Object>
                    {
                        Items = [result],
                        TotalItems = 6,
                        CurrentPage = page,
                        Limit = size
                    };
                    return Ok(pagedResult);
                }
                return Problem(detail: "No se pudo realizar el proceso. Intente más tarde.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Error al listar usuarios en el servidor");
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Error al listar usuarios en el servidor");
            }

        }

        [HttpGet("getuser")]
        //[AllowAnonymous]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUsersByIdSettings([FromQuery] String userId, CancellationToken cancellationToken)
        {
            try
            {
                Auth0UserDto result = await _getUserManagementUseCase.GetUsersByIdAsync(userId);
                return result != null ? Ok(result) : Problem(detail: "No se pudo realizar el proceso. Intente más tarde.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Error al consultando un usuario en el servidor");
            }
            catch (Exception ex )
            {
                return Problem(detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Error al consultar un usuario en el servidor");
                throw;
            }

        }

        [HttpPatch("updateuser/{userId}")]
        //[AllowAnonymous]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateUsersByIdSettings([FromRoute] String userId, [FromBody] Auth0UserDto UsersUpdate, CancellationToken cancellationToken)
        {
            try
            {
                Boolean result = await _getUserManagementUseCase.UpdateUserAsync(userId, UsersUpdate);
                return result ? NoContent() : Problem(detail: "No se pudo realizar el proceso. Intente más tarde.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Error al actualizando usuario en el servidor");
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Error al borrar usuario en el servidor");
            }
        }

        [HttpDelete("deleteuser/{userId}")]
        //[AllowAnonymous]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteUsersByIdSettings([FromRoute] String userId, CancellationToken cancellationToken)
        {
            try
            {
                Boolean result = await _getUserManagementUseCase.DeleteUserAsync(userId);
                return result ? NoContent() : Problem(detail: "No se pudo realizar el proceso. Intente más tarde.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Error al borrar usuario en el servidor");
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Error al borrar usuario en el servidor");
            }
        }

        //Persistencia
        [HttpGet("listallnotificationChannelsdb")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<CustomerNotificationChannelsResponseDto>>> ListAllNotificationChannel(CancellationToken cancellationToken)
        {
            var productos = await _customerNotificationChannelsUseCase.ListAllAsync(cancellationToken);
            return Ok(productos);
        }

        [HttpGet("getByIdnotificationChannelsdb")]
        [AllowAnonymous]
        public async Task<ActionResult<CustomerNotificationChannelsResponseDto>> GetByIdNotificationChannel(Int64 id, CancellationToken cancellationToken)
        {
            var producto = await _customerNotificationChannelsUseCase.GetByIdAsync(id, cancellationToken);
            if (producto is null) return Ok(new {});

            return Ok(producto);
        }

        [HttpPost("createnotificationChannelsdb")]
        [AllowAnonymous]
        public async Task<ActionResult<CustomerNotificationChannelsResponseDto>> CreateNotificationChannel([FromBody] CreateCustomerNotificationChannelsDto request, CancellationToken cancellationToken)
        {
            var resultado = await _customerNotificationChannelsUseCase.CrearAsync(request, cancellationToken);
            return CreatedAtRoute(nameof(GetByIdNotificationChannel), new { id = resultado.IdNotificationChannels}, resultado);
        }

        //Persistencia
        [HttpGet("listallnotificationEventsdb")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<CustomerNotificationEventsResponseDto>>> ListAllNotificationEvent(CancellationToken cancellationToken)
        {
            var productos = await _customerNotificationEventsUseCase.ListAllAsync(cancellationToken);
            return Ok(productos);
        }

        [HttpGet("getByIdnotificationEventsdb")]
        [AllowAnonymous]
        public async Task<ActionResult<CustomerNotificationEventsResponseDto>> GetByIdNotificationEvent(Int64 id, CancellationToken cancellationToken)
        {
            var producto = await _customerNotificationEventsUseCase.GetByIdAsync(id, cancellationToken);
            if (producto is null) return Ok(new { });

            return Ok(producto);
        }

        [HttpPost("createnotificationEventsdb")]
        [AllowAnonymous]
        public async Task<ActionResult<CustomerNotificationEventsResponseDto>> CreateNotificationEvent([FromBody] CreateCustomerNotificationEventsDto request, CancellationToken cancellationToken)
        {
            var resultado = await _customerNotificationEventsUseCase.CrearAsync(request, cancellationToken);
            return CreatedAtRoute(nameof(GetByIdNotificationEvent), new { id = resultado.IdNotificationEvent }, resultado);
        }

    }
}
