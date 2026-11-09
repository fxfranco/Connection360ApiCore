using Connection360.Application.DTOs.Persistence;
using Connection360.Application.Ports.Persistence;
using Connection360.Domain.Entities.Persistence;
using Connection360.Domain.Ports.Persistence;

namespace Connection360.Application.UseCases.Persistence
{
    public class CustomerNotificationChannelsUseCase : ICustomerNotificationChannelsUseCase
    {
        private readonly IUnitOfWork _unitOfWork;

        public CustomerNotificationChannelsUseCase(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<CustomerNotificationChannelsResponseDto> CrearAsync(CreateCustomerNotificationChannelsDto dto, CancellationToken cancellationToken = default)
        {
            // Se resuelve el repositorio de productos SOLAMENTE si se invoca esta línea
            ICustomerNotificationChannelsRepository customerNotificationChannelsRepository = _unitOfWork.GetRepository<ICustomerNotificationChannelsRepository>();

            // 1. Dominio valida las reglas de negocio
            var newCustomerNotificationChannels = new CustomerNotificationChannels(0, dto.IdCustomer, dto.Application, dto.Email, dto.TextMessages);

            try
            {
                // 2. Uso explícito de Unit of Work para garantizar la atomicidad transaccional
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                var idResult = await customerNotificationChannelsRepository.CrearAsync(newCustomerNotificationChannels, cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return new CustomerNotificationChannelsResponseDto(idResult, newCustomerNotificationChannels.IdCustomer, newCustomerNotificationChannels.Application,
                    newCustomerNotificationChannels.Email, newCustomerNotificationChannels.TextMessages);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<CustomerNotificationChannelsResponseDto?> GetByIdAsync(Int64 id, CancellationToken cancellationToken = default)
        {
            // Se resuelve el repositorio de productos SOLAMENTE si se invoca esta línea
            ICustomerNotificationChannelsRepository customerNotificationChannelsRepository = _unitOfWork.GetRepository<ICustomerNotificationChannelsRepository>();

            var customerNotificationChannels = await customerNotificationChannelsRepository.GetByIdAsync(id, cancellationToken);
            if (customerNotificationChannels == null) return null ;

            return new CustomerNotificationChannelsResponseDto(customerNotificationChannels.IdNotificationChannel, customerNotificationChannels.IdCustomer, 
                customerNotificationChannels.Application, customerNotificationChannels.Email, customerNotificationChannels.TextMessages);
        }

        public async Task<IEnumerable<CustomerNotificationChannelsResponseDto>> ListAllAsync(CancellationToken cancellationToken = default)
        {
            // Se resuelve el repositorio de productos SOLAMENTE si se invoca esta línea
            ICustomerNotificationChannelsRepository customerNotificationChannelsRepository = _unitOfWork.GetRepository<ICustomerNotificationChannelsRepository>();

            var customerNotificationChannels = await customerNotificationChannelsRepository.ListAllAsync(cancellationToken);
            return customerNotificationChannels.Select(cnc => new CustomerNotificationChannelsResponseDto(cnc.IdNotificationChannel, cnc.IdCustomer, cnc.Application, cnc.Email, cnc.TextMessages));
        }
    }
}
