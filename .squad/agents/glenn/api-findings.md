# API Security & Comprehensive Audit Report
**Author:** Glenn (Backend Dev Agent)  
**Date:** 2026-03-22  
**Scope:** Full Azure Functions backend audit — authentication, authorization, input validation, Firestore data access, configuration, error handling, secrets, HTTPS/TLS, HTTP headers  
**Audited Files:** `Api/Controllers/*`, `Api/Program.cs`, `Api/host.json`, `Api/local.settings.json`, `Shared/Repository/GoogleFireBaseGenericRepository.cs`, `Shared/Repository/GoogleDbContext.cs`, `Client/wwwroot/staticwebapp.config.json`

---

## Executive Summary

This is a personal/family shopping list app deployed as Azure Static Web Apps (SWA) + Azure Functions v4 (isolated worker, .NET 8). The backend is currently an **open, unauthenticated API with no per-user data isolation**. The majority of endpoints are fully anonymous. Error messages from internal exceptions are returned verbatim to API consumers. Several critical infrastructure bugs exist in the Firestore layer. HTTP security headers are entirely absent. These findings are categorized below.

**Risk posture:** Acceptable for a private, single-household deployment where the function key or SWA URL is not shared publicly. **Not acceptable** for any multi-user, public, or shared deployment.

---

## Severity Classification

| Level | Description |
|---|---|
| 🔴 **Critical** | Data corruption, full data exposure, credential compromise possible |
| 🟠 **High** | Significant security or data integrity risk requiring prompt action |
| 🟡 **Medium** | Moderate risk, degrades security posture or causes incorrect behaviour |
| 🟢 **Low** | Minor issue, defence-in-depth, code quality |

---

## 1. Authentication & Authorization

### 1.1 — Inconsistent Authorization Levels Across Controllers

**Severity: 🔴 Critical (for any multi-user scenario) / �� Low (private single-user)**

**Files:**
- `Api/Controllers/ShopsController.cs` — lines 29, 81
- `Api/Controllers/ShopsItemsController.cs` — lines 30, 82
- `Api/Controllers/FrequentShoppingListController.cs` — lines 26, 104
- `Api/Controllers/ShopItemCategoryController.cs` — lines 27, 75
- `Api/Controllers/DebugFunction.cs` — line 18
- `Api/Controllers/ShoppingListController.cs` — lines 31, 126
- `Api/Controllers/MealRecipeController.cs` — lines 29, 106

**Current State:**

| Controller | Function | Authorization Level |
|---|---|---|
| `ShoppingListController` | `shoppinglists`, `shoppinglist/{id}` | `Function` (shared key) |
| `MealRecipeController` | `mealrecipes`, `mealrecipe/{id}` | `Function` (shared key) |
| `ShopsController` | `shops`, `shop/{id}` | **Anonymous** |
| `ShopsItemsController` | `shopitems`, `shopitem/{id}` | **Anonymous** |
| `FrequentShoppingListController` | `frequentshoppinglists`, `frequentshoppinglist/{id}` | **Anonymous** |
| `ShopItemCategoryController` | `itemcategorys`, `itemcategory/{id}` | **Anonymous** |
| `DebugFunction` | `DebugFunction` | **Anonymous** |

`AuthorizationLevel.Anonymous` means **zero authentication**. Any HTTP client anywhere in the world can read, create, update, or delete shops, shop items, categories, and frequent lists without any credential.

`AuthorizationLevel.Function` provides a shared API key — it is **not user-level authentication**. Anyone with the key can access all data for all users.

**Code Example (ShopsController.cs line 29):**
```csharp
[Function("shops")]
public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put")] HttpRequestData req)
```

**Remediation:**
1. Apply `AuthorizationLevel.Function` (minimum) uniformly to all controllers.
2. For true user auth: use Azure SWA's built-in authentication (AAD, GitHub, custom). SWA injects `x-ms-client-principal` header on requests forwarded to the Functions backend. Parse and validate this header in `ControllerBase`:

```csharp
// ControllerBase.cs
protected ClaimsPrincipal? GetAuthenticatedUser(HttpRequestData req)
{
    var header = req.Headers
        .GetValues("x-ms-client-principal")
        .FirstOrDefault();
    if (string.IsNullOrEmpty(header)) return null;
    var decoded = Convert.FromBase64String(header);
    var principal = JsonSerializer.Deserialize<ClientPrincipal>(decoded);
    return principal?.ToClaimsPrincipal();
}
```

3. Return `401 Unauthorized` if no valid principal is present.

---

### 1.2 — No Per-User Data Isolation

**Severity: 🔴 Critical (for any multi-user scenario)**

**Files:** `Shared/BaseModels/EntityBase.cs`, `Shared/Repository/GoogleFireBaseGenericRepository.cs`, all controllers

**Finding:** `IGenericRepository<T>.Get()` returns **every document in a collection**. There is no `UserId`, `OwnerId`, or tenant discriminator anywhere in `EntityBase` or any derived model. Any caller with API access sees every other user's data.

