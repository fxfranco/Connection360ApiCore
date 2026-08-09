using Connection360.Domain.Constans;
using Connection360.Domain.Dtos;
using Connection360.Domain.Entities;
using Connection360.Domain.Interfaces;

namespace Connection360.Domain.Services
{
    public class MyShipmentsDomainService : IMyShipmentsDomainService
    {
        public MyShipmentsDomainResult GetAllShipments(DynamicDataSet dataSet, String clientId, Int64 page, Int64 size)
        {
            // Crea una nueva List<DynamicRecord> con solo los registros activos
            List<DynamicRecord> clientRecords = dataSet.Rows
                .Where(r => r[ExternalDataFields.ClientNit] == clientId && r[ExternalDataFields.State] != ExternalDataValues.DeliveredState)
                .ToList();

            Int64 totalClientRecords = clientRecords.Count();
            Int64 totalImports = clientRecords.Count(r => r[ExternalDataFields.OperationType] == ExternalDataValues.Import);
            Int64 totalExports = clientRecords.Count(r => r[ExternalDataFields.OperationType] == ExternalDataValues.Export);
            Int64 totalAirShipments = clientRecords.Count(r => r[ExternalDataFields.ShipmentMode] == ExternalDataValues.AirShipment);
            Int64 totalOceanShipments = clientRecords.Count(r => r[ExternalDataFields.ShipmentMode] == ExternalDataValues.OceanShipment);

            /// ToDo: Pendiente corregir ordenamiento de fecha
            // 3. Aplicar ordenamiento (REQUISITO FUNDAMENTAL antes de Skip/Take)
            // y traer solo los registros de la página solicitada
            List<ResumenMyShipmentDto> resumenMyShipmentDto = clientRecords
                .OrderByDescending(s => s[ExternalDataFields.CreationDate].ToDateTimeOrMin()) // Ajusta por tu campo de ordenamiento
                .OrderBy(s => Int64.TryParse(s[ExternalDataFields.ID], out Int64 id) ? id : 0)
                .Skip((Int32)((page - 1) * size))
                .Take((Int32)size)
                .Select(s => new ResumenMyShipmentDto // Mapeo a tu DTO final
                {
                    Id = Int64.TryParse(s[ExternalDataFields.ID], out Int64 id) ? id : 0,
                    ShipmentMode = s[ExternalDataFields.ShipmentMode],
                    DocumentNumber = s[ExternalDataFields.DocumentNumber],
                    State = s[ExternalDataFields.State],
                    OperationType = s[ExternalDataFields.OperationType],
                    ClientName = s[ExternalDataFields.ClientName],
                    Origin = s[ExternalDataFields.Origin],
                    Destination = s[ExternalDataFields.Destination],
                    ETDDate = s[ExternalDataFields.ETDDate].ToDateTimeOrMin(),
                    ATDDate = s[ExternalDataFields.ATDDate].ToDateTimeOrMin(),
                    ETADate = s[ExternalDataFields.ETADate].ToDateTimeOrMin(),
                    ATADate = s[ExternalDataFields.ATADate].ToDateTimeOrMin(),
                })
                .ToList();

            return new MyShipmentsDomainResult
            {
                MyShipments = resumenMyShipmentDto,
                ClientSummaryResponse = new ClientSummaryDomainResult
                {
                    TotalClientRecords = totalClientRecords,
                    TotalImports = totalImports,
                    TotalExports = totalExports,
                    TotalAirShipments = totalAirShipments,
                    TotalOceanShipments = totalOceanShipments
                }
            };
        }

