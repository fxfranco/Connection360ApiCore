using Connection360.Etl.Domain.Constants;
using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Domain.Interfaces;
using Connection360.Etl.Domain.Services;
using FluentAssertions;
using Xunit;
using static Connection360.Etl.Domain.Test.TestSupport.Builders;

namespace Connection360.Etl.Domain.Test.Services
{
    public class LogStatusMappingServiceTests
    {
        private readonly LogStatusMappingService _sut = new();

        [Fact]
        public void Implementa_ILogStatusMappingService()
        {
            _sut.Should().BeAssignableTo<ILogStatusMappingService>();
        }

        [Fact]
        public void Map_DataSetNulo_LanzaArgumentNullException()
        {
            Action act = () => _sut.Map(null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("dataLogsDataSet");
        }

        [Fact]
        public void Map_DataSetVacio_RetornaListaVacia()
        {
            _sut.Map(DynamicDataSet.Empty).Should().BeEmpty();
        }

        [Fact]
        public void Map_FilaCompleta_MapeaTodosLosCampos()
        {
            var ds = DataSet(Array.Empty<String>(), Record(
                (ExternalDataFields.IdLog, "123"),
                (ExternalDataFields.DocumentNumber, "HBL-1"),
                (ExternalDataFields.ChangeDateLog, "07/04/2024 10:11:12"),
                (ExternalDataFields.ChangeUserLog, "jperez"),
                (ExternalDataFields.MessageLog, "Cambio de estado"),
                (ExternalDataFields.OldStateLog, "Pendiente"),
                (ExternalDataFields.NewStateLog, "En tránsito")));

            var result = _sut.Map(ds);

            result.Should().ContainSingle();
            var log = result[0];
            log.IdOperacion.Should().Be(123);
            log.DocumentoTransporteHbl.Should().Be("HBL-1");
            log.FechaCambio.Should().Be(new DateTime(2024, 4, 7, 10, 11, 12));
            log.UsuarioCambio.Should().Be("jperez");
            log.Mensaje.Should().Be("Cambio de estado");
            log.EstadoAnterior.Should().Be("Pendiente");
            log.NuevoEstado.Should().Be("En tránsito");
        }

        [Fact]
        public void Map_FilaSinCamposOpcionales_UsaValoresPorDefecto()
        {
            var ds = DataSet(Array.Empty<String>(), Record((ExternalDataFields.DocumentNumber, "HBL-2")));

            var log = _sut.Map(ds).Single();

            log.IdOperacion.Should().Be(0);
            log.FechaCambio.Should().Be(DateTime.MinValue);
            log.UsuarioCambio.Should().BeEmpty();
            log.Mensaje.Should().BeEmpty();
            log.EstadoAnterior.Should().BeEmpty();
            log.NuevoEstado.Should().BeEmpty();
        }

        [Fact]
        public void Map_IdYFechaInvalidos_UsaDefectos()
        {
            var ds = DataSet(Array.Empty<String>(), Record(
                (ExternalDataFields.DocumentNumber, "HBL-3"),
                (ExternalDataFields.IdLog, "abc"),
                (ExternalDataFields.ChangeDateLog, "no-fecha")));

            var log = _sut.Map(ds).Single();

            log.IdOperacion.Should().Be(0);
            log.FechaCambio.Should().Be(DateTime.MinValue);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Map_SinDocumentoOEnBlanco_DescartaLaFila(String doc)
        {
            var ds = DataSet(Array.Empty<String>(),
                Record((ExternalDataFields.DocumentNumber, doc), (ExternalDataFields.IdLog, "1")),
                Record((ExternalDataFields.IdLog, "2")));

            _sut.Map(ds).Should().BeEmpty();
        }

        [Fact]
        public void Map_VariasFilas_ConservaOrdenYOmiteInvalidas()
        {
            var ds = DataSet(Array.Empty<String>(),
                Record((ExternalDataFields.DocumentNumber, "A"), (ExternalDataFields.IdLog, "1")),
                Record((ExternalDataFields.IdLog, "2")),
                Record((ExternalDataFields.DocumentNumber, "B"), (ExternalDataFields.IdLog, "3")),
                Record((ExternalDataFields.DocumentNumber, "A"), (ExternalDataFields.IdLog, "4")));

            var result = _sut.Map(ds);

            result.Select(l => l.IdOperacion).Should().Equal(1, 3, 4);
            result.Select(l => l.DocumentoTransporteHbl).Should().Equal("A", "B", "A");
        }
    }
}
