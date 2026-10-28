using Connection360.Application.DTOs;

namespace Connection360.Application.Ports
{
    public interface IGetMyShipmentsUseCase
    {
        Task<MyShipmentsResponse> ExecuteGetAllShipmentsAsync(MyShipmentsRequest request, CancellationToken cancellationToken);
        Task<MyShipmentsResponse> ExecuteFilterShipmentsAsync(MyShipmentsRequest request, CancellationToken cancellationToken);
        Task<MyShipmentsResponse> ExecuteGetHistoryAllShipmentsAsync(MyShipmentsRequest request, CancellationToken cancellationToken);
        Task<MyShipmentsResponse> ExecuteFilterHistoryShipmentsAsync(MyShipmentsRequest request, CancellationToken cancellationToken);
        Task<DetailsShipmentsResponse> ExecuteDetailsShipmentsAsync(MyShipmentsRequest request, CancellationToken cancellationToken);
    }
}
