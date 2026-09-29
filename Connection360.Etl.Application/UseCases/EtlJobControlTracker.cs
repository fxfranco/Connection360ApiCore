using Connection360.Etl.Domain.Enums;
using Connection360.Etl.Domain.Ports.Persistence;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Application.UseCases
{
    /// <summary>
    /// Envuelve <see cref="IEtlJobControlRepository"/> para que RunEtlProcessUseCase y
    /// RunLogsEtlProcessUseCase no repitan la mecánica de "recordar el Id del registro de control y
    /// pasarlo en cada llamada". El repositorio se resuelve UNA vez (siempre sobre el mismo
    /// <c>DbSession</c>, ver <see cref="Persistence.UnitOfWork"/>), así que cada método participa
    /// correctamente en la transacción que esté activa en ese momento (o en autocommit, si no hay
    /// ninguna activa) sin necesidad de volver a resolverlo.
    /// </summary>
    internal sealed class EtlJobControlTracker
    {
        private readonly IEtlJobControlRepository _repository;
        private readonly EtlJobName _jobName;
        private Int64? _jobControlId;

        public EtlJobControlTracker(IEtlJobControlRepository repository, EtlJobName jobName)
        {
            _repository = repository;
            _jobName = jobName;
        }

        /// <summary>True si este job ya tiene al menos una corrida COMPLETED registrada.</summary>
        public Task<Boolean> HasCompletedRunAsync(CancellationToken cancellationToken = default)
            => _repository.HasCompletedRunAsync(_jobName, cancellationToken);

        /// <summary>Registra el inicio de la corrida (PROCESSING, autocommit) y recuerda su Id.</summary>
        public async Task StartAsync(Int32? pageSize, CancellationToken cancellationToken = default)
        {
            _jobControlId = await _repository.StartRunAsync(_jobName, pageSize, cancellationToken);
        }

        /// <summary>Suma el avance de una página. Debe llamarse dentro de la transacción de esa página.</summary>
        public Task RegisterPageProgressAsync(Int32 lastProcessedPage, Int32 recordsProcessedInPage, CancellationToken cancellationToken = default)
        {
            EnsureStarted();
            return _repository.RegisterPageProgressAsync(_jobControlId!.Value, lastProcessedPage, recordsProcessedInPage, cancellationToken);
        }

        /// <summary>Marca la corrida como COMPLETED. Solo debe llamarse una vez que la última página cargó sin error.</summary>
        public Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            EnsureStarted();
            return _repository.CompleteRunAsync(_jobControlId!.Value, cancellationToken);
        }

        /// <summary>
        /// Marca la corrida como FAILED (autocommit). Si nunca llegó a registrarse (la corrida falló
        /// antes de poder llamar a <see cref="StartAsync"/>), no hay nada que marcar.
        /// </summary>
        public Task FailAsync(CancellationToken cancellationToken = default)
            => _jobControlId.HasValue ? _repository.FailRunAsync(_jobControlId.Value, cancellationToken) : Task.CompletedTask;

        private void EnsureStarted()
        {
            if (!_jobControlId.HasValue)
                throw new InvalidOperationException("Debe llamarse StartAsync antes de registrar avance o completar la corrida.");
        }
    }
}
