using Connection360.Application.DTOs;

namespace Connection360.Application.Ports
{
    public interface IGetClientSummaryUseCase
    {
        Task<ClientSummaryResponse> ExecuteTotalsAsync(ClientSummaryRequest request, CancellationToken cancellationToken);
        Task<ResumenClienteResponse> ExecuteFilterAsync(ClientSummaryRequest request, CancellationToken cancellationToken);
    }
}
