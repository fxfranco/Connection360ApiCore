using Connection360.Domain.Dtos;
using Connection360.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Connection360.Domain.Interfaces
{
    public interface IClientRecordsFilterService
    {
        List<DynamicRecord> Filter(DynamicDataSet dataSet, String clientId, List<CustomersOfCollaboratorDtoResult>? customersOfCollaborator);
    }
}
