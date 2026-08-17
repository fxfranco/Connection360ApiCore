using Connection360.Domain.Constans;
using Connection360.Domain.Dtos;
using Connection360.Domain.Entities;
using Connection360.Domain.Interfaces;

namespace Connection360.Domain.Services
{
    public class ReportsDomainService : IReportsDomainService
    {
        public ReportsSummaryDomainDtoResult Summarize(DynamicDataSet dataSet, String clientId, Int16 frequentRoutesCount)
        {
            List<DynamicRecord> clientRecords = dataSet.Rows
                .Where(r => r[ExternalDataFields.ClientNit] == clientId)
                .ToList();

            Int64 totalClientRecords = clientRecords.Count();
            Int64 totalWithIssuesStatus = clientRecords.Count(r => r[ExternalDataFields.State] == ExternalDataValues.WithIssuesState);
            Int64 totalDeliveredStatus = clientRecords.Count(r => r[ExternalDataFields.State] == ExternalDataValues.DeliveredState);
            Int64 totalDestinationCustomsStatus = clientRecords.Count(r => r[ExternalDataFields.State] == ExternalDataValues.DestinationCustomsState);
            Int64 totalOriginCustomsStatus = clientRecords.Count(r => r[ExternalDataFields.State] == ExternalDataValues.OriginCustomsState);
            Int64 totalInTransitStatus = clientRecords.Count(r => r[ExternalDataFields.State] == ExternalDataValues.InTransitState);
            Int64 totalPendingStatus = clientRecords.Count(r => r[ExternalDataFields.State] == ExternalDataValues.PendingState);
            Double totalInvoiced = clientRecords.Select(r => r[ExternalDataFields.TotalInvoiceUSD].ToDouble()).Sum();
            Double totalAdvancePayment = clientRecords.Select(r => r[ExternalDataFields.AdvancePaymentAmount].ToDouble()).Sum();
            Double totalDelays = clientRecords.Select(r => r[ExternalDataFields.TotalCostContainerDelays].ToDouble()).Sum();
            Int64 totalImports = clientRecords.Count(r => r[ExternalDataFields.OperationType] == ExternalDataValues.Import);
            Int64 totalExports = clientRecords.Count(r => r[ExternalDataFields.OperationType] == ExternalDataValues.Export);
            Int64 totalAirShipments = clientRecords.Count(r => r[ExternalDataFields.ShipmentMode] == ExternalDataValues.AirShipment);
            Int64 totalOceanShipments = clientRecords.Count(r => r[ExternalDataFields.ShipmentMode] == ExternalDataValues.OceanShipment);

            List<ReportsFrequentRoutesDomainDtoResult> frequentRoutes = clientRecords
                .GroupBy(x => new 
                {
                    Origin = x[ExternalDataFields.Origin],
                    Destination = x[ExternalDataFields.Destination]
                })                
                .Select(g => new ReportsFrequentRoutesDomainDtoResult
                {
                    Origin = g.Key.Origin,
                    Destination = g.Key.Destination,
                    TotalRoute = g.Count()
                })
                .OrderByDescending(x => x.TotalRoute)
                .Take(frequentRoutesCount)
                .ToList();

            return new ReportsSummaryDomainDtoResult
            {
                TotalClientRecords  = totalClientRecords,
                TotalWithIssuesStatus = totalWithIssuesStatus,
                TotalDeliveredStatus = totalDeliveredStatus,
                TotalDestinationCustomsStatus = totalDestinationCustomsStatus,
                TotalOriginCustomsStatus = totalOriginCustomsStatus,
                TotalInTransitStatus = totalInTransitStatus,
                TotalPendingStatus = totalPendingStatus,
                TotalInvoiced = totalInvoiced,
                TotalAdvancePayment = totalAdvancePayment,
                TotalDelays = totalDelays,
                TotalImports  = totalImports,
                TotalExports = totalExports,
                TotalAirShipments = totalAirShipments,
                TotalOceanShipments = totalOceanShipments,
                FrequentRoutes = frequentRoutes
            };
        }
    }
}
