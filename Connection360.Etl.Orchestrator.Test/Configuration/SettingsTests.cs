using Connection360.Etl.Orchestrator.Configuration;
using Connection360.Etl.Orchestrator.Execution;
using FluentAssertions;
using Xunit;

namespace Connection360.Etl.Orchestrator.Test.Configuration
{
    public class SettingsTests
    {
        [Fact]
        public void OrchestratorSettings_SectionName_EsOrchestrator()
        {
            OrchestratorSettings.SectionName.Should().Be("Orchestrator");
        }

        [Fact]
        public void OrchestratorSettings_PorDefecto_NoTieneTrabajos()
        {
            var settings = new OrchestratorSettings();

            settings.CronJobs.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void CronJobSettings_PorDefecto_QuedaHabilitadoYUsaDotnet()
        {
            var job = new CronJobSettings();

            job.Enabled.Should().BeTrue();
            job.ExecutablePath.Should().Be("dotnet");
            job.TimeoutMinutes.Should().BeNull();
            job.TimeZoneId.Should().BeNull();
        }

        [Fact]
        public void CronJobSettings_AsignarPropiedades_ConservaLosValores()
        {
            var job = new CronJobSettings
            {
                Name = "n",
                Enabled = false,
                CronExpression = "* * * * *",
                TimeZoneId = "UTC",
                ExecutablePath = "exe",
                Arguments = "a b",
                WorkingDirectory = "dir",
                TimeoutMinutes = 7,
            };

            job.Name.Should().Be("n");
            job.Enabled.Should().BeFalse();
            job.CronExpression.Should().Be("* * * * *");
            job.TimeZoneId.Should().Be("UTC");
            job.ExecutablePath.Should().Be("exe");
            job.Arguments.Should().Be("a b");
            job.WorkingDirectory.Should().Be("dir");
            job.TimeoutMinutes.Should().Be(7);
        }

        [Fact]
        public void ProcessRunResult_Registro_ExponeValoresYEqualidadPorValor()
        {
            var a = new ProcessRunResult(true, 0, TimeSpan.FromSeconds(3), null);
            var b = new ProcessRunResult(true, 0, TimeSpan.FromSeconds(3), null);
            var c = a with { Success = false, ExitCode = 2, ErrorMessage = "x" };

            a.Should().Be(b);
            a.Success.Should().BeTrue();
            a.ExitCode.Should().Be(0);
            a.Duration.Should().Be(TimeSpan.FromSeconds(3));
            a.ErrorMessage.Should().BeNull();
            c.Should().NotBe(a);
            c.ExitCode.Should().Be(2);
            c.ErrorMessage.Should().Be("x");
        }
    }
}
