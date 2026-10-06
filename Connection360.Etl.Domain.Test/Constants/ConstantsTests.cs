using System.Reflection;
using Connection360.Etl.Domain.Constants;
using FluentAssertions;
using Xunit;

namespace Connection360.Etl.Domain.Test.Constants
{
    public class ExternalDataValuesTests
    {
        [Fact]
        public void TipoOperacion_ValoresEsperados()
        {
            ExternalDataValues.Import.Should().Be("IMPO");
            ExternalDataValues.Export.Should().Be("EXPO");
        }

        [Fact]
        public void Modalidad_ValoresEsperados()
        {
            ExternalDataValues.AirShipment.Should().Be("AIR");
            ExternalDataValues.OceanShipment.Should().Be("SEA");
        }

        [Fact]
        public void Estados_ValoresEsperados()
        {
            ExternalDataValues.WithIssuesState.Should().Be("Con novedad");
            ExternalDataValues.DeliveredState.Should().Be("Entregado");
            ExternalDataValues.DestinationCustomsState.Should().Be("En Aduana destino");
            ExternalDataValues.OriginCustomsState.Should().Be("En Aduana origen");
            ExternalDataValues.InTransitState.Should().Be("En tránsito");
            ExternalDataValues.PendingState.Should().Be("Pendiente");
        }

        [Fact]
        public void TodasLasConstantes_SonUnicasYNoVacias()
        {
            var valores = typeof(ExternalDataValues)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(f => (String)f.GetRawConstantValue()!)
                .ToList();

            valores.Should().NotBeEmpty();
            valores.Should().OnlyContain(v => !String.IsNullOrWhiteSpace(v));
            valores.Should().OnlyHaveUniqueItems();
        }
    }

    public class ExternalDataFieldsTests
    {
        [Theory]
        [InlineData(nameof(ExternalDataFields.ID), "ID")]
        [InlineData(nameof(ExternalDataFields.ClientNit), "NIT CLIENTE")]
        [InlineData(nameof(ExternalDataFields.ClientName), "CLIENTE")]
        [InlineData(nameof(ExternalDataFields.State), "ESTADO")]
        [InlineData(nameof(ExternalDataFields.DocumentNumber), "DOCUMENTO DE TRANSPORTE (HBL)")]
        [InlineData(nameof(ExternalDataFields.Comment), "COMENTARIO")]
        [InlineData(nameof(ExternalDataFields.CommentDate), "FECHA COMENTARIO")]
        [InlineData(nameof(ExternalDataFields.IdLog), "ID")]
        [InlineData(nameof(ExternalDataFields.NewStateLog), "NUEVO ESTADO")]
        [InlineData(nameof(ExternalDataFields.OldStateLog), "ESTADO ANTERIOR")]
        [InlineData(nameof(ExternalDataFields.MessageLog), "MENSAJE")]
        [InlineData(nameof(ExternalDataFields.ChangeUserLog), "USUARIO DE CAMBIO")]
        [InlineData(nameof(ExternalDataFields.ChangeDateLog), "FECHA DE CAMBIO")]
        public void Constante_ValorEsperado(String nombre, String esperado)
        {
            var campo = typeof(ExternalDataFields).GetField(nombre, BindingFlags.Public | BindingFlags.Static);

            campo.Should().NotBeNull();
            campo!.GetRawConstantValue().Should().Be(esperado);
        }

        [Fact]
        public void TodasLasConstantes_NoSonVacias()
        {
            var campos = typeof(ExternalDataFields)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral)
                .ToList();

            campos.Should().NotBeEmpty();
            campos.Select(f => (String)f.GetRawConstantValue()!).Should().OnlyContain(v => !String.IsNullOrWhiteSpace(v));
        }

        [Fact]
        public void NombresDeColumna_ExcluyendoElIdCompartido_SonUnicos()
        {
            var valores = typeof(ExternalDataFields)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.Name != nameof(ExternalDataFields.IdLog))
                .Select(f => (String)f.GetRawConstantValue()!)
                .ToList();

            valores.Should().OnlyHaveUniqueItems();
        }
    }
}
