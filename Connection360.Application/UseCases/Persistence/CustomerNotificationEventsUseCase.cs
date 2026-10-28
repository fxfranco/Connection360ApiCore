using Connection360.Application.DTOs.Persistence;
using Connection360.Application.Ports.Persistence;
using Connection360.Domain.Entities.Persistence;
using Connection360.Domain.Ports.Persistence;

namespace Connection360.Application.UseCases.Persistence
{
    public class CustomerNotificationEventsUseCase : ICustomerNotificationEventsUseCase
    {
        private readonly IUnitOfWork _unitOfWork;

        public CustomerNotificationEventsUseCase(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CustomerNotificationEventsResponseDto> CrearAsync(CreateCustomerNotificationEventsDto dto, CancellationToken cancellationToken = default)
        {

            // Se resuelve el repositorio de productos SOLAMENTE si se invoca esta línea
            ICustomerNotificationEventRepository customerNotificationEventRepository = _unitOfWork.GetRepository<ICustomerNotificationEventRepository>();
            // 1. Dominio valida las reglas de negocio
            var newCustomerNotificationEvents = new CustomerNotificationEvents(0, dto.IdCustomer, dto.ChangeState, dto.SuccessfulDelivery, dto.WithIssues, dto.ShipmentTransit, dto.DeliveryReminder);

            try
            {
                // 2. Uso explícito de Unit of Work para garantizar la atomicidad transaccional
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                var idResult = await customerNotificationEventRepository.CrearAsync(newCustomerNotificationEvents, cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return new CustomerNotificationEventsResponseDto(idResult, newCustomerNotificationEvents.IdCustomer, newCustomerNotificationEvents.ChangeState,
                    newCustomerNotificationEvents.SuccessfulDelivery, newCustomerNotificationEvents.WithIssues, newCustomerNotificationEvents.ShipmentTransit, newCustomerNotificationEvents.DeliveryReminder);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<CustomerNotificationEventsResponseDto?> GetByIdAsync(Int64 id, CancellationToken cancellationToken = default)
        {
            // Se resuelve el repositorio de productos SOLAMENTE si se invoca esta línea
            ICustomerNotificationEventRepository customerNotificationEventRepository = _unitOfWork.GetRepository<ICustomerNotificationEventRepository>();

            var customerNotificationEvents= await customerNotificationEventRepository.GetByIdAsync(id, cancellationToken);
            if (customerNotificationEvents == null) return null;

            return new CustomerNotificationEventsResponseDto(customerNotificationEvents.IdNotificationEvent, customerNotificationEvents.IdCustomer,
                customerNotificationEvents.ChangeState, customerNotificationEvents.SuccessfulDelivery, customerNotificationEvents.WithIssues, customerNotificationEvents.ShipmentTransit, 
                customerNotificationEvents.DeliveryReminder);
        }

        public async Task<IEnumerable<CustomerNotificationEventsResponseDto>> ListAllAsync(CancellationToken cancellationToken = default)
        {
            // Se resuelve el repositorio de productos SOLAMENTE si se invoca esta línea
            ICustomerNotificationEventRepository customerNotificationEventRepository = _unitOfWork.GetRepository<ICustomerNotificationEventRepository>();

            var customerNotificationEvents = await customerNotificationEventRepository.ListAllAsync(cancellationToken);
            return customerNotificationEvents.Select(cnc => new CustomerNotificationEventsResponseDto(cnc.IdNotificationEvent, cnc.IdCustomer, cnc.ChangeState, cnc.SuccessfulDelivery, cnc.WithIssues,
                cnc.ShipmentTransit, cnc.DeliveryReminder));
        }
    }
}
