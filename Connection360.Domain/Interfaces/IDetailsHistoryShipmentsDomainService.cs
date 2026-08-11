using Connection360.Domain.Dtos;
using Connection360.Domain.Entities;

namespace Connection360.Domain.Interfaces
{
    public interface IDetailsHistoryShipmentsDomainService
    {
        HistoryShipmentsDomainDtoResult GetDetailsHistoryShipments(DynamicDataSet dataSet, String DocumentNumber);
    }
}
