using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using Connection360.Domain.Entities;
using Connection360.Domain.Interfaces;
using System.Data;

namespace Connection360.Application.UseCases
{
    public class GetClientSummaryUseCase : IGetClientSummaryUseCase
    {
        private const Int16 UltimosRegistrosCount = 10;

        private readonly IExternalDataGateway _externalDataGateway;
        private readonly IClientSummaryDomainService _summaryService;

        public GetClientSummaryUseCase(IExternalDataGateway externalDataGateway, IClientSummaryDomainService summaryService)
        {
            _externalDataGateway = externalDataGateway;
            _summaryService = summaryService;
        }

        public async Task<ClientSummaryResponse> ExecuteAsync(ClientSummaryRequest request, CancellationToken cancellationToken)
        {
            if (String.IsNullOrWhiteSpace(request.IdClient))
                throw new ArgumentException("El campo 'cliente' es obligatorio.");

            // 1. Aplica filtros a la api si se tienen
            //var filters = new Dictionary<String, String>
            //{
            //    [ExternalDataFields.Cliente] = request.Cliente
            //};

            var filters = new Dictionary<String, String>();
            // 2. Consultar el API externo filtrando solo por cliente
            DynamicDataSet dataSet = await _externalDataGateway.FetchDataAsync(filters, cancellationToken);

            // 2. Pasar los datos al Servicio de Dominio para aplicar las consultas LINQ
            var summary = _summaryService.Summarize(dataSet, clientId: request.IdClient, lastRecordsCount: UltimosRegistrosCount);

            // 3. Mapear a respuesta de aplicación
            return new ClientSummaryResponse
            {
                TotalRegistros = summary.TotalRegistros,
                TotalImportaciones = summary.TotalImportaciones,
                TotalExportaciones = summary.TotalExportaciones,
                TotalModalidadAerea = summary.TotalModalidadAerea,
                TotalModalidadMaritima = summary.TotalModalidadMaritima,
                TotalConNovedad = summary.TotalConNovedad,
                EnviosRecientes = summary.EnviosRecientes.Select(x => new ResumenClienteResponse
                {
                    Id = x.Id,
                    NroDocumento = x.NroDocumento,
                    Origen = x.Origen,
                    Destino = x.Destino,
                    Estado = x.Estado,
                    TipoOperacion = x.TipoOperacion,
                    Modalidad = x.Modalidad

                }).ToList()
            };
        }

        public async Task<ResumenClienteResponse> ExecuteFilterAsync(ClientSummaryRequest request, CancellationToken cancellationToken)
        {
            if (String.IsNullOrWhiteSpace(request.IdClient))
                throw new ArgumentException("El campo 'cliente' es obligatorio.");

            // 1. Aplica filtros a la api si se tienen
            //var filters = new Dictionary<String, String>
            //{
            //    [ExternalDataFields.Cliente] = request.Cliente
            //};

            var filters = new Dictionary<String, String>();
            // 2. Consultar el API externo filtrando solo por cliente
            DynamicDataSet dataSet = await _externalDataGateway.FetchDataAsync(filters, cancellationToken);

            // 2. Pasar los datos al Servicio de Dominio para aplicar las consultas LINQ
            var summary = _summaryService.Filter(dataSet, clientId: request.IdClient, filterDocument: request.FilterValue);

            // 3. Mapear a respuesta de aplicación
            ResumenClienteResponse resumenClienteResponse = new ResumenClienteResponse
            {
                Id = summary.Id,
                NroDocumento = summary.NroDocumento,
                Origen = summary.Origen,
                Destino = summary.Destino,
                Estado = summary.Estado,
                TipoOperacion = summary.TipoOperacion,
                Modalidad = summary.Modalidad
            };
            return resumenClienteResponse;   
        }
    }
}
