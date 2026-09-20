using Connection360.Domain.Entities;
using Connection360.Infrastructure.ExternalApi.DTOs;
using Connection360.Infrastructure.Mappers;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace Connection360.Infrastructure.Tests.Mappers
{
    public class DynamicDataMapperTests
    {
        private static JsonElement ToJsonElement(Object value)
        {
            var json = JsonSerializer.Serialize(value);
            return JsonSerializer.Deserialize<JsonElement>(json);
        }

        [Fact]
        public void ToDomainDataSet_MapeaLosCamposSolicitadosComoAvailableFields()
        {
            var dto = new ExternalApiResponseDto
            {
                RequestedFields = new List<String> { "ID", "NOMBRE" },
                Rows = new List<Dictionary<String, Object?>>()
            };

            DynamicDataSet result = dto.ToDomainDataSet();

            result.AvailableFields.Should().BeEquivalentTo(dto.RequestedFields);
        }

        [Fact]
        public void ToDomainDataSet_ConValoresJsonElementDeTexto_ConvierteAStringCorrectamente()
        {
            var dto = new ExternalApiResponseDto
            {
                RequestedFields = new List<String> { "NOMBRE" },
                Rows = new List<Dictionary<String, Object?>>
                {
                    new() { ["NOMBRE"] = ToJsonElement("Juan") }
                }
            };

            DynamicDataSet result = dto.ToDomainDataSet();

            result.Rows.Single()["NOMBRE"].Should().Be("Juan");
        }

        [Fact]
        public void ToDomainDataSet_ConValorJsonNull_MapeaCadenaVacia()
        {
            var element = JsonSerializer.Deserialize<JsonElement>("null");
            var dto = new ExternalApiResponseDto
            {
                RequestedFields = new List<String> { "CAMPO" },
                Rows = new List<Dictionary<String, Object?>>
                {
                    new() { ["CAMPO"] = element }
                }
            };

            DynamicDataSet result = dto.ToDomainDataSet();

            result.Rows.Single()["CAMPO"].Should().Be(String.Empty);
        }

        [Fact]
        public void ToDomainDataSet_ConCampoAusenteEnLaFila_MapeaCadenaVacia()
        {
            var dto = new ExternalApiResponseDto
            {
                RequestedFields = new List<String> { "CAMPO_AUSENTE" },
                Rows = new List<Dictionary<String, Object?>> { new() }
            };

            DynamicDataSet result = dto.ToDomainDataSet();

            result.Rows.Single()["CAMPO_AUSENTE"].Should().Be(String.Empty);
        }

        [Fact]
        public void ToDomainDataSet_ConValorNoJsonElement_UsaToStringDirectamente()
        {
            var dto = new ExternalApiResponseDto
            {
                RequestedFields = new List<String> { "NUMERO" },
                Rows = new List<Dictionary<String, Object?>>
                {
                    new() { ["NUMERO"] = 42 }
                }
            };

            DynamicDataSet result = dto.ToDomainDataSet();

            result.Rows.Single()["NUMERO"].Should().Be("42");
        }

        [Fact]
        public void ToDomainDataSet_ConMultiplesFilas_GeneraUnRegistroPorCadaUna()
        {
            var dto = new ExternalApiResponseDto
            {
                RequestedFields = new List<String> { "ID" },
                Rows = new List<Dictionary<String, Object?>>
                {
                    new() { ["ID"] = ToJsonElement("1") },
                    new() { ["ID"] = ToJsonElement("2") }
                }
            };

            DynamicDataSet result = dto.ToDomainDataSet();

            result.Rows.Should().HaveCount(2);
        }

        [Fact]
        public void ToDomainDataSet_SinFilas_RetornaDataSetVacio()
        {
            var dto = new ExternalApiResponseDto { RequestedFields = new List<String> { "ID" }, Rows = new List<Dictionary<String, Object?>>() };

            DynamicDataSet result = dto.ToDomainDataSet();

            result.Rows.Should().BeEmpty();
        }
    }
}
