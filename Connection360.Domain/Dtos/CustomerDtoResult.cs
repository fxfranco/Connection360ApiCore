namespace Connection360.Domain.Dtos
{
    /// <summary>Cliente almacenado en connection360write.customers (solo su identificación).</summary>
    public class CustomerDtoResult
    {
        public String Identificacion { get; set; } = String.Empty;
    }
}
