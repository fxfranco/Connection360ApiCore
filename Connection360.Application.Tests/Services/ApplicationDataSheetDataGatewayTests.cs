using System.Globalization;
using Connection360.Application.DTOs;
using Connection360.Application.Services;
using Connection360.Domain.Constans;
using Connection360.Domain.Dtos;
using Connection360.Domain.Entities;
using Connection360.Domain.Enums;
using Connection360.Domain.Ports.Persistence;
using FluentAssertions;
using Moq;
using Xunit;

namespace Connection360.Application.Tests.Services
{
    public class ApplicationDataSheetDataGatewayTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IApplicationDataSheetEntregadosRepository> _entregadosMock = new(MockBehavior.Strict);
        private readonly Mock<IApplicationDataSheetNoEntregadosRepository> _noEntregadosMock = new(MockBehavior.Strict);
        private readonly ApplicationDataSheetDataGateway _gateway;

        public ApplicationDataSheetDataGatewayTests()
        {
            _unitOfWorkMock.Setup(u => u.GetRepository<IApplicationDataSheetEntregadosRepository>()).Returns(_entregadosMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<IApplicationDataSheetNoEntregadosRepository>()).Returns(_noEntregadosMock.Object);
            _gateway = new ApplicationDataSheetDataGateway(_unitOfWorkMock.Object);
        }

        private static ApplicationDataSheetViewResultDto FullRow() => new()
        {
            Id = 1234567890123L,
            FechaCreacion = new DateTime(2025, 1, 2, 3, 4, 5),
            TipoOperacion = "Importacion",
            Modalidad = "Maritimo",
            Incoterm = "FOB",
            Proveedor = "Proveedor SA",
            Cliente = "Cliente SA",
            NitCliente = "900123456",
            Origen = "Shanghai",
            Destino = "Cartagena",
            DescripcionMercancia = "Textiles",
            Estado = "Entregado",
            TipoCarga = "FCL",
            TipoContenedor = "40HC",
            CantidadContenedores = 2,
            NumeroContenedor = "MSKU1234567",
            CantidadBultos = 150,
            PesoKg = 1234.56m,
            VolumenM3 = 78.9m,
            Transportista = "Maersk",
            TipoDocumento = "HBL",
            NombreDocumento = "HBL-0001",
            DocumentoTransporteHbl = "HBL-0001",
            FechaBodegaOrigen = new DateTime(2025, 2, 1, 1, 1, 1),
            FechaEtd = new DateTime(2025, 2, 2, 1, 1, 1),
            FechaAtd = new DateTime(2025, 2, 3, 1, 1, 1),
            FechaEta = new DateTime(2025, 3, 1, 1, 1, 1),
            FechaAta = new DateTime(2025, 3, 2, 1, 1, 1),
            FechaBodegaDestino = new DateTime(2025, 3, 3, 1, 1, 1),
            FechaNacionalizacion = new DateTime(2025, 3, 4, 1, 1, 1),
            FechaDespachoDestino = new DateTime(2025, 3, 5, 1, 1, 1),
            FechaPlanilla = new DateTime(2025, 3, 6, 1, 1, 1),
            FechaEntregaContenedor = new DateTime(2025, 3, 7, 1, 1, 1),
            FechaDevolucionRealContenedor = new DateTime(2025, 3, 8, 1, 1, 1),
            DiasLibres = 7,
            DiasRestantesEntrega = 3,
            DiasDemoraContenedor = 4,
            ValorDiaDemora = 10.5m,
            ValorTotalDemora = 42m,
            DepositoContenedor = 500.25m,
            FechaSolicitudAnticipo = new DateTime(2025, 4, 1, 1, 1, 1),
            FechaPagoAnticipo = new DateTime(2025, 4, 2, 1, 1, 1),
            ValorAnticipo = 1000m,
            FacturaProveedor = "FP-1",
            FacturaTcc = "FT-1",
            NumeroFactura = "NF-1",
            FechaFactura = new DateTime(2025, 4, 3, 1, 1, 1),
            DescripcionGasto = "Flete",
            ValorGastoUsd = 2000.5m,
            SubtotalFacturaUsd = 3000m,
            IvaUsd = 570m,
            TotalFacturaUsd = 3570m,
            Comentario = "Sin novedad",
            FechaComentario = new DateTime(2025, 4, 4, 1, 1, 1)
        };

