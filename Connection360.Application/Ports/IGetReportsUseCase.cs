using Connection360.Application.DTOs;

namespace Connection360.Application.Ports
{
    public interface IGetReportsUseCase
    {
        Task<ReportsSummaryResponse> ExecuteGetReportsTotalsAsync(ClientSummaryRequest request, CancellationToken cancellationToken);
    }
}
