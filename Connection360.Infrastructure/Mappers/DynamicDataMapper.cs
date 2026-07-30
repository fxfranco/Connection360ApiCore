using Connection360.Domain.Entities;
using Connection360.Infrastructure.ExternalApi.DTOs;
using System.Text.Json;

namespace Connection360.Infrastructure.Mappers
{
    public static class DynamicDataMapper
    {
        public static DynamicDataSet ToDomainDataSet(this ExternalApiResponseDto dto)
        {
            var domainRows = new List<DynamicRecord>();

            foreach (var rowDict in dto.Rows)
            {
                var fields = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);

                // Iteramos sobre los campos requeridos para construir la fila
                foreach (var fieldName in dto.RequestedFields)
                {
                    if (rowDict.TryGetValue(fieldName, out var rawVal) && rawVal is JsonElement element)
                    {
                        fields[fieldName] = element.ValueKind switch
                        {
                            JsonValueKind.Null => String.Empty,
                            _ => element.ToString() ?? String.Empty
                        };
                    }
                    else
                    {
                        fields[fieldName] = rawVal?.ToString() ?? String.Empty;
                    }
                }

                domainRows.Add(new DynamicRecord(fields));
            }

            return new DynamicDataSet(dto.RequestedFields, domainRows);
        }
    }
}
