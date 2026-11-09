using Connection360.Domain.Dtos;
using Connection360.Domain.Enums;
using Connection360.Domain.Interfaces;
using Connection360.Domain.Ports.Persistence;

namespace Connection360.Application.Services
{
    public class ClientAccessResolver : IClientAccessResolver
    {
        private readonly IUnitOfWork _unitOfWork;

        private static readonly HashSet<String> RolesConColaboradores =
            new HashSet<String>(StringComparer.OrdinalIgnoreCase) { UserRoleApplication.ANALISTAOPE.ToString(), UserRoleApplication.ANALISTASAC.ToString() };


        public ClientAccessResolver(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<CustomersOfCollaboratorDtoResult>> ResolveAsync(ResolveClientAccessRequest request)
        {
            if (String.IsNullOrWhiteSpace(request.IdClient))
                throw new ArgumentException("El campo 'cliente' es obligatorio.");

            if (!RolesConColaboradores.Contains(request.RoleName))
                return new List<CustomersOfCollaboratorDtoResult>();

            var customersByCollaborators = await this.GetCustomersByCollaboratorAsync(request.IdClient);

            if (!request.AllClient)
            {
                customersByCollaborators = customersByCollaborators
                    .Where(x => x.IdentificacionCustomer == request.IdQueryClient)
                    .Select(p => new CustomersOfCollaboratorDtoResult
                    {
                        IdCollaborator = p.IdCollaborator,
                        IdCustomer = p.IdCustomer,
                        IdentificacionCollaborator = p.IdentificacionCollaborator,
                        IdentificacionCustomer = p.IdentificacionCustomer
                    })
                    .ToList();
            }

            if (customersByCollaborators.Count == 0)
                throw new ArgumentException("No hay clientes asignados.");

            return customersByCollaborators;
        }

        private async Task<List<CustomersOfCollaboratorDtoResult>> GetCustomersByCollaboratorAsync(String collaboratorId, CancellationToken cancellationToken = default)
        {
            // Se resuelve el repositorio de productos SOLAMENTE si se invoca esta línea
            ICustomersOfCollaboratorsRepository customersOfCollaboratorsRepository = _unitOfWork.GetRepository<ICustomersOfCollaboratorsRepository>();

            List<CustomersOfCollaboratorDtoResult> customersOfCollaboratorDtoResult = await customersOfCollaboratorsRepository.ListCustomersByCollaboratorAsync(collaboratorId, cancellationToken);

            return customersOfCollaboratorDtoResult;
        }
    }
}
