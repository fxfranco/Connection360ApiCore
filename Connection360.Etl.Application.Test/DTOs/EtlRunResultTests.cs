using Connection360.Etl.Application.DTOs;
using FluentAssertions;
using Xunit;

namespace Connection360.Etl.Application.Tests.DTOs
{
    public class EtlRunResultTests
    {
        [Fact]
        public void NuevaInstancia_TieneValoresPorDefecto()
        {
            var result = new EtlRunResult();

            result.StartedAtUtc.Should().Be(default);
            result.FinishedAtUtc.Should().Be(default);
            result.ExtractedRecordsByApi.Should().BeEmpty();
            result.TransformedRecords.Should().Be(0);
            result.LoadedRecords.Should().Be(0);
            result.IsInitialMigrationRun.Should().BeFalse();
            result.StateChangesDetected.Should().Be(0);
            result.CommentChangesDetected.Should().Be(0);
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().BeNull();
            result.Duration.Should().Be(TimeSpan.Zero);
        }

        [Fact]
        public void Duration_EsLaDiferenciaEntreFinYInicio()
        {
            var start = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var result = new EtlRunResult { StartedAtUtc = start, FinishedAtUtc = start.AddSeconds(90) };

            result.Duration.Should().Be(TimeSpan.FromSeconds(90));
        }

        [Fact]
        public void Duration_ConFinAnteriorAlInicio_EsNegativa()
        {
            var start = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var result = new EtlRunResult { StartedAtUtc = start, FinishedAtUtc = start.AddSeconds(-5) };

            result.Duration.Should().Be(TimeSpan.FromSeconds(-5));
        }

        [Fact]
        public void ExtractedRecordsByApi_EsInsensibleAMayusculas()
        {
            var result = new EtlRunResult();
            result.ExtractedRecordsByApi["BPMS"] = 4;

            result.ExtractedRecordsByApi["bpms"].Should().Be(4);
            result.ExtractedRecordsByApi.ContainsKey("Bpms").Should().BeTrue();
        }

        [Fact]
        public void Propiedades_SePuedenAsignarYLeer()
        {
            var result = new EtlRunResult
            {
                TransformedRecords = 3,
                LoadedRecords = 2,
                IsInitialMigrationRun = true,
                StateChangesDetected = 5,
                CommentChangesDetected = 6,
                Success = true,
                ErrorMessage = "x",
            };

            result.TransformedRecords.Should().Be(3);
            result.LoadedRecords.Should().Be(2);
            result.IsInitialMigrationRun.Should().BeTrue();
            result.StateChangesDetected.Should().Be(5);
            result.CommentChangesDetected.Should().Be(6);
            result.Success.Should().BeTrue();
            result.ErrorMessage.Should().Be("x");
        }
    }
}
