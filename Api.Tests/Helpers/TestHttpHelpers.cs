using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace Api.Tests.Helpers
{
    /// <summary>
    /// Concrete implementation of HttpRequestData for use in Azure Functions unit tests.
    /// </summary>
    public class TestHttpRequestData : HttpRequestData
    {
        private readonly MemoryStream _body;
        private readonly HttpHeadersCollection _headers;
        private readonly string _method;
        private readonly Uri _url;

        public TestHttpRequestData(FunctionContext context, string method = "GET", string body = "", string url = "http://localhost/api/shoppinglists")
            : base(context)
        {
            _method = method;
            _body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            _headers = new HttpHeadersCollection();
            _url = new Uri(url);
        }

        public override Stream Body => _body;
        public override HttpHeadersCollection Headers => _headers;
        public override IReadOnlyCollection<IHttpCookie> Cookies => new List<IHttpCookie>();
        public override Uri Url => _url;
        public override IEnumerable<ClaimsIdentity> Identities => new List<ClaimsIdentity>();
        public override string Method => _method;

        public override HttpResponseData CreateResponse()
        {
            return new TestHttpResponseData(FunctionContext);
        }
    }

    /// <summary>
    /// Concrete implementation of HttpResponseData for use in Azure Functions unit tests.
    /// </summary>
    public class TestHttpResponseData : HttpResponseData
    {
        public TestHttpResponseData(FunctionContext context) : base(context)
        {
            Body = new MemoryStream();
            Headers = new HttpHeadersCollection();
        }

        public override HttpStatusCode StatusCode { get; set; }
        public override HttpHeadersCollection Headers { get; set; }
        public override Stream Body { get; set; }
        public override HttpCookies Cookies { get; } = null!;
    }

    /// <summary>
    /// Factory for creating test infrastructure for Azure Functions unit tests.
    /// </summary>
    public static class TestHttpFactory
    {
        public static FunctionContext CreateFunctionContext()
        {
            // WriteAsJsonAsync and ReadFromJsonAsync need IOptions<WorkerOptions> with a Serializer
            var services = new ServiceCollection();
            services.Configure<WorkerOptions>(opts => opts.Serializer = new JsonObjectSerializer());
            var serviceProvider = services.BuildServiceProvider();

            var mockContext = new Mock<FunctionContext>();
            mockContext.Setup(c => c.InstanceServices).Returns(serviceProvider);

            return mockContext.Object;
        }

        public static TestHttpRequestData CreateGetRequest(string url = "http://localhost/api/shoppinglists")
        {
            return new TestHttpRequestData(CreateFunctionContext(), "GET", "", url);
        }

        public static TestHttpRequestData CreatePostRequest(string body, string url = "http://localhost/api/shoppinglists")
        {
            return new TestHttpRequestData(CreateFunctionContext(), "POST", body, url);
        }

        public static TestHttpRequestData CreatePutRequest(string body, string url = "http://localhost/api/shoppinglists")
        {
            return new TestHttpRequestData(CreateFunctionContext(), "PUT", body, url);
        }

        public static TestHttpRequestData CreateDeleteRequest(string url = "http://localhost/api/shoppinglist/1")
        {
            return new TestHttpRequestData(CreateFunctionContext(), "DELETE", "", url);
        }

        /// <summary>
        /// Reads the response body as a string (resets stream position first).
        /// </summary>
        public static async Task<string> ReadResponseBodyAsync(HttpResponseData response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(response.Body, leaveOpen: true);
            return await reader.ReadToEndAsync();
        }
    }
}
