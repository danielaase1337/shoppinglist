# Project Context

- **Owner:** Daniel Aase
- **Project:** Shoppinglist — Blazor WebAssembly shopping list app with shop-specific item sorting
- **Stack:** Azure Functions v4 (.NET 9), AutoMapper, repository pattern, Google Cloud Firestore, .NET DI
- **Created:** 2026-03-22

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
- **ToDictionary null-key guard pattern**: `GoogleFireBaseGenericRepository` can return documents whose `Id` field is null (e.g., legacy data without the `[FirestoreProperty] Id` field stored). Always add `.Where(x => x.Id != null)` before `.ToDictionary(x => x.Id, x => x)` in generate/aggregate flows. Same applies to foreign-key fields like `ShopItemId` in `InventoryItem` — filter with `i.ShopItemId != null` before `GroupBy`. Without these guards, `ArgumentNullException` from `Dictionary` is caught by the controller's outer `try/catch` and returned as 500.
- **Unit tests do not surface Firestore data-quality bugs**: Mocked repositories always return clean, non-null Id data. The null-key crash only manifests in production with real Firestore documents. Tests that exercise the generate flow must explicitly include a null-Id scenario to catch this.
- `Api/Program.cs` uses environment variable `GOOGLE_CREDENTIALS` (not `#if DEBUG`) to switch between MemoryGenericRepository (development / no credentials) and GoogleFireBaseGenericRepository (production).
- Controllers follow a function-per-endpoint pattern: `[Function("shopitems")]` (collection) and `[Function("shopitem")]` (single, `Route = "shopitem/{id}"`).
- `Api/Program.cs` has `#if DEBUG` blocks — MemoryGenericRepository in debug, GoogleFireBaseGenericRepository in production.
- `LastModified = DateTime.UtcNow` must be set on all POST and PUT operations. GET operations do lazy migration for null values.
- `Api/ShoppingListProfile.cs` holds all AutoMapper mappings with `.ReverseMap()`.
- Current goals: implement authentication middleware and secure all API endpoints.

### Issue #81 — Unconsume endpoint (2026-04-24) ✅ COMPLETE
- Added `[Function("weekmenuunconsume")]` — `PUT /api/weekmenu/{weekMenuId}/unconsume` — mirrors `ConsumeMeal` exactly in reverse: sets `IsConsumed = false`, restores `QuantityInStock += ingredient.Quantity` for each ingredient (no clamp needed on restore), sets `LastModified = DateTime.UtcNow`.
- Reuses existing `ConsumeMealRequest` shape (DayOfWeek + MealRecipeId) — no new request type needed.
- `WeekMenuUnconsume = 23` added to `ShoppingListKeysEnum`; `"weekmenuunconsume" → "api/weekmenu"` added to `ISettings` dict (same pattern as consume/swap).
- 3 new tests: `Unconsume_SetsIsConsumedFalse_ReturnsOk`, `Unconsume_ReversesInventoryDeduction`, `Unconsume_Returns404_WhenMenuNotFound`.
- ✅ 162 tests pass, 0 failures.
- **Decision D30 merged** to `decisions.md`; orchestration log written; ready for integration with Blair's frontend.

### Package Size Feature — Phase 7 (2026-04-24–2026-04-26) ✅ COMPLETE
- **Design (D36):** Peter documented 3-part solution: unit bridge, backend calculation, display layer. Related to issue #76 (purchase unit sizes already implemented).
- **Unit Compatibility (D36.1):** Created `Shared/MealUnitExtensions.cs` with 4 new public methods:
  - `IsCompatibleWith(MealUnit, string)` — check if ingredient unit and purchase unit are same dimension
  - `NormalizeToBaseUnit(MealUnit, double)` — convert to base unit (gram, dl, stk)
  - `NormalizePurchaseUnitToBase(string, double)` — convert purchase unit string to base unit
  - `CalculatePackagesNeeded(...)` — final package count; returns null on incompatibility
- **Package Conversion Logic (D36.2):** Updated `WeekMenuController.RunGenerateShoppingList()` with:
  - Extended aggregation tuple to carry `MealUnit`: `(double Quantity, MealUnit Unit, string ShopItemName, ShopItem ShopItem)`
  - **CRITICAL pipeline order:** stock comparison (subtract QuantityInStock) → package conversion (calculate packages). Both mutate Mengde; wrong order produces incorrect IsLikelyNotNeeded flags.
  - Fallback to Math.Ceiling when StandardPurchaseQuantity unavailable or units incompatible
  - `Mengde` becomes package count (int) when calculation succeeds; raw quantity otherwise
