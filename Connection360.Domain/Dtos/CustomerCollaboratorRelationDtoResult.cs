namespace Connection360.Domain.Dtos
{
    /// <summary>
    /// Relación cliente - colaborador de connection360write.customers_of_collaborators, expresada con la
    /// identificación de cada lado (customers.identificacion y collaborators.identificacion).
    /// </summary>
    public class CustomerCollaboratorRelationDtoResult
    {
        public String IdentificacionCustomer { get; set; } = String.Empty;
        public String IdentificacionCollaborator { get; set; } = String.Empty;
    }
}
