using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using Connection360.Application.UseCases;
using Connection360.Domain.Dtos;
using Connection360.Domain.Entities;
using Connection360.Domain.Interfaces;
using Connection360.Domain.Services;
using FluentAssertions;
using Moq;
using Xunit;
using static Connection360.Application.Tests.TestSupport.DynamicDataSetBuilder;

namespace Connection360.Application.Tests.UseCases
{
    public class GetClientSummaryUseCaseTests
    {
        private readonly Mock<IExternalDataGateway> _externalDataGatewayMock = new();
        private readonly Mock<IClientSummaryDomainService> _domainServiceMock = new();
        private readonly Mock<IClientAccessResolver> _clientAccessResolverMock = new();
        private readonly List<CustomersOfCollaboratorDtoResult> _customersOfCollaborator = new()
        {
            new() { IdCollaborator = 1, IdentificacionCollaborator = "COL-1", IdCustomer = 10, IdentificacionCustomer = "CUS-10" }
        };
        private readonly GetClientSummaryUseCase _sut;

        public GetClientSummaryUseCaseTests()
        {
            _sut = new GetClientSummaryUseCase(_externalDataGatewayMock.Object, _domainServiceMock.Object, _clientAccessResolverMock.Object);

            _clientAccessResolverMock
                .Setup(r => r.ResolveAsync(It.IsAny<ResolveClientAccessRequest>()))
                .ReturnsAsync(_customersOfCollaborator);

            _externalDataGatewayMock
                .Setup(g => g.FetchDataAsync(It.IsAny<String>(), It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Empty());
        }

        [Fact]
        public async Task ExecuteTotalsAsync_SiElResolverRechazaElAcceso_PropagaLaExcepcionYNoConsultaElGateway()
        {
            var request = new ClientSummaryRequest { IdClient = "" };
            _clientAccessResolverMock
                .Setup(r => r.ResolveAsync(It.IsAny<ResolveClientAccessRequest>()))
                .ThrowsAsync(new ArgumentException("El campo 'cliente' es obligatorio."));

            Func<Task> act = () => _sut.ExecuteTotalsAsync(request, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
            _externalDataGatewayMock.Verify(g => g.FetchDataAsync(It.IsAny<String>(), It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteTotalsAsync_ResuelveElAccesoConLosDatosDeLaSolicitud()
        {
            var request = new ClientSummaryRequest { IdClient = "123", RoleName = "ANALISTAOPE", AllClient = false, IdQueryClient = "CUS-10" };
            ResolveClientAccessRequest? captured = null;
            _clientAccessResolverMock
                .Setup(r => r.ResolveAsync(It.IsAny<ResolveClientAccessRequest>()))
                .Callback<ResolveClientAccessRequest>(r => captured = r)
                .ReturnsAsync(_customersOfCollaborator);
            _domainServiceMock
                .Setup(s => s.Summarize(It.IsAny<DynamicDataSet>(), It.IsAny<String>(), It.IsAny<List<CustomersOfCollaboratorDtoResult>?>(), It.IsAny<Int16>()))
                .Returns(new ClientSummaryDomainResult());

            await _sut.ExecuteTotalsAsync(request, CancellationToken.None);

            captured.Should().NotBeNull();
            captured!.IdClient.Should().Be("123");
            captured.RoleName.Should().Be("ANALISTAOPE");
            captured.AllClient.Should().BeFalse();
            captured.IdQueryClient.Should().Be("CUS-10");
        }

        [Fact]
        public async Task ExecuteTotalsAsync_ConsultaElGatewayConLaApiBPMS()
        {
            var request = new ClientSummaryRequest { IdClient = "123", RoleName = "CLIENT" };
            _domainServiceMock
                .Setup(s => s.Summarize(It.IsAny<DynamicDataSet>(), "123", _customersOfCollaborator, It.IsAny<Int16>()))
                .Returns(new ClientSummaryDomainResult());

            await _sut.ExecuteTotalsAsync(request, CancellationToken.None);

            _externalDataGatewayMock.Verify(g => g.FetchDataAsync("BPMS", It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteTotalsAsync_ConRolAdmin_ConsultaElDominioSinFiltrarPorCliente()
        {
            var request = new ClientSummaryRequest { IdClient = "admin-1", RoleName = "ADMIN" };
            _domainServiceMock
                .Setup(s => s.Summarize(It.IsAny<DynamicDataSet>(), String.Empty, _customersOfCollaborator, 10))
                .Returns(new ClientSummaryDomainResult { TotalClientRecords = 99 });

            ClientSummaryResponse result = await _sut.ExecuteTotalsAsync(request, CancellationToken.None);

            result.TotalClientRecords.Should().Be(99);
            request.IdClient.Should().Be(String.Empty);
        }

        [Fact]
        public async Task ExecuteTotalsAsync_ConRolCliente_ConservaElIdDelCliente()
        {
            var request = new ClientSummaryRequest { IdClient = "123", RoleName = "CLIENT" };
            _domainServiceMock
                .Setup(s => s.Summarize(It.IsAny<DynamicDataSet>(), "123", _customersOfCollaborator, 10))
                .Returns(new ClientSummaryDomainResult { TotalClientRecords = 5 });

            ClientSummaryResponse result = await _sut.ExecuteTotalsAsync(request, CancellationToken.None);

            result.TotalClientRecords.Should().Be(5);
            request.IdClient.Should().Be("123");
        }

        [Fact]
        public async Task ExecuteTotalsAsync_MapeaCorrectamenteElResultadoDelDominio()
        {
            var request = new ClientSummaryRequest { IdClient = "123", RoleName = "CLIENT" };

            var domainResult = new ClientSummaryDomainResult
            {
                TotalClientRecords = 10,
                TotalImports = 4,
                TotalExports = 6,
                TotalAirShipments = 3,
                TotalOceanShipments = 7,
                TotalWithIssues = 2,
                RecentShipments = new List<ResumenClienteDto>
                {
                    new()
                    {
                        Id = 1,
                        ClientNit = "123",
                        ClientName = "Cliente",
                        DocumentNumber = "HBL-001",
                        Origin = "BOG",
                        Destination = "MIA",
                        Status = "En tránsito",
                        OperationType = "IMPO",
                        ShipmentMode = "AIR"
                    }
                }
            };
            _domainServiceMock.Setup(s => s.Summarize(It.IsAny<DynamicDataSet>(), "123", _customersOfCollaborator, 10)).Returns(domainResult);

            ClientSummaryResponse result = await _sut.ExecuteTotalsAsync(request, CancellationToken.None);

            result.TotalClientRecords.Should().Be(10);
            result.TotalImports.Should().Be(4);
            result.TotalExports.Should().Be(6);
            result.TotalAirShipments.Should().Be(3);
            result.TotalOceanShipments.Should().Be(7);
            result.TotalWithIssues.Should().Be(2);
            result.RecentShipments.Should().ContainSingle();

            ResumenClienteResponse shipment = result.RecentShipments![0];
            shipment.Id.Should().Be(1);
            shipment.ClientNit.Should().Be("123");
            shipment.ClientName.Should().Be("Cliente");
            shipment.DocumentNumber.Should().Be("HBL-001");
            shipment.Origin.Should().Be("BOG");
            shipment.Destination.Should().Be("MIA");
            shipment.Status.Should().Be("En tránsito");
            shipment.OperationType.Should().Be("IMPO");
            shipment.ShipmentMode.Should().Be("AIR");
        }

        [Fact]
        public async Task ExecuteFilterAsync_SiElResolverRechazaElAcceso_PropagaLaExcepcion()
        {
            var request = new ClientSummaryRequest { IdClient = "" };
            _clientAccessResolverMock
                .Setup(r => r.ResolveAsync(It.IsAny<ResolveClientAccessRequest>()))
                .ThrowsAsync(new ArgumentException("El campo 'cliente' es obligatorio."));

            Func<Task> act = () => _sut.ExecuteFilterAsync(request, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
            _externalDataGatewayMock.Verify(g => g.FetchDataAsync(It.IsAny<String>(), It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteFilterAsync_MapeaCorrectamenteElResultado()
        {
            var request = new ClientSummaryRequest { IdClient = "123", RoleName = "CLIENT", FilterValue = "HBL-001" };
            _domainServiceMock
                .Setup(s => s.Filter(It.IsAny<DynamicDataSet>(), "123", _customersOfCollaborator, "HBL-001"))
                .Returns(new ResumenClienteDto
                {
                    Id = 5,
                    ClientNit = "123",
                    ClientName = "Cliente",
                    DocumentNumber = "HBL-001",
                    Origin = "BOG",
                    Destination = "MIA",
                    Status = "Pendiente",
                    OperationType = "EXPO",
                    ShipmentMode = "SEA"
                });

            ResumenClienteResponse result = await _sut.ExecuteFilterAsync(request, CancellationToken.None);

            result.Id.Should().Be(5);
            result.ClientNit.Should().Be("123");
            result.ClientName.Should().Be("Cliente");
            result.DocumentNumber.Should().Be("HBL-001");
            result.Origin.Should().Be("BOG");
            result.Destination.Should().Be("MIA");
            result.Status.Should().Be("Pendiente");
            result.OperationType.Should().Be("EXPO");
            result.ShipmentMode.Should().Be("SEA");
        }

        [Fact]
        public async Task ExecuteFilterAsync_ConRolAdmin_ConsultaElDominioSinFiltrarPorCliente()
        {
            var request = new ClientSummaryRequest { IdClient = "admin-1", RoleName = "ADMIN", FilterValue = "HBL-001" };
            _domainServiceMock
                .Setup(s => s.Filter(It.IsAny<DynamicDataSet>(), String.Empty, _customersOfCollaborator, "HBL-001"))
                .Returns(new ResumenClienteDto { Id = 8 });

            ResumenClienteResponse result = await _sut.ExecuteFilterAsync(request, CancellationToken.None);

            result.Id.Should().Be(8);
            _externalDataGatewayMock.Verify(g => g.FetchDataAsync("BPMS", It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
