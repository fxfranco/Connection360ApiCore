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
                .Where(r => r[ExternalDataFields.Cliente] == clientId)
                .ToList();

            Int64 totalClientRecords = clientRecords.Count();
            Int64 totalImports = clientRecords.Count(r => r[ExternalDataFields.TipoOperacion] == TipoOperacionValues.Importacion);
            Int64 totalExports = clientRecords.Count(r => r[ExternalDataFields.TipoOperacion] == TipoOperacionValues.Exportacion);
            Int64 totalAirShipments = clientRecords.Count(r => r[ExternalDataFields.TipoModalidad] == TipoOperacionValues.ModalidadAire);
            Int64 totalOceanShipments = clientRecords.Count(r => r[ExternalDataFields.TipoModalidad] == TipoOperacionValues.ModalidadMaritima);
            Int64 totalWithIssues = clientRecords.Count(r => r[ExternalDataFields.Estado] == TipoOperacionValues.EstadoConNovedad);


            List<ResumenClienteDto> recentShipments = clientRecords
                .OrderByDescending(r => r["FECHA DE CREACIÓN"].ToDateTimeOrMin())
                .Select(r => new ResumenClienteDto
                {
                    Id = Int64.TryParse(r["ID"], out Int64 id) ? id : 0,
                    DocumentNumber = r["DOCUMENTO DE TRANSPORTE (HBL)"],
                    Origin = r["ORIGEN"],
                    Destination = r["DESTINO"],
                    Status = r["ESTADO"],
                    OperationType = r["TIPO DE OPERACIÓN (IMPO - EXPO)"],
                    ShipmentMode = r["MODALIDAD (AIR - SEA)"]
                })
                .Take(10)
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
            //        r[ExternalDataFields.Cliente] == clientId &&
            //        r["DOCUMENTO DE TRANSPORTE (HBL)"].GetDocumentoSinPrefijo().Equals(filterDocument, StringComparison.OrdinalIgnoreCase)
            //    )
            //    .ToList();

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            ResumenClienteDto shipment = dataSet.Rows
                .Where(r =>
                    r[ExternalDataFields.Cliente] == clientId &&
                    r["DOCUMENTO DE TRANSPORTE (HBL)"].GetDocumentoSinPrefijo().Equals(filterDocument, StringComparison.OrdinalIgnoreCase)
                )
                .Select(r => new ResumenClienteDto
                {
                    Id = Int64.TryParse(r["ID"], out Int64 id) ? id : 0,
                    DocumentNumber = r["DOCUMENTO DE TRANSPORTE (HBL)"],
                    Origin = r["ORIGEN"],
                    Destination = r["DESTINO"],
                    Status = r["ESTADO"],
                    OperationType = r["TIPO DE OPERACIÓN (IMPO - EXPO)"],
                    ShipmentMode = r["MODALIDAD (AIR - SEA)"]
                })
                .FirstOrDefault();
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            return shipment == null ? new ResumenClienteDto() : shipment;
        }
    }
}
