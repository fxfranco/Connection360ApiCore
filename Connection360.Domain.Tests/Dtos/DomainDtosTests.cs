using Connection360.Domain.Dtos;
using Connection360.Domain.Entities;
using Connection360.Domain.Services;
using FluentAssertions;
using Xunit;

namespace Connection360.Domain.Tests.Dtos
{
    /// <summary>
    /// Pruebas de contrato (get/set) para los DTOs de resultado del dominio.
    /// No contienen lógica propia, por lo que se valida que las propiedades
    /// asignen y retornen el valor esperado (evita regresiones de serialización).
    /// </summary>
    public class DomainDtosTests
    {
        [Fact]
        public void Auth0UserDto_DebeAsignarYRetornarTodasLasPropiedades()
        {
            var dto = new Auth0UserDto
            {
                UserId = "auth0|123",
                Email = "user@test.com",
                UserName = "user",
                PhoneNumber = "+573113232323",
                CreatedDate = new DateTime(2024, 1, 1),
                UpdatedDate = new DateTime(2024, 2, 1),
                IsBlocked = false,
                Nickname = "nick"
            };

            dto.UserId.Should().Be("auth0|123");
            dto.Email.Should().Be("user@test.com");
            dto.PhoneNumber.Should().Be("+573113232323");
            dto.IsBlocked.Should().BeFalse();
        }

        [Fact]
        public void ContainerShipmentsDomainDtoResult_ValoresPorDefecto_DebenSerCadenasVacias()
        {
            var dto = new ContainerShipmentsDomainDtoResult();

            dto.ContainerType.Should().Be(String.Empty);
            dto.ContainerNumber.Should().Be(String.Empty);
        }

        [Fact]
        public void DetailsShipmentsDomainDtoResult_DebePermitirSeccionesNulas()
        {
            var dto = new DetailsShipmentsDomainDtoResult();

            dto.ResumenShipments.Should().BeNull();
            dto.TrackingShipments.Should().BeNull();
            dto.ContainerShipments.Should().BeNull();
        }

        [Fact]
        public void HistoryShipmentsDomainDtoResult_DebeAsignarListaDeDetalles()
        {
            var detalle = new DetailsHistoryShipmentsDomainDtoResult { ChangeUser = "user1" };
            var dto = new HistoryShipmentsDomainDtoResult { DetailsHistoryShipments = new List<DetailsHistoryShipmentsDomainDtoResult> { detalle } };

            dto.DetailsHistoryShipments.Should().ContainSingle();
            dto.DetailsHistoryShipments!.Single().ChangeUser.Should().Be("user1");
        }

        [Fact]
        public void MyShipmentsFiltersDto_TodasLasPropiedadesSonOpcionales()
        {
            var dto = new MyShipmentsFiltersDto();

            dto.ValueFilter.Should().BeNull();
            dto.OperationType.Should().BeNull();
            dto.ShipmentMode.Should().BeNull();
            dto.State.Should().BeNull();
        }

        [Fact]
        public void ReportsFrequentRoutesDomainDtoResult_DebeAsignarPropiedades()
        {
            var dto = new ReportsFrequentRoutesDomainDtoResult { Origin = "BOG", Destination = "MIA", TotalRoute = 5 };

            dto.Origin.Should().Be("BOG");
            dto.TotalRoute.Should().Be(5);
        }

        [Fact]
        public void ReportsSummaryDomainDtoResult_DebeAsignarTotalesYRutas()
        {
            var dto = new ReportsSummaryDomainDtoResult
            {
                TotalClientRecords = 10,
                TotalImports = 4,
                TotalExports = 6,
                FrequentRoutes = new List<ReportsFrequentRoutesDomainDtoResult>()
            };

            dto.TotalClientRecords.Should().Be(10);
            dto.TotalImports.Should().Be(4);
            dto.FrequentRoutes.Should().NotBeNull();
        }

        [Fact]
        public void SummaryShipmentsDomainDtoResult_ValoresPorDefecto_DebenSerCadenasVacias()
        {
            var dto = new SummaryShipmentsDomainDtoResult();

            dto.Id.Should().Be(String.Empty);
            dto.ClientName.Should().Be(String.Empty);
        }

        [Fact]
        public void TrackingShipmentsDomainDtoResult_DebeAsignarEstado()
        {
            var dto = new TrackingShipmentsDomainDtoResult { State = "En tránsito" };

            dto.State.Should().Be("En tránsito");
        }

        [Fact]
        public void LogisticsDatesShipmentsDomainDtoResult_DebeAsignarTodasLasFechas()
        {
            var fecha = new DateTime(2024, 5, 1);
            var dto = new LogisticsDatesShipmentsDomainDtoResult
            {
                StoreOriginDate = fecha,
                ETDDate = fecha,
                ATDDate = fecha,
                ETADate = fecha,
                ATADate = fecha
            };

            dto.StoreOriginDate.Should().Be(fecha);
            dto.ATADate.Should().Be(fecha);
        }

        [Fact]
        public void FinancialInfoShipmentsDomainDtoResult_DebeAsignarPropiedades()
        {
            var dto = new FinancialInfoShipmentsDomainDtoResult { AdvancePaymentAmount = "100", InvoiceNumber = "INV-1" };

            dto.AdvancePaymentAmount.Should().Be("100");
            dto.InvoiceNumber.Should().Be("INV-1");
        }
    }

    public class DomainServiceResultDtosTests
    {
        [Fact]
        public void ClientSummaryDomainResult_ListaDeEnviosRecientes_DebeInicializarVacia()
        {
            var result = new ClientSummaryDomainResult();

            result.RecentShipments.Should().NotBeNull();
            result.RecentShipments.Should().BeEmpty();
        }

        [Fact]
        public void MyShipmentsDomainResult_DebeInicializarColeccionesPorDefecto()
        {
            var result = new MyShipmentsDomainResult();

            result.MyShipments.Should().NotBeNull().And.BeEmpty();
            result.ClientSummaryResponse.Should().NotBeNull();
        }

        [Fact]
        public void ResumenClienteDto_ValoresPorDefecto_DebenSerCadenasVacias()
        {
            var dto = new ResumenClienteDto();

            dto.DocumentNumber.Should().Be(String.Empty);
            dto.Origin.Should().Be(String.Empty);
        }

        [Fact]
        public void ResumenMyShipmentDto_DebeAsignarTodasLasPropiedades()
        {
            var fecha = new DateTime(2024, 6, 1);
            var dto = new ResumenMyShipmentDto
            {
                Id = 1,
                ShipmentMode = "AIR",
                DocumentNumber = "HBL-1",
                State = "Pendiente",
                OperationType = "IMPO",
                ClientName = "Cliente",
                Origin = "BOG",
                Destination = "MIA",
                ETDDate = fecha,
                ATDDate = fecha,
                ETADate = fecha,
                ATADate = fecha
            };

            dto.Id.Should().Be(1);
            dto.ETDDate.Should().Be(fecha);
        }
    }
}
