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
        public void GetCommentChangeMessage_TituloFijo_YMensajeConElComentario()
        {
            var (title, message) = _sut.GetCommentChangeMessage("Carga lista para despacho");

            title.Should().Be("Nuevo comentario en tu envío");
            message.Should().Be("Se registró nuevo comentario 'Carga lista para despacho' en el seguimiento de tu envío");
        }

        [Fact]
        public void GetCommentChangeMessage_RecortaElComentario()
        {
            var (_, message) = _sut.GetCommentChangeMessage("  hola  ");

            message.Should().Be("Se registró nuevo comentario 'hola' en el seguimiento de tu envío");
        }

        [Fact]
        public void GetCommentChangeMessage_Nulo_UsaCadenaVacia()
        {
            var (title, message) = _sut.GetCommentChangeMessage(null!);

            title.Should().Be("Nuevo comentario en tu envío");
            message.Should().Be("Se registró nuevo comentario '' en el seguimiento de tu envío");
        }

        [Fact]
        public void GetCommentChangeMessage_LlamadasRepetidas_SonDeterministas()
        {
            _sut.GetCommentChangeMessage("b").Should().Be(_sut.GetCommentChangeMessage("b"));
        }

        [Fact]
        public void GetOtherInfoChangeMessage_RetornaMensajeFijo()
        {
            var (title, message) = _sut.GetOtherInfoChangeMessage();

            title.Should().Be("Actualización información de tu envío");
            message.Should().Be("Se registró un cambio en la información general de tu envío");
        }

        [Fact]
        public void GetOtherInfoChangeMessage_LlamadasRepetidas_SonDeterministas()
        {
            _sut.GetOtherInfoChangeMessage().Should().Be(_sut.GetOtherInfoChangeMessage());
        }
    }
}
