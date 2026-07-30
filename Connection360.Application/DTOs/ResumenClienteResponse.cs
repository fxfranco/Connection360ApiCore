namespace Connection360.Application.DTOs
{
    public class ResumenClienteResponse
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
