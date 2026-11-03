using Connection360.Infrastructure.Adapters.Output;
using FluentAssertions;
using System.Net;
using System.Net.Sockets;
using Xunit;

namespace Connection360.Infrastructure.Tests.Adapters
{
    /// <summary>HttpClient de Auth0 con IPv4 primero: orden de direcciones y conexión por loopback (sin salir a internet).</summary>
    public class Auth0HttpClientFactoryTests
    {
        [Fact]
        public void OrderIPv4First_PoneLasDireccionesIPv4AntesQueLasIPv6ConservandoElOrdenDeCadaFamilia()
        {
            IPAddress v6a = IPAddress.Parse("2a06:98c1:310d::6812:2bb6");
            IPAddress v6b = IPAddress.Parse("2a06:98c1:3109::ac40:904a");
            IPAddress v4a = IPAddress.Parse("104.18.43.182");
            IPAddress v4b = IPAddress.Parse("172.64.144.74");

            IReadOnlyList<IPAddress> ordered = Auth0HttpClientFactory.OrderIPv4First(new[] { v6a, v6b, v4a, v4b });

            ordered.Should().Equal(v4a, v4b, v6a, v6b);
        }

        [Fact]
        public void OrderIPv4First_QuitaDireccionesRepetidas()
        {
            IPAddress v4 = IPAddress.Parse("10.0.0.1");

            Auth0HttpClientFactory.OrderIPv4First(new[] { v4, IPAddress.Parse("10.0.0.1") }).Should().ContainSingle();
        }

        [Fact]
        public void OrderIPv4First_ConSoloIPv6_ConservaLasDirecciones()
        {
            IPAddress v6 = IPAddress.Parse("2a06:98c1:310d::6812:2bb6");

            Auth0HttpClientFactory.OrderIPv4First(new[] { v6 }).Should().Equal(v6);
        }

        [Fact]
        public void OrderIPv4First_ConNulo_LanzaArgumentNullException()
        {
            Action act = () => Auth0HttpClientFactory.OrderIPv4First(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task ConnectAsync_ConUnServidorEnIPv4_ConectaYDevuelveUnFlujo()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                Int32 port = ((IPEndPoint)listener.LocalEndpoint).Port;

                await using Stream stream = await Auth0HttpClientFactory.ConnectAsync(new DnsEndPoint("localhost", port), CancellationToken.None);

                stream.Should().NotBeNull();
                stream.CanWrite.Should().BeTrue();
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public async Task ConnectAsync_SiNingunaDireccionAcepta_LanzaHttpRequestExceptionConElUltimoError()
        {
            // Se reserva un puerto libre y se libera: nadie escucha en él.
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            Int32 port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            Func<Task> act = async () => await Auth0HttpClientFactory.ConnectAsync(new DnsEndPoint("127.0.0.1", port), CancellationToken.None);

            (await act.Should().ThrowAsync<HttpRequestException>()).Which.InnerException.Should().NotBeNull();
        }

        [Fact]
        public async Task ConnectAsync_ConTokenCancelado_PropagaLaCancelacion()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Func<Task> act = async () => await Auth0HttpClientFactory.ConnectAsync(new DnsEndPoint("127.0.0.1", 9), cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public void Shared_DevuelveSiempreLaMismaInstancia()
        {
            Auth0HttpClientFactory.Shared.Should().BeSameAs(Auth0HttpClientFactory.Shared);
        }

        [Fact]
        public void PerAddressTimeout_EsMuchoMenorQueElTimeoutTotalDelSdk()
        {
            Auth0HttpClientFactory.PerAddressTimeout.Should().BeLessThan(TimeSpan.FromSeconds(10));
        }
    }
}
