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
    public class LogStatusTrackingDataGatewayTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<ILogStatusTrackingViewRepository> _repositoryMock = new(MockBehavior.Strict);
        private readonly LogStatusTrackingDataGateway _gateway;

        public LogStatusTrackingDataGatewayTests()
        {
            _unitOfWorkMock.Setup(u => u.GetRepository<ILogStatusTrackingViewRepository>()).Returns(_repositoryMock.Object);
            _gateway = new LogStatusTrackingDataGateway(_unitOfWorkMock.Object);
        }

        private static LogStatusTrackingViewResultDto FullRow() => new()
        {
            Id = 10,
            IdOperacion = 20,
            DocumentoTransporteHbl = "HBL-77",
            FechaCambio = new DateTime(2025, 5, 6, 7, 8, 9),
            UsuarioCambio = "jperez",
            Mensaje = "Cambio de estado",
            EstadoAnterior = "En transito",
            NuevoEstado = "Entregado"
        };

        // ---------- FetchDataAsync(IDictionary) ----------

        [Fact]
        public async Task FetchDataAsync_FiltrosConDocumento_DebeConsultarPorDocumento()
        {
            _repositoryMock.Setup(r => r.GetByDocumentoTransporteHblAsync("HBL-77", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<LogStatusTrackingViewResultDto> { FullRow() });

            var filters = new Dictionary<String, String> { [ExternalDataFields.DocumentNumber] = "HBL-77" };

            DynamicDataSet result = await _gateway.FetchDataAsync(filters, CancellationToken.None);

            result.Rows.Should().ContainSingle();
            _repositoryMock.Verify(r => r.GetByDocumentoTransporteHblAsync("HBL-77", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task FetchDataAsync_FiltrosSinDocumento_DebeConsultarTodo()
        {
            _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<LogStatusTrackingViewResultDto>());

            DynamicDataSet result = await _gateway.FetchDataAsync(new Dictionary<String, String>(), CancellationToken.None);

            result.Rows.Should().BeEmpty();
            _repositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        public async Task FetchDataAsync_DocumentoVacioOEspacios_DebeTratarloComoSinFiltro(String documento)
        {
            _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<LogStatusTrackingViewResultDto>());

            var filters = new Dictionary<String, String> { [ExternalDataFields.DocumentNumber] = documento };

            await _gateway.FetchDataAsync(filters, CancellationToken.None);

            _repositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task FetchDataAsync_FiltrosNull_DebeTratarloComoSinFiltro()
        {
            _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<LogStatusTrackingViewResultDto>());

            DynamicDataSet result = await _gateway.FetchDataAsync((IDictionary<String, String>)null!, CancellationToken.None);

            result.AvailableFields.Should().HaveCount(8);
        }

        // ---------- FetchDataAsync(request) ----------

        [Fact]
        public async Task FetchDataAsync_RequestNull_DebeLanzarArgumentNullException()
        {
            Func<Task> act = () => _gateway.FetchDataAsync((LogStatusTrackingDataRequest)null!, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task FetchDataAsync_DocumentoYSeleccion_DebeUsarGetByDocumentoConSeleccion()
        {
            var selection = new LogStatusTrackingViewFieldsSelectionDto
            {
                Fields = new List<LogStatusTrackingViewField> { LogStatusTrackingViewField.Id, LogStatusTrackingViewField.NuevoEstado }
            };
            _repositoryMock.Setup(r => r.GetByDocumentoTransporteHblAsync("HBL-77", selection, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<LogStatusTrackingViewResultDto> { FullRow() });

            var request = new LogStatusTrackingDataRequest { DocumentoTransporteHbl = "HBL-77", FieldsSelection = selection };

            DynamicDataSet result = await _gateway.FetchDataAsync(request, CancellationToken.None);

            result.AvailableFields.Should().Equal(ExternalDataFields.ID, ExternalDataFields.NewStateLog);
            result.Rows.Single()[ExternalDataFields.NewStateLog].Should().Be("Entregado");
            result.Rows.Single().HasField(ExternalDataFields.MessageLog).Should().BeFalse();
        }

        [Fact]
        public async Task FetchDataAsync_SoloSeleccion_DebeUsarGetAllConSeleccion()
        {
            var selection = new LogStatusTrackingViewFieldsSelectionDto
            {
                Fields = new List<LogStatusTrackingViewField> { LogStatusTrackingViewField.Mensaje }
            };
            _repositoryMock.Setup(r => r.GetAllAsync(selection, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<LogStatusTrackingViewResultDto> { FullRow() });

            var request = new LogStatusTrackingDataRequest { FieldsSelection = selection };

            DynamicDataSet result = await _gateway.FetchDataAsync(request, CancellationToken.None);

            result.AvailableFields.Should().Equal(ExternalDataFields.MessageLog);
            result.Rows.Single()[ExternalDataFields.MessageLog].Should().Be("Cambio de estado");
        }

        [Fact]
        public async Task FetchDataAsync_SeleccionSinCampos_DebeTratarseComoSinSeleccion()
        {
            _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<LogStatusTrackingViewResultDto>());

            var request = new LogStatusTrackingDataRequest { FieldsSelection = new LogStatusTrackingViewFieldsSelectionDto() };

            DynamicDataSet result = await _gateway.FetchDataAsync(request, CancellationToken.None);

            result.AvailableFields.Should().HaveCount(8);
        }

        [Fact]
        public async Task FetchDataAsync_DocumentoEnBlancoConSeleccion_DebeUsarGetAllConSeleccion()
        {
            var selection = new LogStatusTrackingViewFieldsSelectionDto { Fields = new List<LogStatusTrackingViewField> { LogStatusTrackingViewField.Id } };
            _repositoryMock.Setup(r => r.GetAllAsync(selection, It.IsAny<CancellationToken>())).ReturnsAsync(new List<LogStatusTrackingViewResultDto>());

            var request = new LogStatusTrackingDataRequest { DocumentoTransporteHbl = " ", FieldsSelection = selection };

            DynamicDataSet result = await _gateway.FetchDataAsync(request, CancellationToken.None);

            result.AvailableFields.Should().Equal(ExternalDataFields.ID);
        }

        [Fact]
        public async Task FetchDataAsync_FilaCompleta_DebeMapearTodosLosCampos()
        {
            _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<LogStatusTrackingViewResultDto> { FullRow() });

            DynamicDataSet result = await _gateway.FetchDataAsync(new LogStatusTrackingDataRequest(), CancellationToken.None);

            result.AvailableFields.Should().Equal(
                ExternalDataFields.ID,
                ExternalDataFields.IdLog,
                ExternalDataFields.DocumentNumber,
                ExternalDataFields.ChangeDateLog,
                ExternalDataFields.ChangeUserLog,
                ExternalDataFields.MessageLog,
                ExternalDataFields.OldStateLog,
                ExternalDataFields.NewStateLog);

            DynamicRecord r = result.Rows.Single();
            r[ExternalDataFields.ID].Should().Be("10");
            r[ExternalDataFields.IdLog].Should().Be("20");
            r[ExternalDataFields.DocumentNumber].Should().Be("HBL-77");
            r[ExternalDataFields.ChangeDateLog].Should().Be("2025-05-06 07:08:09");
            r[ExternalDataFields.ChangeUserLog].Should().Be("jperez");
            r[ExternalDataFields.MessageLog].Should().Be("Cambio de estado");
            r[ExternalDataFields.OldStateLog].Should().Be("En transito");
            r[ExternalDataFields.NewStateLog].Should().Be("Entregado");
        }

        [Fact]
        public async Task FetchDataAsync_ClavesDelRegistro_DebenSerInsensiblesAMayusculas()
        {
            _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<LogStatusTrackingViewResultDto> { FullRow() });

            DynamicDataSet result = await _gateway.FetchDataAsync(new LogStatusTrackingDataRequest(), CancellationToken.None);

            result.Rows[0].HasField(ExternalDataFields.IdLog.ToLowerInvariant()).Should().BeTrue();
        }

        [Fact]
        public async Task FetchDataAsync_VariasFilas_DebeConservarElOrdenDelRepositorio()
        {
            _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<LogStatusTrackingViewResultDto> { new() { Id = 3 }, new() { Id = 1 }, new() { Id = 2 } });

            DynamicDataSet result = await _gateway.FetchDataAsync(new LogStatusTrackingDataRequest(), CancellationToken.None);

            result.Rows.Select(r => r[ExternalDataFields.ID]).Should().Equal("3", "1", "2");
        }

        [Fact]
        public async Task FetchDataAsync_DebePropagarElCancellationToken()
        {
            using var cts = new CancellationTokenSource();
            _repositoryMock.Setup(r => r.GetAllAsync(cts.Token)).ReturnsAsync(new List<LogStatusTrackingViewResultDto>());

            await _gateway.FetchDataAsync(new LogStatusTrackingDataRequest(), cts.Token);

            _repositoryMock.Verify(r => r.GetAllAsync(cts.Token), Times.Once);
        }

        [Fact]
        public async Task FetchDataAsync_SiElRepositorioFalla_DebePropagarLaExcepcion()
        {
            _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("db"));

            Func<Task> act = () => _gateway.FetchDataAsync(new LogStatusTrackingDataRequest(), CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("db");
        }

        [Fact]
        public async Task FetchDataAsync_FormatoDeFecha_DebeSerIndependienteDeLaCultura()
        {
            var original = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("es-CO");
                _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<LogStatusTrackingViewResultDto> { new() { FechaCambio = new DateTime(2025, 12, 31, 23, 59, 58) } });

                DynamicDataSet result = await _gateway.FetchDataAsync(new LogStatusTrackingDataRequest(), CancellationToken.None);

                result.Rows[0][ExternalDataFields.ChangeDateLog].Should().Be("2025-12-31 23:59:58");
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = original;
            }
        }
    }
}
