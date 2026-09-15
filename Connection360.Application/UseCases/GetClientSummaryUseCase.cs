using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using Connection360.Domain.Dtos;
using Connection360.Domain.Entities;
using Connection360.Domain.Enums;
using Connection360.Domain.Interfaces;
using System.Data;

namespace Connection360.Application.UseCases
{
    public class GetClientSummaryUseCase : IGetClientSummaryUseCase
    {
        private const Int16 UltimosRegistrosCount = 10;

        private readonly IExternalDataGateway _externalDataGateway;
        private readonly IClientSummaryDomainService _summaryyDomainService;
        private readonly IClientAccessResolver _clientAccessResolver;

        public GetClientSummaryUseCase(IExternalDataGateway externalDataGateway, IClientSummaryDomainService summaryService, IClientAccessResolver clientAccessResolver)
        {
            _externalDataGateway = externalDataGateway;
            _summaryyDomainService = summaryService;
            _clientAccessResolver = clientAccessResolver;
        }

        public async Task<ClientSummaryResponse> ExecuteTotalsAsync(ClientSummaryRequest request, CancellationToken cancellationToken)
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

            // 1. Aplica filtros a la api si se tienen
            //var filters = new Dictionary<String, String>
            //{
            //    [ExternalDataFields.ClientNit] = request.Cliente
            //};

            var filters = new Dictionary<String, String>();
            // 2. Consultar el API externo filtrando solo por cliente
            DynamicDataSet dataSet = await _externalDataGateway.FetchDataAsync("BPMS", filters, cancellationToken);

            // 2. Pasar los datos al Servicio de Dominio para aplicar las consultas LINQ
            var summary = _summaryyDomainService.Summarize(dataSet, clientId: request.IdClient, customersByCollaborators, lastRecordsCount: UltimosRegistrosCount);

            // 3. Mapear a respuesta de aplicación
            return new ClientSummaryResponse
            {
                TotalClientRecords = summary.TotalClientRecords,
                TotalImports = summary.TotalImports,
                TotalExports = summary.TotalExports,
                TotalAirShipments = summary.TotalAirShipments,
                TotalOceanShipments = summary.TotalOceanShipments,
                TotalWithIssues = summary.TotalWithIssues,
                RecentShipments = summary.RecentShipments.Select(x => new ResumenClienteResponse
                {
                    Id = x.Id,
                    ClientNit = x.ClientNit,
                    DocumentNumber = x.DocumentNumber,
                    Origin = x.Origin,
                    Destination = x.Destination,
                    Status = x.Status,
                    OperationType = x.OperationType,
                    ShipmentMode = x.ShipmentMode
                }).ToList()
            };
        }

        public async Task<ResumenClienteResponse> ExecuteFilterAsync(ClientSummaryRequest request, CancellationToken cancellationToken)
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

            // 1. Aplica filtros a la api si se tienen
            //var filters = new Dictionary<String, String>
            //{
            //    [ExternalDataFields.ClientNit] = request.Cliente
            //};

            var filters = new Dictionary<String, String>();
            // 2. Consultar el API externo filtrando solo por cliente
            DynamicDataSet dataSet = await _externalDataGateway.FetchDataAsync("BPMS", filters, cancellationToken);

            // 2. Pasar los datos al Servicio de Dominio para aplicar las consultas LINQ
            var summary = _summaryyDomainService.Filter(dataSet, clientId: request.IdClient, customersByCollaborators, filterDocument: request.FilterValue);

            // 3. Mapear a respuesta de aplicación
            ResumenClienteResponse resumenClienteResponse = new ResumenClienteResponse
            {
                Id = summary.Id,
                ClientNit = summary.ClientNit,
                DocumentNumber = summary.DocumentNumber,
                Origin = summary.Origin,
                Destination = summary.Destination,
                Status = summary.Status,
                OperationType = summary.OperationType,
                ShipmentMode = summary.ShipmentMode
            };
            return resumenClienteResponse;
        }
    }
}
