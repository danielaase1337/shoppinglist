using System.Net;
using System.Threading.Tasks;
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
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            await response.WriteStringAsync("Welcome to Azure Functions! Its up and running");

            return response;
        }
    }
}
