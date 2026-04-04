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

- **Issue #31 — LastModified Migration Endpoint — sprint/2**
- D10 resolved: Extracted inline lazy migration from `ShoppingListController` (both `RunAll` GET and `RunOne` GET) into a new one-time `GET /api/admin/migrate-lastmodified` endpoint in `Api/Controllers/AdminController.cs`.
- Auth gate: Endpoint checks `"admin"` role in SWA-injected `x-ms-client-principal` via `GetCurrentUser(req)`. Returns `403 Forbidden` if role is absent. Uses `AuthorizationLevel.Function` as an additional layer.
- Response shape: `{ "migratedCount": N }` — consistent with `MigrateFrequentListsController` pattern.
- GET endpoints are now read-only: `ShoppingListController.RunAll` and `RunOne` no longer perform any writes on GET. Eliminates N+1 write pattern.
- No new DI registrations needed: `IGenericRepository<ShoppingList>` was already registered in both debug and production blocks in `Program.cs`.

### Issue #28 — Shop Deletion Safeguards — sprint/2

- Added `GET /api/shop/{id}/dependencies` endpoint as `[Function("shopdependencies")]` with route `shop/{id}/dependencies`.
- Returns `{ "dependencyCount": N, "dependentLists": ["name", ...] }` JSON.
- **Key finding**: `ShoppingList` has NO `ShopId` property. Shop-based sort order is purely client-side (stored in client state, not persisted in Firestore). The endpoint therefore always returns `dependencyCount: 0`. Code is structured so a future `ShopId` field can plug in trivially.
- Used `ShoppingList.ListId` field as the query hook — it's not a shop reference today, but named comment documents this for future devs.
- Injected `IGenericRepository<ShoppingList>` into `ShopsController` constructor (already registered in both `#if DEBUG` and production DI blocks in `Program.cs` — no new registration needed).
- Added structured `ILogger.LogInformation` for DELETE before the delete executes — logs shop name (resolved from repo) and shop id.
- Updated `ShopsControllerTests`: added `_mockShoppingListRepo`, updated constructor call, added 4 `RunDependencies` tests, updated 2 DELETE tests to mock the `Get()` call now required for name logging.
- 122 API tests passing post-change.

### MealRecipeController - Phase 1 API (2026-04-04)
- Controller path: Api/Controllers/MealRecipeController.cs - pre-existed with defects; replaced in full
- Three Functions: [Function(mealrecipes)] GET all by PopularityScore DESC/POST/PUT, [Function(mealrecipe)] GET/{id} 404/soft-DELETE, [Function(mealrecipesimport)] POST bulk import route mealrecipes/import
- Soft delete: GET item, set IsActive=false + LastModified=UtcNow, call _repository.Update(). No hard delete.
- POST sets IsActive=true and LastModified=UtcNow
- Program.cs: MealRecipe and WeekMenu already registered in both branches. InventoryItem skipped, model not in Shared yet.
- ShoppingListProfile.cs: already had all 4 Phase 1 mappings. No changes needed.
- IGenericRepository.Delete() returns bool not T - old controller had broken null check; fixed via soft delete pattern.
- AuthorizationLevel changed to Anonymous (consistent with ShopsItemsController for SWA environment).
- Build: succeeded, 0 errors, 44 pre-existing warnings.

## Orchestration Log — 2026-04-04T05:12:37Z
**Phase 1 Meal Planning — MealRecipeController ✅ COMPLETE**
- Implemented 3 Azure Function handlers with 6 HTTP endpoints
- Soft-delete pattern: DELETE sets IsActive=false + LastModified, calls Update()
- Bulk import: POST /api/mealrecipes/import deserializes list, inserts each, applies AutoMapper
- DI registrations: MealRecipe, WeekMenu already in place (InventoryItem deferred)
- AutoMapper: all 4 Phase 1 mappings already configured
- AuthorizationLevel.Anonymous on all (consistent with SWA pattern)
- ✅ Build clean, 0 errors, 44 pre-existing warnings (no new issues)

### WeekMenuController — Phase 2 API (2026-04-07)
- Controller path: `Api/Controllers/WeekMenuController.cs` — created fresh
- Four Functions: `[Function("weekmenus")]` GET all/POST/PUT, `[Function("weekmenu")]` GET/{id}/soft-DELETE, `[Function("weekmenubyweek")]` GET by week+year, `[Function("weekmenugenerateshoppinglist")]` POST generate preview shopping list
- GET all: ordered by Year DESC, WeekNumber DESC (most recent first)
- POST: auto-generates Name as `"Uke {WeekNumber} {Year}"` when Name not provided; initializes DailyMeals to empty list if null
- Soft delete: same pattern as MealRecipeController — GET → set IsActive=false + LastModified → Update()
- `weekmenubyweek` route: `weekmenu/week/{weekNumber}/year/{year}` — scans all records, filters by weekNumber + year + IsActive
- `weekmenugenerateshoppinglist`: preview-only — loads WeekMenu, builds recipe dict in one `_mealRepository.Get()` call, aggregates ingredients (sum Quantity by ShopItemId), returns `ShoppingListModel` — does NOT persist to Firestore
- Constructor takes TWO repositories: `IGenericRepository<WeekMenu>` and `IGenericRepository<MealRecipe>`
- DI registrations for WeekMenu already existed in Program.cs — no changes needed
- AutoMapper mappings for WeekMenu/DailyMeal already in ShoppingListProfile.cs — no changes needed
- ✅ Build clean, 0 errors, 53 pre-existing warnings (no new issues)
- ✅ Integrated with 16 passing unit tests (josh-weekmenu-tests)
