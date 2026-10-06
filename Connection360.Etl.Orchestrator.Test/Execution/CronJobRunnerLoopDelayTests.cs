using Connection360.Etl.Orchestrator.Execution;
using FluentAssertions;
using Xunit;

namespace Connection360.Etl.Orchestrator.Test.Execution
{
    public class CronJobRunnerLoopDelayTests
    {
        [Fact]
        public void MaxDelayChunk_EsMenorAlMaximoQueAdmiteTaskDelay()
        {
            // Task.Delay no admite más de UInt32.MaxValue - 1 ms (~49.7 días).
            CronJobRunnerLoop.MaxDelayChunk.TotalMilliseconds.Should().BeLessThan(UInt32.MaxValue - 1);
            CronJobRunnerLoop.MaxDelayChunk.Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task DelayAsync_ConEsperaNulaONegativa_TerminaDeInmediato(Int32 milliseconds)
        {
            Func<Task> act = () => CronJobRunnerLoop.DelayAsync(TimeSpan.FromMilliseconds(milliseconds), CancellationToken.None);

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task DelayAsync_ConEsperaCorta_EsperaYTermina()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            await CronJobRunnerLoop.DelayAsync(TimeSpan.FromMilliseconds(60), CancellationToken.None);

            watch.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(40));
        }

        [Fact]
        public async Task DelayAsync_ConEsperaMayorAlMaximoDeTaskDelay_NoLanzaArgumentOutOfRange_YSeCancela()
        {
            // Un año: antes de la corrección, Task.Delay(365 días) lanzaba ArgumentOutOfRangeException.
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

            Func<Task> act = () => CronJobRunnerLoop.DelayAsync(TimeSpan.FromDays(365), cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task DelayAsync_ConTokenYaCancelado_LanzaOperationCanceledException()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Func<Task> act = () => CronJobRunnerLoop.DelayAsync(TimeSpan.FromDays(1), cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}
