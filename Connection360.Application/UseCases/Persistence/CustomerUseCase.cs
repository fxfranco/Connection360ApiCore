using Connection360.Application.Ports.Persistence;
using Connection360.Domain.Ports.Persistence;

namespace Connection360.Application.UseCases.Persistence
{
    public class CustomerUseCase : ICustomerUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        public CustomerUseCase(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Int64> CrearAsync(String clientId, CancellationToken cancellationToken = default)
        {
            // Se resuelve el repositorio de productos SOLAMENTE si se invoca esta línea
            ICustomerRepository customerRepository = _unitOfWork.GetRepository<ICustomerRepository>();

            try
            {
                // 2. Uso explícito de Unit of Work para garantizar la atomicidad transaccional
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                Int64 idResult = await customerRepository.CrearAsync(clientId, cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return idResult;
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<Int64?> GetByIdAsync(String clientId, CancellationToken cancellationToken = default)
        {
            // Se resuelve el repositorio de productos SOLAMENTE si se invoca esta línea
            ICustomerRepository customerRepository = _unitOfWork.GetRepository<ICustomerRepository>();

            Int64? idResult = (Int64?)await customerRepository.GetCustomerByIdAsync(clientId, cancellationToken);

            return idResult;
        }
    }
}
