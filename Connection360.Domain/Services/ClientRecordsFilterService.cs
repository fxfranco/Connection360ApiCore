using Connection360.Domain.Constans;
using Connection360.Domain.Dtos;
using Connection360.Domain.Entities;
using Connection360.Domain.Interfaces;

namespace Connection360.Domain.Services
{
    public class ClientRecordsFilterService : IClientRecordsFilterService
    {
        public List<DynamicRecord> Filter(DynamicDataSet dataSet, String clientId, List<CustomersOfCollaboratorDtoResult>? customersOfCollaborator)
        {
            if (String.IsNullOrEmpty(clientId))
                return dataSet.Rows.ToList();

            HashSet<String> customerIdsSet = BuildCustomerIdsSet(clientId, customersOfCollaborator);

            return dataSet.Rows
                .Where(r => customerIdsSet.Contains(r[ExternalDataFields.ClientNit]))
                .ToList();
        }

        private static HashSet<String> BuildCustomerIdsSet(String clientId, List<CustomersOfCollaboratorDtoResult>? customersOfCollaborator)
        {
            var customerIdsSet = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

            if (customersOfCollaborator is { Count: > 0 })
            {
                customerIdsSet.UnionWith(customersOfCollaborator.Select(o => o.IdentificacionCustomer));
            }
            else
            {
                customerIdsSet.Add(clientId);
            }

            return customerIdsSet;
        }
    }
}
