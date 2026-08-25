using Asp.Versioning;
using Connection360.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
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
        public SettingsController()
        {
            
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
        [Authorize(Roles = "ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC")]
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

    }
}
