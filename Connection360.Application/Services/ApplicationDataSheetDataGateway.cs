using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using Connection360.Domain.Constans;
using Connection360.Domain.Dtos;
using Connection360.Domain.Entities;
using Connection360.Domain.Enums;
using Connection360.Domain.Ports.Persistence;
using System.Globalization;

namespace Connection360.Application.Services
{
    /// <summary>
    /// Implementación de IApplicationDataSheetDataGateway: resuelve
    /// IApplicationDataSheetEntregadosRepository / IApplicationDataSheetNoEntregadosRepository (vía
    /// IUnitOfWork.GetRepository&lt;T&gt;(), mismo patrón que ClientAccessResolver) y adapta el
    /// resultado a un DynamicDataSet, usando Connection360.Domain.Constans.ExternalDataFields como
    /// nombres de campo -los mismos que ya leen ClientSummaryDomainService, ClientRecordsFilterService,
    /// MyShipmentsDomainService, etc. sobre un DynamicRecord-, para que cualquier caso de uso que ya
    /// consuma un DynamicDataSet pueda usar esta fuente sin cambios adicionales.
    /// </summary>
    public class ApplicationDataSheetDataGateway : IApplicationDataSheetDataGateway
    {
        private readonly IUnitOfWork _unitOfWork;

        public ApplicationDataSheetDataGateway(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Reemplazo directo de IExternalDataGateway.FetchDataAsync("BPMS", filters, cancellationToken):
        /// combina ambas vistas y, si <paramref name="filters"/> trae ExternalDataFields.ClientNit,
        /// filtra por ese nit_cliente.
        /// </summary>
        public Task<DynamicDataSet> FetchDataAsync(IDictionary<String, String> filters, CancellationToken cancellationToken)
        {
            String? nitCliente = filters != null && filters.TryGetValue(ExternalDataFields.ClientNit, out var nit) && !String.IsNullOrWhiteSpace(nit)
                ? nit
                : null;

            var request = new ApplicationDataSheetDataRequest
            {
                Scope = ApplicationDataSheetViewScope.Todos,
                NitCliente = nitCliente
            };

            return FetchDataAsync(request, cancellationToken);
        }

        public async Task<DynamicDataSet> FetchDataAsync(ApplicationDataSheetDataRequest request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var rows = new List<ApplicationDataSheetViewResultDto>();

            if (request.Scope != ApplicationDataSheetViewScope.NoEntregados)
            {
                IApplicationDataSheetEntregadosRepository entregadosRepository = _unitOfWork.GetRepository<IApplicationDataSheetEntregadosRepository>();
                rows.AddRange(await QueryViewAsync(entregadosRepository, request.NitCliente, request.FieldsSelection, cancellationToken));
            }

            if (request.Scope != ApplicationDataSheetViewScope.Entregados)
            {
                IApplicationDataSheetNoEntregadosRepository noEntregadosRepository = _unitOfWork.GetRepository<IApplicationDataSheetNoEntregadosRepository>();
                rows.AddRange(await QueryViewAsync(noEntregadosRepository, request.NitCliente, request.FieldsSelection, cancellationToken));
            }

            return ToDynamicDataSet(rows, request.FieldsSelection);
        }

        /// <summary>
        /// Llama al método del repositorio que corresponde según si vino nit_cliente y/o selección
        /// de campos, para aprovechar los 4 métodos de IApplicationDataSheetViewRepository (y no
        /// traer más datos o columnas de los necesarios).
        /// </summary>
        private static Task<List<ApplicationDataSheetViewResultDto>> QueryViewAsync(
            IApplicationDataSheetViewRepository repository,
            String? nitCliente,
            ApplicationDataSheetViewFieldsSelectionDto? fieldsSelection,
            CancellationToken cancellationToken)
        {
            Boolean hasNitCliente = !String.IsNullOrWhiteSpace(nitCliente);
            Boolean hasFieldsSelection = fieldsSelection is { Fields.Count: > 0 };

            if (hasNitCliente && hasFieldsSelection)
                return repository.GetByNitClienteAsync(nitCliente!, fieldsSelection!, cancellationToken);

            if (hasNitCliente)
                return repository.GetByNitClienteAsync(nitCliente!, cancellationToken);

            if (hasFieldsSelection)
                return repository.GetAllAsync(fieldsSelection!, cancellationToken);

            return repository.GetAllAsync(cancellationToken);
        }

        private static DynamicDataSet ToDynamicDataSet(List<ApplicationDataSheetViewResultDto> rows, ApplicationDataSheetViewFieldsSelectionDto? fieldsSelection)
        {
            Boolean hasFieldsSelection = fieldsSelection is { Fields.Count: > 0 };

            List<(ApplicationDataSheetViewField Field, String ExternalFieldName)> entries = hasFieldsSelection
                ? _fieldNames.Where(f => fieldsSelection!.Fields.Contains(f.Field)).ToList()
                : _fieldNames.ToList();

            List<String> availableFields = entries.Select(e => e.ExternalFieldName).ToList();

            List<DynamicRecord> dynamicRows = rows.Select(row =>
            {
                var values = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in entries)
                {
                    values[entry.ExternalFieldName] = GetFieldValue(row, entry.Field);
                }
                return new DynamicRecord(values);
            }).ToList();

            return new DynamicDataSet(availableFields, dynamicRows);
        }

