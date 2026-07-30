using Connection360.Domain.Entities;
using Connection360.Domain.Services;

namespace Connection360.Domain.Interfaces
{
    public interface IClientSummaryDomainService
    {
        ClientSummaryDomainResult Summarize(DynamicDataSet dataSet, String clientId, Int16 lastRecordsCount);
        ResumenClienteDto Filter(DynamicDataSet dataSet, String clientId, String filterDocument);
    }
}