**Code Example (`GoogleFireBaseGenericRepository.cs` lines 62–78):**
```csharp
public async Task<ICollection<TEntity>> Get()
{
    var snapshot = await dbContext.Collection.GetSnapshotAsync(); // returns ALL documents
    var res = snapshot.Documents.Select(f => f.ConvertTo<TEntity>());
    return res.AsQueryable().ToList();
}
```

**Remediation:**
1. Add `public string OwnerId { get; set; }` to `EntityBase` (or at minimum to `ShoppingList`, `Shop`, `MealRecipe`).
2. Add a filtered overload to `IGenericRepository<T>`:
```csharp
Task<ICollection<T>> GetByOwner(string ownerId);
```
3. In `GoogleFireBaseGenericRepository`, implement with a Firestore where-clause:
```csharp
public async Task<ICollection<TEntity>> GetByOwner(string ownerId)
{
    var query = dbContext.Collection.WhereEqualTo("OwnerId", ownerId);
    var snapshot = await query.GetSnapshotAsync();
    return snapshot.Documents.Select(f => f.ConvertTo<TEntity>()).ToList();
}
```
4. Set `OwnerId` from the authenticated principal in all POST handlers before persisting.

---

### 1.3 — `staticwebapp.config.json` Exposes All API Routes to Anonymous Role

**Severity: 🟠 High**

**File:** `Client/wwwroot/staticwebapp.config.json` lines 6–10

```json
"routes": [
  {
    "route": "/api/*",
    "allowedRoles": ["anonymous"]
  }
]
```

This SWA routing configuration explicitly permits unauthenticated access to all API routes at the gateway level, bypassing any future server-side auth that might be added.

**Remediation:** Change to `"authenticated"` once SWA auth is enabled:
```json
{
  "route": "/api/*",
  "allowedRoles": ["authenticated"]
}
```

---

### 1.4 — `DebugFunction` Exposed in Production

**Severity: 🟠 High**

**File:** `Api/Controllers/DebugFunction.cs` lines 18–27

```csharp
[Function("DebugFunction")]
public HttpResponseData Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
{
    response.WriteStringAsync("Welcome to Azure Functions! Its up and running");
    return response;
}
```

Confirms to attackers that the Functions host is alive and reachable. Should not exist in production builds.

**Remediation:**
1. Wrap with a compile-time `#if DEBUG` guard, or
2. Change to `AuthorizationLevel.Admin` (requires master key), or
3. Delete the function entirely. It provides no production value.

---

## 2. Input Validation & Sanitization

### 2.1 — `IsValid()` Never Enforced at API Boundary

**Severity: 🟠 High**

**File:** `Shared/BaseModels/EntityBase.cs` line 28, all controllers

`EntityBase.IsValid()` provides a `Name not empty` check. `WeekMenuModel.IsValid()` checks `WeekNumber > 0 && Year > 0`. **Neither is ever called in any controller before persisting data.**

**Code Example (`ShoppingListController.cs` lines 78–94):**
```csharp
var requestBody = await req.ReadFromJsonAsync<ShoppingListModel>();
var shoppinglist = mapper.Map<ShoppingList>(requestBody);
// No validation — null Name, empty object, etc. pass straight to Firestore
var addRes = await repo.Insert(shoppinglist);
```

**Remediation:** Add validation before any Insert/Update:
```csharp
var requestBody = await req.ReadFromJsonAsync<ShoppingListModel>();
if (requestBody == null || !requestBody.IsValid())
    return await GetNoContentRespons("Invalid or missing request body", req);
var shoppinglist = mapper.Map<ShoppingList>(requestBody);
```

---

### 2.2 — No DataAnnotations on Model Properties

**Severity: 🟡 Medium**

**Files:** All models in `Shared/HandlelisteModels/`

No model property has `[Required]`, `[StringLength]`, `[Range]`, or `[RegularExpression]` constraints. String fields accept arbitrarily long values. This means:
- A `Name` field could receive a 100 MB string, stored in Firestore at cost.
- A `PopularityScore` can be any numeric value with no bounds checking.

**Remediation:** Add DataAnnotations:
```csharp
public class ShoppingListModel
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; }
    // ...
}
```

---

### 2.3 — Route ID Parameters Typed as `object` Instead of `string`

**Severity: 🟡 Medium**

**Files:**
- `Api/Controllers/ShoppingListController.cs` line 126: `object id`
- `Api/Controllers/ShopItemCategoryController.cs` line 75: `object id`
- `Api/Controllers/MealRecipeController.cs` line 106: `object id`

Using `object id` means the value is passed to Firestore's `Collection.Document(id.ToString())` without format validation. A malformed or path-traversal ID (e.g., `../../admin`) could cause unexpected behaviour.

