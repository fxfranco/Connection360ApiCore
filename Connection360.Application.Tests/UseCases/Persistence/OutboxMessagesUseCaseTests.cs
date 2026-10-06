using Connection360.Application.DTOs.Persistence;
using Connection360.Application.UseCases.Persistence;
using Connection360.Domain.Dtos;
using Connection360.Domain.Ports.Persistence;
using FluentAssertions;
using Moq;
using Xunit;

namespace Connection360.Application.Tests.UseCases.Persistence
{
    public class OutboxMessagesUseCaseTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IOutboxMessagesRepository> _repositoryMock = new();
        private readonly OutboxMessagesUseCase _useCase;

        public OutboxMessagesUseCaseTests()
        {
            _unitOfWorkMock.Setup(u => u.GetRepository<IOutboxMessagesRepository>()).Returns(_repositoryMock.Object);
            _useCase = new OutboxMessagesUseCase(_unitOfWorkMock.Object);
        }

        private static CreateOutboxMessagesDto Dto() =>
            new("CLI-1", "ChangeState", "HBL-1", "Titulo", "Mensaje", new DateTime(2025, 1, 2, 3, 4, 5));

        [Fact]
        public async Task CreateAsync_IdValido_DebeRetornarTrueYConfirmarTransaccion()
        {
            _repositoryMock.Setup(r => r.CrearAsync(It.IsAny<OutboxMessagesRequestDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(Guid.NewGuid());

            Boolean result = await _useCase.CreateAsync(Dto(), CancellationToken.None);

            result.Should().BeTrue();
            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_IdVacio_DebeRetornarFalsePeroConfirmarTransaccion()
        {
            _repositoryMock.Setup(r => r.CrearAsync(It.IsAny<OutboxMessagesRequestDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(Guid.Empty);

            Boolean result = await _useCase.CreateAsync(Dto(), CancellationToken.None);

            result.Should().BeFalse();
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_DebeMapearElDtoAlRequestDelRepositorio()
        {
            OutboxMessagesRequestDto? captured = null;
            _repositoryMock.Setup(r => r.CrearAsync(It.IsAny<OutboxMessagesRequestDto>(), It.IsAny<CancellationToken>()))
                .Callback<OutboxMessagesRequestDto, CancellationToken>((req, _) => captured = req)
                .ReturnsAsync(Guid.NewGuid());

            await _useCase.CreateAsync(Dto(), CancellationToken.None);

            captured.Should().NotBeNull();
            captured!.ClientId.Should().Be("CLI-1");
            captured.EventType.Should().Be("ChangeState");
            captured.DocumentNumber.Should().Be("HBL-1");
            captured.Title.Should().Be("Titulo");
            captured.Message.Should().Be("Mensaje");
            captured.MessageDate.Should().Be(new DateTime(2025, 1, 2, 3, 4, 5));
        }

        [Fact]
        public async Task CreateAsync_SiElRepositorioFalla_DebeHacerRollbackYPropagar()
        {
            _repositoryMock.Setup(r => r.CrearAsync(It.IsAny<OutboxMessagesRequestDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("fallo"));

            Func<Task> act = () => _useCase.CreateAsync(Dto(), CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("fallo");
            _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_SiFallaElInicioDeTransaccion_DebeHacerRollbackYPropagar()
        {
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException());

            Func<Task> act = () => _useCase.CreateAsync(Dto(), CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
            _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.CrearAsync(It.IsAny<OutboxMessagesRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_DebePropagarElCancellationToken()
        {
            using var cts = new CancellationTokenSource();
            _repositoryMock.Setup(r => r.CrearAsync(It.IsAny<OutboxMessagesRequestDto>(), cts.Token)).ReturnsAsync(Guid.NewGuid());

            await _useCase.CreateAsync(Dto(), cts.Token);

            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(cts.Token), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(cts.Token), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_DtoNull_DebeLanzarNullReferenceException()
        {
            Func<Task> act = () => _useCase.CreateAsync(null!, CancellationToken.None);

            await act.Should().ThrowAsync<NullReferenceException>();
        }
    }
}
