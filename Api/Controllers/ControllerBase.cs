using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;

namespace Api.Controllers
{
    public class ControllerBase
    {
     

        protected HttpResponseData GetErroRespons(string message, HttpRequestData req)
        {
            var errorRespons = req.CreateResponse(HttpStatusCode.InternalServerError);
            errorRespons.WriteString(message);
            return errorRespons;
        }
        protected HttpResponseData GetNoContentRespons(string v, HttpRequestData req)
        {
            var errorRespons = req.CreateResponse(HttpStatusCode.BadRequest);
            errorRespons.WriteString(v);
            return errorRespons;
        }
    }
}