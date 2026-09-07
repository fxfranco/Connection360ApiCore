using Connection360.Domain.Entities.Persistence;

namespace Connection360.Domain.Ports.Persistence
{
    public interface IMasterSettingsRepository
    {
        Task<MasterSettings> GetAsync(CancellationToken cancellationToken = default);
        Task<Int64> CrearAsync(MasterSettings masterSettings, CancellationToken cancellationToken = default);
        Task<Boolean> UpdateAsync(MasterSettings masterSettings, CancellationToken cancellationToken = default);
    }
}
