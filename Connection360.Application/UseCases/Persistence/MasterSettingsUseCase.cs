using Connection360.Application.DTOs;
using Connection360.Application.DTOs.Persistence;
using Connection360.Application.Ports.Persistence;
using Connection360.Domain.Entities.Persistence;
using Connection360.Domain.Ports.Persistence;

namespace Connection360.Application.UseCases.Persistence
{
    public class MasterSettingsUseCase : IMasterSettingsUseCase
    {
        private readonly IUnitOfWork _unitOfWork;

        public MasterSettingsUseCase(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<MasterSettingsResponseDto> CreateAsync(CreateMasterSettingsDto dto, CancellationToken cancellationToken = default)
        {
            // Se resuelve el repositorio de productos SOLAMENTE si se invoca esta línea
            IMasterSettingsRepository masterSettingsRepository = _unitOfWork.GetRepository<IMasterSettingsRepository>();

            // 1. Dominio valida las reglas de negocio
            var newmasterSettings = new MasterSettings(0, dto.AutomaticTrackingUpdate, dto.RequireDocumentUpload, dto.PublicMonitoring, dto.CurrencyType, dto.Language, dto.TimeZone, dto.DataRetentionDays);

            try
            {
                // 2. Uso explícito de Unit of Work para garantizar la atomicidad transaccional
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                var idResult = await masterSettingsRepository.CrearAsync(newmasterSettings, cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return new MasterSettingsResponseDto(idResult, dto.AutomaticTrackingUpdate, dto.RequireDocumentUpload, dto.PublicMonitoring, dto.CurrencyType, dto.Language, dto.TimeZone, dto.DataRetentionDays);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<MasterSettingsResponse> GetAsync(CancellationToken cancellationToken = default)
        {
            // Se resuelve el repositorio de productos SOLAMENTE si se invoca esta línea
            IMasterSettingsRepository masterSettingsRepository = _unitOfWork.GetRepository<IMasterSettingsRepository>();

            MasterSettings masterSettings = await masterSettingsRepository.GetAsync(cancellationToken);

            if (masterSettings == null)
            {
                masterSettings = new MasterSettings();
            }

            MasterSettingsResponse masterSettingsResponse = new MasterSettingsResponse
            {
                IdMasterSettings = masterSettings.IdMasterSettings,
                GeneralParameters = new GeneralParametersResponse
                {
                    AutomaticTrackingUpdate = masterSettings.AutomaticTrackingUpdate,
                    RequireDocumentUpload = masterSettings.RequireDocumentUpload,
                    PublicMonitoring = masterSettings.PublicMonitoring
                },
                Location = new LocationResponse
                {
                    CurrencyType = masterSettings.CurrencyType ?? String.Empty,
                    Language = masterSettings.Language ?? String.Empty,
                },
                System = new SystemResponse
                {
                    TimeZone = masterSettings.TimeZone ?? String.Empty,
                    DataRetentionDays = masterSettings.DataRetentionDays,
                }
            };
            return masterSettingsResponse;
        }

        public async Task<MasterSettingsResponseDto> UpdateAsync(MasterSettingsResponseDto dto, CancellationToken cancellationToken = default)
        {
            // Se resuelve el repositorio de productos SOLAMENTE si se invoca esta línea
            IMasterSettingsRepository masterSettingsRepository = _unitOfWork.GetRepository<IMasterSettingsRepository>();

            // 1. Dominio valida las reglas de negocio
            var newmasterSettings = new MasterSettings(dto.IdMasterSettings, dto.AutomaticTrackingUpdate, dto.RequireDocumentUpload, dto.PublicMonitoring, dto.CurrencyType, dto.Language, dto.TimeZone, dto.DataRetentionDays);

            try
            {
                // 2. Uso explícito de Unit of Work para garantizar la atomicidad transaccional
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                Boolean idResult = await masterSettingsRepository.UpdateAsync(newmasterSettings, cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                if (idResult)
                {
                    return new MasterSettingsResponseDto(dto.IdMasterSettings, dto.AutomaticTrackingUpdate, dto.RequireDocumentUpload, dto.PublicMonitoring, dto.CurrencyType, dto.Language, dto.TimeZone, dto.DataRetentionDays);
                }
                throw new ArgumentException("No realizó actualización correctamente ", nameof(dto.IdMasterSettings));
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
