using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;

namespace Api.Controllers
{
    public class ControllerBase
    {
     

        protected async Task<HttpResponseData> GetErroRespons(string message, HttpRequestData req)
        {
            var errorRespons = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorRespons.WriteStringAsync(message);
            return errorRespons;
        }
        protected async Task<HttpResponseData> GetNoContentRespons(string v, HttpRequestData req)
        {
            var errorRespons = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorRespons.WriteStringAsync(v);
            return errorRespons;
        }
    }
}