**Remediation:** Change all route parameters to `string id` and add format validation:
```csharp
public async Task<HttpResponseData> RunOne(
    [HttpTrigger(AuthorizationLevel.Function, "get", "delete",
        Route = "shoppinglist/{id}")] HttpRequestData req,
    string id)  // was: object id
{
    if (string.IsNullOrWhiteSpace(id) || id.Length > 128)
        return req.CreateResponse(HttpStatusCode.BadRequest);
```

---

### 2.4 — Blocking `.Result` Call in `ShopsController`

**Severity: 🟡 Medium**

**File:** `Api/Controllers/ShopsController.cs` line 51

```csharp
var newShop = req.ReadFromJsonAsync<ShopModel>();  // NOT awaited
if (newShop.Result == null) return await GetErroRespons("No content", req);
Shop updatOrInsert = mapper.Map<Shop>(newShop.Result);  // blocking .Result
```

`.Result` blocks the calling thread, degrading throughput and risking deadlocks in high-concurrency scenarios (Azure Functions scales horizontally and thread starvation is a real concern).

**Remediation:**
```csharp
var newShop = await req.ReadFromJsonAsync<ShopModel>();
if (newShop == null) return await GetErroRespons("No content in shop body", req);
Shop updatOrInsert = mapper.Map<Shop>(newShop);
```

---

### 2.5 — No Request Body Size Limit

**Severity: 🟡 Medium**

**File:** `Api/host.json`

There is no `maxRequestBodySize` configuration in `host.json`. By default the Azure Functions HTTP trigger accepts request bodies up to 100 MB. A malicious caller could send large payloads to drive up Firestore storage costs or cause memory pressure.

**Remediation:** Add to `host.json`:
```json
"extensions": {
    "http": {
        "routePrefix": "api",
        "maxOutstandingRequests": 200,
        "maxConcurrentRequests": 100,
        "dynamicThrottlesEnabled": true
    }
}
```
And consider adding request-body size validation in `ControllerBase`.

---

## 3. Firestore Data Access

### 3.1 — CRITICAL: `GetCollectionKey()` Falls Through to `"misc"` for 5 Entity Types

**Severity: 🔴 Critical**

**File:** `Shared/Repository/GoogleDbContext.cs` lines 38–49

```csharp
public string GetCollectionKey(Type toTypeGet)
{
    if (toTypeGet == typeof(ShopItem))        return "shopitems";
    if (toTypeGet == typeof(ItemCategory))    return "itemcategories";
    if (toTypeGet == typeof(ShoppingList))    return "shoppinglists";
    if (toTypeGet == typeof(Shop))            return "shopcollection";
    return "misc";  // ← ALL OTHER TYPES SILENTLY FALL THROUGH HERE
}
```

Five registered entity types silently write to the same `"misc"` Firestore collection:

| Entity | Expected Collection | Actual Collection (bug) |
|---|---|---|
| `FrequentShoppingList` | `frequentlists` | **`misc`** |
| `MealRecipe` | `mealrecipes` | **`misc`** |
| `MealIngredient` | `mealingredients` | **`misc`** |
| `WeekMenu` | `weekmenus` | **`misc`** |
| `DailyMeal` | `dailymeals` | **`misc`** |

All 5 entity types share the same collection. Documents from different types will overwrite each other if they share the same auto-generated ID prefix. `ConvertTo<TEntity>()` on a mis-typed document will silently return a partially-hydrated object or throw at runtime.

**Remediation:** Fix immediately before any production deployment:
```csharp
public string GetCollectionKey(Type toTypeGet)
{
    if (toTypeGet == typeof(ShopItem))              return "shopitems";
    if (toTypeGet == typeof(ItemCategory))          return "itemcategories";
    if (toTypeGet == typeof(ShoppingList))          return "shoppinglists";
    if (toTypeGet == typeof(Shop))                  return "shopcollection";
    if (toTypeGet == typeof(FrequentShoppingList))  return "frequentlists";
    if (toTypeGet == typeof(MealRecipe))            return "mealrecipes";
    if (toTypeGet == typeof(MealIngredient))        return "mealingredients";
    if (toTypeGet == typeof(WeekMenu))              return "weekmenus";
    if (toTypeGet == typeof(DailyMeal))             return "dailymeals";
    throw new ArgumentException($"No Firestore collection mapped for type {toTypeGet.Name}");
}
```
Note: throw instead of returning `"misc"` so future unregistered types fail fast at startup, not silently in production.

---

### 3.2 — Firestore Security Rules Not Audited / Likely Permissive

**Severity: 🟠 High**

**File:** Not present in this repository

Firestore security rules are defined in the Firebase console or `firestore.rules` file. No such file exists in this repository, suggesting rules are either at their default (deny all — which would break the app) or have been set permissively (allow all reads/writes). The app authenticates to Firestore using a service account (admin SDK), which bypasses Firestore security rules entirely — so client-side rules are irrelevant here, but:

