using Connection360.Application.DTOs.Persistence;
using Connection360.Application.UseCases.Persistence;
using Connection360.Domain.Entities.Persistence;
using Connection360.Domain.Ports.Persistence;
using FluentAssertions;
using Moq;
using Xunit;

namespace Connection360.Application.Tests.UseCases.Persistence
{
    public class CustomerNotificationChannelsUseCaseTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<ICustomerNotificationChannelsRepository> _repositoryMock = new();
        private readonly CustomerNotificationChannelsUseCase _sut;

        public CustomerNotificationChannelsUseCaseTests()
        {
            _unitOfWorkMock.Setup(u => u.GetRepository<ICustomerNotificationChannelsRepository>()).Returns(_repositoryMock.Object);
            _sut = new CustomerNotificationChannelsUseCase(_unitOfWorkMock.Object);
        }

        [Fact]
        public async Task CrearAsync_UsaTransaccionYRetornaElDtoConElIdGenerado()
        {
            var dto = new CreateCustomerNotificationChannelsDto(5, true, false, true);
            _repositoryMock.Setup(r => r.CrearAsync(It.IsAny<CustomerNotificationChannels>(), It.IsAny<CancellationToken>())).ReturnsAsync(99);

            CustomerNotificationChannelsResponseDto result = await _sut.CrearAsync(dto, CancellationToken.None);

            result.IdNotificationChannels.Should().Be(99);
            result.IdCustomer.Should().Be(5);
            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CrearAsync_SiElRepositorioFalla_HaceRollbackYRelanzaLaExcepcion()
        {
            var dto = new CreateCustomerNotificationChannelsDto(5, true, false, true);
            _repositoryMock.Setup(r => r.CrearAsync(It.IsAny<CustomerNotificationChannels>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("fallo db"));

            Func<Task> act = () => _sut.CrearAsync(dto, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
            _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetByIdAsync_CuandoNoExiste_RetornaNull()
        {
            _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((CustomerNotificationChannels?)null);

            var result = await _sut.GetByIdAsync(1, CancellationToken.None);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_CuandoExiste_MapeaElDto()
        {
            _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CustomerNotificationChannels(1, 5, true, true, false));

            var result = await _sut.GetByIdAsync(1, CancellationToken.None);

            result.Should().NotBeNull();
            result!.IdCustomer.Should().Be(5);
        }

        [Fact]
        public async Task ListAllAsync_MapeaTodosLosRegistros()
        {
            _repositoryMock.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[]
            {
                new CustomerNotificationChannels(1, 5, true, true, false),
                new CustomerNotificationChannels(2, 6, false, false, true)
            });

            var result = await _sut.ListAllAsync(CancellationToken.None);

            result.Should().HaveCount(2);
        }
    }
}
