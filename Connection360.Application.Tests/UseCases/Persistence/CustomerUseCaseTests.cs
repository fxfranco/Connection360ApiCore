using Connection360.Application.UseCases.Persistence;
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
        public async Task GetByIdAsync_DelegaAlRepositorio()
        {
            _repositoryMock.Setup(r => r.GetCustomerByIdAsync("900123", It.IsAny<CancellationToken>())).ReturnsAsync(10);

            Int64? result = await _useCase.GetByIdAsync("900123", CancellationToken.None);

            result.Should().Be(10);
        }
    }
}
