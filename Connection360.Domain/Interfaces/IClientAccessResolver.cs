using Connection360.Domain.Dtos;

namespace Connection360.Domain.Interfaces
{
    public interface IClientAccessResolver
    {
        Task<List<CustomersOfCollaboratorDtoResult>> ResolveAsync(ResolveClientAccessRequest request);
    }
}