        public MyShipmentsDomainResult GetFiltersShipments(DynamicDataSet dataSet, String clientId, Int64 page, Int64 size, MyShipmentsFiltersDto filters)
        {
            // Crea una nueva List<DynamicRecord> con solo los registros activos
            List<DynamicRecord> clientRecords = dataSet.Rows
                .Where(r => r[ExternalDataFields.ClientNit] == clientId && r[ExternalDataFields.State] != ExternalDataValues.DeliveredState)
                .ToList();

            // 1. Convertir la lista a IEnumerable para aplicar LINQ en memoria
            IEnumerable<DynamicRecord> queryFilters = clientRecords;

            // 2. Encadenar los .Where() de forma condicional
            if (!String.IsNullOrWhiteSpace(filters.ValueFilter))
            {
                // 2. Filtramos la lista buscando si coincide con CUALQUIERA (OR / ||) de los campos
                queryFilters = queryFilters.Where(s =>
                    (s[ExternalDataFields.DocumentNumber] != null && s[ExternalDataFields.DocumentNumber].Contains(filters.ValueFilter, StringComparison.OrdinalIgnoreCase)) ||
                    (s[ExternalDataFields.ClientName] != null && s[ExternalDataFields.ClientName].Contains(filters.ValueFilter, StringComparison.OrdinalIgnoreCase)) ||
                    (s[ExternalDataFields.Origin] != null && s[ExternalDataFields.Origin].Contains(filters.ValueFilter, StringComparison.OrdinalIgnoreCase)) ||
                    (s[ExternalDataFields.Destination] != null && s[ExternalDataFields.Destination].Contains(filters.ValueFilter, StringComparison.OrdinalIgnoreCase))
                );
            }

            if (!String.IsNullOrWhiteSpace(filters.OperationType))
            {
                queryFilters = queryFilters.Where(s => !String.IsNullOrEmpty(s[ExternalDataFields.OperationType]) &&
                                         s[ExternalDataFields.OperationType] == filters.OperationType);
            }

            if (!String.IsNullOrWhiteSpace(filters.ShipmentMode))
            {
                queryFilters = queryFilters.Where(s => !String.IsNullOrEmpty(s[ExternalDataFields.ShipmentMode]) &&
                                         s[ExternalDataFields.ShipmentMode] == filters.ShipmentMode);
            }

            if (!String.IsNullOrWhiteSpace(filters.State))
            {
                queryFilters = queryFilters.Where(s => !String.IsNullOrEmpty(s[ExternalDataFields.State]) &&
                                         s[ExternalDataFields.State] == filters.State);
            }

            List<DynamicRecord> clientRecordsFinal = queryFilters.ToList();

            Int64 totalClientRecords = clientRecordsFinal.Count();
            Int64 totalImports = clientRecordsFinal.Count(r => r[ExternalDataFields.OperationType] == ExternalDataValues.Import);
            Int64 totalExports = clientRecordsFinal.Count(r => r[ExternalDataFields.OperationType] == ExternalDataValues.Export);
            Int64 totalAirShipments = clientRecordsFinal.Count(r => r[ExternalDataFields.ShipmentMode] == ExternalDataValues.AirShipment);
            Int64 totalOceanShipments = clientRecordsFinal.Count(r => r[ExternalDataFields.ShipmentMode] == ExternalDataValues.OceanShipment);

            List<ResumenMyShipmentDto> resumenMyShipmentDto = clientRecordsFinal
                .OrderByDescending(s => s[ExternalDataFields.CreationDate].ToDateTimeOrMin()) // Ajusta por tu campo de ordenamiento
                .OrderBy(s => Int64.TryParse(s[ExternalDataFields.ID], out Int64 id) ? id : 0)
                .Skip((Int32)((page - 1) * size))
                .Take((Int32)size)
                .Select(s => new ResumenMyShipmentDto // Mapeo a tu DTO final
                {
                    Id = Int64.TryParse(s[ExternalDataFields.ID], out Int64 id) ? id : 0,
                    ShipmentMode = s[ExternalDataFields.ShipmentMode],
                    DocumentNumber = s[ExternalDataFields.DocumentNumber],
                    State = s[ExternalDataFields.State],
                    OperationType = s[ExternalDataFields.OperationType],
                    ClientName = s[ExternalDataFields.ClientName],
                    Origin = s[ExternalDataFields.Origin],
                    Destination = s[ExternalDataFields.Destination],
                    ETDDate = s[ExternalDataFields.ETDDate].ToDateTimeOrMin(),
                    ATDDate = s[ExternalDataFields.ATDDate].ToDateTimeOrMin(),
                    ETADate = s[ExternalDataFields.ETADate].ToDateTimeOrMin(),
                    ATADate = s[ExternalDataFields.ATADate].ToDateTimeOrMin(),
                })
                .ToList();

            return new MyShipmentsDomainResult
            {
                MyShipments = resumenMyShipmentDto,
                ClientSummaryResponse = new ClientSummaryDomainResult
                {
                    TotalClientRecords = totalClientRecords,
                    TotalImports = totalImports,
                    TotalExports = totalExports,
                    TotalAirShipments = totalAirShipments,
                    TotalOceanShipments = totalOceanShipments
                }
            };
        }

