using Connection360.Observability.Application.Pipeline;
using FluentAssertions;
using Xunit;

namespace Connection360.Observability.Application.Test.Pipeline
{
    public class TelemetryQueueTests
    {
        [Fact]
        public void TryEnqueue_ConEspacio_EncolaYSePuedeLeer()
        {
            var queue = new TelemetryQueue<Int32>("logs", 5);

            queue.TryEnqueue(7).Should().BeTrue();

            queue.Reader.TryRead(out Int32 item).Should().BeTrue();
            item.Should().Be(7);
            queue.Signal.Should().Be("logs");
        }

        [Fact]
        public void TryEnqueue_ConColaLlena_DescartaSinBloquearYCuentaLosDescartes()
        {
            var queue = new TelemetryQueue<Int32>("metrics", 2);
            queue.TryEnqueue(1);
            queue.TryEnqueue(2);

            queue.TryEnqueue(3).Should().BeFalse();
            queue.TryEnqueue(4).Should().BeFalse();

            queue.Dropped.Should().Be(2);
            queue.Reader.TryRead(out Int32 first).Should().BeTrue();
            first.Should().Be(1); // lo que ya estaba en la cola se conserva
        }

        [Fact]
        public void TryEnqueue_DespuesDeCompletar_DescartaYElLectorTerminaCuandoSeVacia()
        {
            var queue = new TelemetryQueue<String>("traces", 3);
            queue.TryEnqueue("a");
            queue.Complete();

            queue.TryEnqueue("b").Should().BeFalse();

            queue.Reader.TryRead(out String? item).Should().BeTrue();
            item.Should().Be("a");
            queue.Reader.Completion.IsCompleted.Should().BeTrue();
        }

        [Fact]
        public void Constructor_ConCapacidadInvalida_UsaAlMenosUnElemento()
        {
            var queue = new TelemetryQueue<Int32>("logs", 0);

            queue.TryEnqueue(1).Should().BeTrue();
            queue.TryEnqueue(2).Should().BeFalse();
        }

        [Fact]
        public async Task TryEnqueue_ConMuchosProductoresConcurrentes_NuncaLanzaYCuadraLaContabilidad()
        {
            var queue = new TelemetryQueue<Int32>("logs", 100);
            Int32 accepted = 0;

            await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
            {
                for (Int32 i = 0; i < 500; i++)
                {
                    if (queue.TryEnqueue(i)) Interlocked.Increment(ref accepted);
                }
            })));

            accepted.Should().Be(100);
            queue.Dropped.Should().Be(8 * 500 - 100);
        }
    }

    public class TelemetrySuppressionTests
    {
        [Fact]
        public void Begin_MarcaElFlujoComoSuprimidoYAlLiberarloRestauraElEstado()
        {
            TelemetrySuppression.IsSuppressed.Should().BeFalse();

            using (TelemetrySuppression.Begin())
            {
                TelemetrySuppression.IsSuppressed.Should().BeTrue();
            }

            TelemetrySuppression.IsSuppressed.Should().BeFalse();
        }

        [Fact]
        public async Task Begin_SePropagaALasContinuacionesAsincronas()
        {
            using (TelemetrySuppression.Begin())
            {
                await Task.Delay(1);
                TelemetrySuppression.IsSuppressed.Should().BeTrue();
            }
        }

        [Fact]
        public void Begin_Anidado_RestauraElValorAnterior()
        {
            using (TelemetrySuppression.Begin())
            {
                using (TelemetrySuppression.Begin()) { }
                TelemetrySuppression.IsSuppressed.Should().BeTrue();
            }
        }
    }
}
