using Connection360Notification.Domain;
using FluentAssertions;
using Xunit;

namespace Connection360Notification.Domain.Tests
{
    public class BasicUserDataTests
    {
        [Fact]
        public void Constructor_AsignaTodosLosCampos()
        {
            var sut = new BasicUserData("auth0|1", "900123", "Ana Perez", "+573001112233", "ana@correo.com");

            sut.Id.Should().Be("auth0|1");
            sut.Document.Should().Be("900123");
            sut.FullName.Should().Be("Ana Perez");
            sut.Phone.Should().Be("+573001112233");
            sut.Email.Should().Be("ana@correo.com");
        }

        [Fact]
        public void Constructor_ConRol_AsignaElNombreDelRol()
        {
            var sut = new BasicUserData("auth0|1", "900123", "Ana Perez", "+573001112233", "ana@correo.com", "ADMIN");

            sut.RoleName.Should().Be("ADMIN");
        }

        [Fact]
        public void Constructor_SinRol_DejaElRolVacio()
        {
            var sut = new BasicUserData("auth0|1", "900123", "Ana Perez", "+573001112233", "ana@correo.com");

            sut.RoleName.Should().BeEmpty();
        }

        [Fact]
        public void Igualdad_SeBasaEnLosValores()
        {
            var a = new BasicUserData("auth0|1", "900123", "Ana Perez", "+573001112233", "ana@correo.com");
            var b = new BasicUserData("auth0|1", "900123", "Ana Perez", "+573001112233", "ana@correo.com");

            a.Should().Be(b);
            (a with { FullName = "Otra Persona" }).Should().NotBe(b);
            (a with { Email = "otro@correo.com" }).Should().NotBe(b);
            (a with { RoleName = "ADMIN" }).Should().NotBe(b);
            (a with { RoleName = "ADMIN" }).Should().Be(b with { RoleName = "ADMIN" });
        }
    }
}
