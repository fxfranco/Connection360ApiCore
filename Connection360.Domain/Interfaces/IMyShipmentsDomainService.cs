using Connection360.Domain.Dtos;
using Connection360.Domain.Entities;
using Connection360.Domain.Services;

namespace Connection360.Domain.Interfaces
{
    public interface IMyShipmentsDomainService
    {
        MyShipmentsDomainResult GetAllShipments(DynamicDataSet dataSet, String clientId, Int64 page, Int64 size);
        MyShipmentsDomainResult GetFiltersShipments(DynamicDataSet dataSet, String clientId, Int64 page, Int64 size, MyShipmentsFiltersDto filters);
    }
}
