namespace Connection360.Domain.Entities
{
    /// <summary>
    /// POR AHORA NO SE ESTA USANDO, PERO SE DEJA A FUTURO
    /// </summary>
    public static class DynamicRecordExtensions
    {
        public static ResumenClienteDto ToResumenClienteDto(this DynamicRecord record)
        {
            return new ResumenClienteDto
            {
                // Conversión a int segura
                Id = int.TryParse(record["ID"], out int idVal) ? idVal : 0
                //,

                //// Cadenas directas
                //NitCliente = record["NIT CLIENTE"],
                //Estado = record["ESTADO"],
                //Documento = record["DOCUMENTO"],

                //// Conversión a DateTime segura
                //FechaRegistro = DateTime.TryParse(record["FECHA"], out DateTime fechaVal)
                //    ? fechaVal
                //    : DateTime.MinValue
            };
        }
    }
}
