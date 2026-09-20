using Connection360.Application.DTOs;
using Connection360.Application.DTOs.Persistence;
using Connection360.Application.UseCases.Persistence;
using Connection360.Domain.Entities.Persistence;
using Connection360.Domain.Ports.Persistence;
using FluentAssertions;
using Moq;
using Xunit;

namespace Connection360.Application.Tests.UseCases.Persistence
{
    public class MasterSettingsUseCaseTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IMasterSettingsRepository> _repositoryMock = new();
        private readonly MasterSettingsUseCase _useCase;

        public MasterSettingsUseCaseTests()
        {
            _unitOfWorkMock.Setup(u => u.GetRepository<IMasterSettingsRepository>()).Returns(_repositoryMock.Object);
            _useCase = new MasterSettingsUseCase(_unitOfWorkMock.Object);
        }

        [Fact]
        public async Task CreateAsync_DebeCrearYRetornarDtoConIdGenerado()
        {
            var dto = new CreateMasterSettingsDto(true, false, true, "COP", "es-CO", "America/Bogota", 30);
            _repositoryMock.Setup(r => r.CrearAsync(It.IsAny<MasterSettings>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);

            MasterSettingsResponseDto result = await _useCase.CreateAsync(dto, CancellationToken.None);

            result.IdMasterSettings.Should().Be(1);
            result.CurrencyType.Should().Be("COP");
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_SiRepositorioFalla_DebeHacerRollback()
        {
            var dto = new CreateMasterSettingsDto(true, false, true, "COP", "es-CO", "America/Bogota", 30);
            _repositoryMock.Setup(r => r.CrearAsync(It.IsAny<MasterSettings>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException());

            Func<Task> act = () => _useCase.CreateAsync(dto, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
            _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAsync_SinConfiguracionExistente_DebeRetornarValoresPorDefecto()
        {
            _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync((MasterSettings)null!);

            MasterSettingsResponse result = await _useCase.GetAsync(CancellationToken.None);

            result.IdMasterSettings.Should().Be(0);
            result.Location!.CurrencyType.Should().Be(String.Empty);
        }

        [Fact]
        public async Task GetAsync_ConConfiguracionExistente_DebeMapearTodasLasSecciones()
        {
            var settings = new MasterSettings(1, true, true, false, "USD", "en-US", "UTC", 90);
            _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

            MasterSettingsResponse result = await _useCase.GetAsync(CancellationToken.None);

            result.IdMasterSettings.Should().Be(1);
            result.GeneralParameters!.AutomaticTrackingUpdate.Should().BeTrue();
            result.Location!.CurrencyType.Should().Be("USD");
            result.System!.DataRetentionDays.Should().Be(90);
        }

        [Fact]
        public async Task UpdateAsync_ActualizacionExitosa_DebeRetornarDtoActualizado()
        {
            var dto = new MasterSettingsResponseDto(1, true, true, true, "COP", "es-CO", "America/Bogota", 60);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MasterSettings>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            MasterSettingsResponseDto result = await _useCase.UpdateAsync(dto, CancellationToken.None);

            result.Should().Be(dto);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ActualizacionFallida_DebeHacerRollbackYLanzarArgumentException()
        {
            var dto = new MasterSettingsResponseDto(1, true, true, true, "COP", "es-CO", "America/Bogota", 60);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MasterSettings>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

            Func<Task> act = () => _useCase.UpdateAsync(dto, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
            _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_UsaTransaccionYRetornaElDtoConElIdGenerado()
        {
            var dto = new CreateMasterSettingsDto(true, false, true, "COP", "es", "America/Bogota", 30);
            _repositoryMock.Setup(r => r.CrearAsync(It.IsAny<MasterSettings>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var result = await _useCase.CreateAsync(dto, CancellationToken.None);

            result.IdMasterSettings.Should().Be(1);
            result.CurrencyType.Should().Be("COP");
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_SiFalla_HaceRollbackYRelanza()
        {
            var dto = new CreateMasterSettingsDto(true, false, true, "COP", "es", "America/Bogota", 30);
            _repositoryMock.Setup(r => r.CrearAsync(It.IsAny<MasterSettings>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException());

            Func<Task> act = () => _useCase.CreateAsync(dto, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
            _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAsync_CuandoNoHayConfiguracion_UsaValoresPorDefecto()
        {
            _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync((MasterSettings)null!);

            var result = await _useCase.GetAsync(CancellationToken.None);

            result.Location.CurrencyType.Should().Be(String.Empty);
            result.System.TimeZone.Should().Be(String.Empty);
        }

        [Fact]
        public async Task GetAsync_MapeaCorrectamenteLasTresSecciones()
        {
            _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MasterSettings(1, true, false, true, "USD", "en", "UTC", 60));

            var result = await _useCase.GetAsync(CancellationToken.None);

            result.IdMasterSettings.Should().Be(1);
            result.GeneralParameters.AutomaticTrackingUpdate.Should().BeTrue();
            result.Location.CurrencyType.Should().Be("USD");
            result.System.DataRetentionDays.Should().Be(60);
        }

        [Fact]
        public async Task UpdateAsync_CuandoElRepositorioRetornaTrue_RetornaElDtoActualizado()
        {
            var dto = new MasterSettingsResponseDto(1, true, false, true, "COP", "es", "America/Bogota", 30);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MasterSettings>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await _useCase.UpdateAsync(dto, CancellationToken.None);

            result.Should().BeEquivalentTo(dto);
        }

        [Fact]
        public async Task UpdateAsync_CuandoElRepositorioRetornaFalse_LanzaArgumentException()
        {
            var dto = new MasterSettingsResponseDto(1, true, false, true, "COP", "es", "America/Bogota", 30);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MasterSettings>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

            Func<Task> act = () => _useCase.UpdateAsync(dto, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task UpdateAsync_SiElRepositorioLanzaExcepcion_HaceRollback()
        {
            var dto = new MasterSettingsResponseDto(1, true, false, true, "COP", "es", "America/Bogota", 30);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MasterSettings>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException());

            Func<Task> act = () => _useCase.UpdateAsync(dto, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
            _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
