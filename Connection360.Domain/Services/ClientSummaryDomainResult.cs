using Connection360.Domain.Entities;

namespace Connection360.Domain.Services
{
    public class ClientSummaryDomainResult
    {
        public Int64 TotalRegistros { get; init; }
        public Int64 TotalImportaciones { get; init; }
        public Int64 TotalExportaciones { get; init; }
        public Int64 TotalModalidadAerea { get; init; }
        public Int64 TotalModalidadMaritima { get; init; }
        public Int64 TotalConNovedad { get; init; }
        public List<ResumenClienteDto> EnviosRecientes { get; init; } = new List<ResumenClienteDto>();
    }
}