- If the Firebase project is ever configured to allow client-side access (e.g., a future mobile app), permissive rules would expose all data.
- There is no server-enforced document-level access control within the Functions layer.

**Remediation:**
1. Ensure Firestore security rules are set to `deny all` for client access (since all access is via the admin SDK in Functions).
2. Commit a `firestore.rules` file to the repository for auditability.
3. Do not enable client-side Firebase SDK access without adding per-user rules.

---

### 3.3 — Exceptions in Repository Layer Silently Swallowed

**Severity: 🟠 High**

**File:** `Shared/Repository/GoogleFireBaseGenericRepository.cs` lines 74–77, 93–96

```csharp
catch (Exception e)
{
    Console.Write(e);  // Written to console, NOT to ILogger — invisible in Azure Monitor
}
return null;  // caller receives null, interprets as data-not-found, not as error
```

This pattern means Firestore authentication failures, network errors, quota exceeded errors, and permission errors all return `null` to the caller. The caller then returns a 500 with the message "could not get shops" — with no traceable log entry in Application Insights or Azure Monitor.

**Remediation:**
1. Inject `ILogger<GoogleFireBaseGenericRepository<TEntity>>` and use `_logger.LogError(e, ...)`.
2. Consider propagating specific exception types so controllers can distinguish "not found" from "Firestore error":
```csharp
catch (Exception e)
{
    _logger.LogError(e, "Firestore {Operation} failed for collection {Collection}",
        nameof(Get), dbContext.CollectionKey);
    throw; // let controller handle with try/catch and return appropriate HTTP status
}
```

---

### 3.4 — No Pagination — Full Collection Scan on Every Request

**Severity: 🟡 Medium**

**File:** `Shared/Repository/GoogleFireBaseGenericRepository.cs` lines 62–78

Every `Get()` call issues a full `GetSnapshotAsync()` — no `Limit()`, `StartAfter()`, or cursor. For large collections (e.g., hundreds of shop items or frequent lists), this returns unbounded data, increasing both latency and Firestore read costs.

**Remediation:** Add pagination support to `IGenericRepository<T>`:
```csharp
Task<(ICollection<T> Items, DocumentSnapshot? LastDocument)> GetPaged(
    int pageSize, DocumentSnapshot? startAfter = null);
```

---

### 3.5 — Singleton Repository Captures Transient DbContext

**Severity: 🟡 Medium**

**File:** `Api/Program.cs` lines 31–48, `Shared/Repository/GoogleFireBaseGenericRepository.cs` constructor

`GoogleFireBaseGenericRepository<T>` is registered as `Singleton` but its constructor dependency `IGoogleDbContext` is registered as `Transient`. Because singletons are resolved once at startup, each of the 7 repository singletons captures a **separate** `GoogleDbContext` instance, each creating its own `FirestoreDb` connection and authentication flow. This works but wastes 7 authenticated connections instead of 1.

Worse, the `GoogleDbContext` constructor sets `CollectionKey` and `Collection` as mutable properties — if two singletons share the same context (due to future refactoring), they would clobber each other's collection reference.

**Remediation:** Register `IGoogleDbContext` as `Singleton`:
```csharp
services.AddSingleton<IGoogleDbContext, GoogleDbContext>(); // was Transient
```
And redesign `GoogleFireBaseGenericRepository<T>` to resolve the collection reference per-call rather than storing it as instance state.

---

## 4. Error Handling & Information Disclosure

### 4.1 — Exception Messages Returned Verbatim to API Consumers

**Severity: 🟠 High**

**Files:**
- `Api/Controllers/ControllerBase.cs` lines 12–17
- `Api/Controllers/ShopsController.cs` line 75
- `Api/Controllers/ShopsItemsController.cs` lines 73, 112
- `Api/Controllers/FrequentShoppingListController.cs` lines 98–99

```csharp
// ShopsController.cs line 75
catch (System.Exception e)
{
    _logger.LogError(e, $"Something went wrong in shops controller, in medtod {req.Method}");
    return await GetErroRespons(e.Message, req);  // ← raw exception message to client
    throw; // unreachable dead code
}
```

`e.Message` from Firestore SDK, AutoMapper, or the .NET runtime may contain:
- Collection names and document IDs
- Firebase project details
- Internal stack context
- Credential-related messages (e.g., "The Application Default Credentials are not available")

**Remediation:**
1. Return a generic message externally; log the full exception internally:
```csharp
// ControllerBase.cs
protected async Task<HttpResponseData> GetErroRespons(
    Exception ex, string publicMessage, HttpRequestData req)
{
    // Log internally with full detail
    _logger.LogError(ex, "Internal error: {PublicMessage}", publicMessage);
    // Return only the safe public message
    var response = req.CreateResponse(HttpStatusCode.InternalServerError);
    await response.WriteStringAsync(publicMessage);
    return response;
}
```
2. Remove the unreachable `throw;` in `ShopsController.cs` line 76.

---

