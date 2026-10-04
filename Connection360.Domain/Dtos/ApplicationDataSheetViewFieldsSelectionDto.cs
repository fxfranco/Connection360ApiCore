using Connection360.Domain.Enums;

namespace Connection360.Domain.Dtos
{
    /// <summary>
    /// Objeto parámetro para pedirle a IApplicationDataSheetEntregadosRepository /
    /// IApplicationDataSheetNoEntregadosRepository que el SELECT solo incluya un subconjunto de
    /// columnas de la vista en lugar de la fila completa (más de 50 columnas). Debe traer al menos
    /// un campo en <see cref="Fields"/>.
    /// </summary>
    public class ApplicationDataSheetViewFieldsSelectionDto
    {
        public List<ApplicationDataSheetViewField> Fields { get; set; } = new();
    }
}
