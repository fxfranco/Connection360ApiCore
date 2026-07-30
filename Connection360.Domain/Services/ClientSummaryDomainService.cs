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
            List<DynamicRecord> registrosCliente = dataSet.Rows
                .Where(r => r[ExternalDataFields.Cliente] == clientId)
                .ToList();

            Int64 totalRegistrosCliente = registrosCliente.Count();
            Int64 totalImportaciones = registrosCliente.Count(r => r[ExternalDataFields.TipoOperacion] == TipoOperacionValues.Importacion);
            Int64 totalExportaciones = registrosCliente.Count(r => r[ExternalDataFields.TipoOperacion] == TipoOperacionValues.Exportacion);
            Int64 totalModalidadAire = registrosCliente.Count(r => r[ExternalDataFields.TipoModalidad] == TipoOperacionValues.ModalidadAire);
            Int64 totalModalidadMaritima = registrosCliente.Count(r => r[ExternalDataFields.TipoModalidad] == TipoOperacionValues.ModalidadMaritima);
            Int64 totalConNovedad = registrosCliente.Count(r => r[ExternalDataFields.Estado] == TipoOperacionValues.EstadoConNovedad);


            List<ResumenClienteDto> enviostRecientes = registrosCliente
                .OrderByDescending(r => r["FECHA DE CREACIÓN"].ToDateTimeOrMin())
                .Select(r => new ResumenClienteDto
                {
                    Id = Int64.TryParse(r["ID"], out Int64 id) ? id : 0,
                    NroDocumento = r["DOCUMENTO DE TRANSPORTE (HBL)"],
                    Origen = r["ORIGEN"],
                    Destino = r["DESTINO"],
                    Estado = r["ESTADO"],
                    TipoOperacion = r["TIPO DE OPERACIÓN (IMPO - EXPO)"],
                    Modalidad = r["MODALIDAD (AIR - SEA)"]
                })
                .Take(10)
                .ToList();

            return new ClientSummaryDomainResult
            {
                TotalRegistros = totalRegistrosCliente,
                TotalImportaciones = totalImportaciones,
                TotalExportaciones = totalExportaciones,
                TotalModalidadAerea = totalModalidadAire,
                TotalModalidadMaritima = totalModalidadMaritima,
                TotalConNovedad = totalConNovedad,
                EnviosRecientes = enviostRecientes
            };
        }
        public ResumenClienteDto Filter(DynamicDataSet dataSet, String clientId, String filterDocument)
        {
            List<DynamicRecord> resultados = dataSet.Rows
                .Where(r =>
                    r[ExternalDataFields.Cliente] == clientId &&
                    r["DOCUMENTO DE TRANSPORTE (HBL)"].GetDocumentoSinPrefijo().Equals(filterDocument, StringComparison.OrdinalIgnoreCase)
                )
                .ToList();

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            ResumenClienteDto envio = dataSet.Rows
                .Where(r =>
                    r[ExternalDataFields.Cliente] == clientId &&
                    r["DOCUMENTO DE TRANSPORTE (HBL)"].GetDocumentoSinPrefijo().Equals(filterDocument, StringComparison.OrdinalIgnoreCase)
                )
                .Select(r => new ResumenClienteDto
                {
                    Id = Int64.TryParse(r["ID"], out Int64 id) ? id : 0,
                    NroDocumento = r["DOCUMENTO DE TRANSPORTE (HBL)"],
                    Origen = r["ORIGEN"],
                    Destino = r["DESTINO"],
                    Estado = r["ESTADO"],
                    TipoOperacion = r["TIPO DE OPERACIÓN (IMPO - EXPO)"],
                    Modalidad = r["MODALIDAD (AIR - SEA)"]
                })
                .FirstOrDefault();
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            return envio == null ? new ResumenClienteDto() : envio;
        }
    }
}
