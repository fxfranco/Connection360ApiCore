using Asp.Versioning;
using Connection360.Api.Models;
using Connection360.Application.DTOs;
using Connection360.Application.DTOs.Persistence;
using Connection360.Application.Ports;
using Connection360.Application.Ports.Persistence;
using Connection360.Domain.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Connection360.Api.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/settings")]
    [Authorize]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    public sealed class SettingsController : ControllerBase
    {
        private readonly IGetUserManagementUseCase _getUserManagementUseCase;
        private readonly ICustomerUseCase _customerUseCase;
        private readonly ICustomerNotificationsSettingsUseCase _customerNotificationsSettingsUseCase;
        private readonly IMasterSettingsUseCase _masterSettingsUseCase;
        private readonly ICollaboratorUseCase _collaboratorUseCase;
        private readonly IOutboxMessagesUseCase _outboxMessagesUseCase;

        public SettingsController(IGetUserManagementUseCase getUserManagementUseCase, ICustomerUseCase customerUseCase, 
            ICustomerNotificationsSettingsUseCase customerNotificationsSettingsUseCase,
            IMasterSettingsUseCase masterSettingsUseCase, ICollaboratorUseCase collaboratorUseCase, IOutboxMessagesUseCase outboxMessagesUseCase)
        {
            _getUserManagementUseCase = getUserManagementUseCase;
            _customerUseCase = customerUseCase;
            _customerNotificationsSettingsUseCase = customerNotificationsSettingsUseCase;
            _masterSettingsUseCase = masterSettingsUseCase;
            _collaboratorUseCase = collaboratorUseCase;
            _outboxMessagesUseCase = outboxMessagesUseCase;
        }

        [HttpGet("viewnotifications")]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNotificationsSettings([FromQuery] String idClient, CancellationToken cancellationToken)
        {
            CustomerNotificationsSettingsResponse result = await _customerNotificationsSettingsUseCase.GetCustomerNotificationSettings(idClient, cancellationToken);
            return Ok(result);
        }

        [HttpPost("createnotifications")]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateNotificationsSettings([FromBody] CustomerNotificationsSettingsResponse customerNotificationsSettings, CancellationToken cancellationToken)
        {
            CustomerNotificationsSettingsResponse result = await _customerNotificationsSettingsUseCase.CreateCustomerNotificationSettings(customerNotificationsSettings, cancellationToken);
            return CreatedAtRoute(nameof(GetNotificationsSettings), new { idChannel = result.NotificationChannels.NotificationChannelId, idEvent = result.NotificationEvents.NotificationEventId}, result);
        }

        [HttpPatch("updatenotifications")]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> UpdateNotificationsSettings([FromBody] CustomerNotificationsSettingsResponse customerNotificationsSettings, CancellationToken cancellationToken)
        {
            Boolean result = await _customerNotificationsSettingsUseCase.UpdateCustomerNotificationSettings(customerNotificationsSettings, cancellationToken);
            return NoContent();
        }

        [HttpGet("viewmaster")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMasterSettings(CancellationToken cancellationToken)
        {
            MasterSettingsResponse MasterSettingsResponse = await _masterSettingsUseCase.GetAsync(cancellationToken);
            return Ok(MasterSettingsResponse);
        }

        [HttpPost("createmaster")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateMasterSettings([FromBody] CreateMasterSettingsDto createMasterSettingsDto, CancellationToken cancellationToken)
        {
            MasterSettingsResponseDto masterSettingsResponseDto = await _masterSettingsUseCase.CreateAsync(createMasterSettingsDto, cancellationToken);
            return CreatedAtRoute(nameof(GetMasterSettings), new { id = masterSettingsResponseDto.IdMasterSettings }, masterSettingsResponseDto);
        }

        [HttpPatch("updatemaster")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> UpdateMasterSettings([FromBody] MasterSettingsResponseDto masterSettingsUpdate, CancellationToken cancellationToken)
        {
            MasterSettingsResponseDto masterSettingsResponseDto = await _masterSettingsUseCase.UpdateAsync(masterSettingsUpdate, cancellationToken);
            return NoContent();
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

        [HttpGet("createcustomerdb")]
        [AllowAnonymous]
        public async Task<ActionResult> CreateCustomerDataBase(String clientId, CancellationToken cancellationToken)
        {
            var resultado = await _customerUseCase.CrearAsync(clientId, cancellationToken);
            return CreatedAtRoute(nameof(CreateCustomerDataBase), new { id = resultado }, resultado);
        }

        [HttpGet("createcollaboratordb")]
        [AllowAnonymous]
        public async Task<ActionResult> CreateCollaboratorDataBase(String clientId, CancellationToken cancellationToken)
        {
            var resultado = await _collaboratorUseCase.CreateAsync(clientId, cancellationToken);
            return CreatedAtRoute(nameof(CreateCollaboratorDataBase), new { id = resultado }, resultado);
        }

        [HttpGet("createcustomercollaboratordb")]
        [AllowAnonymous]
        public async Task<ActionResult> CreateCustomerCollaboratorDataBase(String clientId, String collaborator, CancellationToken cancellationToken)
        {
            var resultado = await _collaboratorUseCase.CreateCustomerCollaboratorAsync(clientId, collaborator, cancellationToken);
            return CreatedAtRoute(nameof(CreateCustomerCollaboratorDataBase), new { id = resultado }, resultado);
        }

        [HttpPost("generatenotificationOutboxTest")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> GeneratenotificationOutbox(CreateOutboxMessagesDto outboxMessages, CancellationToken cancellationToken)
        {
            Boolean result = await _outboxMessagesUseCase.CreateAsync(outboxMessages, cancellationToken);

            return result ? Ok(result) : Problem(detail: "No se pudo realizar el proceso. Intente más tarde.",
                   statusCode: StatusCodes.Status500InternalServerError,
                   title: "Error al generar notificacion outbox en el api core");
        }
    }
}
