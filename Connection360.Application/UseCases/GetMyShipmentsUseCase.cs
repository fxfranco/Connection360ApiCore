using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using Connection360.Domain.Dtos;
using Connection360.Domain.Entities;
using Connection360.Domain.Enum;
using Connection360.Domain.Interfaces;

namespace Connection360.Application.UseCases
{
    public class GetMyShipmentsUseCase : IGetMyShipmentsUseCase
    {
        private readonly IExternalDataGateway _externalDataGateway;
        private readonly IMyShipmentsDomainService _myShipmentsDomainService;
        private readonly IDynamicDataSetMerger _merger;

        public GetMyShipmentsUseCase(IExternalDataGateway externalDataGateway, IMyShipmentsDomainService myShipmentsDomainService, IDynamicDataSetMerger merger)
        {
            _externalDataGateway = externalDataGateway;
            _myShipmentsDomainService = myShipmentsDomainService;
            _merger = merger;
        }

        public async Task<MyShipmentsResponse> ExecuteGetAllShipmentsAsync(MyShipmentsRequest request, CancellationToken cancellationToken)
        {
            if (String.IsNullOrWhiteSpace(request.IdClient))
                throw new ArgumentException("El campo 'cliente' es obligatorio.");

            var filters = new Dictionary<String, String>();
            // 1. Consultar el API externo filtrando solo por cliente
            DynamicDataSet dataSetSIM = await _externalDataGateway.FetchDataAsync("SIM", filters, cancellationToken);
            //DynamicDataSet dataSetBPMS = await _externalDataGateway.FetchDataAsync("BPMS", filters, cancellationToken);
            //DynamicDataSet dataSetOPENCOMEX = await _externalDataGateway.FetchDataAsync("OPENCOMEX", filters, cancellationToken);
            //DynamicDataSet dataSetASISCOMEX = await _externalDataGateway.FetchDataAsync("ASISCOMEX", filters, cancellationToken);
            //DynamicDataSet dataSetSYSTEMCARRIER = await _externalDataGateway.FetchDataAsync("SYSTEMCARRIER", filters, cancellationToken);

            //var datasetUnificado = _merger.Merge(new[] { dataSetBPMS, dataSetSIM, dataSetOPENCOMEX, dataSetASISCOMEX, dataSetSYSTEMCARRIER },joinField: "DOCUMENTO DE TRANSPORTE (HBL)", joinType: DataSetJoinType.FullOuter);

            // 2. Pasar los datos al Servicio de Dominio para aplicar las consultas LINQ
            var myShipments = _myShipmentsDomainService.GetAllShipments(dataSetSIM, clientId: request.IdClient, page: request.Page, size: request.Size);


            // 3. Mapear a respuesta de aplicación
            return new MyShipmentsResponse
            {
                ClientSummaryResponseData = new ClientSummaryResponse
                {
                    TotalClientRecords = myShipments.ClientSummaryResponse.TotalClientRecords,
                    TotalImports = myShipments.ClientSummaryResponse.TotalImports,
                    TotalExports = myShipments.ClientSummaryResponse.TotalExports,
                    TotalAirShipments = myShipments.ClientSummaryResponse.TotalAirShipments,
                    TotalOceanShipments = myShipments.ClientSummaryResponse.TotalOceanShipments,
                    MyShipments = myShipments.MyShipments.Select(x => new ResumeMyShipmentsResponse
                    {
                        Id = x.Id,
                        ShipmentMode = x.ShipmentMode,
                        DocumentNumber = x.DocumentNumber,
                        State = x.State,
                        OperationType = x.OperationType,
                        ClientName = x.ClientName,
                        Origin = x.Origin,
                        Destination = x.Destination,
                        ETDDate = x.ETDDate,
                        ATDDate = x.ATDDate,
                        ETADate = x.ETADate,
                        ATADate = x.ATADate
                    })
                    .ToList(),
                }
            };
        }

        public async Task<MyShipmentsResponse> ExecuteFilterShipmentsAsync(MyShipmentsRequest request, CancellationToken cancellationToken)
        {
            if (String.IsNullOrWhiteSpace(request.IdClient))
                throw new ArgumentException("El campo 'cliente' es obligatorio.");

            var filters = new Dictionary<String, String>();
            // 1. Consultar el API externo filtrando solo por cliente
            DynamicDataSet dataSetSIM = await _externalDataGateway.FetchDataAsync("SIM", filters, cancellationToken);

            MyShipmentsFiltersDto myShipmentsFiltersDto = new MyShipmentsFiltersDto
            {
                ValueFilter = request.Filters.ValueFilter,
                OperationType = request.Filters.OperationType,
                ShipmentMode = request.Filters.ShipmentMode,
                State = request.Filters.State,
            };

            // 2. Pasar los datos al Servicio de Dominio para aplicar las consultas LINQ
            var myShipments = _myShipmentsDomainService.GetFiltersShipments(dataSetSIM, clientId: request.IdClient, page: request.Page, size: request.Size, filters: myShipmentsFiltersDto);

            // 3. Mapear a respuesta de aplicación
            return new MyShipmentsResponse
            {
                ClientSummaryResponseData = new ClientSummaryResponse
                {
                    TotalClientRecords = myShipments.ClientSummaryResponse.TotalClientRecords,
                    TotalImports = myShipments.ClientSummaryResponse.TotalImports,
                    TotalExports = myShipments.ClientSummaryResponse.TotalExports,
                    TotalAirShipments = myShipments.ClientSummaryResponse.TotalAirShipments,
                    TotalOceanShipments = myShipments.ClientSummaryResponse.TotalOceanShipments,
                    MyShipments = myShipments.MyShipments.Select(x => new ResumeMyShipmentsResponse
                    {
                        Id = x.Id,
                        ShipmentMode = x.ShipmentMode,
                        DocumentNumber = x.DocumentNumber,
                        State = x.State,
                        OperationType = x.OperationType,
                        ClientName = x.ClientName,
                        Origin = x.Origin,
                        Destination = x.Destination,
                        ETDDate = x.ETDDate,
                        ATDDate = x.ATDDate,
                        ETADate = x.ETADate,
                        ATADate = x.ATADate
                    })
                    .ToList(),
                }
            };
        }

