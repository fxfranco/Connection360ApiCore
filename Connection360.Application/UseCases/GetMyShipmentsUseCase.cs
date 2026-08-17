using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using Connection360.Domain.Constans;
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
        private readonly IExternalApiOpenStreetMap _externalApiOpenStreetMap;
        private readonly IDetailsHistoryShipmentsDomainService _detailsHistoryShipmentsDomainService;

        public GetMyShipmentsUseCase(IExternalDataGateway externalDataGateway, IMyShipmentsDomainService myShipmentsDomainService, IDynamicDataSetMerger merger, 
            IExternalApiOpenStreetMap externalApiOpenStreetMap, IDetailsHistoryShipmentsDomainService detailsHistoryShipmentsDomainService)
        {
            _externalDataGateway = externalDataGateway;
            _myShipmentsDomainService = myShipmentsDomainService;
            _merger = merger;
            _externalApiOpenStreetMap = externalApiOpenStreetMap;
            _detailsHistoryShipmentsDomainService = detailsHistoryShipmentsDomainService;
        }

        public async Task<MyShipmentsResponse> ExecuteGetAllShipmentsAsync(MyShipmentsRequest request, CancellationToken cancellationToken)
        {
            if (String.IsNullOrWhiteSpace(request.IdClient))
                throw new ArgumentException("El campo 'cliente' es obligatorio.");

            var filters = new Dictionary<String, String>();
            // 1. Consultar el API externo filtrando solo por cliente
            DynamicDataSet dataSetSIM = await _externalDataGateway.FetchDataAsync("SIM", filters, cancellationToken);

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
                    TotalWithIssues = null,
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
                    TotalWithIssues = null,
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
                    TotalWithIssues = null,
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
                    TotalWithIssues = null,
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

        public async Task<DetailsShipmentsResponse> ExecuteDetailsShipmentsAsync(MyShipmentsRequest request, CancellationToken cancellationToken)
        {
            if (String.IsNullOrWhiteSpace(request.IdClient))
                throw new ArgumentException("El campo 'cliente' es obligatorio.");

            if (String.IsNullOrWhiteSpace(request.DocumentNumber))
                throw new ArgumentException("El campo 'Documento' es obligatorio.");

            var filters = new Dictionary<String, String>();
            // 1. Consultar el API externo filtrando solo por cliente
            DynamicDataSet dataSetBPMS = await _externalDataGateway.FetchDataAsync("BPMS", filters, cancellationToken);
            DynamicDataSet dataSetSIM = await _externalDataGateway.FetchDataAsync("SIM", filters, cancellationToken);            
            DynamicDataSet dataSetOPENCOMEX = await _externalDataGateway.FetchDataAsync("OPENCOMEX", filters, cancellationToken);
            DynamicDataSet dataSetASISCOMEX = await _externalDataGateway.FetchDataAsync("ASISCOMEX", filters, cancellationToken);
            DynamicDataSet dataSetSYSTEMCARRIER = await _externalDataGateway.FetchDataAsync("SYSTEMCARRIER", filters, cancellationToken);
            DynamicDataSet dataSetDATALOGS = await _externalDataGateway.FetchDataAsync("DATALOGS", filters, cancellationToken);

            DynamicDataSet datasetUnificado = _merger.Merge(new[] { dataSetBPMS, dataSetSIM, dataSetOPENCOMEX, dataSetASISCOMEX, dataSetSYSTEMCARRIER },joinField: ExternalDataFields.DocumentNumber, joinType: DataSetJoinType.FullOuter);

            // 2. Pasar los datos al Servicio de Dominio para aplicar las consultas LINQ
            var myShipmentsDetails = _myShipmentsDomainService.GetDetailsShipments(datasetUnificado, clientId: request.IdClient, DocumentNumber: request.DocumentNumber);
            var detailsHistory = _detailsHistoryShipmentsDomainService.GetDetailsHistoryShipments(dataSetDATALOGS, DocumentNumber: request.DocumentNumber);

            String originName = myShipmentsDetails.ResumenShipments.Origin;
            String DestinationName = myShipmentsDetails.ResumenShipments.Destination;

            OpenStreetMapDto OpenStreetMapOrigin = await _externalApiOpenStreetMap.GetCoordinates("OPENSTREETMAP", originName, cancellationToken);
            OpenStreetMapDto OpenStreetMapDestination = await _externalApiOpenStreetMap.GetCoordinates("OPENSTREETMAP", DestinationName, cancellationToken);

            return new DetailsShipmentsResponse
            {
                ResumenShipments = new SummaryShipmentsResponse
                {
                    Id = myShipmentsDetails.ResumenShipments.Id,
                    ClientName = myShipmentsDetails.ResumenShipments.ClientName,
                    Supplier = myShipmentsDetails.ResumenShipments.Supplier,
                    Carrier = myShipmentsDetails.ResumenShipments.Carrier,
                    MerchandiseDescription = myShipmentsDetails.ResumenShipments.MerchandiseDescription,
                    DocumentNumber = myShipmentsDetails.ResumenShipments.DocumentNumber,
                    DocumentType = myShipmentsDetails.ResumenShipments.DocumentType,
                    Origin = myShipmentsDetails.ResumenShipments.Origin,
                    Destination = myShipmentsDetails.ResumenShipments.Destination,
                    LoadType = myShipmentsDetails.ResumenShipments.LoadType,
                    PackagesNumbers = myShipmentsDetails.ResumenShipments.PackagesNumbers,
                    WeightKg = myShipmentsDetails.ResumenShipments.WeightKg,
                    VolumeM3 = myShipmentsDetails.ResumenShipments.VolumeM3,
                    Incoterm = myShipmentsDetails.ResumenShipments.Incoterm,
                    OperationType = myShipmentsDetails.ResumenShipments.OperationType,
                    ShipmentMode = myShipmentsDetails.ResumenShipments.ShipmentMode,
                },
                TrackingShipments = new TrackingShipmentsResponse
                {
                    State = myShipmentsDetails.TrackingShipments.State,
                    OriginNameCoordinates = OpenStreetMapOrigin.PlaceName,
                    OriginLatitudCoordinates = OpenStreetMapOrigin.Latitud,
                    OriginLongitudCoordinates = OpenStreetMapOrigin.Longitud,
                    DestinationNameCoordinates = OpenStreetMapDestination.PlaceName,
                    DestinationLatitudCoordinates = OpenStreetMapDestination.Latitud,
                    DestinationLongitudCoordinates = OpenStreetMapDestination.Longitud
                },
                LogisticsDatesShipments = new LogisticsDatesShipmentsResponse
                {
                    StoreOriginDate = myShipmentsDetails.LogisticsDatesShipments.StoreDestinationDate,
                    ETDDate = myShipmentsDetails.LogisticsDatesShipments.ETDDate,
                    ATDDate = myShipmentsDetails.LogisticsDatesShipments.ATDDate,
                    ETADate = myShipmentsDetails.LogisticsDatesShipments.ETADate,
                    ATADate = myShipmentsDetails.LogisticsDatesShipments.ATADate,
                    StoreDestinationDate = myShipmentsDetails.LogisticsDatesShipments.StoreDestinationDate,
                    NationalizationDate = myShipmentsDetails.LogisticsDatesShipments.NationalizationDate,
                    DispatchDestinationDate = myShipmentsDetails.LogisticsDatesShipments.DispatchDestinationDate,
                    FormDate = myShipmentsDetails.LogisticsDatesShipments.FormDate,
                    ContainerDeliveryDate = myShipmentsDetails.LogisticsDatesShipments.ContainerDeliveryDate,
                },
                ContainerShipments = new ContainerShipmentsResponse
                {
                    ContainerType = myShipmentsDetails.ContainerShipments.ContainerType,
                    ContainerAmount = myShipmentsDetails.ContainerShipments.ContainerAmount,
                    ContainerNumber = myShipmentsDetails.ContainerShipments.ContainerNumber,
                    DaysOff = myShipmentsDetails.ContainerShipments.DaysOff,
                    DaysRemainingDelivery = myShipmentsDetails.ContainerShipments.DaysRemainingDelivery,
                    ActualContainerReturnDate = myShipmentsDetails.ContainerShipments.ActualContainerReturnDate,
                    ContainerDelayDays = myShipmentsDetails.ContainerShipments.ContainerDelayDays,
                    CostDayOfDelay = myShipmentsDetails.ContainerShipments.CostDayOfDelay,
                    TotalCostContainerDelays = myShipmentsDetails.ContainerShipments.TotalCostContainerDelays,
                    ContainerDepot = myShipmentsDetails.ContainerShipments.ContainerDepot
                },
                FinancialInfoShipments = new FinancialInfoShipmentsResponse
                {
                    AdvancePaymentRequestDate = myShipmentsDetails.FinancialInfoShipments.AdvancePaymentRequestDate,
                    AdvancePaymentDate = myShipmentsDetails.FinancialInfoShipments.AdvancePaymentDate,
                    AdvancePaymentAmount = myShipmentsDetails.FinancialInfoShipments.AdvancePaymentAmount,
                    SupplierInvoice = myShipmentsDetails.FinancialInfoShipments.SupplierInvoice,
                    TCCInvoice = myShipmentsDetails.FinancialInfoShipments.TCCInvoice,
                    InvoiceNumber = myShipmentsDetails.FinancialInfoShipments.InvoiceNumber,
                    InvoiceDate = myShipmentsDetails.FinancialInfoShipments.InvoiceDate,
                    ExpenseDescription = myShipmentsDetails.FinancialInfoShipments.ExpenseDescription,
                    ExpenseAmountUSD = myShipmentsDetails.FinancialInfoShipments.ExpenseAmountUSD,
                    InvoiceSubtotalUSD = myShipmentsDetails.FinancialInfoShipments.InvoiceSubtotalUSD,
                    IvaUSD = myShipmentsDetails.FinancialInfoShipments.IvaUSD,
                    TotalInvoiceUSD = myShipmentsDetails.FinancialInfoShipments.TotalInvoiceUSD,
                },
                HistoryShipments = new HistoryShipmentsResponse
                {

                    DetailsHistoryShipments = detailsHistory.DetailsHistoryShipments.Select(x => new DetailsHistoryShipmentsResponse
                    {
                        ChangeDate = x.ChangeDate,
                        ChangeUser= x.ChangeUser,
                        Message= x.Message,
                        OldState = x.OldState,
                        NewState= x.NewState
                    })
                    .ToList(),
                }
            };
        }
    }
}
