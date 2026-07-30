using Connection360.Domain.Entities;

namespace Connection360.Application.Ports
{
    // Puerto secundario (saliente): lo implementa Infrastructure
    public interface IExternalDataGateway
    {
        Task<DynamicDataSet> FetchDataAsync(IDictionary<String, String> filters, CancellationToken cancellationToken);
    }
}
