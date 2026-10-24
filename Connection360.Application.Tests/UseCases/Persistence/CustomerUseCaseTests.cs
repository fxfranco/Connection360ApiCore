using Connection360.Application.UseCases.Persistence;
using Connection360.Domain.Exceptions;
using Connection360.Domain.Ports.Persistence;
using FluentAssertions;
using Moq;
using Xunit;

namespace Connection360.Application.Tests.UseCases.Persistence
{
    public class CustomerUseCaseTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<ICustomerRepository> _repositoryMock = new();
        private readonly CustomerUseCase _useCase;

        public CustomerUseCaseTests()
        {
            _unitOfWorkMock.Setup(u => u.GetRepository<ICustomerRepository>()).Returns(_repositoryMock.Object);
            _useCase = new CustomerUseCase(_unitOfWorkMock.Object);
        }

        [Fact]
        public async Task CrearAsync_DebeIniciarYConfirmarLaTransaccionYRetornarId()
        {
            _repositoryMock.Setup(r => r.CrearAsync("900123456", It.IsAny<CancellationToken>())).ReturnsAsync(7);

            Int64 result = await _useCase.CrearAsync("900123456", CancellationToken.None);

            result.Should().Be(7);
            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CrearAsync_SiRepositorioFalla_DebeHacerRollbackYPropagar()
        {
            _repositoryMock.Setup(r => r.CrearAsync(It.IsAny<String>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException());

            Func<Task> act = () => _useCase.CrearAsync("900123456", CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
            _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ClienteExistente_DebeRetornarId()
        {
            _repositoryMock.Setup(r => r.GetCustomerByIdAsync("900123456", It.IsAny<CancellationToken>())).ReturnsAsync(42L);

            Int64? result = await _useCase.GetByIdAsync("900123456", CancellationToken.None);

            result.Should().Be(42);
        }

        [Fact]
        public async Task GetByIdAsync_ClienteNoExistente_DebeRetornarNull()
        {
            _repositoryMock.Setup(r => r.GetCustomerByIdAsync(It.IsAny<String>(), It.IsAny<CancellationToken>())).ReturnsAsync((Int64?)null);

            Int64? result = await _useCase.GetByIdAsync("no-existe", CancellationToken.None);

            result.Should().BeNull();
        }

        [Fact]
        public async Task CrearAsync_UsaTransaccionYRetornaElIdGenerado()
        {
            _repositoryMock.Setup(r => r.CrearAsync("900123", It.IsAny<CancellationToken>())).ReturnsAsync(77);

            Int64 result = await _useCase.CrearAsync("900123", CancellationToken.None);

            result.Should().Be(77);
            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CrearAsync_SiFalla_HaceRollbackYRelanza()
        {
            _repositoryMock.Setup(r => r.CrearAsync(It.IsAny<String>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException());

            Func<Task> act = () => _useCase.CrearAsync("900123", CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
            _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CrearAsync_ConIdentificacionYaExistente_LanzaDuplicateRecordExceptionSinCrearNiAbrirTransaccion()
        {
            _repositoryMock.Setup(r => r.GetCustomerByIdAsync("900123456", It.IsAny<CancellationToken>())).ReturnsAsync(7L);

            Func<Task> act = () => _useCase.CrearAsync("900123456", CancellationToken.None);

            (await act.Should().ThrowAsync<DuplicateRecordException>()).Which.Message.Should().Contain("900123456");
            _repositoryMock.Verify(r => r.CrearAsync(It.IsAny<String>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CrearAsync_ConIdentificacionNueva_ConsultaSiExisteAntesDeCrear()
        {
            _repositoryMock.Setup(r => r.GetCustomerByIdAsync("900999", It.IsAny<CancellationToken>())).ReturnsAsync((Int64?)null);
            _repositoryMock.Setup(r => r.CrearAsync("900999", It.IsAny<CancellationToken>())).ReturnsAsync(12);

            Int64 result = await _useCase.CrearAsync("900999", CancellationToken.None);

            result.Should().Be(12);
            _repositoryMock.Verify(r => r.GetCustomerByIdAsync("900999", It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.CrearAsync("900999", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_DelegaAlRepositorio()
        {
            _repositoryMock.Setup(r => r.GetCustomerByIdAsync("900123", It.IsAny<CancellationToken>())).ReturnsAsync(10);

            Int64? result = await _useCase.GetByIdAsync("900123", CancellationToken.None);

            result.Should().Be(10);
        }

        [Fact]
        public async Task ListAsync_DelegaAlRepositorioYRetornaLosClientes_SinAbrirTransaccion()
        {
            var customers = new List<Connection360.Domain.Dtos.CustomerDtoResult>
            {
                new() { Identificacion = "900111" },
                new() { Identificacion = "900222" }
            };
            _repositoryMock.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(customers);

            List<Connection360.Domain.Dtos.CustomerDtoResult> result = await _useCase.ListAsync(CancellationToken.None);

            result.Should().BeSameAs(customers);
            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ListAsync_SiElRepositorioFalla_PropagaLaExcepcion()
        {
            _repositoryMock.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException());

            Func<Task> act = () => _useCase.ListAsync(CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
