namespace Connection360.Domain.Entities
{
    public class ResumenClienteDto
    {
        public Int64 Id { get; set; }
        public String DocumentNumber { get; set; } = String.Empty;
        public String Origin { get; set; } = String.Empty;
        public String Destination { get; set; } = String.Empty;
        public String Status { get; set; } = String.Empty;
        public String OperationType { get; set; } = String.Empty;
        public String ShipmentMode { get; set; } = String.Empty;
    }
}
