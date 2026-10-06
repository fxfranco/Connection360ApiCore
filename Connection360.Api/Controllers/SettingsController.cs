using Asp.Versioning;
using Connection360.Api.Models;
using Connection360.Application.DTOs;
using Connection360.Application.DTOs.Persistence;
using Connection360.Application.Ports;
using Connection360.Application.Ports.Persistence;
using Connection360.Domain.Dtos;
using Connection360.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Connection360.Api.Controllers
{
    /// <summary>
    /// Expone la configuración de la plataforma: preferencias de notificación por cliente,
    /// configuración maestra (global) del sistema, gestión de usuarios (Auth0) y utilidades de
    /// aprovisionamiento de bases de datos por cliente/colaborador.
    /// </summary>
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

        /// <summary>Obtiene la configuración de notificaciones (canales y eventos) de un cliente.</summary>
        /// <param name="idClient">Identificador del cliente.</param>
        /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
        /// <returns>La configuración de notificaciones del cliente, envuelta en la respuesta estándar de la API.</returns>
        /// <response code="200">Configuración de notificaciones obtenida correctamente.</response>
        /// <response code="403">No tiene un rol asignado, acceso denegado.</response>
        [HttpGet("viewnotifications")]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(typeof(CustomerNotificationsSettingsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetNotificationsSettings([FromQuery] String idClient, CancellationToken cancellationToken)
        {
            UserRoleApplication role = Enum.GetValues<UserRoleApplication>().FirstOrDefault(r => User.IsInRole(r.ToString()));
            if (role == UserRoleApplication.UNASSIGNED)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Acceso denegado. No se tiene un rol asignado.");
            }
            CustomerNotificationsSettingsResponse result = await _customerNotificationsSettingsUseCase.GetCustomerNotificationSettings(idClient, cancellationToken);
            return Ok(result);
        }

        /// <summary>Crea la configuración de notificaciones (canales y eventos) de un cliente.</summary>
        /// <param name="customerNotificationsSettings">Configuración de canales y eventos de notificación a crear.</param>
        /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
        /// <returns>La configuración de notificaciones creada, envuelta en la respuesta estándar de la API.</returns>
        /// <response code="200">Configuración de notificaciones creada correctamente.</response>
        /// <response code="403">No tiene un rol asignado, acceso denegado.</response>
        [HttpPost("createnotifications", Name = nameof(CreateNotificationsSettings))]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(typeof(CustomerNotificationsSettingsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateNotificationsSettings([FromBody] CustomerNotificationsSettingsResponse customerNotificationsSettings, CancellationToken cancellationToken)
        {
            UserRoleApplication role = Enum.GetValues<UserRoleApplication>().FirstOrDefault(r => User.IsInRole(r.ToString()));
            if (role == UserRoleApplication.UNASSIGNED)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Acceso denegado. No se tiene un rol asignado.");
            }
            CustomerNotificationsSettingsResponse result = await _customerNotificationsSettingsUseCase.CreateCustomerNotificationSettings(customerNotificationsSettings, cancellationToken);
            return CreatedAtRoute(nameof(CreateNotificationsSettings), new { idClient = result.NotificationChannels.ClientId ?? result.NotificationEvents.ClientId, idChannel = result.NotificationChannels.NotificationChannelId, idEvent = result.NotificationEvents.NotificationEventId}, result);
        }

        /// <summary>Actualiza la configuración de notificaciones (canales y eventos) de un cliente.</summary>
        /// <param name="customerNotificationsSettings">Configuración de canales y eventos de notificación a actualizar.</param>
        /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
        /// <returns>Sin contenido.</returns>
        /// <response code="200">Reservado para compatibilidad; este endpoint siempre responde 204.</response>
        /// <response code="204">Configuración de notificaciones actualizada correctamente.</response>
        /// <response code="403">No tiene un rol asignado, acceso denegado.</response>
        [HttpPatch("updatenotifications")]
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateNotificationsSettings([FromBody] CustomerNotificationsSettingsResponse customerNotificationsSettings, CancellationToken cancellationToken)
        {
            UserRoleApplication role = Enum.GetValues<UserRoleApplication>().FirstOrDefault(r => User.IsInRole(r.ToString()));
            if (role == UserRoleApplication.UNASSIGNED)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Acceso denegado. No se tiene un rol asignado.");
            }
            Boolean result = await _customerNotificationsSettingsUseCase.UpdateCustomerNotificationSettings(customerNotificationsSettings, cancellationToken);
            return NoContent();
        }

        /// <summary>Obtiene la configuración maestra (global) del sistema.</summary>
        /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
        /// <returns>La configuración maestra del sistema, envuelta en la respuesta estándar de la API.</returns>
        /// <response code="200">Configuración maestra obtenida correctamente.</response>
        /// <response code="403">No tiene un rol asignado, acceso denegado.</response>
        [HttpGet("viewmaster")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(MasterSettingsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMasterSettings(CancellationToken cancellationToken)
        {
            UserRoleApplication role = Enum.GetValues<UserRoleApplication>().FirstOrDefault(r => User.IsInRole(r.ToString()));
            if (role != UserRoleApplication.ADMIN)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Acceso denegado. No se tiene un rol asignado.");
            }
            MasterSettingsResponse MasterSettingsResponse = await _masterSettingsUseCase.GetAsync(cancellationToken);
            return Ok(MasterSettingsResponse);
        }

        /// <summary>Crea la configuración maestra (global) del sistema.</summary>
        /// <param name="createMasterSettingsDto">Datos de la configuración maestra a crear.</param>
        /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
        /// <returns>La configuración maestra creada, envuelta en la respuesta estándar de la API.</returns>
        /// <response code="200">Reservado para compatibilidad; este endpoint responde 201 al crear el recurso.</response>
        /// <response code="201">Configuración maestra creada correctamente.</response>
        /// <response code="403">No tiene un rol asignado, acceso denegado.</response>
        [HttpPost("createmaster", Name = nameof(CreateMasterSettings))]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(MasterSettingsResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MasterSettingsResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateMasterSettings([FromBody] CreateMasterSettingsDto createMasterSettingsDto, CancellationToken cancellationToken)
        {
            UserRoleApplication role = Enum.GetValues<UserRoleApplication>().FirstOrDefault(r => User.IsInRole(r.ToString()));
            if (role != UserRoleApplication.ADMIN)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Acceso denegado. No se tiene un rol asignado.");
            }
            MasterSettingsResponseDto masterSettingsResponseDto = await _masterSettingsUseCase.CreateAsync(createMasterSettingsDto, cancellationToken);
            return CreatedAtRoute(nameof(CreateMasterSettings), new { id = masterSettingsResponseDto.IdMasterSettings }, masterSettingsResponseDto);
        }

        /// <summary>Actualiza la configuración maestra (global) del sistema.</summary>
        /// <param name="masterSettingsUpdate">Datos de la configuración maestra a actualizar.</param>
        /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
        /// <returns>Sin contenido.</returns>
        /// <response code="200">Reservado para compatibilidad; este endpoint siempre responde 204.</response>
        /// <response code="204">Configuración maestra actualizada correctamente.</response>
        /// <response code="403">No tiene un rol asignado, acceso denegado.</response>
        [HttpPatch("updatemaster")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateMasterSettings([FromBody] MasterSettingsResponseDto masterSettingsUpdate, CancellationToken cancellationToken)
        {
            UserRoleApplication role = Enum.GetValues<UserRoleApplication>().FirstOrDefault(r => User.IsInRole(r.ToString()));
            if (role != UserRoleApplication.ADMIN)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Acceso denegado. No se tiene un rol asignado.");
            }
            MasterSettingsResponseDto masterSettingsResponseDto = await _masterSettingsUseCase.UpdateAsync(masterSettingsUpdate, cancellationToken);
            return NoContent();
        }

        /// <summary>Obtiene el listado paginado de usuarios administrables (Auth0).</summary>
        /// <param name="page">Número de página solicitada (base 1; internamente se ajusta a base 0).</param>
        /// <param name="size">Cantidad máxima de usuarios por página.</param>
        /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
        /// <returns>El listado paginado de usuarios, envuelto en la respuesta estándar de la API.</returns>
        /// <response code="200">Listado de usuarios obtenido correctamente.</response>
        /// <response code="500">Error inesperado al listar los usuarios.</response>
        /// <response code="403">No tiene un rol asignado, acceso denegado.</response>
        [HttpGet("listusers")]
        //[AllowAnonymous]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(PagedResult<IList<Auth0UserDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUsersListSettings([FromQuery] Int32 page, [FromQuery] Int32 size, CancellationToken cancellationToken)
        {
            UserRoleApplication role = Enum.GetValues<UserRoleApplication>().FirstOrDefault(r => User.IsInRole(r.ToString()));
            if (role != UserRoleApplication.ADMIN)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Acceso denegado. No se tiene un rol asignado.");
            }
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

        /// <summary>Obtiene un usuario administrable (Auth0) por su identificador.</summary>
        /// <param name="userId">Identificador del usuario en Auth0.</param>
        /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
        /// <returns>El usuario solicitado, envuelto en la respuesta estándar de la API.</returns>
        /// <response code="200">Usuario obtenido correctamente.</response>
        /// <response code="500">Error inesperado al consultar el usuario (incluye el caso en que no se encuentra).</response>
        /// <response code="403">No tiene un rol asignado, acceso denegado.</response>
        [HttpGet("getuser")]
        //[AllowAnonymous]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(Auth0UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUsersByIdSettings([FromQuery] String userId, CancellationToken cancellationToken)
        {
            UserRoleApplication role = Enum.GetValues<UserRoleApplication>().FirstOrDefault(r => User.IsInRole(r.ToString()));
            if (role != UserRoleApplication.ADMIN)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Acceso denegado. No se tiene un rol asignado.");
            }
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

        /// <summary>Actualiza los datos de un usuario administrable (Auth0).</summary>
        /// <param name="userId">Identificador del usuario en Auth0 a actualizar.</param>
        /// <param name="UsersUpdate">Nuevos datos del usuario.</param>
        /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
        /// <returns>Sin contenido.</returns>
        /// <response code="204">Usuario actualizado correctamente.</response>
        /// <response code="500">Error inesperado al actualizar el usuario.</response>
        /// <response code="403">No tiene un rol asignado, acceso denegado.</response>
        [HttpPatch("updateuser/{userId}")]
        //[AllowAnonymous]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateUsersByIdSettings([FromRoute] String userId, [FromBody] Auth0UserDto UsersUpdate, CancellationToken cancellationToken)
        {
            UserRoleApplication role = Enum.GetValues<UserRoleApplication>().FirstOrDefault(r => User.IsInRole(r.ToString()));
            if (role != UserRoleApplication.ADMIN)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Acceso denegado. No se tiene un rol asignado.");
            }
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

        /// <summary>Elimina un usuario administrable (Auth0).</summary>
        /// <param name="userId">Identificador del usuario en Auth0 a eliminar.</param>
        /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
        /// <returns>Sin contenido.</returns>
        /// <response code="204">Usuario eliminado correctamente.</response>
        /// <response code="500">Error inesperado al eliminar el usuario.</response>
        /// <response code="403">No tiene un rol asignado, acceso denegado.</response>
        [HttpDelete("deleteuser/{userId}")]
        //[AllowAnonymous]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteUsersByIdSettings([FromRoute] String userId, CancellationToken cancellationToken)
        {
            UserRoleApplication role = Enum.GetValues<UserRoleApplication>().FirstOrDefault(r => User.IsInRole(r.ToString()));
            if (role != UserRoleApplication.ADMIN)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Acceso denegado. No se tiene un rol asignado.");
            }
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

        /// <summary>
        /// Utilidad de aprovisionamiento: crea la base de datos/esquema de un nuevo cliente.
        /// Endpoint anónimo pensado para uso interno/administrativo, no para el flujo normal de la aplicación.
        /// </summary>
        /// <param name="clientId">Identificador del cliente a aprovisionar.</param>
        /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
        /// <returns>El identificador del recurso creado.</returns>
        [HttpGet("createcustomerdb", Name = nameof(CreateCustomerDataBase))]
        [AllowAnonymous]
        public async Task<ActionResult> CreateCustomerDataBase(String clientId, CancellationToken cancellationToken)
        {
            var resultado = await _customerUseCase.CrearAsync(clientId, cancellationToken);
            return CreatedAtRoute(nameof(CreateCustomerDataBase), new { id = resultado }, resultado);
        }

        /// <summary>
        /// Utilidad de aprovisionamiento: crea la base de datos/esquema de un nuevo colaborador.
        /// Endpoint anónimo pensado para uso interno/administrativo, no para el flujo normal de la aplicación.
        /// </summary>
        /// <param name="clientId">Identificador del colaborador a aprovisionar.</param>
        /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
        /// <returns>El identificador del recurso creado.</returns>
        [HttpGet("createcollaboratordb", Name = nameof(CreateCollaboratorDataBase))]
        [AllowAnonymous]
        public async Task<ActionResult> CreateCollaboratorDataBase(String clientId, CancellationToken cancellationToken)
        {
            var resultado = await _collaboratorUseCase.CreateAsync(clientId, cancellationToken);
            return CreatedAtRoute(nameof(CreateCollaboratorDataBase), new { id = resultado }, resultado);
        }

        /// <summary>
        /// Utilidad de aprovisionamiento: asocia un colaborador existente a un cliente existente.
        /// Endpoint anónimo pensado para uso interno/administrativo, no para el flujo normal de la aplicación.
        /// </summary>
        /// <param name="clientId">Identificador del cliente.</param>
        /// <param name="collaborator">Identificador del colaborador a asociar.</param>
        /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
        /// <returns>El identificador del recurso creado.</returns>
        [HttpGet("createcustomercollaboratordb", Name = nameof(CreateCustomerCollaboratorDataBase))]
        [AllowAnonymous]
        public async Task<ActionResult> CreateCustomerCollaboratorDataBase(String clientId, String collaborator, CancellationToken cancellationToken)
        {
            var resultado = await _collaboratorUseCase.CreateCustomerCollaboratorAsync(clientId, collaborator, cancellationToken);
            return CreatedAtRoute(nameof(CreateCustomerCollaboratorDataBase), new { id = resultado }, resultado);
        }

        /// <summary>
        /// Endpoint de prueba: encola un mensaje de notificación en el patrón Outbox para su
        /// posterior publicación. Endpoint anónimo pensado para pruebas, no para el flujo normal de
        /// la aplicación.
        /// </summary>
        /// <param name="outboxMessages">Datos del mensaje a encolar.</param>
        /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
        /// <returns>El resultado de la operación.</returns>
        /// <response code="204">Reservado para compatibilidad; en caso de éxito este endpoint responde 200 con el resultado.</response>
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
