using Connection360.Domain.Dtos;
using Connection360.Domain.Entities;
using Connection360.Domain.Services;
using FluentAssertions;
using Xunit;

namespace Connection360.Domain.Tests.Dtos
{
    // Los DTOs de dominio son contenedores de datos (POCOs). Se valida que las propiedades
    // asignen y expongan correctamente los valores, y que los valores por defecto sean los esperados.

    public class Auth0UserDtoTests
    {
        [Fact]
        public void PropiedadesPorDefecto_SonCadenasVacias()
        {
            var dto = new Auth0UserDto();

            dto.UserId.Should().Be(String.Empty);
            dto.Email.Should().Be(String.Empty);
        }

        [Fact]
        public void Propiedades_AsignanYRetornanElValorProvisto()
        {
            var dto = new Auth0UserDto { UserId = "abc", Email = "a@a.com", UserName = "Juan", PhoneNumber = "+573001112233", IsBlocked = true };

            dto.UserId.Should().Be("abc");
            dto.Email.Should().Be("a@a.com");
            dto.UserName.Should().Be("Juan");
            dto.PhoneNumber.Should().Be("+573001112233");
            dto.IsBlocked.Should().BeTrue();
        }
    }

    public class ContainerShipmentsDomainDtoResultTests
    {
        [Fact]
        public void Propiedades_AsignanYRetornanElValorProvisto()
        {
            var dto = new ContainerShipmentsDomainDtoResult { ContainerType = "40HC", ContainerAmount = "2", ContainerNumber = "ABC123" };

            dto.ContainerType.Should().Be("40HC");
            dto.ContainerAmount.Should().Be("2");
            dto.ContainerNumber.Should().Be("ABC123");
        }
    }

    public class DetailsHistoryShipmentsDomainDtoResultTests
    {
        [Fact]
        public void Propiedades_AsignanYRetornanElValorProvisto()
        {
            var date = new DateTime(2024, 1, 1);
            var dto = new DetailsHistoryShipmentsDomainDtoResult { ChangeDate = date, ChangeUser = "u1", Message = "m1", OldState = "A", NewState = "B" };

            dto.ChangeDate.Should().Be(date);
            dto.ChangeUser.Should().Be("u1");
            dto.OldState.Should().Be("A");
            dto.NewState.Should().Be("B");
        }
    }

    public class DetailsShipmentsDomainDtoResultTests
    {
        [Fact]
        public void PropiedadesAnidadas_SonNulasPorDefecto()
        {
            var dto = new DetailsShipmentsDomainDtoResult();

            dto.ResumenShipments.Should().BeNull();
            dto.TrackingShipments.Should().BeNull();
            dto.HistoryShipments.Should().BeNull();
        }

        [Fact]
        public void Propiedades_AceptanObjetosAnidados()
        {
            var dto = new DetailsShipmentsDomainDtoResult
            {
                ResumenShipments = new SummaryShipmentsDomainDtoResult { Id = "1" },
                TrackingShipments = new TrackingShipmentsDomainDtoResult { State = "OK" }
            };

            dto.ResumenShipments!.Id.Should().Be("1");
            dto.TrackingShipments!.State.Should().Be("OK");
        }
    }

    public class FinancialInfoShipmentsDomainDtoResultTests
    {
        [Fact]
        public void Propiedades_AsignanYRetornanElValorProvisto()
        {
            var dto = new FinancialInfoShipmentsDomainDtoResult { AdvancePaymentAmount = "100", TotalInvoiceUSD = "500" };

            dto.AdvancePaymentAmount.Should().Be("100");
            dto.TotalInvoiceUSD.Should().Be("500");
        }
    }

    public class HistoryShipmentsDomainDtoResultTests
    {
        [Fact]
        public void DetailsHistoryShipments_EsNuloPorDefecto()
        {
            new HistoryShipmentsDomainDtoResult().DetailsHistoryShipments.Should().BeNull();
        }

        [Fact]
        public void DetailsHistoryShipments_AceptaUnaLista()
        {
            var dto = new HistoryShipmentsDomainDtoResult
            {
                DetailsHistoryShipments = new List<DetailsHistoryShipmentsDomainDtoResult> { new() { Message = "m" } }
            };

            dto.DetailsHistoryShipments.Should().ContainSingle(x => x.Message == "m");
        }
    }

