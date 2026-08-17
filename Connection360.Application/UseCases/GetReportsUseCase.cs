using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using Connection360.Domain.Entities;
using Connection360.Domain.Interfaces;
using System.Data;

namespace Connection360.Application.UseCases
{
    public class GetReportsUseCase : IGetReportsUseCase
    {
        private const Int16 FrequentRoutesCount = 5;
        private readonly IExternalDataGateway _externalDataGateway;
        private readonly IReportsDomainService _reportsDomainService;

        public GetReportsUseCase(IExternalDataGateway externalDataGateway, IReportsDomainService reportsDomainService)
        {
            _externalDataGateway = externalDataGateway;
            _reportsDomainService = reportsDomainService;
        }

        public async Task<ReportsSummaryResponse> ExecuteGetReportsTotalsAsync(ClientSummaryRequest request, CancellationToken cancellationToken)
        {
            if (String.IsNullOrWhiteSpace(request.IdClient))
                throw new ArgumentException("El campo 'cliente' es obligatorio.");

            var filters = new Dictionary<String, String>();
            DynamicDataSet dataSetOPENCOMEX = await _externalDataGateway.FetchDataAsync("OPENCOMEX", filters, cancellationToken);

            var summary = _reportsDomainService.Summarize(dataSetOPENCOMEX, clientId: request.IdClient, frequentRoutesCount: FrequentRoutesCount);


            return new ReportsSummaryResponse
            {
                TotalClientRecords = summary.TotalClientRecords,
                TotalWithIssuesStatus = summary.TotalWithIssuesStatus,
                TotalDeliveredStatus = summary.TotalDeliveredStatus,
                TotalDestinationCustomsStatus = summary.TotalDestinationCustomsStatus,
                TotalOriginCustomsStatus = summary.TotalOriginCustomsStatus,
                TotalInTransitStatus = summary.TotalInTransitStatus,
                TotalPendingStatus = summary.TotalPendingStatus,
                TotalInvoiced = summary.TotalInvoiced,
                TotalAdvancePayment = summary.TotalAdvancePayment,
                TotalDelays = summary.TotalDelays,
                TotalImports = summary.TotalImports,
                TotalExports = summary.TotalExports,
                TotalAirShipments = summary.TotalAirShipments,
                TotalOceanShipments = summary.TotalOceanShipments,
                FrequentRoutes = summary.FrequentRoutes.Select(x => new ReportsFrequentRoutesResponse
                {
                    Origin = x.Origin,
                    Destination = x.Destination,
                    TotalRoute = x.TotalRoute,
                }).ToList()
            };
        }
    }
}
