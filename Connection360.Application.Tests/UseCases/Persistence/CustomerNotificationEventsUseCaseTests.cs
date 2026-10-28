using Connection360.Application.DTOs.Persistence;
using Connection360.Application.UseCases.Persistence;
using Connection360.Domain.Entities.Persistence;
using Connection360.Domain.Ports.Persistence;
using FluentAssertions;
using Moq;
using Xunit;

namespace Connection360.Application.Tests.UseCases.Persistence
{
    public class CustomerNotificationEventsUseCaseTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<ICustomerNotificationEventRepository> _repositoryMock = new();
        private readonly CustomerNotificationEventsUseCase _sut;

        public CustomerNotificationEventsUseCaseTests()
        {
            _unitOfWorkMock.Setup(u => u.GetRepository<ICustomerNotificationEventRepository>()).Returns(_repositoryMock.Object);
            _sut = new CustomerNotificationEventsUseCase(_unitOfWorkMock.Object);
        }

        [Fact]
        public async Task CrearAsync_UsaTransaccionYRetornaElDtoConElIdGenerado()
        {
            var dto = new CreateCustomerNotificationEventsDto(5, true, true, false, true, false);
            _repositoryMock.Setup(r => r.CrearAsync(It.IsAny<CustomerNotificationEvents>(), It.IsAny<CancellationToken>())).ReturnsAsync(50);

            var result = await _sut.CrearAsync(dto, CancellationToken.None);

            result.IdNotificationEvent.Should().Be(50);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CrearAsync_SiFalla_HaceRollbackYRelanza()
        {
            var dto = new CreateCustomerNotificationEventsDto(5, true, true, false, true, false);
            _repositoryMock.Setup(r => r.CrearAsync(It.IsAny<CustomerNotificationEvents>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException());

            Func<Task> act = () => _sut.CrearAsync(dto, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
            _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_CuandoNoExiste_RetornaNull()
        {
            _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((CustomerNotificationEvents?)null);

            var result = await _sut.GetByIdAsync(1, CancellationToken.None);

            result.Should().BeNull();
        }

        [Fact]
        public async Task ListAllAsync_MapeaTodosLosRegistros()
        {
            _repositoryMock.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[]
            {
                new CustomerNotificationEvents(1, 5, true, true, true, true, true)
            });

            var result = await _sut.ListAllAsync(CancellationToken.None);

            result.Should().ContainSingle();
        }
    }
}
