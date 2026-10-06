using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Infrastructure.ExternalApi;
using Connection360.Etl.Infrastructure.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Connection360.Etl.Infrastructure.Tests.ExternalApi
{
    public class ExternalDataApiGatewayTests
    {
        private const String Endpoint = "https://api.test/data";

        private static String Json(Int32 rows, String idPrefix = "R")
        {
            var rowList = Enumerable.Range(1, rows).Select(i => new Dictionary<String, Object?> { ["ID"] = $"{idPrefix}{i}", ["NOMBRE"] = $"Nombre {i}" });
            return JsonSerializer.Serialize(new
            {
                requestedFields = new[] { "ID", "NOMBRE" },
                missingColumns = Array.Empty<String>(),
                rows = rowList,
            });
        }

        private static ExternalApiSettings Settings(Boolean paginated = false, Int32 pageSize = 0, String apiName = "BPMS")
        {
            var settings = new ExternalApiSettings { PaginationEnabled = paginated, PageSize = pageSize };
            settings.Apis[apiName] = new ExternalApisDetail { BaseUrl = "https://api.test", DataEndpoint = Endpoint };
            return settings;
        }

        private static (ExternalDataApiGateway Gateway, FakeHttpClientFactory Factory, ListLogger<ExternalDataApiGateway> Logger) Create(
            FakeHttpMessageHandler handler, ExternalApiSettings settings)
        {
            var factory = new FakeHttpClientFactory(handler);
            var logger = new ListLogger<ExternalDataApiGateway>();
            return (new ExternalDataApiGateway(factory, Options.Create(settings), logger), factory, logger);
        }

        private static async Task<List<DynamicDataSet>> ToListAsync(IAsyncEnumerable<DynamicDataSet> source)
        {
            var list = new List<DynamicDataSet>();
            await foreach (DynamicDataSet item in source)
                list.Add(item);
            return list;
        }

        // ------------------------------------------------------------ sin paginación

        [Fact]
        public async Task FetchDataPagedAsync_SinPaginacion_EntregaUnaSolaPaginaConTodosLosDatos()
        {
            var handler = FakeHttpMessageHandler.ReturningJson(Json(3));
            var (gateway, _, _) = Create(handler, Settings(paginated: false));

            var pages = await ToListAsync(gateway.FetchDataPagedAsync("BPMS", new Dictionary<String, String>(), CancellationToken.None));

            pages.Should().ContainSingle();
            pages[0].Rows.Should().HaveCount(3);
            pages[0].AvailableFields.Should().Equal("ID", "NOMBRE");
            pages[0].Rows[1]["NOMBRE"].Should().Be("Nombre 2");
            handler.Requests.Should().ContainSingle();
        }

        [Fact]
        public async Task FetchDataPagedAsync_SinPaginacion_NoAgregaParametrosDePaginacionNiSignoDeInterrogacion()
        {
            var handler = FakeHttpMessageHandler.ReturningJson(Json(1));
            var (gateway, _, _) = Create(handler, Settings());

            await ToListAsync(gateway.FetchDataPagedAsync("BPMS", new Dictionary<String, String>(), CancellationToken.None));

            handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
            handler.LastRequest.RequestUri!.ToString().Should().Be(Endpoint);
        }

        [Fact]
        public async Task FetchDataPagedAsync_PaginacionHabilitadaConPageSizeCero_SeComportaSinPaginacion()
        {
            var handler = FakeHttpMessageHandler.ReturningJson(Json(2));
            var (gateway, _, _) = Create(handler, Settings(paginated: true, pageSize: 0));

            var pages = await ToListAsync(gateway.FetchDataPagedAsync("BPMS", new Dictionary<String, String>(), CancellationToken.None));

            pages.Should().ContainSingle();
            handler.Requests.Should().ContainSingle();
            handler.LastRequest!.RequestUri!.Query.Should().BeEmpty();
        }

        [Fact]
        public async Task FetchDataPagedAsync_PaginacionDeshabilitadaConPageSize_SeComportaSinPaginacion()
        {
            var handler = FakeHttpMessageHandler.ReturningJson(Json(2));
            var (gateway, _, _) = Create(handler, Settings(paginated: false, pageSize: 50));

            var pages = await ToListAsync(gateway.FetchDataPagedAsync("BPMS", new Dictionary<String, String>(), CancellationToken.None));

            pages.Should().ContainSingle();
            handler.LastRequest!.RequestUri!.Query.Should().BeEmpty();
        }

        [Fact]
        public async Task FetchDataPagedAsync_SinPaginacionYSinFilas_EntregaUnaPaginaVacia()
        {
            var handler = FakeHttpMessageHandler.ReturningJson(Json(0));
            var (gateway, _, _) = Create(handler, Settings());

            var pages = await ToListAsync(gateway.FetchDataPagedAsync("BPMS", new Dictionary<String, String>(), CancellationToken.None));

            pages.Should().ContainSingle();
            pages[0].Rows.Should().BeEmpty();
        }

        [Fact]
        public async Task FetchDataPagedAsync_ConFiltros_LosEscapaEnLaQueryString()
        {
            var handler = FakeHttpMessageHandler.ReturningJson(Json(1));
            var (gateway, _, _) = Create(handler, Settings());
            var filters = new Dictionary<String, String> { ["nit cliente"] = "900 123&x", ["año"] = "2025" };

            await ToListAsync(gateway.FetchDataPagedAsync("BPMS", filters, CancellationToken.None));

            String url = handler.LastRequest!.RequestUri!.AbsoluteUri;
            url.Should().Contain("nit%20cliente=900%20123%26x");
            url.Should().Contain("a%C3%B1o=2025");
            url.Should().Contain("&");
            url.Should().StartWith(Endpoint + "?");
        }

        [Fact]
        public async Task FetchDataPagedAsync_UsaElHttpClientNombradoConElNombreDeLaApi()
        {
            var handler = FakeHttpMessageHandler.ReturningJson(Json(1));
            var (gateway, factory, _) = Create(handler, Settings(apiName: "SIM"));

            await ToListAsync(gateway.FetchDataPagedAsync("SIM", new Dictionary<String, String>(), CancellationToken.None));

            factory.LastClientName.Should().Be("SIM");
        }

        [Fact]
        public async Task FetchDataPagedAsync_NombreDeApiConOtraCapitalizacion_ResuelveLaConfiguracion()
        {
            var handler = FakeHttpMessageHandler.ReturningJson(Json(1));
            var (gateway, _, _) = Create(handler, Settings(apiName: "BPMS"));

            var pages = await ToListAsync(gateway.FetchDataPagedAsync("bpms", new Dictionary<String, String>(), CancellationToken.None));

            pages.Should().ContainSingle();
        }

        [Fact]
        public async Task FetchDataPagedAsync_RegistraLaUrlConsultada()
        {
            var handler = FakeHttpMessageHandler.ReturningJson(Json(1));
            var (gateway, _, logger) = Create(handler, Settings());

            await ToListAsync(gateway.FetchDataPagedAsync("BPMS", new Dictionary<String, String>(), CancellationToken.None));

            logger.Entries.Should().Contain(e => e.Level == LogLevel.Information && e.Message.Contains(Endpoint));
        }

        // ------------------------------------------------------------ con paginación

        [Fact]
        public async Task FetchDataPagedAsync_ConPaginacion_PideLasPaginasHastaRecibirUnaIncompleta()
        {
            var responses = new Queue<String>(new[] { Json(2, "A"), Json(2, "B"), Json(1, "C") });
            var handler = new FakeHttpMessageHandler(_ => Ok(responses.Dequeue()));
            var (gateway, _, _) = Create(handler, Settings(paginated: true, pageSize: 2));

            var pages = await ToListAsync(gateway.FetchDataPagedAsync("BPMS", new Dictionary<String, String>(), CancellationToken.None));

            pages.Select(p => p.Rows.Count).Should().Equal(2, 2, 1);
            pages[2].Rows[0]["ID"].Should().Be("C1");
            handler.Requests.Should().HaveCount(3);
            handler.Requests.Select(r => r.RequestUri!.Query).Should().Equal(
                "?pageNumber=1&pageSize=2", "?pageNumber=2&pageSize=2", "?pageNumber=3&pageSize=2");
        }

        [Fact]
        public async Task FetchDataPagedAsync_ConPaginacion_UltimaPaginaCompletaSeguidaDeVacia_TerminaSinEntregarLaVacia()
        {
            var responses = new Queue<String>(new[] { Json(2, "A"), Json(2, "B"), Json(0) });
            var handler = new FakeHttpMessageHandler(_ => Ok(responses.Dequeue()));
            var (gateway, _, _) = Create(handler, Settings(paginated: true, pageSize: 2));

            var pages = await ToListAsync(gateway.FetchDataPagedAsync("BPMS", new Dictionary<String, String>(), CancellationToken.None));

            pages.Should().HaveCount(2);
            handler.Requests.Should().HaveCount(3);
        }

        [Fact]
        public async Task FetchDataPagedAsync_ConPaginacionYPrimeraPaginaVacia_NoEntregaNada()
        {
            var handler = FakeHttpMessageHandler.ReturningJson(Json(0));
            var (gateway, _, _) = Create(handler, Settings(paginated: true, pageSize: 5));

            var pages = await ToListAsync(gateway.FetchDataPagedAsync("BPMS", new Dictionary<String, String>(), CancellationToken.None));

            pages.Should().BeEmpty();
            handler.Requests.Should().ContainSingle();
        }

        [Fact]
        public async Task FetchDataPagedAsync_ConPaginacionYPaginaUnicaIncompleta_NoPideLaSiguiente()
        {
            var handler = FakeHttpMessageHandler.ReturningJson(Json(3));
            var (gateway, _, _) = Create(handler, Settings(paginated: true, pageSize: 10));

            var pages = await ToListAsync(gateway.FetchDataPagedAsync("BPMS", new Dictionary<String, String>(), CancellationToken.None));

            pages.Should().ContainSingle();
            handler.Requests.Should().ContainSingle();
        }

        [Fact]
        public async Task FetchDataPagedAsync_ConNombresDeParametroPersonalizados_LosUsaEnLaQuery()
        {
            var settings = Settings(paginated: true, pageSize: 4);
            settings.Apis["BPMS"].PageNumberParam = "page";
            settings.Apis["BPMS"].PageSizeParam = "limit";
            var handler = FakeHttpMessageHandler.ReturningJson(Json(1));
            var (gateway, _, _) = Create(handler, settings);

            await ToListAsync(gateway.FetchDataPagedAsync("BPMS", new Dictionary<String, String>(), CancellationToken.None));

            handler.LastRequest!.RequestUri!.Query.Should().Be("?page=1&limit=4");
        }

        [Fact]
        public async Task FetchDataPagedAsync_ConPaginacion_ConservaLosFiltrosYNoMutaElDiccionarioRecibido()
        {
            var handler = FakeHttpMessageHandler.ReturningJson(Json(1));
            var (gateway, _, _) = Create(handler, Settings(paginated: true, pageSize: 2));
            var filters = new Dictionary<String, String> { ["nit"] = "900" };

            await ToListAsync(gateway.FetchDataPagedAsync("BPMS", filters, CancellationToken.None));

            filters.Should().ContainSingle().Which.Should().Be(new KeyValuePair<String, String>("nit", "900"));
            handler.LastRequest!.RequestUri!.Query.Should().Contain("nit=900").And.Contain("pageNumber=1").And.Contain("pageSize=2");
        }

        [Fact]
        public async Task FetchDataPagedAsync_ConPaginacion_ElFiltroDelCallerConElNombreDelParametroDePaginaEsSobrescrito()
        {
            var handler = FakeHttpMessageHandler.ReturningJson(Json(1));
            var (gateway, _, _) = Create(handler, Settings(paginated: true, pageSize: 2));
            var filters = new Dictionary<String, String> { ["PAGENUMBER"] = "99" };

            await ToListAsync(gateway.FetchDataPagedAsync("BPMS", filters, CancellationToken.None));

            handler.LastRequest!.RequestUri!.Query.Should().Be("?PAGENUMBER=1&pageSize=2");
        }

        [Fact]
        public async Task FetchDataPagedAsync_ConPaginacion_RegistraCadaPaginaObtenida()
        {
            var handler = FakeHttpMessageHandler.ReturningJson(Json(1));
            var (gateway, _, logger) = Create(handler, Settings(paginated: true, pageSize: 2));

            await ToListAsync(gateway.FetchDataPagedAsync("BPMS", new Dictionary<String, String>(), CancellationToken.None));

            logger.Entries.Should().Contain(e => e.Message.Contains("Página 1 de BPMS: 1 registros."));
        }

        [Fact]
        public async Task FetchDataPagedAsync_ConPaginacionYTokenCancelado_LanzaOperationCanceledException()
        {
            var handler = FakeHttpMessageHandler.ReturningJson(Json(2));
            var (gateway, _, _) = Create(handler, Settings(paginated: true, pageSize: 2));
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Func<Task> act = () => ToListAsync(gateway.FetchDataPagedAsync("BPMS", new Dictionary<String, String>(), cts.Token));

            await act.Should().ThrowAsync<OperationCanceledException>();
            handler.Requests.Should().BeEmpty();
        }

        [Fact]
        public async Task FetchDataPagedAsync_SeCancelaEntrePaginas_NoPideLaSiguiente()
        {
            using var cts = new CancellationTokenSource();
            var handler = new FakeHttpMessageHandler(_ => Ok(Json(2)));
            var (gateway, _, _) = Create(handler, Settings(paginated: true, pageSize: 2));

            Func<Task> act = async () =>
            {
                await foreach (var _ in gateway.FetchDataPagedAsync("BPMS", new Dictionary<String, String>(), cts.Token))
                    cts.Cancel();
            };

            await act.Should().ThrowAsync<OperationCanceledException>();
            handler.Requests.Should().ContainSingle();
        }

        // ------------------------------------------------------------ errores

        [Fact]
        public async Task FetchDataPagedAsync_ApiNoConfigurada_LanzaArgumentExceptionAlEnumerar()
        {
            var handler = FakeHttpMessageHandler.ReturningJson(Json(1));
            var (gateway, _, _) = Create(handler, Settings());

            var sequence = gateway.FetchDataPagedAsync("NO_EXISTE", new Dictionary<String, String>(), CancellationToken.None);
            Func<Task> act = () => ToListAsync(sequence);

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*NO_EXISTE*");
            handler.Requests.Should().BeEmpty();
        }

        [Fact]
        public void FetchDataPagedAsync_EsPerezosa_NoConsultaNadaHastaEnumerar()
        {
            var handler = FakeHttpMessageHandler.ReturningJson(Json(1));
            var (gateway, factory, _) = Create(handler, Settings());

            _ = gateway.FetchDataPagedAsync("BPMS", new Dictionary<String, String>(), CancellationToken.None);

            handler.Requests.Should().BeEmpty();
            factory.LastClientName.Should().BeNull();
        }

        [Theory]
        [InlineData(HttpStatusCode.InternalServerError, 500)]
        [InlineData(HttpStatusCode.NotFound, 404)]
        [InlineData(HttpStatusCode.Unauthorized, 401)]
        public async Task FetchDataPagedAsync_RespuestaNoExitosa_LanzaHttpRequestExceptionYRegistraElCuerpo(HttpStatusCode status, Int32 code)
        {
            var handler = FakeHttpMessageHandler.ReturningStatus(status, "detalle del error");
            var (gateway, _, logger) = Create(handler, Settings());

            Func<Task> act = () => ToListAsync(gateway.FetchDataPagedAsync("BPMS", new Dictionary<String, String>(), CancellationToken.None));

            await act.Should().ThrowAsync<HttpRequestException>().WithMessage($"*status {code}*");
            logger.Entries.Should().Contain(e => e.Level == LogLevel.Error && e.Message.Contains("detalle del error"));
        }

        [Fact]
        public async Task FetchDataPagedAsync_FallaEnLaSegundaPagina_PropagaElErrorTrasEntregarLaPrimera()
        {
            Int32 call = 0;
            var handler = new FakeHttpMessageHandler(_ => ++call == 1
                ? Ok(Json(2))
                : new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent("x") });
            var (gateway, _, _) = Create(handler, Settings(paginated: true, pageSize: 2));
            var received = new List<DynamicDataSet>();

            Func<Task> act = async () =>
            {
                await foreach (var page in gateway.FetchDataPagedAsync("BPMS", new Dictionary<String, String>(), CancellationToken.None))
                    received.Add(page);
            };

            await act.Should().ThrowAsync<HttpRequestException>();
            received.Should().ContainSingle();
        }

        [Fact]
        public async Task FetchDataPagedAsync_CuerpoJsonNulo_LanzaInvalidOperationException()
        {
            var handler = FakeHttpMessageHandler.ReturningJson("null");
            var (gateway, _, _) = Create(handler, Settings());

            Func<Task> act = () => ToListAsync(gateway.FetchDataPagedAsync("BPMS", new Dictionary<String, String>(), CancellationToken.None));

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*vacía*");
        }

        [Fact]
        public async Task FetchDataPagedAsync_JsonMalformado_LanzaJsonException()
        {
            var handler = FakeHttpMessageHandler.ReturningJson("{ no es json");
            var (gateway, _, _) = Create(handler, Settings());

            Func<Task> act = () => ToListAsync(gateway.FetchDataPagedAsync("BPMS", new Dictionary<String, String>(), CancellationToken.None));

            await act.Should().ThrowAsync<JsonException>();
        }

        [Fact]
        public async Task FetchDataPagedAsync_CuerpoNoJsonConContentTypeHtml_LanzaJsonException()
        {
            var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<html/>", System.Text.Encoding.UTF8, "text/html")
            });
            var (gateway, _, _) = Create(handler, Settings());

            Func<Task> act = () => ToListAsync(gateway.FetchDataPagedAsync("BPMS", new Dictionary<String, String>(), CancellationToken.None));

            await act.Should().ThrowAsync<JsonException>();
        }

        [Fact]
        public async Task FetchDataPagedAsync_SinPaginacionYTokenCancelado_LanzaOperationCanceledException()
        {
            var handler = FakeHttpMessageHandler.ReturningJson(Json(1));
            var (gateway, _, _) = Create(handler, Settings());
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Func<Task> act = () => ToListAsync(gateway.FetchDataPagedAsync("BPMS", new Dictionary<String, String>(), cts.Token));

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task FetchDataPagedAsync_ValoresJsonNoTexto_SeConviertenACadena()
        {
            const String body = "{\"requestedFields\":[\"A\",\"B\",\"C\"],\"missingColumns\":[],\"rows\":[{\"A\":12,\"B\":true,\"C\":null}]}";
            var handler = FakeHttpMessageHandler.ReturningJson(body);
            var (gateway, _, _) = Create(handler, Settings());

            var pages = await ToListAsync(gateway.FetchDataPagedAsync("BPMS", new Dictionary<String, String>(), CancellationToken.None));

            DynamicRecord row = pages.Single().Rows.Single();
            row["A"].Should().Be("12");
            row["B"].Should().Be("True");
            row["C"].Should().BeEmpty();
        }

        private static HttpResponseMessage Ok(String json)
            => new(HttpStatusCode.OK) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
    }
}
