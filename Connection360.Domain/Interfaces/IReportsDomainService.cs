using Connection360.Domain.Dtos;
using Connection360.Domain.Entities;

namespace Connection360.Domain.Interfaces
{
    public interface IReportsDomainService
    {
        ReportsSummaryDomainDtoResult Summarize(DynamicDataSet dataSet, String clientId, Int16 frequentRoutesCount);
    }
}
