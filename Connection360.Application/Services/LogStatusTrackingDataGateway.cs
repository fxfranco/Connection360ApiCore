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
    /// Implementación de ILogStatusTrackingDataGateway: resuelve ILogStatusTrackingViewRepository
    /// (vía IUnitOfWork.GetRepository&lt;T&gt;(), mismo patrón que ApplicationDataSheetDataGateway y
    /// ClientAccessResolver) y adapta el resultado a un DynamicDataSet, usando
    /// Connection360.Domain.Constans.ExternalDataFields como nombres de campo -los mismos que ya lee
    /// DetailsHistoryShipmentsDomainService sobre un DynamicRecord-, para que cualquier caso de uso
    /// que ya consuma un DynamicDataSet de DATALOGS pueda usar esta fuente sin cambios adicionales.
    /// </summary>
    public class LogStatusTrackingDataGateway : ILogStatusTrackingDataGateway
    {
        private readonly IUnitOfWork _unitOfWork;

        public LogStatusTrackingDataGateway(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Reemplazo directo de IExternalDataGateway.FetchDataAsync("DATALOGS", filters, cancellationToken):
        /// si <paramref name="filters"/> trae ExternalDataFields.DocumentNumber, filtra por ese
        /// documento_transporte_hbl.
        /// </summary>
        public Task<DynamicDataSet> FetchDataAsync(IDictionary<String, String> filters, CancellationToken cancellationToken)
        {
            String? documentoTransporteHbl = filters != null && filters.TryGetValue(ExternalDataFields.DocumentNumber, out var hbl) && !String.IsNullOrWhiteSpace(hbl)
                ? hbl
                : null;

            var request = new LogStatusTrackingDataRequest
            {
                DocumentoTransporteHbl = documentoTransporteHbl
            };

            return FetchDataAsync(request, cancellationToken);
        }

        public async Task<DynamicDataSet> FetchDataAsync(LogStatusTrackingDataRequest request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            ILogStatusTrackingViewRepository repository = _unitOfWork.GetRepository<ILogStatusTrackingViewRepository>();

            List<LogStatusTrackingViewResultDto> rows = await QueryViewAsync(repository, request.DocumentoTransporteHbl, request.FieldsSelection, cancellationToken);

            return ToDynamicDataSet(rows, request.FieldsSelection);
        }

        /// <summary>
        /// Llama al método del repositorio que corresponde según si vino documento_transporte_hbl
        /// y/o selección de campos, para aprovechar los 4 métodos de ILogStatusTrackingViewRepository
        /// (y no traer más datos o columnas de los necesarios).
        /// </summary>
        private static Task<List<LogStatusTrackingViewResultDto>> QueryViewAsync(
            ILogStatusTrackingViewRepository repository,
            String? documentoTransporteHbl,
            LogStatusTrackingViewFieldsSelectionDto? fieldsSelection,
            CancellationToken cancellationToken)
        {
            Boolean hasDocumento = !String.IsNullOrWhiteSpace(documentoTransporteHbl);
            Boolean hasFieldsSelection = fieldsSelection is { Fields.Count: > 0 };

            if (hasDocumento && hasFieldsSelection)
                return repository.GetByDocumentoTransporteHblAsync(documentoTransporteHbl!, fieldsSelection!, cancellationToken);

            if (hasDocumento)
                return repository.GetByDocumentoTransporteHblAsync(documentoTransporteHbl!, cancellationToken);

            if (hasFieldsSelection)
                return repository.GetAllAsync(fieldsSelection!, cancellationToken);

            return repository.GetAllAsync(cancellationToken);
        }

        private static DynamicDataSet ToDynamicDataSet(List<LogStatusTrackingViewResultDto> rows, LogStatusTrackingViewFieldsSelectionDto? fieldsSelection)
        {
            Boolean hasFieldsSelection = fieldsSelection is { Fields.Count: > 0 };

            List<(LogStatusTrackingViewField Field, String ExternalFieldName)> entries = hasFieldsSelection
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
        /// vivo de la API DATALOGS. id_operacion se expone como ExternalDataFields.IdLog porque así se
        /// cargó (ver Connection360.Etl.Domain.Services.LogStatusMappingService) y porque
        /// DetailsHistoryShipmentsDomainService ordena el historial por ese campo.
        /// </summary>
        private static String GetFieldValue(LogStatusTrackingViewResultDto row, LogStatusTrackingViewField field)
        {
            return field switch
            {
                LogStatusTrackingViewField.Id => FormatNumber(row.Id),
                LogStatusTrackingViewField.IdOperacion => FormatNumber(row.IdOperacion),
                LogStatusTrackingViewField.DocumentoTransporteHbl => row.DocumentoTransporteHbl,
                LogStatusTrackingViewField.FechaCambio => FormatDate(row.FechaCambio),
                LogStatusTrackingViewField.UsuarioCambio => row.UsuarioCambio,
                LogStatusTrackingViewField.Mensaje => row.Mensaje,
                LogStatusTrackingViewField.EstadoAnterior => row.EstadoAnterior,
                LogStatusTrackingViewField.NuevoEstado => row.NuevoEstado,
                _ => String.Empty
            };
        }

        private static String FormatDate(DateTime value) => value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        private static String FormatNumber(Int64 value) => value.ToString(CultureInfo.InvariantCulture);

        // Nombre de campo "externo" (ExternalDataFields) para cada columna de la vista, en el mismo
        // orden que LogStatusTrackingViewField / LogStatusTrackingViewColumns.
        private static readonly (LogStatusTrackingViewField Field, String ExternalFieldName)[] _fieldNames =
        {
            (LogStatusTrackingViewField.Id, ExternalDataFields.ID),
            (LogStatusTrackingViewField.IdOperacion, ExternalDataFields.IdLog),
            (LogStatusTrackingViewField.DocumentoTransporteHbl, ExternalDataFields.DocumentNumber),
            (LogStatusTrackingViewField.FechaCambio, ExternalDataFields.ChangeDateLog),
            (LogStatusTrackingViewField.UsuarioCambio, ExternalDataFields.ChangeUserLog),
            (LogStatusTrackingViewField.Mensaje, ExternalDataFields.MessageLog),
            (LogStatusTrackingViewField.EstadoAnterior, ExternalDataFields.OldStateLog),
            (LogStatusTrackingViewField.NuevoEstado, ExternalDataFields.NewStateLog),
        };
    }
}
