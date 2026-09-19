using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using Connection360.Domain.Dtos;
using Connection360.Domain.Entities;
using Connection360.Domain.Enums;
using Connection360.Domain.Interfaces;
using System.Data;

namespace Connection360.Application.UseCases
{
    public class GetReportsUseCase : IGetReportsUseCase
    {
        private const Int16 FrequentRoutesCount = 5;
        private readonly IExternalDataGateway _externalDataGateway;
        private readonly IReportsDomainService _reportsDomainService;
        private readonly IClientAccessResolver _clientAccessResolver;

        public GetReportsUseCase(IExternalDataGateway externalDataGateway, IReportsDomainService reportsDomainService, IClientAccessResolver clientAccessResolver)
        {
            _externalDataGateway = externalDataGateway;
            _reportsDomainService = reportsDomainService;
            _clientAccessResolver = clientAccessResolver;
        }

        public async Task<List<ReportsSummaryResponse>> ExecuteGetReportsTotalsAsync(ClientSummaryRequest request, CancellationToken cancellationToken)
        {
            ResolveClientAccessRequest resolveRequest = new ResolveClientAccessRequest
            {
                IdClient = request.IdClient,
                RoleName = request.RoleName,
                AllClient = request.AllClient,
                IdQueryClient = request.IdQueryClient
            };

            List<CustomersOfCollaboratorDtoResult> customersByCollaborators = await _clientAccessResolver.ResolveAsync(resolveRequest);
            request.IdClient = (request.RoleName == UserRoleApplication.ADMIN.ToString()) ? String.Empty : request.IdClient;

            var filters = new Dictionary<String, String>();
            DynamicDataSet dataSetOPENCOMEX = await _externalDataGateway.FetchDataAsync("OPENCOMEX", filters, cancellationToken);

            var summary = _reportsDomainService.Summarize(dataSetOPENCOMEX, clientId: request.IdClient, frequentRoutesCount: FrequentRoutesCount, customersOfCollaborator: customersByCollaborators);

            return summary.Select(reports => new ReportsSummaryResponse
            {
                ClientNit = reports.ClientNit,
                ClientName = reports.ClientName,
                TotalClientRecords = reports.TotalClientRecords,
                TotalWithIssuesStatus = reports.TotalWithIssuesStatus,
                TotalDeliveredStatus = reports.TotalDeliveredStatus,
                TotalDestinationCustomsStatus = reports.TotalDestinationCustomsStatus,
                TotalOriginCustomsStatus = reports.TotalOriginCustomsStatus,
                TotalInTransitStatus = reports.TotalInTransitStatus,
                TotalPendingStatus = reports.TotalPendingStatus,
                TotalInvoiced = reports.TotalInvoiced,
                TotalAdvancePayment = reports.TotalAdvancePayment,
                TotalDelays = reports.TotalDelays,
                TotalImports = reports.TotalImports,
                TotalExports = reports.TotalExports,
                TotalAirShipments = reports.TotalAirShipments,
                TotalOceanShipments = reports.TotalOceanShipments,
                FrequentRoutes = reports.FrequentRoutes.Select(x => new ReportsFrequentRoutesResponse
                {
                    Origin = x.Origin,
                    Destination = x.Destination,
                    TotalRoute = x.TotalRoute,
                }).ToList()
            }).ToList();
        }
    }
}
