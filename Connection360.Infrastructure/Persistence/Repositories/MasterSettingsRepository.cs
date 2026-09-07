using Connection360.Domain.Entities.Persistence;
using Connection360.Domain.Ports.Persistence;
using Dapper;

namespace Connection360.Infrastructure.Persistence.Repositories
{
    public class MasterSettingsRepository : IMasterSettingsRepository
    {
        private readonly DbSession _session;

        public MasterSettingsRepository(DbSession session)
        {
            _session = session;
        }

        public async Task<Int64> CrearAsync(MasterSettings masterSettings, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);

            const string query = @"
                INSERT INTO connection360write.master_settings
                    (automatic_tracking_update, require_document_upload, public_monitoring,
                     currency_type, language, time_zone, data_retention_days)
                VALUES
                    (@AutomaticTrackingUpdate, @RequireDocumentUpload, @PublicMonitoring,
                     @CurrencyType, @Language, @TimeZone, @DataRetentionDays)
                RETURNING id_master_settings;";

            var command = new CommandDefinition(
                query,
                 new
                 {
                     masterSettings.AutomaticTrackingUpdate,
                     masterSettings.RequireDocumentUpload,
                     masterSettings.PublicMonitoring,
                     masterSettings.CurrencyType,
                     masterSettings.Language,
                     masterSettings.TimeZone,
                     masterSettings.DataRetentionDays,

                 },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken
            );

            return await _session.Connection.ExecuteScalarAsync<Int64>(command);
        }

        public async Task<MasterSettings> GetAsync(CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);

            const string query = @"SELECT
                    id_master_settings,
                    automatic_tracking_update,
                    require_document_upload,
                    public_monitoring,
                    currency_type,
                    language,

                    time_zone,
                    data_retention_days
                FROM connection360write.master_settings";

            var command = new CommandDefinition(
                query,
                null,
                transaction: _session.Transaction,
                cancellationToken: cancellationToken
            );

            return await _session.Connection.QueryFirstOrDefaultAsync<MasterSettings>(command);
        }

        public async Task<Boolean> UpdateAsync(MasterSettings masterSettings, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);

            const string query = @"
                UPDATE connection360write.master_settings
                SET
                    automatic_tracking_update = @AutomaticTrackingUpdate,
                    require_document_upload   = @RequireDocumentUpload,
                    public_monitoring         = @PublicMonitoring,
                    currency_type             = @CurrencyType,
                    language                  = @Language,
                    time_zone                 = @TimeZone,
                    data_retention_days       = @DataRetentionDays
                WHERE id_master_settings = @IdMasterSettings;";

            var command = new CommandDefinition(
                query,
                 new
                 {
                     masterSettings.AutomaticTrackingUpdate,
                     masterSettings.RequireDocumentUpload,
                     masterSettings.PublicMonitoring,
                     masterSettings.CurrencyType,
                     masterSettings.Language,
                     masterSettings.TimeZone,
                     masterSettings.DataRetentionDays,
                     masterSettings.IdMasterSettings
                 },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken
            );

            Int16 rowsAffected = (Int16)await _session.Connection.ExecuteAsync(command);
            return rowsAffected > 0;
        }
    }
}