        public MyShipmentsDomainResult GetHistoryAllShipments(DynamicDataSet dataSet, String clientId, Int64 page, Int64 size)
        {
            // Crea una nueva List<DynamicRecord> con solo los registros activos
            List<DynamicRecord> clientRecords = dataSet.Rows
                .Where(r => r[ExternalDataFields.ClientNit] == clientId && r[ExternalDataFields.State] == ExternalDataValues.DeliveredState)
                .ToList();

            Int64 totalClientRecords = clientRecords.Count();
            Int64 totalImports = clientRecords.Count(r => r[ExternalDataFields.OperationType] == ExternalDataValues.Import);
            Int64 totalExports = clientRecords.Count(r => r[ExternalDataFields.OperationType] == ExternalDataValues.Export);
            Int64 totalAirShipments = clientRecords.Count(r => r[ExternalDataFields.ShipmentMode] == ExternalDataValues.AirShipment);
            Int64 totalOceanShipments = clientRecords.Count(r => r[ExternalDataFields.ShipmentMode] == ExternalDataValues.OceanShipment);

            /// ToDo: Pendiente corregir ordenamiento de fecha
            // 3. Aplicar ordenamiento (REQUISITO FUNDAMENTAL antes de Skip/Take)
            // y traer solo los registros de la página solicitada
            List<ResumenMyShipmentDto> resumenMyShipmentDto = clientRecords
                .OrderByDescending(s => s[ExternalDataFields.CreationDate].ToDateTimeOrMin()) // Ajusta por tu campo de ordenamiento
                .OrderBy(s => Int64.TryParse(s[ExternalDataFields.ID], out Int64 id) ? id : 0)
                .Skip((Int32)((page - 1) * size))
                .Take((Int32)size)
                .Select(s => new ResumenMyShipmentDto // Mapeo a tu DTO final
                {
                    Id = Int64.TryParse(s[ExternalDataFields.ID], out Int64 id) ? id : 0,
                    ShipmentMode = s[ExternalDataFields.ShipmentMode],
                    DocumentNumber = s[ExternalDataFields.DocumentNumber],
                    State = s[ExternalDataFields.State],
                    OperationType = s[ExternalDataFields.OperationType],
                    ClientName = s[ExternalDataFields.ClientName],
                    Origin = s[ExternalDataFields.Origin],
                    Destination = s[ExternalDataFields.Destination],
                    ETDDate = s[ExternalDataFields.ETDDate].ToDateTimeOrMin(),
                    ATDDate = s[ExternalDataFields.ATDDate].ToDateTimeOrMin(),
                    ETADate = s[ExternalDataFields.ETADate].ToDateTimeOrMin(),
                    ATADate = s[ExternalDataFields.ATADate].ToDateTimeOrMin(),
                })
                .ToList();

            return new MyShipmentsDomainResult
            {
                MyShipments = resumenMyShipmentDto,
                ClientSummaryResponse = new ClientSummaryDomainResult
                {
                    TotalClientRecords = totalClientRecords,
                    TotalImports = totalImports,
                    TotalExports = totalExports,
                    TotalAirShipments = totalAirShipments,
                    TotalOceanShipments = totalOceanShipments
                }
            };
        }

