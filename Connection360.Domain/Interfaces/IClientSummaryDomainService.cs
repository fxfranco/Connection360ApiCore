using Connection360.Domain.Dtos;
using Connection360.Domain.Entities;
using Connection360.Domain.Services;

namespace Connection360.Domain.Interfaces
{
    public interface IClientSummaryDomainService
    {
        ClientSummaryDomainResult Summarize(DynamicDataSet dataSet, String clientId, List<CustomersOfCollaboratorDtoResult>? customersOfCollaborator, Int16 lastRecordsCount);
        ResumenClienteDto Filter(DynamicDataSet dataSet, String clientId, List<CustomersOfCollaboratorDtoResult>? customersOfCollaborator, String filterDocument);
    }
}
