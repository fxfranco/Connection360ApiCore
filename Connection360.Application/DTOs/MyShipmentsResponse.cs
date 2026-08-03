using Connection360.Domain.Entities;
using Connection360.Domain.Services;

namespace Connection360.Application.DTOs
{
    public class MyShipmentsResponse
    {
        public ClientSummaryResponse ClientSummaryResponseData { get; set; } = new();
    }
}
