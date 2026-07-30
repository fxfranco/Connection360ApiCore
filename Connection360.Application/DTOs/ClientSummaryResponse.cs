using Connection360.Domain.Entities;

namespace Connection360.Application.DTOs
{
    public class ClientSummaryResponse
    {
        public Int64 TotalRegistros { get; set; }
        public Int64 TotalImportaciones { get; set; }
        public Int64 TotalExportaciones { get; set; }
        public Int64 TotalModalidadAerea { get; init; }
        public Int64 TotalModalidadMaritima { get; init; }
        public Int64 TotalConNovedad { get; init; }
        public List<ResumenClienteResponse> EnviosRecientes { get; set; } = new();
    }
}