        /// <summary>
        /// Valor (siempre String, como requiere DynamicRecord) de <paramref name="field"/> para
        /// <paramref name="row"/>. Mapeo 1 a 1 entre cada columna de la vista y el nombre de campo
        /// "externo" (ExternalDataFields) con el que ya se conocía ese mismo dato cuando venía en
        /// vivo de las APIs operativas.
        /// </summary>
        private static String GetFieldValue(ApplicationDataSheetViewResultDto row, ApplicationDataSheetViewField field)
        {
            return field switch
            {
                ApplicationDataSheetViewField.Id => FormatNumber(row.Id),
                ApplicationDataSheetViewField.FechaCreacion => FormatDate(row.FechaCreacion),
                ApplicationDataSheetViewField.TipoOperacion => row.TipoOperacion,
                ApplicationDataSheetViewField.Modalidad => row.Modalidad,
                ApplicationDataSheetViewField.Incoterm => row.Incoterm,
                ApplicationDataSheetViewField.Proveedor => row.Proveedor,
                ApplicationDataSheetViewField.Cliente => row.Cliente,
                ApplicationDataSheetViewField.NitCliente => row.NitCliente,
                ApplicationDataSheetViewField.Origen => row.Origen,
                ApplicationDataSheetViewField.Destino => row.Destino,
                ApplicationDataSheetViewField.DescripcionMercancia => row.DescripcionMercancia,
                ApplicationDataSheetViewField.Estado => row.Estado,
                ApplicationDataSheetViewField.TipoCarga => row.TipoCarga,
                ApplicationDataSheetViewField.TipoContenedor => row.TipoContenedor ?? String.Empty,
                ApplicationDataSheetViewField.CantidadContenedores => FormatNumber(row.CantidadContenedores),
                ApplicationDataSheetViewField.NumeroContenedor => row.NumeroContenedor ?? String.Empty,
                ApplicationDataSheetViewField.CantidadBultos => FormatNumber(row.CantidadBultos),
                ApplicationDataSheetViewField.PesoKg => FormatNumber(row.PesoKg),
                ApplicationDataSheetViewField.VolumenM3 => FormatNumber(row.VolumenM3),
                ApplicationDataSheetViewField.Transportista => row.Transportista,
                ApplicationDataSheetViewField.TipoDocumento => row.TipoDocumento,
                ApplicationDataSheetViewField.NombreDocumento => row.NombreDocumento,
                ApplicationDataSheetViewField.DocumentoTransporteHbl => row.DocumentoTransporteHbl,
                ApplicationDataSheetViewField.FechaBodegaOrigen => FormatDate(row.FechaBodegaOrigen),
                ApplicationDataSheetViewField.FechaEtd => FormatDate(row.FechaEtd),
                ApplicationDataSheetViewField.FechaAtd => FormatDate(row.FechaAtd),
                ApplicationDataSheetViewField.FechaEta => FormatDate(row.FechaEta),
                ApplicationDataSheetViewField.FechaAta => FormatDate(row.FechaAta),
                ApplicationDataSheetViewField.FechaBodegaDestino => FormatDate(row.FechaBodegaDestino),
                ApplicationDataSheetViewField.FechaNacionalizacion => FormatDate(row.FechaNacionalizacion),
                ApplicationDataSheetViewField.FechaDespachoDestino => FormatDate(row.FechaDespachoDestino),
                ApplicationDataSheetViewField.FechaPlanilla => FormatDate(row.FechaPlanilla),
                ApplicationDataSheetViewField.FechaEntregaContenedor => FormatDate(row.FechaEntregaContenedor),
                ApplicationDataSheetViewField.FechaDevolucionRealContenedor => FormatDate(row.FechaDevolucionRealContenedor),
                ApplicationDataSheetViewField.DiasLibres => FormatNumber(row.DiasLibres),
                ApplicationDataSheetViewField.DiasRestantesEntrega => FormatNumber(row.DiasRestantesEntrega),
                ApplicationDataSheetViewField.DiasDemoraContenedor => FormatNumber(row.DiasDemoraContenedor),
                ApplicationDataSheetViewField.ValorDiaDemora => FormatNumber(row.ValorDiaDemora),
                ApplicationDataSheetViewField.ValorTotalDemora => FormatNumber(row.ValorTotalDemora),
                ApplicationDataSheetViewField.DepositoContenedor => FormatNumber(row.DepositoContenedor),
                ApplicationDataSheetViewField.FechaSolicitudAnticipo => FormatDate(row.FechaSolicitudAnticipo),
                ApplicationDataSheetViewField.FechaPagoAnticipo => FormatDate(row.FechaPagoAnticipo),
                ApplicationDataSheetViewField.ValorAnticipo => FormatNumber(row.ValorAnticipo),
                ApplicationDataSheetViewField.FacturaProveedor => row.FacturaProveedor,
                ApplicationDataSheetViewField.FacturaTcc => row.FacturaTcc,
                ApplicationDataSheetViewField.NumeroFactura => row.NumeroFactura,
                ApplicationDataSheetViewField.FechaFactura => FormatDate(row.FechaFactura),
                ApplicationDataSheetViewField.DescripcionGasto => row.DescripcionGasto,
                ApplicationDataSheetViewField.ValorGastoUsd => FormatNumber(row.ValorGastoUsd),
                ApplicationDataSheetViewField.SubtotalFacturaUsd => FormatNumber(row.SubtotalFacturaUsd),
                ApplicationDataSheetViewField.IvaUsd => FormatNumber(row.IvaUsd),
                ApplicationDataSheetViewField.TotalFacturaUsd => FormatNumber(row.TotalFacturaUsd),
                ApplicationDataSheetViewField.Comentario => row.Comentario,
                ApplicationDataSheetViewField.FechaComentario => FormatDate(row.FechaComentario),
                _ => String.Empty
            };
        }

