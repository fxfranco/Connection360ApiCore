using Connection360.Application.DTOs;
using Connection360.Application.DTOs.Persistence;
using FluentAssertions;
using Xunit;

namespace Connection360.Application.Tests.DTOs
{
    /// <summary>
    /// Pruebas de contrato (get/set) para los DTOs de la capa Application.
    /// No contienen lógica propia; se valida asignación/lectura y valores por defecto.
    /// </summary>
    public class ApplicationDtosTests
    {
        [Fact]
        public void ClientSummaryRequest_DebeAsignarTodasLasPropiedades()
        {
            var dto = new ClientSummaryRequest { IdClient = "1", RoleName = "admin", FilterValue = "HBL-1" };

            dto.IdClient.Should().Be("1");
            dto.RoleName.Should().Be("admin");
            dto.FilterValue.Should().Be("HBL-1");
        }

        [Fact]
        public void ClientSummaryResponse_ListasOpcionales_DebenPermitirNull()
        {
            var dto = new ClientSummaryResponse();

            dto.RecentShipments.Should().BeNull();
            dto.MyShipments.Should().BeNull();
            dto.TotalWithIssues.Should().BeNull();
        }

        [Fact]
        public void MyShipmentsRequest_DebeAsignarFiltrosOpcionales()
        {
            var dto = new MyShipmentsRequest
            {
                IdClient = "1",
                RoleName = "user",
                DocumentNumber = "HBL-1",
                Page = 1,
                Size = 10,
                Filters = new MyShipmentsFiltersRequest { ValueFilter = "x" }
            };

            dto.Filters!.ValueFilter.Should().Be("x");
            dto.Page.Should().Be(1);
        }

        [Fact]
        public void MyShipmentsResponse_DebeInicializarClientSummaryPorDefecto()
        {
            var dto = new MyShipmentsResponse();

            dto.ClientSummaryResponseData.Should().NotBeNull();
        }

        [Fact]
        public void NotificationsSettingsResponse_DebePermitirSeccionesNulas()
        {
            var dto = new NotificationsSettingsResponse();

            dto.NotificationChannels.Should().BeNull();
            dto.NotificationEvents.Should().BeNull();
        }

        [Fact]
        public void MasterSettingsResponse_DebeAsignarSeccionesAnidadas()
        {
            var dto = new MasterSettingsResponse
            {
                IdMasterSettings = 1,
                GeneralParameters = new GeneralParametersResponse { AutomaticTrackingUpdate = true },
                Location = new LocationResponse { CurrencyType = "COP" },
                System = new SystemResponse { TimeZone = "UTC", DataRetentionDays = 30 }
            };

            dto.GeneralParameters!.AutomaticTrackingUpdate.Should().BeTrue();
            dto.Location!.CurrencyType.Should().Be("COP");
            dto.System!.DataRetentionDays.Should().Be(30);
        }

        [Fact]
        public void UsersManagementUpdateRequest_ValoresPorDefecto_DebenSerCadenasVacias()
        {
            var dto = new UsersManagementUpdateRequest();

            dto.Email.Should().Be(String.Empty);
            dto.UserName.Should().Be(String.Empty);
        }

        [Fact]
        public void CreateCustomerNotificationChannelsDto_EsUnRecordConIgualdadPorValor()
        {
            var dto1 = new CreateCustomerNotificationChannelsDto(1, true, true, false);
            var dto2 = new CreateCustomerNotificationChannelsDto(1, true, true, false);

            dto1.Should().Be(dto2);
        }

        [Fact]
        public void MasterSettingsResponseDto_EsUnRecordConIgualdadPorValor()
        {
            var dto1 = new MasterSettingsResponseDto(1, true, true, true, "COP", "es", "UTC", 30);
            var dto2 = new MasterSettingsResponseDto(1, true, true, true, "COP", "es", "UTC", 30);

            dto1.Should().Be(dto2);
        }

        [Fact]
        public void ReportsSummaryResponse_FrequentRoutes_DebePermitirNull()
        {
            var dto = new ReportsSummaryResponse();

            dto.FrequentRoutes.Should().BeNull();
        }

        [Fact]
        public void TrackingShipmentsResponse_DebeAsignarCoordenadasDeOrigenYDestino()
        {
            var dto = new TrackingShipmentsResponse
            {
                State = "En tránsito",
                OriginNameCoordinates = "Bogota",
                OriginLatitudCoordinates = "4.6",
                OriginLongitudCoordinates = "-74.0",
                DestinationNameCoordinates = "Miami",
                DestinationLatitudCoordinates = "25.7",
                DestinationLongitudCoordinates = "-80.1"
            };

            dto.OriginNameCoordinates.Should().Be("Bogota");
            dto.DestinationNameCoordinates.Should().Be("Miami");
        }

        [Fact]
        public void ClientSummaryRequest_DebeAsignarLosDatosDeAccesoDeClientes()
        {
            var dto = new ClientSummaryRequest { IdClient = "1", RoleName = "ANALISTAOPE", IdQueryClient = "CUS-1", AllClient = true };

            dto.IdQueryClient.Should().Be("CUS-1");
            dto.AllClient.Should().BeTrue();
        }

        [Fact]
        public void MyShipmentsRequest_DebeAsignarLosDatosDeAccesoDeClientes()
        {
            var dto = new MyShipmentsRequest { IdClient = "1", IdQueryClient = "CUS-1", AllClient = true };

            dto.IdQueryClient.Should().Be("CUS-1");
            dto.AllClient.Should().BeTrue();
        }

        [Fact]
        public void ReportsSummaryResponse_DebeAsignarClienteYTotales()
        {
            var dto = new ReportsSummaryResponse
            {
                ClientNit = "900123456",
                ClientName = "Cliente",
                TotalClientRecords = 3,
                TotalInvoiced = 10.5,
                FrequentRoutes = new List<ReportsFrequentRoutesResponse> { new() { Origin = "BOG", Destination = "MIA", TotalRoute = 2 } }
            };

            dto.ClientNit.Should().Be("900123456");
            dto.ClientName.Should().Be("Cliente");
            dto.TotalClientRecords.Should().Be(3);
            dto.TotalInvoiced.Should().Be(10.5);
            dto.FrequentRoutes.Should().ContainSingle(x => x.TotalRoute == 2);
        }

        [Fact]
        public void ResumenClienteResponse_DebeAsignarTodasLasPropiedades()
        {
            var dto = new ResumenClienteResponse
            {
                Id = 1,
                ClientNit = "900123456",
                ClientName = "Cliente",
                DocumentNumber = "HBL-1",
                Origin = "BOG",
                Destination = "MIA",
                Status = "Pendiente",
                OperationType = "IMPO",
                ShipmentMode = "AIR"
            };

            dto.ClientNit.Should().Be("900123456");
            dto.ClientName.Should().Be("Cliente");
            dto.Status.Should().Be("Pendiente");
            dto.ShipmentMode.Should().Be("AIR");
        }

        [Fact]
        public void ResumeMyShipmentsResponse_DebeAsignarClienteYFechas()
        {
            var fecha = new DateTime(2024, 6, 1);
            var dto = new ResumeMyShipmentsResponse { Id = 1, ClientNit = "900123456", ETDDate = fecha, ATADate = fecha };

            dto.ClientNit.Should().Be("900123456");
            dto.ETDDate.Should().Be(fecha);
            dto.ATADate.Should().Be(fecha);
        }
    }
}