### 4.2 — `ShopItemCategoryController.RunOne` Has No Exception Handling

**Severity: 🟠 High**

**File:** `Api/Controllers/ShopItemCategoryController.cs` lines 75–98

The entire `RunOne` method has no `try/catch`. Any Firestore error or mapping exception will surface as an unhandled exception, potentially returning a raw .NET exception response body to the caller and losing the structured logging context.

**Remediation:** Wrap in try/catch consistent with other controllers:
```csharp
[Function("itemcategory")]
public async Task<HttpResponseData> RunOne(...)
{
    try
    {
        // existing logic
    }
    catch (Exception e)
    {
        _logger.LogError(e, "Error in itemcategory/{Id}, method {Method}", id, req.Method);
        return await GetErroRespons("An error occurred processing the item category", req);
    }
}
```

---

### 4.3 — Unawaited `WriteString` in DELETE Response Body

**Severity: 🟡 Medium**

**File:** `Api/Controllers/ShoppingListController.cs` lines 158–160

```csharp
var errorrespons = req.CreateResponse(HttpStatusCode.InternalServerError);
errorrespons.WriteString("Could not delete item"); // NOT awaited — fire-and-forget
// falls through to: return req.CreateResponse(HttpStatusCode.NotFound);
```

The error response body is silently discarded; the client receives a 404 instead of a 500 with an error message. The caller has no way to distinguish "not found" from "delete failed".

**Remediation:**
```csharp
var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
await errorResponse.WriteStringAsync("Could not delete item");
return errorResponse;
```

---

### 4.4 — Incorrect HTTP Status Codes (500 Instead of 404 for Not Found)

**Severity: 🟡 Medium**

**File:** `Api/Controllers/ShoppingListController.cs` lines 132–136

```csharp
var result = await repo.Get(id);
if (result == null)
{
    var res = req.CreateResponse(HttpStatusCode.InternalServerError); // wrong — should be 404
    return res;
}
```

Returning 500 for a missing record confuses clients and monitoring tools (triggers alerts for normal "not found" conditions), and hides actual 500 errors.

Additional occurrence: `ShopsController.RunOne` returns 500 from `GetErroRespons` when `repo.Get(id)` returns null (could be "not found", line 92).

**Remediation:** Distinguish between "not found" and "error":
```csharp
if (result == null)
    return req.CreateResponse(HttpStatusCode.NotFound);
```

---

### 4.5 — Error Logging Using Wrong Log Level in `ShopsItemsController`

**Severity: 🟢 Low**

**File:** `Api/Controllers/ShopsItemsController.cs` lines 73, 112

```csharp
catch (System.Exception e)
{
    _logger.LogInformation(e.Message); // should be LogError
    return await GetErroRespons(e.Message, req);
}
```

Using `LogInformation` for caught exceptions means these errors are invisible in Application Insights alert rules that filter on `Error` or `Critical` severity.

**Remediation:** Change to `_logger.LogError(e, "Error in shopitems/{Method}", req.Method)`.

---

### 4.6 — Request Body Logged in Error Path

**Severity: 🟡 Medium**

**File:** `Api/Controllers/MealRecipeController.cs` line 41

```csharp
_logger.LogError($"Could not get any meal recipes. Error: {req.ReadAsString()}");
```

`req.ReadAsString()` on a GET request returns the query string or empty string — in this context it is harmless. However, if this pattern were copied to POST/PUT paths, it would log the entire request body (including any user-supplied data) to Application Insights, which could include PII or sensitive content.

**Remediation:** Remove `req.ReadAsString()` from log messages. Log the method and path instead:
```csharp
_logger.LogError("Could not get meal recipes. Method: {Method}, Path: {Path}",
    req.Method, req.Url.PathAndQuery);
```

---

## 5. CORS Configuration

### 5.1 — Wildcard CORS in Development Settings

**Severity: 🟡 Medium**

**File:** `Api/local.settings.json` line 9, `Api/local.settings.example.json` line 9

```json
"Host": {
    "LocalHttpPort": 7071,
    "CORS": "*",      ← wildcard — allows all origins
    "CORSCredentials": false
}
```

This is in local settings (gitignored, not deployed) so it does not affect production. However, `CORSCredentials: false` correctly prevents credential-bearing cross-origin requests.

In production, CORS is handled by the Azure Static Web Apps routing layer, not by the Functions host — this is the correct architecture for SWA deployments. However, **this is not documented anywhere in the codebase**.

**Remediation:**
1. Add a comment to `local.settings.example.json` explaining that `CORS: "*"` is dev-only.
2. Add a `CORS` section to `host.json` or `README.md` documenting that production CORS is managed by SWA.

---

### 5.2 — No Rate Limiting

**Severity: 🟡 Medium**

**Files:** `Api/host.json`, `Api/Program.cs`

