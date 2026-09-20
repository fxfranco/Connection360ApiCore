using Connection360.Domain.Constans;
using Connection360.Domain.Dtos;
using Connection360.Domain.Services;
using FluentAssertions;
using Xunit;
using static Connection360.Domain.Tests.TestSupport.DynamicDataSetBuilder;

namespace Connection360.Domain.Tests.Services
{
    public class DetailsHistoryShipmentsDomainServiceTests
    {
        private readonly DetailsHistoryShipmentsDomainService _sut = new();

        [Fact]
        public void GetDetailsHistoryShipments_FiltraSoloLosRegistrosDelDocumentoSolicitado()
        {
            var rows = new[]
            {
                Row((ExternalDataFields.DocumentNumber, "HBL-001"), (ExternalDataFields.IdLog, "2"), (ExternalDataFields.MessageLog, "Segundo cambio")),
                Row((ExternalDataFields.DocumentNumber, "HBL-001"), (ExternalDataFields.IdLog, "1"), (ExternalDataFields.MessageLog, "Primer cambio")),
                Row((ExternalDataFields.DocumentNumber, "OTRO-DOC"), (ExternalDataFields.IdLog, "3"), (ExternalDataFields.MessageLog, "No debe salir")),
            };
            var dataSet = DataSet(new[] { ExternalDataFields.DocumentNumber }, rows);

            HistoryShipmentsDomainDtoResult result = _sut.GetDetailsHistoryShipments(dataSet, "HBL-001");

            result.DetailsHistoryShipments.Should().HaveCount(2);
        }

        [Fact]
        public void GetDetailsHistoryShipments_OrdenaAscendentementePorIdLog()
        {
            var rows = new[]
            {
                Row((ExternalDataFields.DocumentNumber, "HBL-001"), (ExternalDataFields.IdLog, "5"), (ExternalDataFields.MessageLog, "Quinto")),
                Row((ExternalDataFields.DocumentNumber, "HBL-001"), (ExternalDataFields.IdLog, "1"), (ExternalDataFields.MessageLog, "Primero")),
            };
            var dataSet = DataSet(new[] { ExternalDataFields.DocumentNumber }, rows);

            HistoryShipmentsDomainDtoResult result = _sut.GetDetailsHistoryShipments(dataSet, "HBL-001");

            result.DetailsHistoryShipments!.Select(x => x.Message).Should().ContainInOrder("Primero", "Quinto");
        }

        [Fact]
        public void GetDetailsHistoryShipments_MapeaTodosLosCamposDelLog()
        {
            var rows = new[]
            {
                Row((ExternalDataFields.DocumentNumber, "HBL-001"), (ExternalDataFields.IdLog, "1"),
                    (ExternalDataFields.ChangeDateLog, "07/04/2024"), (ExternalDataFields.ChangeUserLog, "usuario1"),
                    (ExternalDataFields.MessageLog, "Cambio de estado"), (ExternalDataFields.OldStateLog, "Pendiente"),
                    (ExternalDataFields.NewStateLog, "En tránsito")),
            };
            var dataSet = DataSet(new[] { ExternalDataFields.DocumentNumber }, rows);

            HistoryShipmentsDomainDtoResult result = _sut.GetDetailsHistoryShipments(dataSet, "HBL-001");

            var item = result.DetailsHistoryShipments!.Single();
            item.ChangeUser.Should().Be("usuario1");
            item.OldState.Should().Be("Pendiente");
            item.NewState.Should().Be("En tránsito");
            item.ChangeDate.Should().Be(new DateTime(2024, 4, 7));
        }

        [Fact]
        public void GetDetailsHistoryShipments_SinCoincidencias_RetornaListaVacia()
        {
            var dataSet = DataSet(new[] { ExternalDataFields.DocumentNumber },
                Row((ExternalDataFields.DocumentNumber, "OTRO-DOC")));

            HistoryShipmentsDomainDtoResult result = _sut.GetDetailsHistoryShipments(dataSet, "HBL-001");

            result.DetailsHistoryShipments.Should().BeEmpty();
        }
    }
}