        private static ApplicationDataSheetViewFieldsSelectionDto Selection(params ApplicationDataSheetViewField[] fields) =>
            new() { Fields = fields.ToList() };

        // ---------- FetchDataAsync(IDictionary) ----------

        [Fact]
        public async Task FetchDataAsync_FiltrosConNit_DebeConsultarAmbasVistasPorNit()
        {
            _entregadosMock.Setup(r => r.GetByNitClienteAsync("900123456", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ApplicationDataSheetViewResultDto> { new() { Id = 1, NitCliente = "900123456" } });
            _noEntregadosMock.Setup(r => r.GetByNitClienteAsync("900123456", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ApplicationDataSheetViewResultDto> { new() { Id = 2, NitCliente = "900123456" } });

            var filters = new Dictionary<String, String> { [ExternalDataFields.ClientNit] = "900123456" };

            DynamicDataSet result = await _gateway.FetchDataAsync(filters, CancellationToken.None);

            result.Rows.Should().HaveCount(2);
            result.Rows.Select(r => r[ExternalDataFields.ID]).Should().Equal("1", "2");
            _entregadosMock.Verify(r => r.GetByNitClienteAsync("900123456", It.IsAny<CancellationToken>()), Times.Once);
            _noEntregadosMock.Verify(r => r.GetByNitClienteAsync("900123456", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task FetchDataAsync_FiltrosSinNit_DebeConsultarTodoDeAmbasVistas()
        {
            _entregadosMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ApplicationDataSheetViewResultDto>());
            _noEntregadosMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ApplicationDataSheetViewResultDto> { new() { Id = 5 } });

            DynamicDataSet result = await _gateway.FetchDataAsync(new Dictionary<String, String>(), CancellationToken.None);

            result.Rows.Should().HaveCount(1);
            _entregadosMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
            _noEntregadosMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task FetchDataAsync_NitVacioOEspacios_DebeTratarloComoSinFiltro(String nit)
        {
            _entregadosMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ApplicationDataSheetViewResultDto>());
            _noEntregadosMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ApplicationDataSheetViewResultDto>());

            var filters = new Dictionary<String, String> { [ExternalDataFields.ClientNit] = nit };

            DynamicDataSet result = await _gateway.FetchDataAsync(filters, CancellationToken.None);

            result.Rows.Should().BeEmpty();
            _entregadosMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
            _noEntregadosMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task FetchDataAsync_FiltrosNull_DebeTratarloComoSinFiltro()
        {
            _entregadosMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ApplicationDataSheetViewResultDto>());
            _noEntregadosMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ApplicationDataSheetViewResultDto>());

            DynamicDataSet result = await _gateway.FetchDataAsync((IDictionary<String, String>)null!, CancellationToken.None);

            result.Rows.Should().BeEmpty();
            result.AvailableFields.Should().HaveCount(Enum.GetValues<ApplicationDataSheetViewField>().Length);
        }

        // ---------- FetchDataAsync(request): alcance ----------

        [Fact]
        public async Task FetchDataAsync_RequestNull_DebeLanzarArgumentNullException()
        {
            Func<Task> act = () => _gateway.FetchDataAsync((ApplicationDataSheetDataRequest)null!, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task FetchDataAsync_ScopeEntregados_SoloConsultaVistaDeEntregados()
        {
            _entregadosMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ApplicationDataSheetViewResultDto> { new() { Id = 1 } });

            var request = new ApplicationDataSheetDataRequest { Scope = ApplicationDataSheetViewScope.Entregados };

            DynamicDataSet result = await _gateway.FetchDataAsync(request, CancellationToken.None);

            result.Rows.Should().HaveCount(1);
            _unitOfWorkMock.Verify(u => u.GetRepository<IApplicationDataSheetNoEntregadosRepository>(), Times.Never);
            _noEntregadosMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task FetchDataAsync_ScopeNoEntregados_SoloConsultaVistaDeNoEntregados()
        {
            _noEntregadosMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ApplicationDataSheetViewResultDto> { new() { Id = 9 } });

            var request = new ApplicationDataSheetDataRequest { Scope = ApplicationDataSheetViewScope.NoEntregados };

            DynamicDataSet result = await _gateway.FetchDataAsync(request, CancellationToken.None);

            result.Rows.Should().ContainSingle().Which[ExternalDataFields.ID].Should().Be("9");
            _unitOfWorkMock.Verify(u => u.GetRepository<IApplicationDataSheetEntregadosRepository>(), Times.Never);
            _entregadosMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task FetchDataAsync_ScopeTodos_DebeConcatenarPrimeroEntregadosYLuegoNoEntregados()
        {
            _entregadosMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ApplicationDataSheetViewResultDto> { new() { Id = 1 }, new() { Id = 2 } });
            _noEntregadosMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ApplicationDataSheetViewResultDto> { new() { Id = 3 } });

            DynamicDataSet result = await _gateway.FetchDataAsync(new ApplicationDataSheetDataRequest(), CancellationToken.None);

            result.Rows.Select(r => r[ExternalDataFields.ID]).Should().Equal("1", "2", "3");
        }

        // ---------- FetchDataAsync(request): seleccion de metodo del repositorio ----------

        [Fact]
        public async Task FetchDataAsync_NitYSeleccion_DebeUsarGetByNitClienteConSeleccion()
        {
            var selection = Selection(ApplicationDataSheetViewField.Id, ApplicationDataSheetViewField.Estado);
            _entregadosMock.Setup(r => r.GetByNitClienteAsync("900", selection, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ApplicationDataSheetViewResultDto> { new() { Id = 1, Estado = "Entregado" } });
            _noEntregadosMock.Setup(r => r.GetByNitClienteAsync("900", selection, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ApplicationDataSheetViewResultDto>());

            var request = new ApplicationDataSheetDataRequest { NitCliente = "900", FieldsSelection = selection };

            DynamicDataSet result = await _gateway.FetchDataAsync(request, CancellationToken.None);

            result.AvailableFields.Should().Equal(ExternalDataFields.ID, ExternalDataFields.State);
            result.Rows.Should().ContainSingle();
            result.Rows[0][ExternalDataFields.State].Should().Be("Entregado");
            result.Rows[0].HasField(ExternalDataFields.ClientName).Should().BeFalse();
            _entregadosMock.Verify(r => r.GetByNitClienteAsync("900", selection, It.IsAny<CancellationToken>()), Times.Once);
            _noEntregadosMock.Verify(r => r.GetByNitClienteAsync("900", selection, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task FetchDataAsync_SoloSeleccion_DebeUsarGetAllConSeleccion()
        {
            var selection = Selection(ApplicationDataSheetViewField.Cliente);
            _entregadosMock.Setup(r => r.GetAllAsync(selection, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ApplicationDataSheetViewResultDto> { new() { Cliente = "ACME" } });
            _noEntregadosMock.Setup(r => r.GetAllAsync(selection, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ApplicationDataSheetViewResultDto>());

            var request = new ApplicationDataSheetDataRequest { FieldsSelection = selection };

            DynamicDataSet result = await _gateway.FetchDataAsync(request, CancellationToken.None);

            result.AvailableFields.Should().Equal(ExternalDataFields.ClientName);
            result.Rows.Should().ContainSingle().Which[ExternalDataFields.ClientName].Should().Be("ACME");
        }

        [Fact]
        public async Task FetchDataAsync_SeleccionSinCampos_DebeTratarseComoSinSeleccion()
        {
            _entregadosMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ApplicationDataSheetViewResultDto>());
            _noEntregadosMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ApplicationDataSheetViewResultDto>());

            var request = new ApplicationDataSheetDataRequest { FieldsSelection = new ApplicationDataSheetViewFieldsSelectionDto() };

            DynamicDataSet result = await _gateway.FetchDataAsync(request, CancellationToken.None);

            result.AvailableFields.Should().HaveCount(Enum.GetValues<ApplicationDataSheetViewField>().Length);
            _entregadosMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task FetchDataAsync_NitEnBlancoConSeleccion_DebeUsarGetAllConSeleccion()
        {
            var selection = Selection(ApplicationDataSheetViewField.Id);
            _entregadosMock.Setup(r => r.GetAllAsync(selection, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ApplicationDataSheetViewResultDto>());
            _noEntregadosMock.Setup(r => r.GetAllAsync(selection, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ApplicationDataSheetViewResultDto>());

            var request = new ApplicationDataSheetDataRequest { NitCliente = "  ", FieldsSelection = selection };

            DynamicDataSet result = await _gateway.FetchDataAsync(request, CancellationToken.None);

            result.AvailableFields.Should().Equal(ExternalDataFields.ID);
        }

        [Fact]
        public async Task FetchDataAsync_DebePropagarElCancellationToken()
        {
            using var cts = new CancellationTokenSource();
            _entregadosMock.Setup(r => r.GetAllAsync(cts.Token)).ReturnsAsync(new List<ApplicationDataSheetViewResultDto>());
            _noEntregadosMock.Setup(r => r.GetAllAsync(cts.Token)).ReturnsAsync(new List<ApplicationDataSheetViewResultDto>());

            await _gateway.FetchDataAsync(new ApplicationDataSheetDataRequest(), cts.Token);

            _entregadosMock.Verify(r => r.GetAllAsync(cts.Token), Times.Once);
            _noEntregadosMock.Verify(r => r.GetAllAsync(cts.Token), Times.Once);
        }

        [Fact]
        public async Task FetchDataAsync_SiElRepositorioLanzaExcepcion_DebePropagarla()
        {
            _entregadosMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));

            Func<Task> act = () => _gateway.FetchDataAsync(new ApplicationDataSheetDataRequest(), CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
        }

        [Fact]
        public async Task FetchDataAsync_SiLaCancelacionSeSolicita_DebePropagarOperationCanceled()
        {
            _entregadosMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new OperationCanceledException());

            Func<Task> act = () => _gateway.FetchDataAsync(new ApplicationDataSheetDataRequest(), new CancellationToken(true));

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        // ---------- Mapeo de campos ----------

        [Fact]
        public async Task FetchDataAsync_FilaCompleta_DebeMapearTodosLosCamposAlosNombresExternos()
        {
            _entregadosMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ApplicationDataSheetViewResultDto> { FullRow() });

            var request = new ApplicationDataSheetDataRequest { Scope = ApplicationDataSheetViewScope.Entregados };

            DynamicDataSet result = await _gateway.FetchDataAsync(request, CancellationToken.None);

            DynamicRecord r = result.Rows.Should().ContainSingle().Subject;
            result.AvailableFields.Should().HaveCount(Enum.GetValues<ApplicationDataSheetViewField>().Length).And.OnlyHaveUniqueItems();

            r[ExternalDataFields.ID].Should().Be("1234567890123");
            r[ExternalDataFields.CreationDate].Should().Be("2025-01-02 03:04:05");
            r[ExternalDataFields.OperationType].Should().Be("Importacion");
            r[ExternalDataFields.ShipmentMode].Should().Be("Maritimo");
            r[ExternalDataFields.Incoterm].Should().Be("FOB");
            r[ExternalDataFields.Supplier].Should().Be("Proveedor SA");
            r[ExternalDataFields.ClientName].Should().Be("Cliente SA");
            r[ExternalDataFields.ClientNit].Should().Be("900123456");
            r[ExternalDataFields.Origin].Should().Be("Shanghai");
            r[ExternalDataFields.Destination].Should().Be("Cartagena");
            r[ExternalDataFields.MerchandiseDescription].Should().Be("Textiles");
            r[ExternalDataFields.State].Should().Be("Entregado");
            r[ExternalDataFields.LoadType].Should().Be("FCL");
            r[ExternalDataFields.ContainerType].Should().Be("40HC");
            r[ExternalDataFields.ContainerAmount].Should().Be("2");
            r[ExternalDataFields.ContainerNumber].Should().Be("MSKU1234567");
            r[ExternalDataFields.PackagesNumbers].Should().Be("150");
            r[ExternalDataFields.WeightKg].Should().Be("1234.56");
            r[ExternalDataFields.VolumeM3].Should().Be("78.9");
            r[ExternalDataFields.Carrier].Should().Be("Maersk");
            r[ExternalDataFields.DocumentType].Should().Be("HBL");
            r[ExternalDataFields.DocumentName].Should().Be("HBL-0001");
            r[ExternalDataFields.DocumentNumber].Should().Be("HBL-0001");
            r[ExternalDataFields.StoreOriginDate].Should().Be("2025-02-01 01:01:01");
            r[ExternalDataFields.ETDDate].Should().Be("2025-02-02 01:01:01");
            r[ExternalDataFields.ATDDate].Should().Be("2025-02-03 01:01:01");
            r[ExternalDataFields.ETADate].Should().Be("2025-03-01 01:01:01");
            r[ExternalDataFields.ATADate].Should().Be("2025-03-02 01:01:01");
            r[ExternalDataFields.StoreDestinationDate].Should().Be("2025-03-03 01:01:01");
            r[ExternalDataFields.NationalizationDate].Should().Be("2025-03-04 01:01:01");
            r[ExternalDataFields.DispatchDestinationDate].Should().Be("2025-03-05 01:01:01");
            r[ExternalDataFields.FormDate].Should().Be("2025-03-06 01:01:01");
            r[ExternalDataFields.ContainerDeliveryDate].Should().Be("2025-03-07 01:01:01");
            r[ExternalDataFields.ActualContainerReturnDate].Should().Be("2025-03-08 01:01:01");
            r[ExternalDataFields.DaysOff].Should().Be("7");
            r[ExternalDataFields.DaysRemainingDelivery].Should().Be("3");
            r[ExternalDataFields.ContainerDelayDays].Should().Be("4");
            r[ExternalDataFields.CostDayOfDelay].Should().Be("10.5");
            r[ExternalDataFields.TotalCostContainerDelays].Should().Be("42");
            r[ExternalDataFields.ContainerDepot].Should().Be("500.25");
            r[ExternalDataFields.AdvancePaymentRequestDate].Should().Be("2025-04-01 01:01:01");
            r[ExternalDataFields.AdvancePaymentDate].Should().Be("2025-04-02 01:01:01");
            r[ExternalDataFields.AdvancePaymentAmount].Should().Be("1000");
            r[ExternalDataFields.SupplierInvoice].Should().Be("FP-1");
            r[ExternalDataFields.TCCInvoice].Should().Be("FT-1");
            r[ExternalDataFields.InvoiceNumber].Should().Be("NF-1");
            r[ExternalDataFields.InvoiceDate].Should().Be("2025-04-03 01:01:01");
            r[ExternalDataFields.ExpenseDescription].Should().Be("Flete");
            r[ExternalDataFields.ExpenseAmountUSD].Should().Be("2000.5");
            r[ExternalDataFields.InvoiceSubtotalUSD].Should().Be("3000");
            r[ExternalDataFields.IvaUSD].Should().Be("570");
            r[ExternalDataFields.TotalInvoiceUSD].Should().Be("3570");
            r[ExternalDataFields.Comment].Should().Be("Sin novedad");
            r[ExternalDataFields.CommentDate].Should().Be("2025-04-04 01:01:01");
        }

        [Fact]
        public async Task FetchDataAsync_ValoresOpcionalesNulos_DebeMapearloComoCadenaVacia()
        {
            ApplicationDataSheetViewResultDto row = FullRow();
            row.TipoContenedor = null;
            row.NumeroContenedor = null;
            row.FechaDevolucionRealContenedor = null;
            _entregadosMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ApplicationDataSheetViewResultDto> { row });

            var request = new ApplicationDataSheetDataRequest { Scope = ApplicationDataSheetViewScope.Entregados };

            DynamicDataSet result = await _gateway.FetchDataAsync(request, CancellationToken.None);

            DynamicRecord r = result.Rows.Single();
            r[ExternalDataFields.ContainerType].Should().BeEmpty();
            r[ExternalDataFields.ContainerNumber].Should().BeEmpty();
            r[ExternalDataFields.ActualContainerReturnDate].Should().BeEmpty();
            r.HasField(ExternalDataFields.ContainerType).Should().BeTrue();
        }

        [Fact]
        public async Task FetchDataAsync_ClavesDelRegistro_DebenSerInsensiblesAMayusculas()
        {
            _entregadosMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ApplicationDataSheetViewResultDto> { new() { NitCliente = "900" } });

            var request = new ApplicationDataSheetDataRequest { Scope = ApplicationDataSheetViewScope.Entregados };

            DynamicDataSet result = await _gateway.FetchDataAsync(request, CancellationToken.None);

            result.Rows[0].HasField(ExternalDataFields.ClientNit.ToLowerInvariant()).Should().BeTrue();
            result.Rows[0].Fields[ExternalDataFields.ClientNit.ToLowerInvariant()].Should().Be("900");
        }

        [Fact]
        public async Task FetchDataAsync_FormatoNumerico_DebeUsarCulturaInvariante()
        {
            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("es-CO");
                _entregadosMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<ApplicationDataSheetViewResultDto> { new() { PesoKg = 1234.5m, FechaCreacion = new DateTime(2025, 12, 31, 23, 59, 58) } });

                var request = new ApplicationDataSheetDataRequest { Scope = ApplicationDataSheetViewScope.Entregados };

                DynamicDataSet result = await _gateway.FetchDataAsync(request, CancellationToken.None);

                result.Rows[0][ExternalDataFields.WeightKg].Should().Be("1234.5");
                result.Rows[0][ExternalDataFields.CreationDate].Should().Be("2025-12-31 23:59:58");
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public async Task FetchDataAsync_SinFilas_DebeRetornarConjuntoVacioConTodosLosCampos()
        {
            _entregadosMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ApplicationDataSheetViewResultDto>());
            _noEntregadosMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ApplicationDataSheetViewResultDto>());

            DynamicDataSet result = await _gateway.FetchDataAsync(new ApplicationDataSheetDataRequest(), CancellationToken.None);

            result.Rows.Should().BeEmpty();
            result.AvailableFields.Should().Contain(new[] { ExternalDataFields.ID, ExternalDataFields.ClientNit, ExternalDataFields.CommentDate });
        }

        [Fact]
        public async Task FetchDataAsync_SeleccionConTodosLosCampos_DebeMantenerElOrdenDeLaTabla()
        {
            var all = Enum.GetValues<ApplicationDataSheetViewField>().Reverse().ToArray();
            var selection = Selection(all);
            _entregadosMock.Setup(r => r.GetAllAsync(selection, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ApplicationDataSheetViewResultDto> { FullRow() });

            var request = new ApplicationDataSheetDataRequest { Scope = ApplicationDataSheetViewScope.Entregados, FieldsSelection = selection };

            DynamicDataSet result = await _gateway.FetchDataAsync(request, CancellationToken.None);

            result.AvailableFields.Should().HaveCount(all.Length);
            result.AvailableFields[0].Should().Be(ExternalDataFields.ID);
            result.AvailableFields[^1].Should().Be(ExternalDataFields.CommentDate);
        }
    }
}
