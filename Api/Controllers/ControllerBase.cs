using Api.Auth;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;

namespace Api.Controllers
{
    public class ControllerBase
    {
        // ── Auth helpers (v1: read principal for logging; writes can enforce later) ──

        /// <summary>
        /// Returns the SWA-injected user principal, or null for unauthenticated requests.
        /// Per D2: v1 is a single shared family app — reads are open, but the principal
        /// is available for logging and future per-user enforcement (v2 / FamilyId).
        /// </summary>
        protected ClientPrincipal? GetCurrentUser(HttpRequestData req) =>
            req.GetClientPrincipal();

        protected string? GetCurrentUserId(HttpRequestData req) =>
            req.GetUserId();

        protected string? GetCurrentUserName(HttpRequestData req) =>
            req.GetUserName();

        // ── Error response helpers ────────────────────────────────────────────────

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
