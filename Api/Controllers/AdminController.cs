using System.Net;
using AutoMapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Shared.FireStoreDataModels;
using Shared.Repository;

namespace Api.Controllers
{
    /// <summary>
    /// One-time admin migration endpoint for backfilling LastModified on ShoppingList documents.
    /// Gated by requiring the "admin" role in the SWA-injected x-ms-client-principal header.
    /// Run once against production after deployment, then re-run as needed if new legacy data appears.
    /// </summary>
    public class AdminController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IGenericRepository<ShoppingList> _repository;

        public AdminController(ILoggerFactory loggerFactory, IGenericRepository<ShoppingList> repository)
        {
            _logger = loggerFactory.CreateLogger<AdminController>();
            _repository = repository;
        }

        [Function("admin-migrate-lastmodified")]
        public async Task<HttpResponseData> MigrateLastModified(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "admin/migrate-lastmodified")]
            HttpRequestData req)
        {
            try
            {
                var principal = GetCurrentUser(req);
                var isAdmin = principal?.UserRoles?.Contains("admin") == true;
                if (!isAdmin)
                {
                    _logger.LogWarning("Unauthorized attempt to call migrate-lastmodified by user {UserId}", principal?.UserId ?? "anonymous");
                    var forbidden = req.CreateResponse(HttpStatusCode.Forbidden);
                    await forbidden.WriteStringAsync("Forbidden: admin role required.");
                    return forbidden;
                }

                var all = await _repository.Get();
                if (all == null)
                {
                    _logger.LogError("migrate-lastmodified: Could not retrieve ShoppingList documents");
                    return await GetErroRespons("Could not retrieve shopping lists", req);
                }

                int migratedCount = 0;
                foreach (var list in all)
                {
                    if (!list.LastModified.HasValue)
                    {
                        list.LastModified = DateTime.UtcNow;
                        await _repository.Update(list);
                        migratedCount++;
                        _logger.LogInformation("Migrated LastModified for ShoppingList: {Name} ({Id})", list.Name, list.Id);
                    }
                }

                _logger.LogInformation("migrate-lastmodified complete. Migrated {Count} documents.", migratedCount);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { migratedCount });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during LastModified migration");
                return await GetErroRespons("An unexpected error occurred", req);
            }
        }
    }
}