        public MyShipmentsDomainResult GetFiltersHistoryShipments(DynamicDataSet dataSet, String clientId, Int64 page, Int64 size, MyShipmentsFiltersDto filters)
        {
            // Crea una nueva List<DynamicRecord> con solo los registros activos
            List<DynamicRecord> clientRecords = dataSet.Rows
                .Where(r => r[ExternalDataFields.ClientNit] == clientId && r[ExternalDataFields.State] == ExternalDataValues.DeliveredState)
                .ToList();

            // 1. Convertir la lista a IEnumerable para aplicar LINQ en memoria
            IEnumerable<DynamicRecord> queryFilters = clientRecords;

            // 2. Encadenar los .Where() de forma condicional
            if (!String.IsNullOrWhiteSpace(filters.ValueFilter))
            {
                // 2. Filtramos la lista buscando si coincide con CUALQUIERA (OR / ||) de los campos
                queryFilters = queryFilters.Where(s =>
                    (s[ExternalDataFields.DocumentNumber] != null && s[ExternalDataFields.DocumentNumber].Contains(filters.ValueFilter, StringComparison.OrdinalIgnoreCase)) ||
                    (s[ExternalDataFields.ClientName] != null && s[ExternalDataFields.ClientName].Contains(filters.ValueFilter, StringComparison.OrdinalIgnoreCase)) ||
                    (s[ExternalDataFields.Origin] != null && s[ExternalDataFields.Origin].Contains(filters.ValueFilter, StringComparison.OrdinalIgnoreCase)) ||
                    (s[ExternalDataFields.Destination] != null && s[ExternalDataFields.Destination].Contains(filters.ValueFilter, StringComparison.OrdinalIgnoreCase))
                );
            }

            if (!String.IsNullOrWhiteSpace(filters.OperationType))
            {
                queryFilters = queryFilters.Where(s => !String.IsNullOrEmpty(s[ExternalDataFields.OperationType]) &&
                                         s[ExternalDataFields.OperationType] == filters.OperationType);
            }

            if (!String.IsNullOrWhiteSpace(filters.ShipmentMode))
            {
                queryFilters = queryFilters.Where(s => !String.IsNullOrEmpty(s[ExternalDataFields.ShipmentMode]) &&
                                         s[ExternalDataFields.ShipmentMode] == filters.ShipmentMode);
            }

            List<DynamicRecord> clientRecordsFinal = queryFilters.ToList();

            Int64 totalClientRecords = clientRecordsFinal.Count();
            Int64 totalImports = clientRecordsFinal.Count(r => r[ExternalDataFields.OperationType] == ExternalDataValues.Import);
            Int64 totalExports = clientRecordsFinal.Count(r => r[ExternalDataFields.OperationType] == ExternalDataValues.Export);
            Int64 totalAirShipments = clientRecordsFinal.Count(r => r[ExternalDataFields.ShipmentMode] == ExternalDataValues.AirShipment);
            Int64 totalOceanShipments = clientRecordsFinal.Count(r => r[ExternalDataFields.ShipmentMode] == ExternalDataValues.OceanShipment);

            List<ResumenMyShipmentDto> resumenMyShipmentDto = clientRecordsFinal
                .OrderByDescending(s => s[ExternalDataFields.CreationDate].ToDateTimeOrMin()) // Ajusta por tu campo de ordenamiento
                .OrderBy(s => Int64.TryParse(s[ExternalDataFields.ID], out Int64 id) ? id : 0)
                .Skip((Int32)((page - 1) * size))
                .Take((Int32)size)
                .Select(s => new ResumenMyShipmentDto // Mapeo a tu DTO final
                {
                    Id = Int64.TryParse(s[ExternalDataFields.ID], out Int64 id) ? id : 0,
                    ShipmentMode = s[ExternalDataFields.ShipmentMode],
                    DocumentNumber = s[ExternalDataFields.DocumentNumber],
                    State = s[ExternalDataFields.State],
                    OperationType = s[ExternalDataFields.OperationType],
                    ClientName = s[ExternalDataFields.ClientName],
                    Origin = s[ExternalDataFields.Origin],
                    Destination = s[ExternalDataFields.Destination],
                    ETDDate = s[ExternalDataFields.ETDDate].ToDateTimeOrMin(),
                    ATDDate = s[ExternalDataFields.ATDDate].ToDateTimeOrMin(),
                    ETADate = s[ExternalDataFields.ETADate].ToDateTimeOrMin(),
                    ATADate = s[ExternalDataFields.ATADate].ToDateTimeOrMin(),
                })
                .ToList();

            return new MyShipmentsDomainResult
            {
                MyShipments = resumenMyShipmentDto,
                ClientSummaryResponse = new ClientSummaryDomainResult
                {
                    TotalClientRecords = totalClientRecords,
                    TotalImports = totalImports,
                    TotalExports = totalExports,
                    TotalAirShipments = totalAirShipments,
                    TotalOceanShipments = totalOceanShipments
                }
            };
        }