        private static String FormatDate(DateTime value) => value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        private static String FormatDate(DateTime? value) => value.HasValue ? FormatDate(value.Value) : String.Empty;
        private static String FormatNumber(Decimal value) => value.ToString(CultureInfo.InvariantCulture);
        private static String FormatNumber(Int32 value) => value.ToString(CultureInfo.InvariantCulture);
        private static String FormatNumber(Int64 value) => value.ToString(CultureInfo.InvariantCulture);

        // Nombre de campo "externo" (ExternalDataFields) para cada columna de la vista, en el mismo
        // orden que ApplicationDataSheetViewField / ApplicationDataSheetViewColumns.
        private static readonly (ApplicationDataSheetViewField Field, String ExternalFieldName)[] _fieldNames =
        {
            (ApplicationDataSheetViewField.Id, ExternalDataFields.ID),
            (ApplicationDataSheetViewField.FechaCreacion, ExternalDataFields.CreationDate),
            (ApplicationDataSheetViewField.TipoOperacion, ExternalDataFields.OperationType),
            (ApplicationDataSheetViewField.Modalidad, ExternalDataFields.ShipmentMode),
            (ApplicationDataSheetViewField.Incoterm, ExternalDataFields.Incoterm),
            (ApplicationDataSheetViewField.Proveedor, ExternalDataFields.Supplier),
            (ApplicationDataSheetViewField.Cliente, ExternalDataFields.ClientName),
            (ApplicationDataSheetViewField.NitCliente, ExternalDataFields.ClientNit),
            (ApplicationDataSheetViewField.Origen, ExternalDataFields.Origin),
            (ApplicationDataSheetViewField.Destino, ExternalDataFields.Destination),
            (ApplicationDataSheetViewField.DescripcionMercancia, ExternalDataFields.MerchandiseDescription),
            (ApplicationDataSheetViewField.Estado, ExternalDataFields.State),
            (ApplicationDataSheetViewField.TipoCarga, ExternalDataFields.LoadType),
            (ApplicationDataSheetViewField.TipoContenedor, ExternalDataFields.ContainerType),
            (ApplicationDataSheetViewField.CantidadContenedores, ExternalDataFields.ContainerAmount),
            (ApplicationDataSheetViewField.NumeroContenedor, ExternalDataFields.ContainerNumber),
            (ApplicationDataSheetViewField.CantidadBultos, ExternalDataFields.PackagesNumbers),
            (ApplicationDataSheetViewField.PesoKg, ExternalDataFields.WeightKg),
            (ApplicationDataSheetViewField.VolumenM3, ExternalDataFields.VolumeM3),
            (ApplicationDataSheetViewField.Transportista, ExternalDataFields.Carrier),
            (ApplicationDataSheetViewField.TipoDocumento, ExternalDataFields.DocumentType),
            (ApplicationDataSheetViewField.NombreDocumento, ExternalDataFields.DocumentName),
            (ApplicationDataSheetViewField.DocumentoTransporteHbl, ExternalDataFields.DocumentNumber),
            (ApplicationDataSheetViewField.FechaBodegaOrigen, ExternalDataFields.StoreOriginDate),
            (ApplicationDataSheetViewField.FechaEtd, ExternalDataFields.ETDDate),
            (ApplicationDataSheetViewField.FechaAtd, ExternalDataFields.ATDDate),
            (ApplicationDataSheetViewField.FechaEta, ExternalDataFields.ETADate),
            (ApplicationDataSheetViewField.FechaAta, ExternalDataFields.ATADate),
            (ApplicationDataSheetViewField.FechaBodegaDestino, ExternalDataFields.StoreDestinationDate),
            (ApplicationDataSheetViewField.FechaNacionalizacion, ExternalDataFields.NationalizationDate),
            (ApplicationDataSheetViewField.FechaDespachoDestino, ExternalDataFields.DispatchDestinationDate),
            (ApplicationDataSheetViewField.FechaPlanilla, ExternalDataFields.FormDate),
            (ApplicationDataSheetViewField.FechaEntregaContenedor, ExternalDataFields.ContainerDeliveryDate),
            (ApplicationDataSheetViewField.FechaDevolucionRealContenedor, ExternalDataFields.ActualContainerReturnDate),
            (ApplicationDataSheetViewField.DiasLibres, ExternalDataFields.DaysOff),
            (ApplicationDataSheetViewField.DiasRestantesEntrega, ExternalDataFields.DaysRemainingDelivery),
            (ApplicationDataSheetViewField.DiasDemoraContenedor, ExternalDataFields.ContainerDelayDays),
            (ApplicationDataSheetViewField.ValorDiaDemora, ExternalDataFields.CostDayOfDelay),
            (ApplicationDataSheetViewField.ValorTotalDemora, ExternalDataFields.TotalCostContainerDelays),
            (ApplicationDataSheetViewField.DepositoContenedor, ExternalDataFields.ContainerDepot),
            (ApplicationDataSheetViewField.FechaSolicitudAnticipo, ExternalDataFields.AdvancePaymentRequestDate),
            (ApplicationDataSheetViewField.FechaPagoAnticipo, ExternalDataFields.AdvancePaymentDate),
            (ApplicationDataSheetViewField.ValorAnticipo, ExternalDataFields.AdvancePaymentAmount),
            (ApplicationDataSheetViewField.FacturaProveedor, ExternalDataFields.SupplierInvoice),
            (ApplicationDataSheetViewField.FacturaTcc, ExternalDataFields.TCCInvoice),
            (ApplicationDataSheetViewField.NumeroFactura, ExternalDataFields.InvoiceNumber),
            (ApplicationDataSheetViewField.FechaFactura, ExternalDataFields.InvoiceDate),
            (ApplicationDataSheetViewField.DescripcionGasto, ExternalDataFields.ExpenseDescription),
            (ApplicationDataSheetViewField.ValorGastoUsd, ExternalDataFields.ExpenseAmountUSD),
            (ApplicationDataSheetViewField.SubtotalFacturaUsd, ExternalDataFields.InvoiceSubtotalUSD),
            (ApplicationDataSheetViewField.IvaUsd, ExternalDataFields.IvaUSD),
            (ApplicationDataSheetViewField.TotalFacturaUsd, ExternalDataFields.TotalInvoiceUSD),
            (ApplicationDataSheetViewField.Comentario, ExternalDataFields.Comment),
            (ApplicationDataSheetViewField.FechaComentario, ExternalDataFields.CommentDate),
        };
    }
}
