using System.Net;

namespace Connection360.Infrastructure.Tests.TestHelpers
{
    /// <summary>
    /// HttpMessageHandler de prueba que permite simular respuestas HTTP sin
    /// realizar llamadas de red reales. Se inyecta en un HttpClient a través
    /// de un IHttpClientFactory falso.
    /// </summary>
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public HttpRequestMessage? LastRequest { get; private set; }

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        public static FakeHttpMessageHandler ReturningJson(String json, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return new FakeHttpMessageHandler(_ => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }

        public static FakeHttpMessageHandler ReturningStatus(HttpStatusCode statusCode, String body = "")
        {
            return new FakeHttpMessageHandler(_ => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body)
            });
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_responder(request));
        }
    }

    public class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        public String? LastClientName { get; private set; }

        public FakeHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(String name)
        {
            LastClientName = name;
            return new HttpClient(_handler, disposeHandler: false) { BaseAddress = null };
        }
    }
}
