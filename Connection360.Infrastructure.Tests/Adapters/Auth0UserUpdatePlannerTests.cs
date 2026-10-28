using Connection360.Infrastructure.Adapters.Output;
using FluentAssertions;
using Xunit;

namespace Connection360.Infrastructure.Tests.Adapters
{
    /// <summary>
    /// Reglas que deciden qué campos se envían a Auth0 al actualizar un usuario (solo lo que cambia).
    /// </summary>
    public class Auth0UserUpdatePlannerTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void HasChanged_SiNoSeInformoElValor_NoHayCambio(String? requested)
        {
            Auth0UserUpdatePlanner.HasChanged(requested, "actual@correo.com", ignoreCase: true).Should().BeFalse();
        }

        [Fact]
        public void HasChanged_SiElValorEsIgualAlActual_NoHayCambio()
        {
            Auth0UserUpdatePlanner.HasChanged("+573113232323", "+573113232323", ignoreCase: false).Should().BeFalse();
        }

        [Fact]
        public void HasChanged_ConEspaciosAlrededor_SeComparaSinEllos()
        {
            Auth0UserUpdatePlanner.HasChanged("  +573113232323 ", "+573113232323", ignoreCase: false).Should().BeFalse();
        }

        [Fact]
        public void HasChanged_SiElValorEsDistinto_HayCambio()
        {
            Auth0UserUpdatePlanner.HasChanged("+573009998877", "+573113232323", ignoreCase: false).Should().BeTrue();
        }

        [Fact]
        public void HasChanged_ConCorreoQueSoloCambiaDeMayusculas_NoHayCambioSiSeIgnoraElCaso()
        {
            Auth0UserUpdatePlanner.HasChanged("Usuario@Correo.com", "usuario@correo.com", ignoreCase: true).Should().BeFalse();
        }

        [Fact]
        public void HasChanged_ConDiferenciaDeMayusculas_HayCambioSiNoSeIgnoraElCaso()
        {
            Auth0UserUpdatePlanner.HasChanged("Apodo", "apodo", ignoreCase: false).Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void HasChanged_SiElUsuarioNoTeniaValor_UnValorNuevoEsCambio(String? current)
        {
            Auth0UserUpdatePlanner.HasChanged("+573113232323", current, ignoreCase: false).Should().BeTrue();
        }
    }
}
