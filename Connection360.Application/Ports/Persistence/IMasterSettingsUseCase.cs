using Connection360.Application.DTOs;
using Connection360.Application.DTOs.Persistence;

namespace Connection360.Application.Ports.Persistence
{
    public interface IMasterSettingsUseCase
    {
        Task<MasterSettingsResponse> GetAsync(CancellationToken cancellationToken = default);
        Task<MasterSettingsResponseDto> CreateAsync(CreateMasterSettingsDto dto, CancellationToken cancellationToken = default);
        Task<MasterSettingsResponseDto> UpdateAsync(MasterSettingsResponseDto dto, CancellationToken cancellationToken = default);
    }
}
