using Connection360.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Connection360.Domain.Tests.Entities
{
    public class DynamicRecordExtensionsTests
    {
        [Fact]
        public void ToResumenClienteDto_ConIdValidoNumerico_MapeaElId()
        {
            var record = new DynamicRecord(new Dictionary<String, String> { ["ID"] = "42" });

            ResumenClienteDto dto = record.ToResumenClienteDto();

            dto.Id.Should().Be(42);
        }

        [Fact]
        public void ToResumenClienteDto_ConIdNoNumerico_MapeaCero()
        {
            var record = new DynamicRecord(new Dictionary<String, String> { ["ID"] = "no-numerico" });

            ResumenClienteDto dto = record.ToResumenClienteDto();

            dto.Id.Should().Be(0);
        }

        [Fact]
        public void ToResumenClienteDto_SinCampoId_MapeaCero()
        {
            var record = new DynamicRecord(new Dictionary<String, String>());

            ResumenClienteDto dto = record.ToResumenClienteDto();

            dto.Id.Should().Be(0);
        }
    }
}
