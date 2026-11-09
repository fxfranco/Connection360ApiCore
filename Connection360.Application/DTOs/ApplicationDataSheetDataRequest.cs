using Connection360.Domain.Dtos;
using Connection360.Domain.Enums;

namespace Connection360.Application.DTOs
{
    /// <summary>
    /// Parámetros de consulta para IApplicationDataSheetDataGateway.FetchDataAsync(ApplicationDataSheetDataRequest, CancellationToken):
    /// qué vista(s) consultar (<see cref="Scope"/>), un filtro opcional por nit_cliente, y una
    /// selección opcional de columnas (ver <see cref="ApplicationDataSheetViewFieldsSelectionDto"/>).
    /// Dejar <see cref="NitCliente"/> vacío/nulo trae todas las filas; dejar
    /// <see cref="FieldsSelection"/> en null trae todas las columnas.
    /// </summary>
    public class ApplicationDataSheetDataRequest
    {
        public ApplicationDataSheetViewScope Scope { get; set; } = ApplicationDataSheetViewScope.Todos;

        public String? NitCliente { get; set; }

        public ApplicationDataSheetViewFieldsSelectionDto? FieldsSelection { get; set; }
    }
}
