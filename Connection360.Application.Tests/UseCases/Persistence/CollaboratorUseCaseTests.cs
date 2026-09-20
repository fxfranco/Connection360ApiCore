using Connection360.Application.UseCases.Persistence;
using Connection360.Domain.Ports.Persistence;
using FluentAssertions;
using Moq;
using Xunit;

namespace Connection360.Application.Tests.UseCases.Persistence
{
    public class CollaboratorUseCaseTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<ICollaboratorRepository> _collaboratorRepositoryMock = new();
        private readonly Mock<ICustomerRepository> _customerRepositoryMock = new();
        private readonly Mock<ICustomersOfCollaboratorsRepository> _customersOfCollaboratorsRepositoryMock = new();
        private readonly CollaboratorUseCase _sut;

        public CollaboratorUseCaseTests()
        {
            _unitOfWorkMock.Setup(u => u.GetRepository<ICollaboratorRepository>()).Returns(_collaboratorRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<ICustomerRepository>()).Returns(_customerRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<ICustomersOfCollaboratorsRepository>()).Returns(_customersOfCollaboratorsRepositoryMock.Object);

            _sut = new CollaboratorUseCase(_unitOfWorkMock.Object);
        }

        [Fact]
        public async Task CreateAsync_DebeIniciarYConfirmarLaTransaccionYRetornarId()
        {
            _collaboratorRepositoryMock.Setup(r => r.CrearAsync("COL-1", It.IsAny<CancellationToken>())).ReturnsAsync(15);

            Int64 result = await _sut.CreateAsync("COL-1", CancellationToken.None);

            result.Should().Be(15);
            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_SiRepositorioFalla_DebeHacerRollbackYPropagar()
        {
            _collaboratorRepositoryMock
                .Setup(r => r.CrearAsync(It.IsAny<String>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException());

            Func<Task> act = () => _sut.CreateAsync("COL-1", CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
            _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateCustomerCollaboratorAsync_ConClienteInexistente_LanzaArgumentException()
        {
            _customerRepositoryMock.Setup(r => r.GetCustomerByIdAsync("CUS-1", It.IsAny<CancellationToken>())).ReturnsAsync((Int64?)null);

            Func<Task> act = () => _sut.CreateCustomerCollaboratorAsync("CUS-1", "COL-1", CancellationToken.None);

            (await act.Should().ThrowAsync<ArgumentException>()).Which.ParamName.Should().Be("customerId");
            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateCustomerCollaboratorAsync_ConColaboradorInexistente_LanzaArgumentException()
        {
            _customerRepositoryMock.Setup(r => r.GetCustomerByIdAsync("CUS-1", It.IsAny<CancellationToken>())).ReturnsAsync(10L);
            _collaboratorRepositoryMock.Setup(r => r.GetCollaboratorByIdAsync("COL-1", It.IsAny<CancellationToken>())).ReturnsAsync((Int64?)null);

            Func<Task> act = () => _sut.CreateCustomerCollaboratorAsync("CUS-1", "COL-1", CancellationToken.None);

            (await act.Should().ThrowAsync<ArgumentException>()).Which.ParamName.Should().Be("collaboratorId");
            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateCustomerCollaboratorAsync_ConDatosValidos_CreaLaRelacionYConfirmaLaTransaccion()
        {
            _customerRepositoryMock.Setup(r => r.GetCustomerByIdAsync("CUS-1", It.IsAny<CancellationToken>())).ReturnsAsync(10L);
            _collaboratorRepositoryMock.Setup(r => r.GetCollaboratorByIdAsync("COL-1", It.IsAny<CancellationToken>())).ReturnsAsync(20L);
            _customersOfCollaboratorsRepositoryMock.Setup(r => r.CrearAsync(10, 20, It.IsAny<CancellationToken>())).ReturnsAsync(99);

            Int64 result = await _sut.CreateCustomerCollaboratorAsync("CUS-1", "COL-1", CancellationToken.None);

            result.Should().Be(99);
            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateCustomerCollaboratorAsync_SiRepositorioFalla_HaceRollbackYPropaga()
        {
            _customerRepositoryMock.Setup(r => r.GetCustomerByIdAsync("CUS-1", It.IsAny<CancellationToken>())).ReturnsAsync(10L);
            _collaboratorRepositoryMock.Setup(r => r.GetCollaboratorByIdAsync("COL-1", It.IsAny<CancellationToken>())).ReturnsAsync(20L);
            _customersOfCollaboratorsRepositoryMock
                .Setup(r => r.CrearAsync(It.IsAny<Int64>(), It.IsAny<Int64>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException());

            Func<Task> act = () => _sut.CreateCustomerCollaboratorAsync("CUS-1", "COL-1", CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
            _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
