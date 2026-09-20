using Connection360.Application.Ports.Persistence;
using Connection360.Domain.Ports.Persistence;

namespace Connection360.Application.UseCases.Persistence
{
    public class CollaboratorUseCase : ICollaboratorUseCase
    {
        private readonly IUnitOfWork _unitOfWork;

        public CollaboratorUseCase(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Int64> CreateAsync(String collaboratorId, CancellationToken cancellationToken = default)
        {
            // Se resuelve el repositorio de productos SOLAMENTE si se invoca esta línea
            ICollaboratorRepository collaboratorRepository = _unitOfWork.GetRepository<ICollaboratorRepository>();

            try
            {
                // 2. Uso explícito de Unit of Work para garantizar la atomicidad transaccional
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                Int64 idResult = await collaboratorRepository.CrearAsync(collaboratorId, cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return idResult;
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<Int64> CreateCustomerCollaboratorAsync(String customerId, String collaboratorId, CancellationToken cancellationToken = default)
        {
            Int64 customerIdDb = await this.GetCustomerId(customerId, cancellationToken);
            if (customerIdDb == 0)
            {
                throw new ArgumentException("No existe un cliente identificado.", nameof(customerId));
            }
            Int64 collaboratorIdDb = await this.GetCollaboratorId(collaboratorId, cancellationToken);
            if (collaboratorIdDb == 0)
            {
                throw new ArgumentException("No existe un colaborador identificado.", nameof(collaboratorId));
            }

            ICustomersOfCollaboratorsRepository customersOfCollaboratorsRepository = _unitOfWork.GetRepository<ICustomersOfCollaboratorsRepository>();

            try
            {
                // 2. Uso explícito de Unit of Work para garantizar la atomicidad transaccional
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                Int64 idResult = await customersOfCollaboratorsRepository.CrearAsync(customerIdDb, collaboratorIdDb, cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return idResult;
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private async Task<Int64> GetCustomerId(String clientId, CancellationToken cancellationToken = default)
        {
            // Se resuelve el repositorio de productos SOLAMENTE si se invoca esta línea
            ICustomerRepository customerRepository = _unitOfWork.GetRepository<ICustomerRepository>();

            Int64? idResult = await customerRepository.GetCustomerByIdAsync(clientId, cancellationToken);

            return (Int64)(idResult == null ? 0 : idResult);
        }

        private async Task<Int64> GetCollaboratorId(String collaboratorId, CancellationToken cancellationToken = default)
        {
            // Se resuelve el repositorio de productos SOLAMENTE si se invoca esta línea
            ICollaboratorRepository collaboratorRepository = _unitOfWork.GetRepository<ICollaboratorRepository>();

            Int64? idResult = await collaboratorRepository.GetCollaboratorByIdAsync(collaboratorId, cancellationToken);

            return (Int64)(idResult == null ? 0 : idResult);
        }

    }
}
