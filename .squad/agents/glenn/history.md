# Project Context

- **Owner:** Daniel Aase
- **Project:** Shoppinglist — Blazor WebAssembly shopping list app with shop-specific item sorting
- **Stack:** Azure Functions v4 (.NET 9), AutoMapper, repository pattern, Google Cloud Firestore, .NET DI
- **Created:** 2026-03-22

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
- Controllers follow a function-per-endpoint pattern: `[Function("shopitems")]` (collection) and `[Function("shopitem")]` (single, `Route = "shopitem/{id}"`).
- `Api/Program.cs` has `#if DEBUG` blocks — MemoryGenericRepository in debug, GoogleFireBaseGenericRepository in production.
- `LastModified = DateTime.UtcNow` must be set on all POST and PUT operations. GET operations do lazy migration for null values.
- `Api/ShoppingListProfile.cs` holds all AutoMapper mappings with `.ReverseMap()`.
- Current goals: implement authentication middleware and secure all API endpoints.

### Security Audit — 2025-01-29
- **CRITICAL BUG**: `GoogleDbContext.GetCollectionKey()` only maps 4 entity types. `FrequentShoppingList`, `MealRecipe`, `MealIngredient`, `WeekMenu`, and `DailyMeal` all resolve to `"misc"` in Firestore production — data corruption bug.
- **Auth inconsistency**: `ShoppingListController` and `MealRecipeController` use `AuthorizationLevel.Function`; all other controllers are `AuthorizationLevel.Anonymous`. No user-level auth exists anywhere.
- **No per-user data isolation**: `IGenericRepository<T>.Get()` returns all records with no user filtering. `EntityBase` has no `OwnerId` field.
- **Exception messages leaked to callers**: `GetErroRespons(e.Message, req)` pattern exposes internal error text.
- **ShopsController** uses `.Result` blocking call on `ReadFromJsonAsync` — deadlock risk under load.
- **WeekMenuController is missing entirely**: DI registration for `WeekMenu`/`DailyMeal` also absent.
- **AutoMapper** does not ignore UI-state properties (`EditClicked`, `CssComleteEditClassName`) in reverse mapping direction.
- **Azure SWA auth path**: Parse `x-ms-client-principal` header injected by the SWA runtime — no external JWT library needed.
- Full findings written to `.squad/agents/glenn/api-findings.md`.

### Sprint 0 Controller Fixes — 2026-03-23 ✅ COMPLETED
- **Issue #21 fixed**: Replaced `.Result` blocking call in `ShopsController.Run()` with `await`. Scanned all controllers — only ShopsController had the anti-pattern.
- **Issue #20 fixed**: Added `try/catch` to `ShopItemCategoryController.RunOne()`. Also fixed a pre-existing GET return bug (method was creating and writing to `okRespons` then returning a *different* `NoContent` response — body was silently discarded).
- **S4 compliance**: New catch block in RunOne uses generic error message (`"An unexpected error occurred"`) — does NOT expose exception details. Exception logged in full via `_logger.LogError(e, msg)`.
- **Testing pattern**: Azure Functions `HttpRequestData.CreateResponse(HttpStatusCode)` is a non-mockable extension method. Must mock `CreateResponse()` (no args) instead and let the extension set StatusCode. Setup: `mockRequest.Setup(r => r.CreateResponse()).Returns(mockResponse.Object)` with `mockResponse.SetupProperty(r => r.StatusCode)`.
- **Pre-existing test failures**: `ShoppingListControllerRealTests` (9 tests) were already failing before these changes — unrelated, not introduced by this PR.
- **PR #36 merged** (`squad/sprint0-controller-fixes`)

## Branching Strategy Update (2026-03-28)

**Broadcast by:** Peter (Lead) — Daniel Aase directive

**New branching strategy is in effect as of 2026-03-28:**
- `development` is now the base branch for ALL feature branches
- Cut new branches from `development`, not `main`
- Merging into `development` triggers a **staging** deployment (Azure SWA staging environment)
- Only `main` deploys to **production** — never push features directly to `main`
- PRs for feature work target `development`; only release PRs target `main`

**CI/CD updated:** `.github/workflows/azure-static-web-apps-purple-meadow-02a012403.yml` now has three separate jobs: production (main), staging (development), and PR previews.


### Auth Infrastructure — squad/auth-workflow ✅ COMPLETED
- **Files created**: `Api/Auth/ClientPrincipal.cs`, `Api/Auth/AuthExtensions.cs`
- **ClientPrincipal.cs**: Parses `x-ms-client-principal` header (base64 JSON). Uses `HttpRequestData` (Azure Functions Isolated Worker pattern — NOT `HttpRequest`). Returns null on missing/malformed header, never throws. `IsAuthenticated` requires non-null `UserId` AND `"authenticated"` in `UserRoles`.
- **AuthExtensions.cs**: Extension methods on `HttpRequestData` — `GetClientPrincipal()`, `IsAuthenticated()`, `GetUserId()`, `GetUserName()`.
- **ControllerBase.cs updated**: Added `GetCurrentUser(req)`, `GetCurrentUserId(req)`, `GetCurrentUserName(req)` helpers. All take `HttpRequestData` parameter (no ambient `Request` property in Azure Functions isolated worker).
- **DebugFunction.cs updated**: Production guard via `#if !DEBUG` — returns `HttpStatusCode.NotFound` in Release builds.
- **Program.cs updated**: Added `ILogger<Program>` startup log after host build. Added `using Microsoft.Extensions.Logging;`.
- **Key architectural decision**: v1 = family app (D2). Auth is parsed everywhere but NOT enforced as a gate on reads. Write enforcement deferred to future sprint. Principal available for logging and v2 FamilyId path.
- **Auth type**: Microsoft provider only (D14). SWA injects header automatically — zero JWT library dependencies.
- **99 API tests passing** post-change.

### Issue #31 — LastModified Migration Endpoint — sprint/2

- **D10 resolved**: Extracted inline lazy migration from `ShoppingListController` (both `RunAll` GET and `RunOne` GET) into a new one-time `GET /api/admin/migrate-lastmodified` endpoint in `Api/Controllers/AdminController.cs`.
- **Auth gate**: Endpoint checks `"admin"` role in SWA-injected `x-ms-client-principal` via `GetCurrentUser(req)`. Returns `403 Forbidden` if role is absent. Uses `AuthorizationLevel.Function` as an additional layer.
- **Response shape**: `{ "migratedCount": N }` — consistent with `MigrateFrequentListsController` pattern.
- **GET endpoints are now read-only**: `ShoppingListController.RunAll` and `RunOne` no longer perform any writes on GET. Eliminates N+1 write pattern.
- **No new DI registrations needed**: `IGenericRepository<ShoppingList>` was already registered in both debug and production blocks in `Program.cs`.
