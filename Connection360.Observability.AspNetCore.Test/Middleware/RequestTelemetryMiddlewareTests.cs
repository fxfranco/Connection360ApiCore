using System.Diagnostics;
using Connection360.Observability.AspNetCore.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Xunit;

namespace Connection360.Observability.AspNetCore.Test.Middleware
{
    public class RequestTelemetryMiddlewareTests : IDisposable
    {
        private readonly ActivitySource _source = new("test.http." + Guid.NewGuid());
        private readonly ActivityListener _listener;
        private AllDataMode _mode = AllDataMode.AllData;

        private enum AllDataMode { AllData, PropagationOnly }

        public RequestTelemetryMiddlewareTests()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == _source.Name,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                    _mode == AllDataMode.AllData ? ActivitySamplingResult.AllDataAndRecorded : ActivitySamplingResult.PropagationData,
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public void Dispose()
        {
            _listener.Dispose();
            _source.Dispose();
        }

        private static DefaultHttpContext NewContext(String method = "GET", String path = "/api/v1/home/totals", String? route = null, String query = "?idClient=123&token=secreto")
        {
            var context = new DefaultHttpContext();
            context.Request.Method = method;
            context.Request.Path = path;
            context.Request.QueryString = new QueryString(query);
            context.Request.Scheme = "https";
            context.Request.Host = new HostString("api.connection360.test", 8443);
            context.Request.Headers.UserAgent = "pruebas/1.0";
            if (route is not null)
            {
                context.SetEndpoint(new RouteEndpoint(_ => Task.CompletedTask, RoutePatternFactory.Parse(route), 0, EndpointMetadataCollection.Empty, "ep"));
            }

            return context;
        }

        private Task Run(HttpContext context, RequestDelegate next) => new RequestTelemetryMiddleware(next).InvokeAsync(context);

        [Fact]
        public async Task ConSpanActivo_AgregaLosDatosDeLaPeticion()
        {
            using Activity activity = _source.StartActivity("Microsoft.AspNetCore.Hosting.HttpRequestIn", ActivityKind.Server)!;
            DefaultHttpContext context = NewContext();

            await Run(context, ctx => { ctx.Response.StatusCode = 200; return Task.CompletedTask; });

            activity.GetTagItem("http.request.method").Should().Be("GET");
            activity.GetTagItem("url.path").Should().Be("/api/v1/home/totals");
            activity.GetTagItem("url.scheme").Should().Be("https");
            activity.GetTagItem("server.address").Should().Be("api.connection360.test");
            activity.GetTagItem("server.port").Should().Be(8443);
            activity.GetTagItem("user_agent.original").Should().Be("pruebas/1.0");
            activity.GetTagItem("http.response.status_code").Should().Be(200);
        }

        [Fact]
        public async Task NoRegistraLaCadenaDeConsulta()
        {
            using Activity activity = _source.StartActivity("x", ActivityKind.Server)!;

            await Run(NewContext(), _ => Task.CompletedTask);

            activity.TagObjects.Select(t => t.Key).Should().NotContain("url.query");
            activity.TagObjects.Select(t => Convert.ToString(t.Value)).Should().NotContain(v => v!.Contains("secreto"));
        }

        [Fact]
        public async Task ConEndpoint_AgregaLaRutaPlantillaYElNombreDelSpan()
        {
            using Activity activity = _source.StartActivity("Microsoft.AspNetCore.Hosting.HttpRequestIn", ActivityKind.Server)!;
            DefaultHttpContext context = NewContext(path: "/api/v1/notifications/readnotification/15/2", route: "/api/v{version:apiVersion}/notifications/readnotification/{idClient}/{idNotification}");

            await Run(context, _ => Task.CompletedTask);

            activity.GetTagItem("http.route").Should().Be("/api/v{version:apiVersion}/notifications/readnotification/{idClient}/{idNotification}");
            activity.DisplayName.Should().Be("GET /api/v{version:apiVersion}/notifications/readnotification/{idClient}/{idNotification}");
        }

        [Fact]
        public async Task LaRutaPlantillaSinBarraInicial_SeNormalizaConBarra()
        {
            using Activity activity = _source.StartActivity("x", ActivityKind.Server)!;

            await Run(NewContext(route: "api/v1/clientes/{id}"), _ => Task.CompletedTask);

            activity.GetTagItem("http.route").Should().Be("/api/v1/clientes/{id}");
            activity.DisplayName.Should().Be("GET /api/v1/clientes/{id}");
        }

        [Fact]
        public async Task SinEndpoint_ElNombreDelSpanEsSoloElMetodo()
        {
            using Activity activity = _source.StartActivity("x", ActivityKind.Server)!;

            await Run(NewContext(method: "POST"), _ => Task.CompletedTask);

            activity.GetTagItem("http.route").Should().BeNull();
            activity.DisplayName.Should().Be("POST");
        }

