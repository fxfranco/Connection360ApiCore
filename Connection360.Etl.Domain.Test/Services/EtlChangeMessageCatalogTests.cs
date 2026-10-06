using Connection360.Etl.Domain.Interfaces;
using Connection360.Etl.Domain.Services;
using FluentAssertions;
using Xunit;

namespace Connection360.Etl.Domain.Test.Services
{
    public class EtlChangeMessageCatalogTests
    {
        private readonly EtlChangeMessageCatalog _sut = new();

        [Fact]
        public void Implementa_IEtlChangeMessageCatalog()
        {
            _sut.Should().BeAssignableTo<IEtlChangeMessageCatalog>();
        }

        [Theory]
        [InlineData("Entregado")]
        [InlineData("En tránsito")]
        [InlineData("ENTREGADO")]
        public void GetStateChangeMessage_EstadoConocido_ArmaMensajeGenerico(String estado)
        {
            var (title, message) = _sut.GetStateChangeMessage(estado);

            title.Should().Be($"Actualización de estado: {estado}");
            message.Should().Be($"El estado de tu envío cambió a '{estado}'.");
        }

        [Fact]
        public void GetStateChangeMessage_EstadoConEspacios_LoRecorta()
        {
            var (title, message) = _sut.GetStateChangeMessage("   Pendiente  ");

            title.Should().Be("Actualización de estado: Pendiente");
            message.Should().Be("El estado de tu envío cambió a 'Pendiente'.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("    ")]
        public void GetStateChangeMessage_NuloOVacio_UsaEstadoVacio(String? estado)
        {
            var (title, message) = _sut.GetStateChangeMessage(estado!);

            title.Should().Be("Actualización de estado: ");
            message.Should().Be("El estado de tu envío cambió a ''.");
        }

        [Fact]
        public void GetCommentChangeMessage_RetornaMensajeFijo()
        {
            var (title, message) = _sut.GetCommentChangeMessage();

            title.Should().Be("Nuevo comentario en tu envío");
            message.Should().Be("Se registró un nuevo comentario en el seguimiento de tu envío.");
        }

        [Fact]
        public void GetCommentChangeMessage_LlamadasRepetidas_SonDeterministas()
        {
            _sut.GetCommentChangeMessage().Should().Be(_sut.GetCommentChangeMessage());
        }
    }
}