        public DetailsShipmentsDomainDtoResult GetDetailsShipments(DynamicDataSet dataSet, String clientId, String DocumentNumber)
        {
            // Crea una nueva List<DynamicRecord> con solo los registros activos
            DynamicRecord clientRecords = dataSet.Rows
                .First(r => r[ExternalDataFields.ClientNit] == clientId && r[ExternalDataFields.DocumentNumber] == DocumentNumber);

            clientRecords = clientRecords ?? new DynamicRecord(new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase));

            return new DetailsShipmentsDomainDtoResult
            {
                ResumenShipments = new SummaryShipmentsDomainDtoResult
                {
                    Id = clientRecords[ExternalDataFields.ID],
                    ClientName = clientRecords[ExternalDataFields.ClientName],
                    Supplier = clientRecords[ExternalDataFields.Supplier],
                    Carrier = clientRecords[ExternalDataFields.Carrier],
                    MerchandiseDescription = clientRecords[ExternalDataFields.MerchandiseDescription],
                    DocumentNumber = clientRecords[ExternalDataFields.DocumentNumber],
                    DocumentType = clientRecords[ExternalDataFields.DocumentType],
                    Origin = clientRecords[ExternalDataFields.Origin],
                    Destination = clientRecords[ExternalDataFields.Destination],
                    LoadType = clientRecords[ExternalDataFields.LoadType],
                    PackagesNumbers = clientRecords[ExternalDataFields.PackagesNumbers],
                    WeightKg = clientRecords[ExternalDataFields.WeightKg],
                    VolumeM3 = clientRecords[ExternalDataFields.VolumeM3],
                    Incoterm = clientRecords[ExternalDataFields.Incoterm],
                    OperationType = clientRecords[ExternalDataFields.OperationType],
                    ShipmentMode = clientRecords[ExternalDataFields.ShipmentMode],
                },
                TrackingShipments = new TrackingShipmentsDomainDtoResult
                {
                    State = clientRecords[ExternalDataFields.State]
                },
                LogisticsDatesShipments = new LogisticsDatesShipmentsDomainDtoResult
                {
                    StoreOriginDate = clientRecords[ExternalDataFields.StoreOriginDate].ToDateTimeOrMin(),
                    ETDDate = clientRecords[ExternalDataFields.ETDDate].ToDateTimeOrMin(),
                    ATDDate = clientRecords[ExternalDataFields.ATDDate].ToDateTimeOrMin(),
                    ETADate = clientRecords[ExternalDataFields.ETADate].ToDateTimeOrMin(),
                    ATADate = clientRecords[ExternalDataFields.ATADate].ToDateTimeOrMin(),
                    StoreDestinationDate = clientRecords[ExternalDataFields.StoreDestinationDate].ToDateTimeOrMin(),
                    NationalizationDate = clientRecords[ExternalDataFields.NationalizationDate].ToDateTimeOrMin(),
                    DispatchDestinationDate = clientRecords[ExternalDataFields.DispatchDestinationDate].ToDateTimeOrMin(),
                    FormDate = clientRecords[ExternalDataFields.FormDate].ToDateTimeOrMin(),
                    ContainerDeliveryDate = clientRecords[ExternalDataFields.ContainerDeliveryDate].ToDateTimeOrMin(),
                },
                ContainerShipments = new ContainerShipmentsDomainDtoResult
                {
                    ContainerType = clientRecords[ExternalDataFields.ContainerType],
                    ContainerAmount = clientRecords[ExternalDataFields.ContainerAmount],
                    ContainerNumber = clientRecords[ExternalDataFields.ContainerNumber],
                    DaysOff = clientRecords[ExternalDataFields.DaysOff],
                    DaysRemainingDelivery = clientRecords[ExternalDataFields.DaysRemainingDelivery],
                    ActualContainerReturnDate = clientRecords[ExternalDataFields.ActualContainerReturnDate].ToDateTimeOrMin(),
                    ContainerDelayDays = clientRecords[ExternalDataFields.ContainerDelayDays],
                    CostDayOfDelay = clientRecords[ExternalDataFields.CostDayOfDelay],
                    TotalCostContainerDelays = clientRecords[ExternalDataFields.TotalCostContainerDelays],
                    ContainerDepot = clientRecords[ExternalDataFields.ContainerDepot]
                },
                FinancialInfoShipments = new FinancialInfoShipmentsDomainDtoResult
                {
                    AdvancePaymentRequestDate = clientRecords[ExternalDataFields.AdvancePaymentRequestDate].ToDateTimeOrMin(),
                    AdvancePaymentDate = clientRecords[ExternalDataFields.AdvancePaymentDate].ToDateTimeOrMin(),
                    AdvancePaymentAmount = clientRecords[ExternalDataFields.AdvancePaymentAmount],
                    SupplierInvoice = clientRecords[ExternalDataFields.SupplierInvoice],
                    TCCInvoice = clientRecords[ExternalDataFields.TCCInvoice],
                    InvoiceNumber = clientRecords[ExternalDataFields.InvoiceNumber],
                    InvoiceDate = clientRecords[ExternalDataFields.InvoiceDate],
                    ExpenseDescription = clientRecords[ExternalDataFields.ExpenseDescription],
                    ExpenseAmountUSD = clientRecords[ExternalDataFields.ExpenseAmountUSD],
                    InvoiceSubtotalUSD = clientRecords[ExternalDataFields.InvoiceSubtotalUSD],
                    IvaUSD = clientRecords[ExternalDataFields.IvaUSD],
                    TotalInvoiceUSD = clientRecords[ExternalDataFields.TotalInvoiceUSD],
                },
                HistoryShipments = new HistoryShipmentsDomainDtoResult
                {
                    //ToDo: Pendiente implementar logica de consulta de logs de cambios de estado
                    DetailsHistoryShipments = new List<DetailsHistoryShipmentsDomainDtoResult>()
                    {
                        new DetailsHistoryShipmentsDomainDtoResult
                        {
                            ChangeDate = DateTime.Now,
                            ChangeUser = "ANALISTASAC",
                            Message = "Creación del envio",
                            OldState = "",
                            NewState = "Pendiente"
                        },
                        new DetailsHistoryShipmentsDomainDtoResult
                        {
                            ChangeDate = DateTime.Now.AddMonths(1),
                            ChangeUser = "ANALISTASAC",
                            Message = "Se asigna el envio para iniciar su despacho",
                            OldState = "Pendiente",
                            NewState = "En tránsito"
                        },
                        new DetailsHistoryShipmentsDomainDtoResult
                        {
                            ChangeDate = DateTime.Now.AddMonths(2),
                            ChangeUser = "ANALISTAOPE",
                            Message = "Llega a la aduana origen para revisar",
                            OldState = "En tránsito",
                            NewState = "En Aduana origen"
                        },
                        new DetailsHistoryShipmentsDomainDtoResult
                        {
                            ChangeDate = DateTime.Now.AddMonths(4),
                            ChangeUser = "ANALISTAOPE",
                            Message = "Llega a la aduana destinoi para revisar",
                            OldState = "En Aduana origen",
                            NewState = "En Aduana destino"
                        },
                        new DetailsHistoryShipmentsDomainDtoResult
                        {
                            ChangeDate = DateTime.Now.AddMonths(6),
                            ChangeUser = "CLIENT",
                            Message = "Presentan demoras en el envío",
                            OldState = "En Aduana destino",
                            NewState = "Con novedad"
                        },
                        new DetailsHistoryShipmentsDomainDtoResult
                        {
                            ChangeDate = DateTime.Now.AddMonths(7),
                            ChangeUser = "ADMIN",
                            Message = "El envìo a sido entregado",
                            OldState = "Con novedad",
                            NewState = "Entregado"
                        }
                    }
                }
            };
        }
    }
}
