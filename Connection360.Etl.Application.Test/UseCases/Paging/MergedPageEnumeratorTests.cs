using Connection360.Etl.Application.Tests.Support;
using Connection360.Etl.Application.UseCases.Paging;
using Connection360.Etl.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Connection360.Etl.Application.Tests.UseCases.Paging
{
    public class MergedPageEnumeratorTests
    {
        /// <summary>Fuente asíncrona que registra el token recibido y si el enumerador fue liberado.</summary>
        private sealed class TrackingSource : IAsyncEnumerable<DynamicDataSet>
        {
            private readonly DynamicDataSet[] _pages;
            public Int32 EnumeratorsCreated { get; private set; }
            public Int32 Disposed { get; private set; }
            public Int32 MoveNextCalls { get; private set; }
            public CancellationToken ReceivedToken { get; private set; }

            public TrackingSource(params DynamicDataSet[] pages) => _pages = pages;

            public IAsyncEnumerator<DynamicDataSet> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                EnumeratorsCreated++;
                ReceivedToken = cancellationToken;
                return new Enumerator(this);
            }

            private sealed class Enumerator : IAsyncEnumerator<DynamicDataSet>
            {
                private readonly TrackingSource _owner;
                private Int32 _index = -1;

                public Enumerator(TrackingSource owner) => _owner = owner;

                public DynamicDataSet Current => _owner._pages[_index];

                public ValueTask<Boolean> MoveNextAsync()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return new ValueTask<Boolean>(_index < _owner._pages.Length);
                }

                public ValueTask DisposeAsync()
                {
                    _owner.Disposed++;
                    return ValueTask.CompletedTask;
                }
            }
        }

        private static (String, IAsyncEnumerable<DynamicDataSet>) Source(String api, params DynamicDataSet[] pages)
            => (api, AsyncSequence.From(pages));

        [Fact]
        public async Task MoveNextRoundAsync_SinFuentes_RetornaNullDeInmediato()
        {
            await using var sut = new MergedPageEnumerator(Array.Empty<(String, IAsyncEnumerable<DynamicDataSet>)>(), CancellationToken.None);

            var round = await sut.MoveNextRoundAsync();

            round.Should().BeNull();
        }

        [Fact]
        public async Task MoveNextRoundAsync_UnaFuenteConUnaPagina_EntregaLaPaginaYLuegoUnaRondaVaciaAntesDeTerminar()
        {
            var page = DataSets.WithDocuments("A");
            await using var sut = new MergedPageEnumerator(new[] { Source("BPMS", page) }, CancellationToken.None);

            var first = await sut.MoveNextRoundAsync();
            var second = await sut.MoveNextRoundAsync();
            var third = await sut.MoveNextRoundAsync();

            first.Should().NotBeNull();
            first!["BPMS"].Should().BeSameAs(page);
            second.Should().NotBeNull("la ronda en que la fuente se descubre agotada todavía se reporta");
            second!["BPMS"].Should().BeSameAs(DynamicDataSet.Empty);
            third.Should().BeNull();
        }

        [Fact]
        public async Task MoveNextRoundAsync_FuentesDeDistintaLongitud_RellenaConEmptyLasAgotadas()
        {
            var a1 = DataSets.WithDocuments("A1");
            var a2 = DataSets.WithDocuments("A2");
            var a3 = DataSets.WithDocuments("A3");
            var b1 = DataSets.WithDocuments("B1");
            await using var sut = new MergedPageEnumerator(new[] { Source("A", a1, a2, a3), Source("B", b1) }, CancellationToken.None);

            var r1 = await sut.MoveNextRoundAsync();
            var r2 = await sut.MoveNextRoundAsync();
            var r3 = await sut.MoveNextRoundAsync();
            var r4 = await sut.MoveNextRoundAsync();
            var r5 = await sut.MoveNextRoundAsync();

            r1!["A"].Should().BeSameAs(a1);
            r1["B"].Should().BeSameAs(b1);
            r2!["A"].Should().BeSameAs(a2);
            r2["B"].Should().BeSameAs(DynamicDataSet.Empty);
            r3!["A"].Should().BeSameAs(a3);
            r3["B"].Should().BeSameAs(DynamicDataSet.Empty);
            r4!["A"].Should().BeSameAs(DynamicDataSet.Empty);
            r4["B"].Should().BeSameAs(DynamicDataSet.Empty);
            r5.Should().BeNull();
        }

        [Fact]
        public async Task MoveNextRoundAsync_FuenteExhaustaNoVuelveAConsultarseEnRondasPosteriores()
        {
            var shortSource = new TrackingSource(DataSets.WithDocuments("B1"));
            var longSource = new TrackingSource(DataSets.WithDocuments("A1"), DataSets.WithDocuments("A2"), DataSets.WithDocuments("A3"));
            await using var sut = new MergedPageEnumerator(new (String, IAsyncEnumerable<DynamicDataSet>)[] { ("LONG", longSource), ("SHORT", shortSource) }, CancellationToken.None);

            while (await sut.MoveNextRoundAsync() is not null) { }

            shortSource.MoveNextCalls.Should().Be(2, "una llamada con página y otra que descubre el agotamiento");
            longSource.MoveNextCalls.Should().Be(4);
        }

        [Fact]
        public async Task MoveNextRoundAsync_FuenteSinPaginas_EntregaEmptyEnLaPrimeraRondaYLuegoTermina()
        {
            await using var sut = new MergedPageEnumerator(new[] { Source("VACIA") }, CancellationToken.None);

            var first = await sut.MoveNextRoundAsync();
            var second = await sut.MoveNextRoundAsync();

            first.Should().NotBeNull();
            first!["VACIA"].Should().BeSameAs(DynamicDataSet.Empty);
            second.Should().BeNull();
        }

        [Fact]
        public async Task MoveNextRoundAsync_ClavesDelResultado_SonInsensiblesAMayusculas()
        {
            await using var sut = new MergedPageEnumerator(new[] { Source("Bpms", DataSets.WithDocuments("A")) }, CancellationToken.None);

            var round = await sut.MoveNextRoundAsync();

            round!.ContainsKey("BPMS").Should().BeTrue();
            round.ContainsKey("bpms").Should().BeTrue();
        }

        [Fact]
        public async Task MoveNextRoundAsync_ContieneUnaEntradaPorCadaFuenteEnCadaRonda()
        {
            await using var sut = new MergedPageEnumerator(new[]
            {
                Source("A", DataSets.WithDocuments("1")),
                Source("B", DataSets.WithDocuments("2")),
                Source("C"),
            }, CancellationToken.None);

            var round = await sut.MoveNextRoundAsync();

            round!.Keys.Should().BeEquivalentTo(new[] { "A", "B", "C" });
        }

        [Fact]
        public async Task MoveNextRoundAsync_UnaFuenteLanzaExcepcion_LaPropaga()
        {
            var failing = AsyncSequence.Throwing<DynamicDataSet>(new InvalidOperationException("fuente rota"));
            await using var sut = new MergedPageEnumerator(new[] { ("ROTA", failing) }, CancellationToken.None);

            Func<Task> act = () => sut.MoveNextRoundAsync();

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("fuente rota");
        }

        [Fact]
        public async Task Constructor_ObtieneElEnumeradorConElTokenSinAvanzarLaFuente()
        {
            using var cts = new CancellationTokenSource();
            var source = new TrackingSource(DataSets.WithDocuments("A"));

            await using var sut = new MergedPageEnumerator(new (String, IAsyncEnumerable<DynamicDataSet>)[] { ("A", source) }, cts.Token);

            source.EnumeratorsCreated.Should().Be(1);
            source.ReceivedToken.Should().Be(cts.Token);
            source.MoveNextCalls.Should().Be(0, "las páginas son perezosas");
        }

        [Fact]
        public async Task DisposeAsync_LiberaElEnumeradorDeCadaFuente()
        {
            var a = new TrackingSource(DataSets.WithDocuments("A"));
            var b = new TrackingSource();
            var sut = new MergedPageEnumerator(new (String, IAsyncEnumerable<DynamicDataSet>)[] { ("A", a), ("B", b) }, CancellationToken.None);

            await sut.DisposeAsync();

            a.Disposed.Should().Be(1);
            b.Disposed.Should().Be(1);
        }

        [Fact]
        public async Task DisposeAsync_SinFuentes_NoLanza()
        {
            var sut = new MergedPageEnumerator(Array.Empty<(String, IAsyncEnumerable<DynamicDataSet>)>(), CancellationToken.None);

            Func<Task> act = async () => await sut.DisposeAsync();

            await act.Should().NotThrowAsync();
        }
    }
}
