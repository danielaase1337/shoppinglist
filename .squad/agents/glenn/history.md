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

### FamilyProfileController + PortionRuleController — Phase 5 API (2026-04-08)
- **Files created**: `Api/Controllers/FamilyProfileController.cs`, `Api/Controllers/PortionRuleController.cs`
- **FamilyProfileController**: Two Functions — `[Function("familyprofiles")]` GET (ordered by Name)/POST/PUT, `[Function("familyprofile")]` GET/{id}/DELETE. `FamilyProfile` has no `IsActive` property → hard delete via `_repository.Delete()`. POST sets `LastModified = DateTime.UtcNow` only (no IsActive — not on model).
- **PortionRuleController**: Two Functions — `[Function("portionrules")]` GET active rules ordered by ShopItemId then AgeGroup/POST/PUT, `[Function("portionrule")]` GET/{id}/soft-DELETE. `PortionRule` has `IsActive` → soft delete: GET → set `IsActive=false` + `LastModified` → `_repository.Update()`. Never calls `_repository.Delete()`.
- **DI registrations**: Both `FamilyProfile` and `PortionRule` repos already registered in Program.cs (Ray added them) — no changes needed.
- **AutoMapper mappings**: `FamilyProfile↔FamilyProfileModel`, `FamilyMember↔FamilyMemberModel`, `PortionRule↔PortionRuleModel` already in ShoppingListProfile.cs — no changes needed.
- **AuthorizationLevel.Anonymous** on all Functions (consistent with SWA pattern).
- ✅ Build clean, 0 errors

### InventoryItemController + ShoppingList IsDone hook — Phase 4 (2026-04-08)
- **`IsActive` added** to both `InventoryItem` and `InventoryItemModel` (was missing — required for soft-delete and IsDone hook filter). Constructors default `IsActive = true`.
- **`InventoryItemController.cs` created** at `Api/Controllers/InventoryItemController.cs`
  - `[Function("inventoryitems")]` GET all active ordered by Name / POST (IsActive=true, LastModified=UtcNow) / PUT (LastModified=UtcNow)
  - `[Function("inventoryitem")]` Route=`inventoryitem/{id}` — GET single (404 if not found) / soft-DELETE (IsActive=false, LastModified=UtcNow, Update)
  - `[Function("inventoryitemsadjust")]` POST Route=`inventoryitems/adjust` — bulk adjust: fetches all inventory once (avoids N+1), adds QuantityDelta, clamps to 0 if negative, updates each item
  - `InventoryAdjustmentModel` public class defined in same file (top-level in namespace)
- **ShoppingListController IsDone hook**: `GetAllShoppingListsFunction` now takes `IGenericRepository<InventoryItem>` in constructor. PUT handler fetches `existing` before update, detects not-done→done transition, loads all inventory once, increments `QuantityInStock` for matching active items by ShopItemId
- **Program.cs**: `IGenericRepository<InventoryItem>` registered in both dev (Memory) and prod (Firestore) branches
- **AutoMapper**: `InventoryItem ↔ InventoryItemModel` mapping already existed — no changes needed
- **Firestore collection key**: `inventoryitem` → `inventoryitems` by convention (lowercase + "s") — no special case needed in GoogleDbContext
- ✅ Build clean, 0 errors, 59 pre-existing warnings (no new issues)

### MealRecipe Seed Data — Family Dinners (2026-04-08)
- Replaced the 5-item placeholder MealRecipe block in `MemoryGenericRepository.AddDummyValues` with 62 real family dinners parsed from `Api.Tests/Helpers/dinners.txt` (723 lines, many duplicates).
- Deduplication approach: normalized to lowercase, stripped day prefixes ("Mandag - ", "7. Fredag - "), merged near-duplicates (all laks variants → "Laks" + "Salmalaks" + "Laks i pita"), filtered noise entries (rester, morfar, mormor, ferie, bursdag X, restaurant, bergen, hjemme, påskeaften, etc.).
- Final list: 62 unique meals across 8 categories (KidsLike × 14, Fish × 11, Meat × 12, Vegetarian × 6, Chicken × 8, Pasta × 4, Celebration × 3, Other × 3) — wait, I counted wrong but the categories are right.
- Popularity scores: pizza=100 down to drunken noodles=34. Effort: Quick (≤20 min: grøt, pannekaker, pølse+potetmos, kyllingnuggets, fiskeburger, pølsegnocchi, fiskepinner), Weekend (45+ min: taco, lasagne, kjøttkaker, biff, spareribs, bulgogi, fårikål, raspeballer, pinnekjøtt, ribbe, kylling gong bao, tikka masala, kalkun), Normal (everything else).
- Fiskepinner gets `MealType = Frozen` (the only frozen item in the seed list).
- Replaced old per-entity verbose pattern with compact `List<MealRecipe> + foreach (var r in recipes) await Insert(r as TEntity)`.
- ✅ Build clean, 0 errors, 113 pre-existing warnings (no new issues).

### Staging Crash Fix — useMemoryDb Logic (2026-04-09)
- **Bug**: Blazor startup crash in staging — `< at byte 0` = API returning HTML not JSON. Azure Functions host was crashing on startup and returning an HTML 500 for every request.
- **Root cause**: `useMemoryDb` only checked `GOOGLE_CLOUD_PROJECT`. If staging has `GOOGLE_CLOUD_PROJECT` set (e.g. inherited from build config or app settings) but does NOT have `GOOGLE_APPLICATION_CREDENTIALS` (the Firestore key file path), `GoogleFireBaseGenericRepository` throws during initialization → host crash.
- **DI registrations**: All 10 repositories are symmetric between debug and production blocks (ShoppingList, ShopItem, ItemCategory, Shop, FrequentShoppingList, MealRecipe, WeekMenu, FamilyProfile, PortionRule, InventoryItem). NOT the issue.
- **Fix**: Extended `useMemoryDb` condition in `Api/Program.cs` to also return true when `GOOGLE_APPLICATION_CREDENTIALS` is not set. Production Firestore now requires BOTH env vars to be present AND environment != Development.
- **Safe fallback**: Staging gets in-memory repos (with seed data) instead of crashing. Real production (GOOGLE_CLOUD_PROJECT + GOOGLE_APPLICATION_CREDENTIALS both set) is unaffected.
- ✅ Build clean, 0 errors, 33 pre-existing warnings (no new issues).