There is no rate limiting, throttling, or DDoS protection at the API level. The Azure Functions Consumption Plan provides natural concurrency limits, but:
- A caller can issue unlimited sequential requests.
- Each request that reaches Firestore incurs a billable read/write.
- Anonymous endpoints (`shops`, `shopitems`, `itemcategorys`) are especially exposed.

**Remediation:**
1. Enable Azure API Management (APIM) or Azure Front Door WAF for production if the app becomes public.
2. As a minimum, enable `dynamicThrottlesEnabled: true` in `host.json`:
```json
"extensions": {
    "http": {
        "dynamicThrottlesEnabled": true
    }
}
```

---

## 6. Secrets Management

### 6.1 — Google Cloud Project ID Hardcoded in Source

**Severity: 🟡 Medium**

**File:** `Shared/Repository/GoogleDbContext.cs` line 14

```csharp
readonly string _projectId = "supergnisten-shoppinglist";
```

The Firebase/Firestore project ID is hardcoded. This is a **low-security risk** (project IDs are not secrets), but it:
- Makes it impossible to use a separate dev/staging Firestore project without a code change.
- Violates the principle of environment-based configuration.
- The project name is now embedded in git history.

**Remediation:** Read from environment variable:
```csharp
public GoogleDbContext()
{
    var projectId = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT")
        ?? throw new InvalidOperationException("GOOGLE_CLOUD_PROJECT env var not set");
    var json = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS")
        ?? throw new InvalidOperationException("GOOGLE_CREDENTIALS env var not set");
    // ...
}
```

---

### 6.2 — `GOOGLE_CREDENTIALS` Handling Is Correct

**Severity: 🟢 (Positive Finding)**

**File:** `Shared/Repository/GoogleDbContext.cs` lines 19–27

```csharp
var json = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS");
if (Path.IsPathFullyQualified(json)) // local dev: env var is a file path
    json = File.ReadAllText(json);   // read the JSON from disk
// production: env var IS the JSON content (Azure App Settings)
```

This dual-mode pattern (path in dev, raw JSON in prod) is a sound approach. The `local.settings.json` is properly gitignored. ✅

**Remaining concern:** A `NullReferenceException` is thrown with an uninformative message ("Fant ikke googl cred") if the env var is missing. A more descriptive exception improves debugging:
```csharp
if (json == null)
    throw new InvalidOperationException(
        "GOOGLE_CREDENTIALS environment variable is required. " +
        "Set it to the path of your service account JSON file (local) " +
        "or the JSON content directly (production).");
```

---

### 6.3 — `local.settings.json` Properly Gitignored, Not Published

**Severity: 🟢 (Positive Finding)**

**File:** `Api/Api.csproj` lines 25–28

```xml
<None Update="local.settings.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <CopyToPublishDirectory>Never</CopyToPublishDirectory>  <!-- ✅ not published -->
</None>
```

`local.settings.json` is excluded from publish output and excluded from git via `.gitignore`. No credentials are at risk of being committed to source control. ✅

---

### 6.4 — README Contains Developer's Local File System Path

**Severity: 🟢 Low**

**File:** `README.md` lines 171, 176, 179

```markdown
Save it to a secure location (e.g., `D:\Privat\GIT\Google keys\supergnisten-shoppinglist-eb82277057ad.json`)
$env:GOOGLE_CREDENTIALS = "D:\Privat\GIT\Google keys\supergnisten-shoppinglist-eb82277057ad.json"
```

The actual credential filename suffix (`eb82277057ad`) uniquely identifies a specific GCP service account. This is in a public repository README and should be generalised.

**Remediation:** Replace with a placeholder:
```markdown
$env:GOOGLE_CREDENTIALS = "C:\path\to\your-project-service-account.json"
```

---

## 7. HTTPS / TLS

### 7.1 — HTTPS Enforced by Azure Static Web Apps Platform

**Severity: 🟢 (Positive Finding)**

Azure Static Web Apps automatically provisions and renews TLS certificates and enforces HTTPS redirects at the platform level. No application-level HTTPS configuration is needed or possible for SWA-hosted Functions. All traffic from the SWA gateway to the Functions backend is private VNet traffic (HTTPS). ✅

The development setting `"API_Prefix": "http://localhost:7071"` uses HTTP — this is acceptable for local development only.

---

## 8. HTTP Security Headers

### 8.1 — No Security Headers Configured

**Severity: 🟠 High**

**File:** `Client/wwwroot/staticwebapp.config.json`

The SWA configuration file contains **no HTTP security headers**. The following are entirely absent:

| Header | Risk Without It |
|---|---|
| `Content-Security-Policy` | XSS attacks can execute arbitrary scripts in-page |
| `X-Frame-Options` | Clickjacking — the app can be embedded in a malicious iframe |
| `X-Content-Type-Options` | MIME-type sniffing attacks |
| `Strict-Transport-Security` | Users may be served over HTTP if they bypass the HTTPS redirect |
| `Referrer-Policy` | API URLs may leak in Referer headers to third-party resources |
| `Permissions-Policy` | Unnecessary browser APIs (camera, microphone, geolocation) are accessible |