    public class LogisticsDatesShipmentsDomainDtoResultTests
    {
        [Fact]
        public void Propiedades_AsignanYRetornanElValorProvisto()
        {
            var date = new DateTime(2024, 5, 1);
            var dto = new LogisticsDatesShipmentsDomainDtoResult { ETDDate = date, ATDDate = date };

            dto.ETDDate.Should().Be(date);
            dto.ATDDate.Should().Be(date);
        }
    }

    public class MyShipmentsFiltersDtoTests
    {
        [Fact]
        public void PropiedadesPorDefecto_SonNulas()
        {
            var dto = new MyShipmentsFiltersDto();

            dto.ValueFilter.Should().BeNull();
            dto.OperationType.Should().BeNull();
        }

        [Fact]
        public void Propiedades_AsignanYRetornanElValorProvisto()
        {
            var dto = new MyShipmentsFiltersDto { ValueFilter = "v", OperationType = "IMPO", ShipmentMode = "AIR", State = "Pendiente" };

            dto.ValueFilter.Should().Be("v");
            dto.OperationType.Should().Be("IMPO");
            dto.ShipmentMode.Should().Be("AIR");
            dto.State.Should().Be("Pendiente");
        }
    }

    public class ReportsFrequentRoutesDomainDtoResultTests
    {
        [Fact]
        public void Propiedades_AsignanYRetornanElValorProvisto()
        {
            var dto = new ReportsFrequentRoutesDomainDtoResult { Origin = "BOG", Destination = "MIA", TotalRoute = 5 };

            dto.Origin.Should().Be("BOG");
            dto.Destination.Should().Be("MIA");
            dto.TotalRoute.Should().Be(5);
        }
    }

    public class ReportsSummaryDomainDtoResultTests
    {
        [Fact]
        public void FrequentRoutes_EsNuloPorDefecto()
        {
            new ReportsSummaryDomainDtoResult().FrequentRoutes.Should().BeNull();
        }

        [Fact]
        public void Propiedades_AsignanYRetornanElValorProvisto()
        {
            var dto = new ReportsSummaryDomainDtoResult { TotalClientRecords = 10, TotalInvoiced = 999.9 };

            dto.TotalClientRecords.Should().Be(10);
            dto.TotalInvoiced.Should().Be(999.9);
        }
    }

    public class SummaryShipmentsDomainDtoResultTests
    {
        [Fact]
        public void PropiedadesPorDefecto_SonCadenasVacias()
        {
            var dto = new SummaryShipmentsDomainDtoResult();

            dto.Id.Should().Be(String.Empty);
            dto.ClientName.Should().Be(String.Empty);
        }
    }

    public class TrackingShipmentsDomainDtoResultTests
    {
        [Fact]
        public void State_AsignaYRetornaElValorProvisto()
        {
            var dto = new TrackingShipmentsDomainDtoResult { State = "En tránsito" };

            dto.State.Should().Be("En tránsito");
        }
    }

    public class ResumenClienteDtoTests
    {
        [Fact]
        public void PropiedadesPorDefecto_SonLosEsperados()
        {
            var dto = new ResumenClienteDto();

            dto.Id.Should().Be(0);
            dto.DocumentNumber.Should().Be(String.Empty);
        }
    }

    public class ResumenMyShipmentDtoTests
    {
        [Fact]
        public void Propiedades_AsignanYRetornanElValorProvisto()
        {
            var dto = new ResumenMyShipmentDto { Id = 1, ShipmentMode = "AIR", ClientName = "Cliente" };

            dto.Id.Should().Be(1);
            dto.ShipmentMode.Should().Be("AIR");
            dto.ClientName.Should().Be("Cliente");
        }
    }

    public class ClientSummaryDomainResultTests
    {
        [Fact]
        public void RecentShipments_EsUnaListaVaciaPorDefecto()
        {
            new ClientSummaryDomainResult().RecentShipments.Should().NotBeNull().And.BeEmpty();
        }
    }

    public class MyShipmentsDomainResultTests
    {
        [Fact]
        public void PropiedadesPorDefecto_SonInstanciasVacias()
        {
            var result = new MyShipmentsDomainResult();

            result.MyShipments.Should().NotBeNull().And.BeEmpty();
            result.ClientSummaryResponse.Should().NotBeNull();
        }
    }
}