        public async Task<MyShipmentsResponse> ExecuteGetHistoryAllShipmentsAsync(MyShipmentsRequest request, CancellationToken cancellationToken)
        {
            if (String.IsNullOrWhiteSpace(request.IdClient))
                throw new ArgumentException("El campo 'cliente' es obligatorio.");

            var filters = new Dictionary<String, String>();
            // 1. Consultar el API externo filtrando solo por cliente
            DynamicDataSet dataSetSIM = await _externalDataGateway.FetchDataAsync("SIM", filters, cancellationToken);
            //DynamicDataSet dataSetBPMS = await _externalDataGateway.FetchDataAsync("BPMS", filters, cancellationToken);

            // 2. Pasar los datos al Servicio de Dominio para aplicar las consultas LINQ
            var myShipments = _myShipmentsDomainService.GetHistoryAllShipments(dataSetSIM, clientId: request.IdClient, page: request.Page, size: request.Size);


            // 3. Mapear a respuesta de aplicación
            return new MyShipmentsResponse
            {
                ClientSummaryResponseData = new ClientSummaryResponse
                {
                    TotalClientRecords = myShipments.ClientSummaryResponse.TotalClientRecords,
                    TotalImports = myShipments.ClientSummaryResponse.TotalImports,
                    TotalExports = myShipments.ClientSummaryResponse.TotalExports,
                    TotalAirShipments = myShipments.ClientSummaryResponse.TotalAirShipments,
                    TotalOceanShipments = myShipments.ClientSummaryResponse.TotalOceanShipments,
                    MyShipments = myShipments.MyShipments.Select(x => new ResumeMyShipmentsResponse
                    {
                        Id = x.Id,
                        ShipmentMode = x.ShipmentMode,
                        DocumentNumber = x.DocumentNumber,
                        State = x.State,
                        OperationType = x.OperationType,
                        ClientName = x.ClientName,
                        Origin = x.Origin,
                        Destination = x.Destination,
                        ETDDate = x.ETDDate,
                        ATDDate = x.ATDDate,
                        ETADate = x.ETADate,
                        ATADate = x.ATADate
                    })
                    .ToList(),
                }
            };
        }

        public async Task<MyShipmentsResponse> ExecuteFilterHistoryShipmentsAsync(MyShipmentsRequest request, CancellationToken cancellationToken)
        {
            if (String.IsNullOrWhiteSpace(request.IdClient))
                throw new ArgumentException("El campo 'cliente' es obligatorio.");

            var filters = new Dictionary<String, String>();
            // 1. Consultar el API externo filtrando solo por cliente
            DynamicDataSet dataSetSIM = await _externalDataGateway.FetchDataAsync("SIM", filters, cancellationToken);

            MyShipmentsFiltersDto myShipmentsFiltersDto = new MyShipmentsFiltersDto
            {
                ValueFilter = request.Filters.ValueFilter,
                OperationType = request.Filters.OperationType,
                ShipmentMode = request.Filters.ShipmentMode,
                State = request.Filters.State,
            };

            // 2. Pasar los datos al Servicio de Dominio para aplicar las consultas LINQ
            var myShipments = _myShipmentsDomainService.GetFiltersHistoryShipments(dataSetSIM, clientId: request.IdClient, page: request.Page, size: request.Size, filters: myShipmentsFiltersDto);

            // 3. Mapear a respuesta de aplicación
            return new MyShipmentsResponse
            {
                ClientSummaryResponseData = new ClientSummaryResponse
                {
                    TotalClientRecords = myShipments.ClientSummaryResponse.TotalClientRecords,
                    TotalImports = myShipments.ClientSummaryResponse.TotalImports,
                    TotalExports = myShipments.ClientSummaryResponse.TotalExports,
                    TotalAirShipments = myShipments.ClientSummaryResponse.TotalAirShipments,
                    TotalOceanShipments = myShipments.ClientSummaryResponse.TotalOceanShipments,
                    MyShipments = myShipments.MyShipments.Select(x => new ResumeMyShipmentsResponse
                    {
                        Id = x.Id,
                        ShipmentMode = x.ShipmentMode,
                        DocumentNumber = x.DocumentNumber,
                        State = x.State,
                        OperationType = x.OperationType,
                        ClientName = x.ClientName,
                        Origin = x.Origin,
                        Destination = x.Destination,
                        ETDDate = x.ETDDate,
                        ATDDate = x.ATDDate,
                        ETADate = x.ETADate,
                        ATADate = x.ATADate
                    })
                    .ToList(),
                }
            };
        }
    }
}