For a Blazor WASM application, `Content-Security-Policy` is especially important — the app loads and executes .NET assemblies as WebAssembly modules.

**Remediation:** Add a `globalHeaders` section to `staticwebapp.config.json`:

```json
{
  "globalHeaders": {
    "X-Frame-Options": "DENY",
    "X-Content-Type-Options": "nosniff",
    "Referrer-Policy": "strict-origin-when-cross-origin",
    "Strict-Transport-Security": "max-age=31536000; includeSubDomains",
    "Permissions-Policy": "camera=(), microphone=(), geolocation=()",
    "Content-Security-Policy": "default-src 'self'; script-src 'self' 'wasm-unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self' https://purple-meadow-02a012403.azurestaticapps.net"
  }
}
```

> **Note:** Blazor WASM requires `'wasm-unsafe-eval'` in `script-src` for WebAssembly execution. Do not use `'unsafe-eval'` — the more restrictive WASM-specific directive is correct.

---

## 9. AutoMapper / Data Mapping

### 9.1 — UI-State Properties Written to Firestore

**Severity: 🟡 Medium**

**File:** `Api/ShoppingListProfile.cs`, `Shared/BaseModels/EntityBase.cs`

`EntityBase` contains two UI-state properties with no `[FirestoreProperty]` attribute or `[JsonIgnore]`:

```csharp
public bool EditClicked { get; set; }       // UI state
public string CssComleteEditClassName { get; }  // computed from EditClicked
```

AutoMapper's `ReverseMap()` maps all public properties by default. If a client sends `EditClicked: true` in a POST/PUT body, AutoMapper maps it into the Firestore entity. While `EditClicked` has no `[FirestoreProperty]` attribute (so Firestore ignores it on write), AutoMapper may still map it in unexpected ways on read, and this pattern becomes a liability if the Firestore model is ever extended.

**Remediation:**
```csharp
// ShoppingListProfile.cs
this.CreateMap<ShoppingListModel, ShoppingList>()
    .ForMember(dest => dest.EditClicked, opt => opt.Ignore())
    .ReverseMap();
```

---

### 9.2 — `ShopItemCategoryController` Returns Firestore Model Instead of DTO

**Severity: 🟡 Medium**

**File:** `Api/Controllers/ShopItemCategoryController.cs` lines 57, 86

```csharp
await okRespons.WriteAsJsonAsync(mapper.Map<ItemCategory>(newItemCat)); // Firestore model
await okRespons.WriteAsJsonAsync(mapper.Map<ItemCategory>(itemCategories)); // Firestore model
```

The controller maps to `ItemCategory` (Firestore model) instead of `ItemCategoryModel` (DTO). The Firestore model is serialized directly to the HTTP response, exposing any internal Firestore-specific properties or annotations. The correct return type is `ItemCategoryModel`.

**Remediation:**
```csharp
await okRespons.WriteAsJsonAsync(mapper.Map<ItemCategoryModel>(newItemCat));
await okRespons.WriteAsJsonAsync(mapper.Map<ItemCategoryModel>(itemCategories));
```

---

## 10. Logging Quality

### 10.1 — Repository Errors Written to `Console` Instead of `ILogger`

**Severity: 🟡 Medium**

**Files:**
- `Shared/Repository/GoogleFireBaseGenericRepository.cs` lines 75, 95
- `Shared/Repository/GoogleDbContext.cs` line 28

```csharp
Console.Write(e);           // GoogleFireBaseGenericRepository — exception swallowed
Console.WriteLine("Googel cred is found");  // GoogleDbContext — credential noise
```

`Console.Write` output is not captured by Azure Monitor / Application Insights in a Functions host. These log entries are silently lost in production.

**Remediation:** Inject `ILogger<T>` and use structured logging:
```csharp
_logger.LogError(e, "Firestore Get failed for collection {Collection}", dbContext.CollectionKey);
```

---

### 10.2 — PII Risk in Debug-Level Logs

**Severity: 🟢 Low**

**Files:**
- `Api/Controllers/FrequentShoppingListController.cs` line 72
- `Api/Controllers/ShoppingListController.cs` lines 63, 69, 133, 143

```csharp
_logger.LogInformation($"Updating frequent list. ID: {updateListModel.Id ?? "NULL"}, " +
    $"Name: {updateListModel.Name}, Items count: {updateListModel.Items?.Count ?? 0}");
```

User-supplied `Name` values are logged. If the app is ever used for actual grocery shopping, list names (e.g., "Weekly shop for the Smith family") could constitute PII in some jurisdictions (GDPR). Log IDs and counts — not user-supplied string values.

---

## 11. Dependency Versions

**File:** `Api/Api.csproj`

