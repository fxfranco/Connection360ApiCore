namespace Connection360.Domain.Entities
{
    public class ResumenClienteDto
    {
        public Int64 Id { get; set; }
        public String NroDocumento { get; set; } = String.Empty;
        public String Origen { get; set; } = String.Empty;
        public String Destino { get; set; } = String.Empty;
        public String Estado { get; set; } = String.Empty;
        public String TipoOperacion { get; set; } = String.Empty;
        public String Modalidad { get; set; } = String.Empty;
    }
}
