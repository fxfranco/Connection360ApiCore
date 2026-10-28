using Connection360.Domain.Ports.Persistence;
using Connection360.Infrastructure.Persistence;
using Connection360.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Npgsql;
using Xunit;

namespace Connection360.Infrastructure.Tests.Persistence
{
    // NOTA IMPORTANTE:
    // UnitOfWork.GetRepository<T>() es la única operación testeable de forma aislada:
    // delega directamente en IServiceProvider y no depende de una conexión real.
    //
    // BeginTransactionAsync / CommitAsync / RollbackAsync / DisposeAsync dependen de DbSession,
    // que internamente crea una conexión real (NpgsqlConnection) a través de NpgsqlDataSource
    // (DbSession no expone una abstracción/interfaz inyectable para sustituir la conexión).
    // Por diseño actual, esa parte de UnitOfWork NO es unit-testeable de forma aislada:
    // requiere una base de datos Postgres real (o de prueba, p.ej. vía Testcontainers) y
    // debe cubrirse con pruebas de integración, no con pruebas unitarias con mocks.
    public class UnitOfWorkTests
    {
        [Fact]
        public void GetRepository_DelegaLaResolucionAlContenedorDeServicios()
        {
            var repositoryMock = new Mock<ICustomerRepository>();
            var services = new ServiceCollection();
            services.AddSingleton(repositoryMock.Object);
            var serviceProvider = services.BuildServiceProvider();

            var dataSource = NpgsqlDataSource.Create("Host=localhost;Database=test;Username=test;Password=test");
            var session = new DbSession(dataSource);
            var sut = new UnitOfWork(session, serviceProvider);

            ICustomerRepository result = sut.GetRepository<ICustomerRepository>();

            result.Should().BeSameAs(repositoryMock.Object);
        }

        [Fact]
        public void GetRepository_ConTipoNoRegistrado_LanzaInvalidOperationException()
        {
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();

            var dataSource = NpgsqlDataSource.Create("Host=localhost;Database=test;Username=test;Password=test");
            var session = new DbSession(dataSource);
            var sut = new UnitOfWork(session, serviceProvider);

            Action act = () => sut.GetRepository<ICustomerRepository>();

            act.Should().Throw<InvalidOperationException>();
        }
    }
}
