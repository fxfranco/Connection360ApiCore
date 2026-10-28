using Connection360.Domain.Enums;

namespace Connection360.Domain.Dtos
{
    /// <summary>
    /// Objeto parámetro para pedirle a ILogStatusTrackingViewRepository que el SELECT solo incluya
    /// un subconjunto de columnas de la vista en lugar de la fila completa. Debe traer al menos un
    /// campo en <see cref="Fields"/>.
    /// </summary>
    public class LogStatusTrackingViewFieldsSelectionDto
    {
        public List<LogStatusTrackingViewField> Fields { get; set; } = new();
    }
}