| Package | Current Version | Status |
|---|---|---|
| `AutoMapper` | 12.0.0 | Current major, no known CVEs |
| `Google.Cloud.Firestore` | 3.0.0 | Current — check for updates (3.x released) |
| `Microsoft.Azure.Functions.Worker` | 2.0.0 | Current GA for .NET 8 isolated |
| `Microsoft.Azure.Functions.Worker.Extensions.Http` | 3.3.0 | Current |
| `Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore` | 1.3.3 | Current |

No immediately critical CVEs identified in current versions. Recommend enabling Dependabot alerts on the repository for automated dependency monitoring.

---

## Priority Summary

| Priority | Finding | File | Fix Complexity |
|---|---|---|---|
| 🔴 Critical | `GetCollectionKey()` missing 5 entity types — data corruption | `GoogleDbContext.cs:38` | Simple (add 5 if-statements) |
| 🔴 Critical | No user auth — entire API is open | All controllers | Large (requires SWA auth integration) |
| 🔴 Critical | No per-user data isolation — all users see all data | All repos + models | Large (requires `OwnerId` + query filters) |
| 🟠 High | `staticwebapp.config.json` routes all `/api/*` as anonymous | `staticwebapp.config.json:9` | Simple (change role to `authenticated`) |
| 🟠 High | No HTTP security headers (CSP, X-Frame-Options, HSTS, etc.) | `staticwebapp.config.json` | Simple (add `globalHeaders`) |
| 🟠 High | Exception messages returned verbatim to clients | `ControllerBase.cs:14` + all controllers | Medium |
| 🟠 High | `ShopItemCategoryController.RunOne` has no exception handling | `ShopItemCategoryController.cs:75` | Simple (add try/catch) |
| 🟠 High | `DebugFunction` exposed anonymously in production | `DebugFunction.cs:18` | Simple (delete or gate) |
| 🟠 High | Repository exceptions silently swallowed with `Console.Write` | `GoogleFireBaseGenericRepository.cs:75` | Medium (inject ILogger) |
| 🟠 High | Firestore security rules not in repository or audited | Firebase console | Medium |
| 🟡 Medium | `IsValid()` never called before persisting any data | All controllers | Simple |
| 🟡 Medium | Blocking `.Result` in `ShopsController` | `ShopsController.cs:51` | Trivial (add await) |
| 🟡 Medium | Route params typed as `object` instead of `string` | `ShoppingListController.cs:126` et al. | Simple |
| 🟡 Medium | Incorrect 500 returned for not-found GET requests | `ShoppingListController.cs:134` | Simple |
| 🟡 Medium | `ShopItemCategoryController` returns Firestore model not DTO | `ShopItemCategoryController.cs:57` | Trivial |
| 🟡 Medium | `_projectId` hardcoded in source | `GoogleDbContext.cs:14` | Simple |
| 🟡 Medium | No DataAnnotations on model properties | All model files | Medium |
| 🟡 Medium | Wildcard CORS undocumented in example settings | `local.settings.example.json` | Simple (add comment) |
| 🟡 Medium | No rate limiting or request size limits | `host.json` | Simple (config change) |
| 🟡 Medium | Singleton repo captures transient DbContext | `Program.cs:20` | Medium |
| 🟡 Medium | UI-state properties (`EditClicked`) may be mapped to Firestore | `ShoppingListProfile.cs` | Simple |
| 🟢 Low | Unawaited `WriteString` in DELETE path | `ShoppingListController.cs:159` | Trivial |
| 🟢 Low | `ShopsItemsController` uses `LogInformation` for errors | `ShopsItemsController.cs:73` | Trivial |
| 🟢 Low | Request body logged in error path | `MealRecipeController.cs:41` | Trivial |
| 🟢 Low | Developer file path in README | `README.md:171` | Trivial |
| 🟢 Low | `Console.WriteLine` in `GoogleDbContext` | `GoogleDbContext.cs:28` | Trivial |
| 🟢 Low | Uninformative null-ref exception for missing credentials | `GoogleDbContext.cs:24` | Trivial |
| 🟢 Low | No Dependabot / vulnerability scanning configured | `.github/` | Simple |

---

## Quick Wins (Can Be Done Now, < 1 Hour Total)

1. **`GetCollectionKey()` fix** — add 5 missing entity mappings + change fallback to `throw`. (`GoogleDbContext.cs`)
2. **Security headers** — add `globalHeaders` block to `staticwebapp.config.json`. (~10 lines of JSON)
3. **`DebugFunction`** — delete or add `#if DEBUG` guard.
4. **`ShopsController` blocking `.Result`** — add `await`.
5. **Fix `ShopItemCategoryController`** — add try/catch + return `ItemCategoryModel` not `ItemCategory`.
6. **Fix not-found 500 → 404** in `ShoppingListController.RunOne`.
7. **Await `WriteString`** in `ShoppingListController.RunOne` DELETE path.
8. **Change `LogInformation` to `LogError`** in `ShopsItemsController` catch blocks.
