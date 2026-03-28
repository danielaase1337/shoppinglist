using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Shared.Repository;

namespace Api.Controllers
{
    /// <summary>
    /// One-time migration endpoint: moves FrequentShoppingList documents that were
    /// incorrectly written to the "misc" collection into "frequentshoppinglists".
    ///
    /// Run once against production, then this endpoint can be removed.
    /// </summary>
    public class MigrateFrequentListsController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IGoogleDbContext _dbContext;

        public MigrateFrequentListsController(ILoggerFactory loggerFactory, IGoogleDbContext dbContext)
        {
            _logger = loggerFactory.CreateLogger<MigrateFrequentListsController>();
            _dbContext = dbContext;
        }

        [Function("migrate-frequent-lists")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "tools/migrate-frequent-lists")]
            HttpRequestData req)
        {
            try
            {
                var db = _dbContext.DB;
                var miscCollection = db.Collection("misc");
                var targetCollection = db.Collection("frequentshoppinglists");

                var snapshot = await miscCollection.GetSnapshotAsync();
                int migrated = 0;
                int skipped = 0;

                foreach (var doc in snapshot.Documents)
                {
                    var fields = doc.ToDictionary();

                    // FrequentShoppingList documents have an "Items" field.
                    // ShoppingList documents have "ShoppingItems" instead.
                    // Any document without "Items" is not a FrequentShoppingList — leave it alone.
                    if (!fields.ContainsKey("Items"))
                    {
                        skipped++;
                        _logger.LogInformation("Skipped misc doc {Id} (not a FrequentShoppingList)", doc.Id);
                        continue;
                    }

                    // Copy to the correct collection, preserving the document ID so existing
                    // client-side IDs remain valid.
                    var targetDocRef = targetCollection.Document(doc.Id);
                    await targetDocRef.SetAsync(fields);

                    // Remove the original from misc.
                    await miscCollection.Document(doc.Id).DeleteAsync();

                    migrated++;
                    _logger.LogInformation("Migrated FrequentShoppingList {Id} from misc → frequentshoppinglists", doc.Id);
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    message = $"Migration complete. Migrated: {migrated}, Skipped (non-FrequentShoppingList): {skipped}",
                    migrated,
                    skipped
                });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during frequent list migration");
                return await GetErroRespons(ex.Message, req);
            }
        }
    }
}
