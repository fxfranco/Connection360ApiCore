using Connection360.Domain.Dtos;

namespace Connection360.Application.DTOs
{
    /// <summary>
    /// Parámetros de consulta para ILogStatusTrackingDataGateway.FetchDataAsync(LogStatusTrackingDataRequest, CancellationToken):
    /// un filtro opcional por documento_transporte_hbl y una selección opcional de columnas (ver
    /// <see cref="LogStatusTrackingViewFieldsSelectionDto"/>). Dejar
    /// <see cref="DocumentoTransporteHbl"/> vacío/nulo trae todas las filas; dejar
    /// <see cref="FieldsSelection"/> en null trae todas las columnas.
    /// </summary>
    public class LogStatusTrackingDataRequest
    {
        public String? DocumentoTransporteHbl { get; set; }

        public LogStatusTrackingViewFieldsSelectionDto? FieldsSelection { get; set; }
    }
}
