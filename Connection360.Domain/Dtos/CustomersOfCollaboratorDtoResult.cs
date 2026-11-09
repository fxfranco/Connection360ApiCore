namespace Connection360.Domain.Dtos
{
    public class CustomersOfCollaboratorDtoResult
    {
        public Int64 IdCollaborator { get; set; }
        public String IdentificacionCollaborator { get; set; } = String.Empty;
        public Int64 IdCustomer { get; set; }
        public String IdentificacionCustomer { get; set; } = String.Empty;
    }
}
