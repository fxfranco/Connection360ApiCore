using Connection360.Application.DTOs.Persistence;
using Connection360.Application.Ports.Persistence;
using Connection360.Domain.Dtos;
using Connection360.Domain.Ports.Persistence;

namespace Connection360.Application.UseCases.Persistence
{
    public class OutboxMessagesUseCase : IOutboxMessagesUseCase
    {
        private readonly IUnitOfWork _unitOfWork;

        public OutboxMessagesUseCase(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Boolean> CreateAsync(CreateOutboxMessagesDto outboxMessages, CancellationToken cancellationToken = default)
        {
            OutboxMessagesRequestDto outboxMessagesRequest = new OutboxMessagesRequestDto
            {
                ClientId= outboxMessages.ClientId,
                EventType= outboxMessages.EventType,
                DocumentNumber= outboxMessages.DocumentNumber,
                Title= outboxMessages.Title,
                Message= outboxMessages.Message,
                MessageDate= outboxMessages.MessageDate
            };

            IOutboxMessagesRepository outboxMessagesRepository = _unitOfWork.GetRepository<IOutboxMessagesRepository>();
            Boolean result = false;
            try
            {
                // 2. Uso explícito de Unit of Work para garantizar la atomicidad transaccional
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                Guid? idResult = await outboxMessagesRepository.CrearAsync(outboxMessagesRequest, cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                if (idResult != null && idResult != Guid.Empty) {
                    result = true;
                }
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
            return result;
        }
    }
}
