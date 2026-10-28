using Connection360Notification.Application.DTOs;
using Connection360Notification.Application.Ports.Inbound;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Ports.Outbound;

namespace Connection360Notification.Application.UseCases
{
    /// <summary>
    /// Guarda un lote de usuarios sin duplicarlos y, al llegar el último lote de la sincronización, elimina los
    /// usuarios que ya no existen en Auth0.
    ///
    /// Sin duplicados: cada usuario se escribe con upsert por su Id de Auth0 (que es el "_id" del documento), por lo
    /// que reprocesar un lote —Kafka entrega "al menos una vez"— o recibir el mismo usuario en varias sincronizaciones
    /// actualiza el mismo registro. Dentro del propio lote también se descartan los Id repetidos.
    ///
    /// Limpieza de obsoletos: cada registro queda marcado con el SyncId que lo trajo. En el último lote se verifica que
    /// la cantidad de registros marcados con ese SyncId sea igual a TotalUsers (es decir, que no se perdió ningún lote)
    /// y solo entonces se borran los que tienen otra marca. Si la sincronización está incompleta no se borra nada: es
    /// preferible conservar un usuario obsoleto hasta la siguiente sincronización que borrar uno vigente.
    /// </summary>
    public class SyncBasicUsersUseCase : ISyncBasicUsersUseCase
    {
        private readonly IBasicUserDataRepository _repository;

        public SyncBasicUsersUseCase(IBasicUserDataRepository repository)
        {
            _repository = repository;
        }

        public async Task<BasicUsersSyncResult> ExecuteAsync(BasicUsersSyncMessage message, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(message);

            List<BasicUserItem> received = message.Users ?? new List<BasicUserItem>();

            List<BasicUserData> users = received
                .Where(user => user != null && !String.IsNullOrWhiteSpace(user.Id))
                .GroupBy(user => user.Id.Trim(), StringComparer.Ordinal)
                .Select(group => group.Last())
                .Select(user => new BasicUserData(user.Id.Trim(), user.Document ?? String.Empty, user.FullName ?? String.Empty, user.Phone ?? String.Empty, user.Email ?? String.Empty))
                .ToList();

            Int32 discarded = received.Count(user => user == null || String.IsNullOrWhiteSpace(user.Id));

            if (users.Count > 0)
            {
                await _repository.UpsertBatchAsync(users, message.SyncId, cancellationToken);
            }

            Boolean isLastBatch = message.BatchCount > 0 && message.BatchIndex == message.BatchCount - 1;
            if (!isLastBatch)
            {
                return new BasicUsersSyncResult(users.Count, discarded, IsLastBatch: false, CleanupPerformed: false, Deleted: 0, StoredInSync: 0);
            }

            Int64 stored = await _repository.CountBySyncAsync(message.SyncId, cancellationToken);
            if (stored != message.TotalUsers)
            {
                return new BasicUsersSyncResult(users.Count, discarded, IsLastBatch: true, CleanupPerformed: false, Deleted: 0, StoredInSync: stored);
            }

            Int64 deleted = await _repository.DeleteNotInSyncAsync(message.SyncId, cancellationToken);
            return new BasicUsersSyncResult(users.Count, discarded, IsLastBatch: true, CleanupPerformed: true, Deleted: deleted, StoredInSync: stored);
        }
    }
}
