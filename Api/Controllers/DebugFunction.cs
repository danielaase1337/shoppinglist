using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Api.Controllers
{
    public class DebugFunction
    {
        private readonly ILogger _logger;

        public DebugFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DebugFunction>();
        }

        [Function("DebugFunction")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions! Its up and running");

            return response;
        }
    }
}