- **Test Coverage:** 26 new MealUnitExtensionsTests + 3 new WeekMenuControllerTests (integration). Total: 211 tests, 0 failures.
- **Decision D36 merged** (with D36.1, D36.2, D36.3 sub-decisions) to `decisions.md`.
- **PR #89 status:** Ready for review → development branch.



### IsBasic Population Audit (2026-04-24) ✅ COMPLETE
- Scanned all `Api/Controllers/` for inline `new ShopItemModel` constructions bypassing AutoMapper.
- Found single bug in `WeekMenuController.RunGenerateShoppingList` line 265.
- Fixed: replaced inline construction with `_mapper.Map<ShopItemModel>(shopItem)` + graceful fallback.
- Verified no additional inline mappings in other controllers.
- **Decision D34 merged** to `decisions.md`.

### Issue #77 — Generated shopping lists keep ShopItem metadata (2026-05-29) ✅ COMPLETE
- `WeekMenuController.RunGenerateShoppingList()` must not drop `IsBasic` items during aggregation; the frontend expects them in the collapsed “Basisvarer / Trolig ikke nødvendig” section.
- When a `ShopItem` lookup exists, map the full entity with AutoMapper so `IsBasic`, `ItemCategory`, `Unit`, `StockBehaviour`, and purchase-size fields stay intact.
- When the catalogue lookup misses, fallback `ShopItemModel` creation must still preserve `MealIngredient.IsBasic` so generated lists do not silently flatten basic items into ordinary items.
- Added regression tests covering both mapped and fallback `IsBasic` scenarios.

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

### Issues #74, #75, #76 Backend — Consume/Swap, IsDone Hook, Stock Comparison (2026-04-23)
- **Collaboration note**: Blair's commit `9d5da56` landed simultaneously and already contained the WeekMenuController extensions (ConsumeMeal, SwapMeal, stock comparison in generate-shoppinglist), model changes (IsConsumed on DailyMeal, IsLikelyNotNeeded on ShoppingListItem), and the IsDone StockBehaviour hook in ShoppingListController. My net contribution was fixing `WeekMenuControllerTests.cs` which had the old 4-parameter constructor — Blair's new InventoryItem injection made the test file fail to compile.
- **#74 endpoints** (WeekMenuController): `PUT /api/weekmenu/{weekMenuId}/consume` marks a DailyMeal as consumed and deducts each ingredient's Quantity from InventoryItem.QuantityInStock (clamped at 0); `PUT /api/weekmenu/{weekMenuId}/swap` swaps MealRecipeId for a day (no inventory deduction).
- **#75 IsDone hook** (ShoppingListController PUT): detects false→true transition, iterates ShoppingItems, skips DoNotTrack items, increments QuantityInStock on existing InventoryItems and auto-creates new ones for Track items not yet in inventory.
- **#76 stock comparison** (WeekMenuController generate-shoppinglist): after aggregating ingredients, loads all active InventoryItems, sets `IsLikelyNotNeeded = true` when stock covers full demand, reduces Mengde to shortfall for partially-covered items.
- **Testing pattern**: When adding a new repo dependency to a controller, always update the test constructor immediately. `WeekMenuControllerTests` needs a mock `IGenericRepository<InventoryItem>` with a default `ReturnsAsync(new List<InventoryItem>())` setup to avoid null reference in the stock comparison loop.
- **Branch timing**: Multiple agents can push to the same branch concurrently. Always check `git diff origin/branch..HEAD --stat` after committing to confirm net diff. If most changes are already present from a teammate's commit, only your delta appears.
- ✅ Build clean, 0 errors, 118 pre-existing warnings (no new issues).

### IsBasic / StockBehaviour fix — RunGenerateShoppingList (2026-04-23)
- **Root cause**: `aggregated` dict stored only `(double Quantity, string ShopItemName)`. Final `ShopItemModel` was hand-constructed with only `Id` and `Name` — `IsBasic`, `StockBehaviour`, `StandardPurchaseQuantity`, `StandardPurchaseUnit` were never copied.
- **Fix**: Injected `IGenericRepository<ShopItem>` into `WeekMenuController`. In `RunGenerateShoppingList`, load all ShopItems once, build a lookup dict by Id, store the `ShopItem` ref in the aggregated tuple, then use `_mapper.Map<ShopItemModel>(shopItem)` for the final mapping. Graceful fallback (`new ShopItemModel { Id, Name }`) when ShopItem not found.
- **Only one inline `ShopItemModel` construction existed** (line 265) — no other occurrences in other controllers.
- **Test update pattern**: Every new repo injected into a controller also needs a new `Mock<IGenericRepository<T>>` with default `ReturnsAsync(new List<T>())` setup in the test constructor. 15/15 WeekMenu tests pass.
- ✅ Build clean, 0 errors, 68 pre-existing warnings (no new issues). Pushed to mealplanningv2 as commit 6a98801.

