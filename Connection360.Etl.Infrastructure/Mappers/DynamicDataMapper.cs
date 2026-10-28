using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Infrastructure.ExternalApi.DTOs;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Connection360.Etl.Infrastructure.Mappers
{
    /// <summary>
    /// Convierte la respuesta cruda de una API externa (<see cref="ExternalApiResponseDto"/>) en el
    /// <see cref="DynamicDataSet"/> de dominio. Copiado de
    /// Connection360.Infrastructure.Mappers.DynamicDataMapper.
    /// </summary>
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
