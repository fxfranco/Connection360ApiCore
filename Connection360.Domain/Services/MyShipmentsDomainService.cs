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
                .Where(r => r[ExternalDataFields.Cliente] == clientId && r[ExternalDataFields.Estado] != TipoOperacionValues.EstadoEntregado)
                .ToList();

            Int64 totalClientRecords = clientRecords.Count();
            Int64 totalImports = clientRecords.Count(r => r[ExternalDataFields.TipoOperacion] == TipoOperacionValues.Importacion);
            Int64 totalExports = clientRecords.Count(r => r[ExternalDataFields.TipoOperacion] == TipoOperacionValues.Exportacion);
            Int64 totalAirShipments = clientRecords.Count(r => r[ExternalDataFields.TipoModalidad] == TipoOperacionValues.ModalidadAire);
            Int64 totalOceanShipments = clientRecords.Count(r => r[ExternalDataFields.TipoModalidad] == TipoOperacionValues.ModalidadMaritima);

            /// ToDo: Pendiente corregir ordenamiento de fecha
            // 3. Aplicar ordenamiento (REQUISITO FUNDAMENTAL antes de Skip/Take)
            // y traer solo los registros de la página solicitada
            List<ResumenMyShipmentDto> resumenMyShipmentDto = clientRecords
                .OrderByDescending(s => s["FECHA DE CREACIÓN"].ToDateTimeOrMin()) // Ajusta por tu campo de ordenamiento
                .OrderBy(s => Int64.TryParse(s["ID"], out Int64 id) ? id : 0)
                .Skip((Int32)((page - 1) * size))
                .Take((Int32)size)
                .Select(s => new ResumenMyShipmentDto // Mapeo a tu DTO final
                {
                    Id = Int64.TryParse(s["ID"], out Int64 id) ? id : 0,
                    ShipmentMode = s["MODALIDAD (AIR - SEA)"],
                    DocumentNumber = s["DOCUMENTO DE TRANSPORTE (HBL)"],
                    State = s["ESTADO"],
                    OperationType = s["TIPO DE OPERACIÓN (IMPO - EXPO)"],
                    ClientName = s["CLIENTE"],
                    Origin = s["ORIGEN"],
                    Destination = s["DESTINO"],
                    ETDDate = s["ETD (Fecha Estimada Salida)"].ToDateTimeOrMin(),
                    ATDDate = s["ATD (Fecha Real Salida)"].ToDateTimeOrMin(),
                    ETADate = s["ETA (Fecha Estimada Llegada)"].ToDateTimeOrMin(),
                    ATADate = s["ATA (Fecha Real Llegada)"].ToDateTimeOrMin(),
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
                .Where(r => r[ExternalDataFields.Cliente] == clientId && r[ExternalDataFields.Estado] != TipoOperacionValues.EstadoEntregado)
                .ToList();

            // 1. Convertir la lista a IEnumerable para aplicar LINQ en memoria
            IEnumerable<DynamicRecord> queryFilters = clientRecords;

            // 2. Encadenar los .Where() de forma condicional
            if (!String.IsNullOrWhiteSpace(filters.ValueFilter))
            {
                // 2. Filtramos la lista buscando si coincide con CUALQUIERA (OR / ||) de los campos
                queryFilters = queryFilters.Where(s =>
                    (s["DOCUMENTO DE TRANSPORTE (HBL)"] != null && s["DOCUMENTO DE TRANSPORTE (HBL)"].Contains(filters.ValueFilter, StringComparison.OrdinalIgnoreCase)) ||
                    (s["CLIENTE"] != null && s["CLIENTE"].Contains(filters.ValueFilter, StringComparison.OrdinalIgnoreCase)) ||
                    (s["ORIGEN"] != null && s["ORIGEN"].Contains(filters.ValueFilter, StringComparison.OrdinalIgnoreCase)) ||
                    (s["DESTINO"] != null && s["DESTINO"].Contains(filters.ValueFilter, StringComparison.OrdinalIgnoreCase))
                );
            }

            if (!String.IsNullOrWhiteSpace(filters.OperationType))
            {
                queryFilters = queryFilters.Where(s => !String.IsNullOrEmpty(s[ExternalDataFields.TipoOperacion]) &&
                                         s[ExternalDataFields.TipoOperacion] == filters.OperationType);
            }

            if (!String.IsNullOrWhiteSpace(filters.ShipmentMode))
            {
                queryFilters = queryFilters.Where(s => !String.IsNullOrEmpty(s[ExternalDataFields.TipoModalidad]) &&
                                         s[ExternalDataFields.TipoModalidad] == filters.ShipmentMode);
            }

            if (!String.IsNullOrWhiteSpace(filters.State))
            {
                queryFilters = queryFilters.Where(s => !String.IsNullOrEmpty(s[ExternalDataFields.Estado]) &&
                                         s[ExternalDataFields.Estado] == filters.State);
            }

            List<DynamicRecord> clientRecordsFinal = queryFilters.ToList();

            Int64 totalClientRecords = clientRecordsFinal.Count();
            Int64 totalImports = clientRecordsFinal.Count(r => r[ExternalDataFields.TipoOperacion] == TipoOperacionValues.Importacion);
            Int64 totalExports = clientRecordsFinal.Count(r => r[ExternalDataFields.TipoOperacion] == TipoOperacionValues.Exportacion);
            Int64 totalAirShipments = clientRecordsFinal.Count(r => r[ExternalDataFields.TipoModalidad] == TipoOperacionValues.ModalidadAire);
            Int64 totalOceanShipments = clientRecordsFinal.Count(r => r[ExternalDataFields.TipoModalidad] == TipoOperacionValues.ModalidadMaritima);

            List<ResumenMyShipmentDto> resumenMyShipmentDto = clientRecordsFinal
                .OrderByDescending(s => s["FECHA DE CREACIÓN"].ToDateTimeOrMin()) // Ajusta por tu campo de ordenamiento
                .OrderBy(s => Int64.TryParse(s["ID"], out Int64 id) ? id : 0)
                .Skip((Int32)((page - 1) * size))
                .Take((Int32)size)
                .Select(s => new ResumenMyShipmentDto // Mapeo a tu DTO final
                {
                    Id = Int64.TryParse(s["ID"], out Int64 id) ? id : 0,
                    ShipmentMode = s["MODALIDAD (AIR - SEA)"],
                    DocumentNumber = s["DOCUMENTO DE TRANSPORTE (HBL)"],
                    State = s["ESTADO"],
                    OperationType = s["TIPO DE OPERACIÓN (IMPO - EXPO)"],
                    ClientName = s["CLIENTE"],
                    Origin = s["ORIGEN"],
                    Destination = s["DESTINO"],
                    ETDDate = s["ETD (Fecha Estimada Salida)"].ToDateTimeOrMin(),
                    ATDDate = s["ATD (Fecha Real Salida)"].ToDateTimeOrMin(),
                    ETADate = s["ETA (Fecha Estimada Llegada)"].ToDateTimeOrMin(),
                    ATADate = s["ATA (Fecha Real Llegada)"].ToDateTimeOrMin(),
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
                .Where(r => r[ExternalDataFields.Cliente] == clientId && r[ExternalDataFields.Estado] == TipoOperacionValues.EstadoEntregado)
                .ToList();

            Int64 totalClientRecords = clientRecords.Count();
            Int64 totalImports = clientRecords.Count(r => r[ExternalDataFields.TipoOperacion] == TipoOperacionValues.Importacion);
            Int64 totalExports = clientRecords.Count(r => r[ExternalDataFields.TipoOperacion] == TipoOperacionValues.Exportacion);
            Int64 totalAirShipments = clientRecords.Count(r => r[ExternalDataFields.TipoModalidad] == TipoOperacionValues.ModalidadAire);
            Int64 totalOceanShipments = clientRecords.Count(r => r[ExternalDataFields.TipoModalidad] == TipoOperacionValues.ModalidadMaritima);

            /// ToDo: Pendiente corregir ordenamiento de fecha
            // 3. Aplicar ordenamiento (REQUISITO FUNDAMENTAL antes de Skip/Take)
            // y traer solo los registros de la página solicitada
            List<ResumenMyShipmentDto> resumenMyShipmentDto = clientRecords
                .OrderByDescending(s => s["FECHA DE CREACIÓN"].ToDateTimeOrMin()) // Ajusta por tu campo de ordenamiento
                .OrderBy(s => Int64.TryParse(s["ID"], out Int64 id) ? id : 0)
                .Skip((Int32)((page - 1) * size))
                .Take((Int32)size)
                .Select(s => new ResumenMyShipmentDto // Mapeo a tu DTO final
                {
                    Id = Int64.TryParse(s["ID"], out Int64 id) ? id : 0,
                    ShipmentMode = s["MODALIDAD (AIR - SEA)"],
                    DocumentNumber = s["DOCUMENTO DE TRANSPORTE (HBL)"],
                    State = s["ESTADO"],
                    OperationType = s["TIPO DE OPERACIÓN (IMPO - EXPO)"],
                    ClientName = s["CLIENTE"],
                    Origin = s["ORIGEN"],
                    Destination = s["DESTINO"],
                    ETDDate = s["ETD (Fecha Estimada Salida)"].ToDateTimeOrMin(),
                    ATDDate = s["ATD (Fecha Real Salida)"].ToDateTimeOrMin(),
                    ETADate = s["ETA (Fecha Estimada Llegada)"].ToDateTimeOrMin(),
                    ATADate = s["ATA (Fecha Real Llegada)"].ToDateTimeOrMin(),
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
                .Where(r => r[ExternalDataFields.Cliente] == clientId && r[ExternalDataFields.Estado] == TipoOperacionValues.EstadoEntregado)
                .ToList();

            // 1. Convertir la lista a IEnumerable para aplicar LINQ en memoria
            IEnumerable<DynamicRecord> queryFilters = clientRecords;

            // 2. Encadenar los .Where() de forma condicional
            if (!String.IsNullOrWhiteSpace(filters.ValueFilter))
            {
                // 2. Filtramos la lista buscando si coincide con CUALQUIERA (OR / ||) de los campos
                queryFilters = queryFilters.Where(s =>
                    (s["DOCUMENTO DE TRANSPORTE (HBL)"] != null && s["DOCUMENTO DE TRANSPORTE (HBL)"].Contains(filters.ValueFilter, StringComparison.OrdinalIgnoreCase)) ||
                    (s["CLIENTE"] != null && s["CLIENTE"].Contains(filters.ValueFilter, StringComparison.OrdinalIgnoreCase)) ||
                    (s["ORIGEN"] != null && s["ORIGEN"].Contains(filters.ValueFilter, StringComparison.OrdinalIgnoreCase)) ||
                    (s["DESTINO"] != null && s["DESTINO"].Contains(filters.ValueFilter, StringComparison.OrdinalIgnoreCase))
                );
            }

            if (!String.IsNullOrWhiteSpace(filters.OperationType))
            {
                queryFilters = queryFilters.Where(s => !String.IsNullOrEmpty(s[ExternalDataFields.TipoOperacion]) &&
                                         s[ExternalDataFields.TipoOperacion] == filters.OperationType);
            }

            if (!String.IsNullOrWhiteSpace(filters.ShipmentMode))
            {
                queryFilters = queryFilters.Where(s => !String.IsNullOrEmpty(s[ExternalDataFields.TipoModalidad]) &&
                                         s[ExternalDataFields.TipoModalidad] == filters.ShipmentMode);
            }

            List<DynamicRecord> clientRecordsFinal = queryFilters.ToList();

            Int64 totalClientRecords = clientRecordsFinal.Count();
            Int64 totalImports = clientRecordsFinal.Count(r => r[ExternalDataFields.TipoOperacion] == TipoOperacionValues.Importacion);
            Int64 totalExports = clientRecordsFinal.Count(r => r[ExternalDataFields.TipoOperacion] == TipoOperacionValues.Exportacion);
            Int64 totalAirShipments = clientRecordsFinal.Count(r => r[ExternalDataFields.TipoModalidad] == TipoOperacionValues.ModalidadAire);
            Int64 totalOceanShipments = clientRecordsFinal.Count(r => r[ExternalDataFields.TipoModalidad] == TipoOperacionValues.ModalidadMaritima);

            List<ResumenMyShipmentDto> resumenMyShipmentDto = clientRecordsFinal
                .OrderByDescending(s => s["FECHA DE CREACIÓN"].ToDateTimeOrMin()) // Ajusta por tu campo de ordenamiento
                .OrderBy(s => Int64.TryParse(s["ID"], out Int64 id) ? id : 0)
                .Skip((Int32)((page - 1) * size))
                .Take((Int32)size)
                .Select(s => new ResumenMyShipmentDto // Mapeo a tu DTO final
                {
                    Id = Int64.TryParse(s["ID"], out Int64 id) ? id : 0,
                    ShipmentMode = s["MODALIDAD (AIR - SEA)"],
                    DocumentNumber = s["DOCUMENTO DE TRANSPORTE (HBL)"],
                    State = s["ESTADO"],
                    OperationType = s["TIPO DE OPERACIÓN (IMPO - EXPO)"],
                    ClientName = s["CLIENTE"],
                    Origin = s["ORIGEN"],
                    Destination = s["DESTINO"],
                    ETDDate = s["ETD (Fecha Estimada Salida)"].ToDateTimeOrMin(),
                    ATDDate = s["ATD (Fecha Real Salida)"].ToDateTimeOrMin(),
                    ETADate = s["ETA (Fecha Estimada Llegada)"].ToDateTimeOrMin(),
                    ATADate = s["ATA (Fecha Real Llegada)"].ToDateTimeOrMin(),
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
    }
}