### Package-size Calc — squad/package-size-calc (2026-04-24) ✅ COMPLETE
- **Four new methods** added to `Shared/Shared/MealUnitExtensions.cs`: `IsCompatibleWith`, `NormalizeToBaseUnit`, `NormalizePurchaseUnitToBase`, `CalculatePackagesNeeded`. All are pure static — zero side-effects, easy to test.
- **`CalculatePackagesNeeded` returns null** (not 0) as the fallback signal: callers do `packages ?? (int)Math.Ceiling(raw)`. This preserves existing behaviour for unconfigured items.
- **Ordering invariant**: stock comparison MUST run before package conversion. Both operate on `item.Mengde`, but stock comparison subtracts raw ingredient units (grams/dl/stk) while package conversion divides by package size. If package conversion ran first, `QuantityInStock` would be compared against "number of bags" instead of raw demand — wrong result.
- **UnitMismatch flag**: added to the aggregated dict tuple. Set to true when the same ShopItemId appears with different `MealUnit` values across meals. Prevents nonsensical cross-unit summation before package calc.
- **`using Shared;` must be added** to any test file that references `MealUnit` — it lives in the root `Shared` namespace, not `Shared.FireStoreDataModels` or `Shared.HandlelisteModels`.
- **Branch confusion pattern**: teammates' unstaged changes travel with the working tree across branch switches. After checking out a new branch, always check `git status` to confirm only your intended files are modified before staging.
- **211 tests pass**, 0 failures. PR #89 created targeting `development`.

### 500 Error in generate-shoppinglist — Null-Key Guards (2026-05-30) ✅ COMPLETE
- **Bug**: `POST api/weekmenu/{id}/generate-shoppinglist` threw unhandled `ArgumentNullException` inside `RunGenerateShoppingList` when building lookup dictionaries. Caught by outer `try/catch` → HTTP 500.
- **Root cause**: Three call sites lacked null-key validation before `.ToDictionary()`:
  1. `recipeDict` — MealRecipe with null `Id` throws on key insertion
  2. `shopItemDict` — ShopItem with null `Id` throws on key insertion
  3. `inventoryDict` — InventoryItem with null `ShopItemId` throws via GroupBy → ToDictionary
- **Firestore data quality issue**: `GoogleFireBaseGenericRepository<T>.Get()` returns documents successfully even when they lack the `Id` field (e.g., legacy documents stored before `[FirestoreProperty] Id` was added). Those deserialize with `Id = null`. `Dictionary<string, T>` rejects null keys with `ArgumentNullException`.
- **Unit tests never caught this** because mock repositories always return objects with non-null Ids.
- **Fix**: Added `.Where(r => r.Id != null)`, `.Where(s => s.Id != null)`, and `.Where(i => i.IsActive && i.ShopItemId != null)` guards immediately before each `ToDictionary` call.
- **Pattern for future**: Always guard `ToDictionary` on Firestore-sourced collections:
  ```csharp
  var dict = collection?
      .Where(x => x.Id != null)
      .ToDictionary(x => x.Id, x => x) ?? new Dictionary<string, T>();
  
  var dict = collection?
      .Where(x => x.ForeignKeyId != null)
      .GroupBy(x => x.ForeignKeyId)
      .ToDictionary(g => g.Key, g => g.First()) ?? new Dictionary<string, T>();
  ```
- ✅ Build clean, 0 errors. dotnet test Api.Tests → 221 passed, 0 failed. Committed to `integration/all-squad-fixes`.
- **Decision D-BUGFIX-1 merged** to `decisions.md`; session log written.

### IsBasic Propagation — Generated Shopping Lists (2026-05-30) ✅ COMPLETE
- **Context**: `WeekMenuController.RunGenerateShoppingList()` was losing `IsBasic` behaviour in two cases:
  1. `MealIngredient.IsBasic` items were filtered out during aggregation → never reached generated list
  2. Fallback inline `ShopItemModel` (when catalogue lookup misses) defaulted `IsBasic` to `false`
- **Decision**: Keep `IsBasic` ingredients in generated shopping lists so the frontend can group them under collapsed "Basisvarer / Trolig ikke nødvendig" section.
- **Implementation**: Continue using existing catalogue lookup + AutoMapper path for full `ShopItem` metadata (`IsBasic`, `ItemCategory`, `Unit`, `StockBehaviour`, purchase-size fields). When the catalogue lookup misses, preserve `MealIngredient.IsBasic` on the fallback `ShopItemModel`.
- **Rationale**: Matches frontend contract in `OneWeekMenuPage.razor`, fixes issue #77 without adding new Firestore queries, keeps generated shopping lists resilient when meal data references stale/missing catalogue entries.
- **Decision D-BUGFIX-2 merged** to `decisions.md`.

