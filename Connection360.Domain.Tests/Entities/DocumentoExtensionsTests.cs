using Connection360.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Connection360.Domain.Tests.Entities
{
    public class DocumentoExtensionsTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void GetDocumentoSinPrefijo_ConCadenaNulaOVacia_RetornaCadenaVacia(String? input)
        {
            input!.GetDocumentoSinPrefijo().Should().Be(String.Empty);
        }

        [Fact]
        public void GetDocumentoSinPrefijo_ConPrefijoYGuion_RetornaParteSinPrefijo()
        {
            "HBL-5U6HC36K".GetDocumentoSinPrefijo().Should().Be("5U6HC36K");
        }

        [Fact]
        public void GetDocumentoSinPrefijo_ConEspaciosAlrededorDelValor_HaceTrim()
        {
            "HBL- 5U6HC36K ".GetDocumentoSinPrefijo().Should().Be("5U6HC36K");
        }

        [Fact]
        public void GetDocumentoSinPrefijo_SinGuion_RetornaCadenaOriginal()
        {
            "5U6HC36K".GetDocumentoSinPrefijo().Should().Be("5U6HC36K");
        }

        [Fact]
        public void GetDocumentoSinPrefijo_ConMultiplesGuiones_RetornaSoloLaSegundaParte()
        {
            "HBL-5U6HC36K-EXTRA".GetDocumentoSinPrefijo().Should().Be("5U6HC36K");
        }
    }
}
