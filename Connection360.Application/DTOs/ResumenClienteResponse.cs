namespace Connection360.Application.DTOs
{
    public class ResumenClienteResponse
    {
        public Int64 Id { get; set; }
        public String ClientNit { get; set; } = String.Empty;
        public String DocumentNumber { get; set; } = String.Empty;
        public String Origin { get; set; } = String.Empty;
        public String Destination { get; set; } = String.Empty;
        public String Status { get; set; } = String.Empty;
        public String OperationType { get; set; } = String.Empty;
        public String ShipmentMode { get; set; } = String.Empty;
    }
}
