using Connection360.Application.Ports.Persistence;
using Connection360.Domain.Dtos;
using Connection360.Domain.Exceptions;
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

            // 1. No se permiten clientes con la misma identificación
            if (await customerRepository.GetCustomerByIdAsync(clientId, cancellationToken) != null)
            {
                throw new DuplicateRecordException($"Ya existe un cliente con la identificación '{clientId}'.");
            }

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

            Int64? idResult = await customerRepository.GetCustomerByIdAsync(clientId, cancellationToken);

            return idResult;
        }

        public async Task<List<CustomerDtoResult>> ListAsync(CancellationToken cancellationToken = default)
        {
            // Consulta de solo lectura: no requiere transacción.
            ICustomerRepository customerRepository = _unitOfWork.GetRepository<ICustomerRepository>();

            return await customerRepository.ListAsync(cancellationToken);
        }
    }
}
