using Connection360.Domain.Constans;
using Connection360.Domain.Entities;
using Connection360.Domain.Interfaces;
using System.Data;

namespace Connection360.Domain.Services
{
    public class ClientSummaryDomainService : IClientSummaryDomainService
    {
        public ClientSummaryDomainResult Summarize(DynamicDataSet dataSet, String clientId, Int16 lastRecordsCount)
        {
            // Crea una nueva List<DynamicRecord> con solo los registros activos
            List<DynamicRecord> clientRecords = dataSet.Rows
                .Where(r => r[ExternalDataFields.ClientNit] == clientId)
                .ToList();

            Int64 totalClientRecords = clientRecords.Count();
            Int64 totalImports = clientRecords.Count(r => r[ExternalDataFields.OperationType] == ExternalDataValues.Import);
            Int64 totalExports = clientRecords.Count(r => r[ExternalDataFields.OperationType] == ExternalDataValues.Export);
            Int64 totalAirShipments = clientRecords.Count(r => r[ExternalDataFields.ShipmentMode] == ExternalDataValues.AirShipment);
            Int64 totalOceanShipments = clientRecords.Count(r => r[ExternalDataFields.ShipmentMode] == ExternalDataValues.OceanShipment);
            Int64 totalWithIssues = clientRecords.Count(r => r[ExternalDataFields.State] == ExternalDataValues.WithIssuesState);

            /// ToDo: Pendiente corregir ordenamiento de fecha
            List<ResumenClienteDto> recentShipments = clientRecords
                .OrderByDescending(r => r[ExternalDataFields.CreationDate].ToDateTimeOrMin())
                .Select(r => new ResumenClienteDto
                {
                    Id = Int64.TryParse(r[ExternalDataFields.ID], out Int64 id) ? id : 0,
                    DocumentNumber = r[ExternalDataFields.DocumentNumber],
                    Origin = r[ExternalDataFields.Origin],
                    Destination = r[ExternalDataFields.Destination],
                    Status = r[ExternalDataFields.State],
                    OperationType = r[ExternalDataFields.OperationType],
                    ShipmentMode = r[ExternalDataFields.ShipmentMode]
                })
                .Take(lastRecordsCount)
                .ToList();

            return new ClientSummaryDomainResult
            {
                TotalClientRecords = totalClientRecords,
                TotalImports = totalImports,
                TotalExports = totalExports,
                TotalAirShipments = totalAirShipments,
                TotalOceanShipments = totalOceanShipments,
                TotalWithIssues = totalWithIssues,
                RecentShipments = recentShipments
            };
        }
        public ResumenClienteDto Filter(DynamicDataSet dataSet, String clientId, String filterDocument)
        {
            //List<DynamicRecord> searchResults = dataSet.Rows
            //    .Where(r =>
            //        r[ExternalDataFields.ClientNit] == clientId &&
            //        r[ExternalDataFields.DocumentNumber].GetDocumentoSinPrefijo().Equals(filterDocument, StringComparison.OrdinalIgnoreCase)
            //    )
            //    .ToList();

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            ResumenClienteDto shipment = dataSet.Rows
                .Where(r =>
                    r[ExternalDataFields.ClientNit] == clientId && r[ExternalDataFields.DocumentNumber].Equals(filterDocument, StringComparison.OrdinalIgnoreCase)
                )
                .Select(r => new ResumenClienteDto
                {
                    Id = Int64.TryParse(r[ExternalDataFields.ID], out Int64 id) ? id : 0,
                    DocumentNumber = r[ExternalDataFields.DocumentNumber],
                    Origin = r[ExternalDataFields.Origin],
                    Destination = r[ExternalDataFields.Destination],
                    Status = r[ExternalDataFields.State],
                    OperationType = r[ExternalDataFields.OperationType],
                    ShipmentMode = r[ExternalDataFields.ShipmentMode]
                })
                .FirstOrDefault();
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            return shipment == null ? new ResumenClienteDto() : shipment;
        }
    }
}
