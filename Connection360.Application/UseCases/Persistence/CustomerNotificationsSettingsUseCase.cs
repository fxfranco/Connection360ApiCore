using Connection360.Application.DTOs;
using Connection360.Application.DTOs.Persistence;
using Connection360.Application.Ports.Persistence;
using Connection360.Domain.Entities.Persistence;
using Connection360.Domain.Ports.Persistence;

namespace Connection360.Application.UseCases.Persistence
{
    public class CustomerNotificationsSettingsUseCase : ICustomerNotificationsSettingsUseCase
    {
        private readonly IUnitOfWork _unitOfWork;

        public CustomerNotificationsSettingsUseCase(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CustomerNotificationsSettingsResponse> GetCustomerNotificationSettings(String clientId, CancellationToken cancellationToken = default)
        {
            //ToDo: Pendiente crear exceptiones personalizadas tipo NotFoundException : Exception
            Int64 customerId = await this.GetCustomerId(clientId, cancellationToken);
            if (customerId == 0)
            {
                throw new ArgumentException("No existe un cliente identificado.", nameof(clientId));
            }
            CustomerNotificationChannelsResponseDto? customerNotificationChannelsResponseDto = await this.GetCustomerNotificationChannels(customerId);
            CustomerNotificationEventsResponseDto? customerNotificationEventsResponseDto = await this.GetCustomerNotificationEvent(customerId);

            if (customerNotificationChannelsResponseDto == null)
            {
                customerNotificationChannelsResponseDto = new CustomerNotificationChannelsResponseDto(0, customerId, false, false, false);
            }

            if (customerNotificationEventsResponseDto == null)
            {
                customerNotificationEventsResponseDto = new CustomerNotificationEventsResponseDto(0, customerId, false, false, false, false, false);
            }

            return new CustomerNotificationsSettingsResponse
            {
                NotificationChannels = new NotificationChannelsResponse
                {
                    ClientId = clientId,
                    NotificationChannelId = customerNotificationChannelsResponseDto.IdNotificationChannels,
                    Application = customerNotificationChannelsResponseDto.Application,
                    Email = customerNotificationChannelsResponseDto.Email,
                    TextMessages = customerNotificationChannelsResponseDto.TextMessages
                },
                NotificationEvents = new NotificationEventsResponse
                {
                    ClientId = clientId,
                    NotificationEventId = customerNotificationEventsResponseDto.IdNotificationEvent,
                    ChangeState = customerNotificationEventsResponseDto.ChangeState,
                    SuccessfulDelivery = customerNotificationEventsResponseDto.SuccessfulDelivery,
                    WithIssues = customerNotificationEventsResponseDto.WithIssues,
                    ShipmentTransit = customerNotificationEventsResponseDto.ShipmentTransit,
                    DeliveryReminder = customerNotificationEventsResponseDto.DeliveryReminder
                }
            };
        }

        private async Task<Int64> GetCustomerId(String clientId, CancellationToken cancellationToken = default)
        {
            // Se resuelve el repositorio de productos SOLAMENTE si se invoca esta línea
            ICustomerRepository customerRepository = _unitOfWork.GetRepository<ICustomerRepository>();

            Int64? idResult = await customerRepository.GetCustomerByIdAsync(clientId, cancellationToken);

            return (Int64)(idResult == null ? 0 : idResult);
        }

        private async Task<CustomerNotificationEventsResponseDto> GetCustomerNotificationEvent(Int64 customerId, CancellationToken cancellationToken = default)
        {
            // Se resuelve el repositorio de productos SOLAMENTE si se invoca esta línea
            ICustomerNotificationEventRepository customerNotificationEventRepository = _unitOfWork.GetRepository<ICustomerNotificationEventRepository>();

            var customerNotificationEvents = await customerNotificationEventRepository.GetByCustomerIdAsync(customerId, cancellationToken);
            if (customerNotificationEvents == null) return null;

            return new CustomerNotificationEventsResponseDto(customerNotificationEvents.IdNotificationEvent, customerNotificationEvents.IdCustomer,
                customerNotificationEvents.ChangeState, customerNotificationEvents.SuccessfulDelivery, customerNotificationEvents.WithIssues, customerNotificationEvents.ShipmentTransit,
                customerNotificationEvents.DeliveryReminder);
        }

        private async Task<CustomerNotificationChannelsResponseDto> GetCustomerNotificationChannels(Int64 customerId, CancellationToken cancellationToken = default)
        {
            // Se resuelve el repositorio de productos SOLAMENTE si se invoca esta línea
            ICustomerNotificationChannelsRepository customerNotificationChannelsRepository = _unitOfWork.GetRepository<ICustomerNotificationChannelsRepository>();

            var customerNotificationChannels = await customerNotificationChannelsRepository.GetByCustomerIdAsync(customerId, cancellationToken);
            if (customerNotificationChannels == null) return null;

            return new CustomerNotificationChannelsResponseDto(customerNotificationChannels.IdNotificationChannel, customerNotificationChannels.IdCustomer,
                customerNotificationChannels.Application, customerNotificationChannels.Email, customerNotificationChannels.TextMessages);
        }

        public async Task<CustomerNotificationsSettingsResponse> CreateCustomerNotificationSettings(CustomerNotificationsSettingsResponse customerNotificationsSettings, CancellationToken cancellationToken = default)
        {
            try
            {
                String clientId = !String.IsNullOrEmpty(customerNotificationsSettings.NotificationChannels?.ClientId) ? customerNotificationsSettings.NotificationChannels.ClientId : customerNotificationsSettings.NotificationEvents?.ClientId;

                if (String.IsNullOrEmpty(clientId))
                {
                    throw new ArgumentException("Debe suministrar la identificacion del cliente", nameof(clientId));
                }

                Int64 customerId = await this.GetCustomerId(clientId, cancellationToken);
                if (customerId == 0)
                {
                    throw new ArgumentException("No existe un cliente identificado.", nameof(clientId));
                }

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                Int64 createdChannels = await this.CreateCustomerNotificationChannels(customerNotificationsSettings.NotificationChannels, customerId, cancellationToken);
                if (createdChannels <= 0)
                {
                    throw new ArgumentException("Error creando notificaciones de canales");                    
                }
                customerNotificationsSettings.NotificationChannels.NotificationChannelId = createdChannels;

                Int64 createdEvents = await this.CreateCustomerNotificationEvents(customerNotificationsSettings.NotificationEvents, customerId, cancellationToken);

                if (createdEvents <= 0)
                {
                    throw new ArgumentException("Error creando notificaciones de eventos");
                }
                customerNotificationsSettings.NotificationEvents.NotificationEventId = createdEvents;
                await _unitOfWork.CommitAsync(cancellationToken);
            }
            catch (Exception)
            {
                await _unitOfWork.CommitAsync(cancellationToken);
                throw;
            }

            return customerNotificationsSettings;
        }

        private async Task<Int64> CreateCustomerNotificationEvents(NotificationEventsResponse dto, Int64 idCustomer, CancellationToken cancellationToken = default)
        {
            ICustomerNotificationEventRepository customerNotificationEventRepository = _unitOfWork.GetRepository<ICustomerNotificationEventRepository>();

            CustomerNotificationEvents customerNotificationEvents = new CustomerNotificationEvents(0, idCustomer, dto.ChangeState, dto.SuccessfulDelivery, dto.WithIssues, dto.ShipmentTransit, dto.DeliveryReminder);

            return await customerNotificationEventRepository.CrearAsync(customerNotificationEvents, cancellationToken);
        }

        private async Task<Int64> CreateCustomerNotificationChannels(NotificationChannelsResponse dto, Int64 idCustomer, CancellationToken cancellationToken = default)
        {
            ICustomerNotificationChannelsRepository customerNotificationChannelsRepository = _unitOfWork.GetRepository<ICustomerNotificationChannelsRepository>();

            CustomerNotificationChannels customerNotificationEvents = new CustomerNotificationChannels(0, idCustomer, dto.Application, dto.Email, dto.TextMessages);

            return await customerNotificationChannelsRepository.CrearAsync(customerNotificationEvents, cancellationToken);
        }

        public async Task<Boolean> UpdateCustomerNotificationSettings(CustomerNotificationsSettingsResponse customerNotificationsSettings, CancellationToken cancellationToken = default)
        {
            try
            {
                String clientId = !String.IsNullOrEmpty(customerNotificationsSettings.NotificationChannels?.ClientId) ? customerNotificationsSettings.NotificationChannels.ClientId : customerNotificationsSettings.NotificationEvents?.ClientId;

                if (String.IsNullOrEmpty(clientId))
                {
                    throw new ArgumentException("Debe suministrar la identificacion del cliente", nameof(clientId));
                }

                Int64 customerId = await this.GetCustomerId(clientId, cancellationToken);
                if (customerId == 0)
                {
                    throw new ArgumentException("No existe un cliente identificado.", nameof(clientId));
                }
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                Boolean updateChannels = await this.UpdateCustomerNotificationChannels(customerNotificationsSettings.NotificationChannels, customerId, cancellationToken);
                if (!updateChannels)
                {
                    throw new ArgumentException("Error modificando notificaciones de canales");
                }

                Boolean updateEvents = await this.UpdateCustomerNotificationEvents(customerNotificationsSettings.NotificationEvents, customerId, cancellationToken);

                if (!updateEvents)
                {
                    throw new ArgumentException("Error modificando notificaciones de eventos");
                }
                await _unitOfWork.CommitAsync(cancellationToken);
                return true;
            }
            catch (Exception)
            {
                await _unitOfWork.CommitAsync(cancellationToken);
                throw;
            }
        }

        private async Task<Boolean> UpdateCustomerNotificationEvents(NotificationEventsResponse dto, Int64 idCustomer, CancellationToken cancellationToken = default)
        {
            ICustomerNotificationEventRepository customerNotificationEventRepository = _unitOfWork.GetRepository<ICustomerNotificationEventRepository>();

            CustomerNotificationEvents customerNotificationEvents = new CustomerNotificationEvents(dto.NotificationEventId, idCustomer, dto.ChangeState, dto.SuccessfulDelivery, dto.WithIssues, dto.ShipmentTransit, dto.DeliveryReminder);

            return await customerNotificationEventRepository.UpdateAsync(customerNotificationEvents, cancellationToken);
        }

        private async Task<Boolean> UpdateCustomerNotificationChannels(NotificationChannelsResponse dto, Int64 idCustomer, CancellationToken cancellationToken = default)
        {
            ICustomerNotificationChannelsRepository customerNotificationChannelsRepository = _unitOfWork.GetRepository<ICustomerNotificationChannelsRepository>();

            CustomerNotificationChannels customerNotificationChannels = new CustomerNotificationChannels(dto.NotificationChannelId, idCustomer, dto.Application, dto.Email, dto.TextMessages);

            return await customerNotificationChannelsRepository.UpdateAsync(customerNotificationChannels, cancellationToken);
        }

    }
}