        [Theory]
        [InlineData(200, ActivityStatusCode.Unset)]
        [InlineData(404, ActivityStatusCode.Unset)]
        [InlineData(499, ActivityStatusCode.Unset)]
        [InlineData(500, ActivityStatusCode.Error)]
        [InlineData(503, ActivityStatusCode.Error)]
        public async Task EstadoDelSpan_SoloEsErrorParaRespuestas5xx(Int32 statusCode, ActivityStatusCode expected)
        {
            using Activity activity = _source.StartActivity("x", ActivityKind.Server)!;

            await Run(NewContext(), ctx => { ctx.Response.StatusCode = statusCode; return Task.CompletedTask; });

            activity.Status.Should().Be(expected);
            activity.GetTagItem("http.response.status_code").Should().Be(statusCode);
        }

        [Fact]
        public async Task ConRespuesta500_MarcaElTipoDeError()
        {
            using Activity activity = _source.StartActivity("x", ActivityKind.Server)!;

            await Run(NewContext(), ctx => { ctx.Response.StatusCode = 500; return Task.CompletedTask; });

            activity.GetTagItem("error.type").Should().Be("500");
        }

        [Fact]
        public async Task SiElSiguienteLanza_AnotaLaExcepcionYLaRelanza()
        {
            using Activity activity = _source.StartActivity("x", ActivityKind.Server)!;
            DefaultHttpContext context = NewContext();
            context.Response.StatusCode = 500;

            Func<Task> act = () => Run(context, _ => throw new InvalidOperationException("falló"));

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("falló");
            activity.Status.Should().Be(ActivityStatusCode.Error);
            activity.StatusDescription.Should().Be("falló");
            activity.GetTagItem("error.type").Should().Be(typeof(InvalidOperationException).FullName);
            activity.Events.Should().ContainSingle(e => e.Name == "exception");
            activity.GetTagItem("http.response.status_code").Should().Be(500, "el estado también se registra cuando hay excepción");
        }

        [Fact]
        public async Task SinSpanActivo_SoloLlamaAlSiguiente()
        {
            Activity.Current = null;
            Boolean called = false;

            await Run(NewContext(), _ => { called = true; return Task.CompletedTask; });

            called.Should().BeTrue();
        }

        [Fact]
        public async Task ConSpanQueNoRecolectaDatos_NoAgregaEtiquetas()
        {
            _mode = AllDataMode.PropagationOnly;
            using Activity activity = _source.StartActivity("x", ActivityKind.Server)!;
            activity.IsAllDataRequested.Should().BeFalse();
            Boolean called = false;

            await Run(NewContext(), _ => { called = true; return Task.CompletedTask; });

            called.Should().BeTrue();
            activity.TagObjects.Should().BeEmpty();
            activity.DisplayName.Should().Be("x");
        }

        [Fact]
        public async Task UserAgentMuyLargo_SeTrunca()
        {
            using Activity activity = _source.StartActivity("x", ActivityKind.Server)!;
            DefaultHttpContext context = NewContext();
            context.Request.Headers.UserAgent = new String('u', 1000);

            await Run(context, _ => Task.CompletedTask);

            ((String)activity.GetTagItem("user_agent.original")!).Length.Should().Be(256);
        }

        [Fact]
        public async Task SinUserAgentNiPuerto_OmiteEsasEtiquetas()
        {
            using Activity activity = _source.StartActivity("x", ActivityKind.Server)!;
            DefaultHttpContext context = NewContext();
            context.Request.Headers.Remove("User-Agent");
            context.Request.Host = new HostString("localhost");

            await Run(context, _ => Task.CompletedTask);

            activity.GetTagItem("user_agent.original").Should().BeNull();
            activity.GetTagItem("server.port").Should().BeNull();
            activity.GetTagItem("server.address").Should().Be("localhost");
        }

        [Fact]
        public async Task UseConnection360RequestTelemetry_RegistraElMiddlewareEnElPipeline()
        {
            using Activity activity = _source.StartActivity("x", ActivityKind.Server)!;
            var services = new ServiceCollectionStub().Build();
            var builder = new ApplicationBuilder(services);
            builder.UseConnection360RequestTelemetry();
            builder.Run(ctx => { ctx.Response.StatusCode = 201; return Task.CompletedTask; });
            RequestDelegate pipeline = builder.Build();

            await pipeline(NewContext());

            activity.GetTagItem("http.response.status_code").Should().Be(201);
        }

        private sealed class ServiceCollectionStub
        {
            public IServiceProvider Build() => Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions
                .BuildServiceProvider(new Microsoft.Extensions.DependencyInjection.ServiceCollection());
        }
    }
}
