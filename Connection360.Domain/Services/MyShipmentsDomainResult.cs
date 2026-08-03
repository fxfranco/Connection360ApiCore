using Connection360.Domain.Entities;

namespace Connection360.Domain.Services
{
    public class MyShipmentsDomainResult
    {
        public List<ResumenMyShipmentDto> MyShipments { get; init; } = new List<ResumenMyShipmentDto>();
        public ClientSummaryDomainResult ClientSummaryResponse { get; set; } = new();
    }
}
