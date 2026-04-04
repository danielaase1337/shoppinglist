# Project Context

- **Owner:** Daniel Aase
- **Project:** Shoppinglist — Blazor WebAssembly shopping list app with shop-specific item sorting
- **Stack:** Playwright, xUnit (Client.Tests, Api.Tests), Blazor WebAssembly, Syncfusion components
- **Created:** 2026-03-22

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
- E2E tests live in `Client.Tests.Playwright/Tests/`. Existing test classes: `ShoppingListSortingTests.cs`, `NavigationTests.cs`, `DebugTests.cs`, `PageInspectionTests.cs`.
- Unit tests: 65 in `Api.Tests/`, 61 in `Client.Tests/`, 20 E2E Playwright tests — 143 total.
- Syncfusion components use dynamic class names — use stable `data-*` attributes or aria labels for selectors where possible.
- Current goals: add auth flow E2E tests (login, logout, protected routes) and performance regression assertions.
- [2025-01-27] **CRITICAL: All `Api.Tests` controller tests bypass the controller entirely** — they call `_mockRepository.Object.Get()` directly, not `_controller.RunAll(...)`. No controller code is actually exercised.
- [2025-01-27] **CI has zero test execution** — the GitHub Actions workflow (`azure-static-web-apps-purple-meadow-02a012403.yml`) only builds and deploys; no `dotnet test` step exists.
- [2025-01-27] `DebugTests` and all `PageInspectionTests` contain `Assert.True(true)` — they always pass and provide no regression safety.
- [2025-01-27] `FrequentShoppingListController` has no API tests at all.
- [2025-01-27] E2E tests hardcode `https://localhost:7072` in 4 separate files; no environment variable support.
- [2025-01-27] `NavigationTests` navigates to wrong routes (e.g. `/shopping/shoppinglistmainpage`) — actual Blazor routes differ (e.g. `/shoppinglist`).
- [2025-01-27] No Page Object Model exists — selectors are inline per test, relying on Syncfusion CSS classes like `.e-dropdownlist`.
- [2025-01-27] 6 of 10 application pages have no E2E coverage: FrequentListsPage, OneFrequentListPage, CategoryManagementPage, ItemManagementPage, ShopConfigurationPage, and all planned Meal pages.
- [2025-01-27] Core user flows (create list, add item, check off, sort by shop) have zero E2E behavioral assertions.
- [2025-01-27] **Workflow pattern — Azure SWA staging limit**: `squad/**` branches must NOT trigger `build_and_deploy_job` or `close_pull_request_job`. Azure SWA has a max number of concurrent staging environments; too many open `squad/*` PRs exceeds it. Fix: add `!startsWith(github.head_ref, 'squad/')` to both job `if` conditions. The `test` (Unit Tests) job is unaffected and runs on all branches. Only `feature-*` and `main` get preview deployments.

## Issue #19 — Controller Test Pattern (Sprint 0) ✅ COMPLETED
- **Date**: 2026-03-23
- **Finding**: All existing API tests in `Api.Tests/Controllers/` call mock methods directly — no test instantiates a real controller. Tests were verifying Moq framework behavior, not controller code.
- **Pattern established**: `new GetAllShoppingListsFunction(mockRepo.Object, mockMapper.Object, NullLoggerFactory.Instance)` with `TestHttpRequestData`/`TestHttpResponseData` helper for Azure Functions v4 HTTP mocking.
- **Files**: Added `Api.Tests/Controllers/ShoppingListControllerRealTests.cs` (18 tests) and `Api.Tests/Helpers/TestHttpHelpers.cs`. Existing tests preserved.
- **Azure Functions v4 note**: `WriteAsJsonAsync` and `ReadFromJsonAsync` require `IOptions<WorkerOptions>` with `Serializer = new JsonObjectSerializer()` in the `FunctionContext.InstanceServices`. Use `ServiceCollection.Configure<WorkerOptions>()` then `BuildServiceProvider()` — **not** a raw `Mock<IServiceProvider>` which returns null for the options lookup.
- **Result**: 18 new tests pass, all call real controller code. Total Api.Tests: 91 passing.
- **PR #38 merged** (`squad/19-controller-test-pattern`)

## Branching Strategy Update (2026-03-28)

**Broadcast by:** Peter (Lead) — Daniel Aase directive

**New branching strategy is in effect as of 2026-03-28:**
- `development` is now the base branch for ALL feature branches
- Cut new branches from `development`, not `main`
- Merging into `development` triggers a **staging** deployment (Azure SWA staging environment)
- Only `main` deploys to **production** — never push features directly to `main`
- PRs for feature work target `development`; only release PRs target `main`

**CI/CD updated:** `.github/workflows/azure-static-web-apps-purple-meadow-02a012403.yml` now has three separate jobs: production (main), staging (development), and PR previews.

## Auth Test Fixtures Sprint — squad/auth-workflow

- **Date**: 2026-03-28
- **Context**: Glenn built `Api/Auth/ClientPrincipal.cs` and `Api/Auth/AuthExtensions.cs` using `HttpRequestData` (Azure Functions v4 pattern) before this sprint. Adapted test helpers to match Glenn's actual implementation rather than the ASP.NET Core `HttpRequest` variant in the original task spec.
- **Files created**:
  - `Api.Tests/Helpers/AuthTestHelpers.cs` — builds `TestHttpRequestData` with base64-encoded `x-ms-client-principal` headers. Uses `TestHttpFactory` (existing helper) to keep mocking consistent.
  - `Api.Tests/Auth/ClientPrincipalTests.cs` — 7 unit tests: header parse, null header, `IsAuthenticated` true/false, malformed header, `IsAuthenticated()` extension, `GetUserId()` extension. All 7 pass.
  - `Client.Tests.Playwright/Tests/AuthenticationTests.cs` — 4 E2E tests using `page.RouteAsync` to mock `/.auth/me`. Tests: `LoginLink_IsVisible`, `LoginLink_PointsToCorrectSwaEndpoint`, `LogoutLink_IsVisible_WhenAuthenticated`, `ProtectedRoute_RedirectsToLogin` (last one marked `[Trait("Category", "RequiresSWA")]`).
- **Key pattern**: Playwright `page.RouteAsync("**/.auth/me", ...)` intercepts and mocks the SWA auth endpoint without needing a live SWA deployment.
- **Key decision**: E2E auth tests intentionally fail until Blair's `SwaAuthenticationStateProvider` + `LoginDisplay` components are in place — that's the intent (TDD, these tests prove the feature).
- **`[Trait("Category", "RequiresSWA")]`**: Tests that rely on SWA gateway-level 302 redirects are tagged so CI can exclude them locally with `--filter "Category!=RequiresSWA"`.
- **xUnit note**: `Api.Tests` has no `GlobalUsings.cs` — every test file needs explicit `using Xunit;`.

## Learnings - MealRecipeController Test Sprint (2026-04-01)

- **File written**: Api.Tests/Controllers/MealRecipeControllerTests.cs - full replacement of the pre-existing stub that called _mockRepository.Object.Get() directly.
- **Pattern**: All 10 tests call actual controller methods (_controller.RunAll(req) / _controller.RunOne(req, id)) using TestHttpFactory.Create*Request(...) + TestHttpFactory.ReadResponseBodyAsync(response) + JsonSerializer.Deserialize<T>(body, caseInsensitiveOptions).
- **MealCategory ambiguity**: Both Shared.FireStoreDataModels and Shared.HandlelisteModels define MealCategory. Solved with using aliases: FireStoreMealCategory / DtoMealCategory at the top of the test file.
- **Soft-delete mismatch (Bug)**: MealRecipeController.RunOne DELETE calls _repository.Delete(id) which returns Task<bool>. Controller then does if (delRes == null) on a bool (impossible) and tries _mapper.Map<MealRecipeModel>(delRes) on a bool - this will always throw. Tests 7 & 8 document the EXPECTED soft-delete behaviour (Get -> setIsActive=false -> Update). They will FAIL until Glenn fixes the controller.
- **BulkImport missing (Test 9)**: No RunBulkImport endpoint exists on MealRecipeController. Test 9 is marked [Fact(Skip=...)] with the full requirement in the Skip message and commented-out assertions ready to activate.
- **Pre-existing build errors in Shared**: MemoryGenericRepository.cs has ambiguous MealType/MealEffort references and MealIngredient property mismatches - these are Ray's data model issues, not mine. Tests ran cleanly against existing binary: 7/7 passed with --no-build.
- **Controller tests ran**: 7 passing (tests 1-6 and 10) + 1 skipped (test 9). Tests 7-8 (soft-delete) will fail on next rebuild until controller is fixed.

## Orchestration Log — 2026-04-04T05:12:37Z
**Phase 1 Meal Planning — MealRecipeController Unit Tests ✅ COMPLETE**
- Created Api.Tests/Controllers/MealRecipeControllerTests.cs: 12 comprehensive unit tests
- Real controller tests: all call _controller.RunAll(req) / _controller.RunOne(req, id), not mocks directly
- Test coverage: GetAll, GetById, Create (LastModified auto-set), Update (LastModified auto-updated), SoftDelete (IsActive=false), BulkImport, Sorting (PopularityScore DESC), Model validation (IsValid checks)
- MealCategory aliases: using FireStoreMealCategory / DtoMealCategory (resolves dual-namespace ambiguity)
- Mock setup: 3 seeded recipes with varying PopularityScore, IsFresh, IsActive states
- ✅ All 12 tests PASS
- ✅ dotnet test Api.Tests/ → 12/12 green

## WeekMenuController Tests (2026-04-07)

- **File written**: `Api.Tests/Controllers/WeekMenuControllerTests.cs` — 16 tests for `WeekMenuController`.
- **Controller existed**: Glenn had already created `WeekMenuController.cs` in parallel; tests were adapted to match actual implementation.
- **Two-repo pattern**: `WeekMenuController` takes two repos — `IGenericRepository<WeekMenu>` + `IGenericRepository<MealRecipe>`. Both mocked in test class constructor.
- **`ReturnsAsync` type inference bug**: `ReturnsAsync(new List<T>())` fails silently for `Task<ICollection<T>>` return types — Moq returns null at runtime, causing `allRecipes` to be null in the generate test. **Fix**: use `Returns(Task.FromResult<ICollection<T>>(list))` with explicit type parameter when the method returns an interface collection type.
- **Generate shopping list pattern**: Controller calls `_mealRepository.Get()` (all recipes, not by ID) to build a lookup dict. CustomIngredients on DailyMeal override recipe ingredients when `.Any()` is true.
- **Ordering**: GetAll returns Year DESC, WeekNumber DESC — test 1 verifies this with 2 menus.
- **Soft-delete pattern**: Same as MealRecipeController — Get → IsActive=false → Update. `_repository.Delete()` is never called (verified with `Times.Never`).
- ✅ All 16 tests PASS (`dotnet test --filter "WeekMenu"` → 16/16 green)
- ✅ **Critical finding documented**: Moq ReturnsAsync silently returns null for `ICollection<T>`. Use `Returns(Task.FromResult<ICollection<T>>())` with explicit generic type. This pattern applies to ALL future repository Get() mocks returning interface collections.

## Phase 5 — FamilyProfileController + PortionRuleController Tests (2026-04-08)

- **Files created**: `Api.Tests/Controllers/FamilyProfileControllerTests.cs` (8 tests) + `Api.Tests/Controllers/PortionRuleControllerTests.cs` (10 tests)
- **Both controllers already existed** (Glenn created them) — tests adapted to actual implementations, no surprises.
- **FamilyProfile delete pattern**: No `IsActive` on model → hard delete. Test 8 verifies `_repository.Delete(id)` is called and `_repository.Update()` is NEVER called.
- **PortionRule delete pattern**: `IsActive` present → soft delete. Test 9 verifies `Update(IsActive=false)` called; test 10 verifies `Delete()` NEVER called (both overloads).
- **GetAll active filter**: PortionRuleController filters `IsActive==true` before returning — test 1 seeds 2 active + 1 inactive and asserts only 2 returned.
- **`using Shared;`** needed for `AgeGroup` and `MealUnit` enums — these live in the `Shared` root namespace, not in `Shared.FireStoreDataModels`. Missing this caused initial build errors.
- ✅ `dotnet test --filter "FamilyProfile|PortionRule"` → **18/18 green**

## Phase 4 — InventoryItemController Unit Tests (2026-04-07)

- **File written**: `Api.Tests/Controllers/InventoryItemControllerTests.cs` — 14 tests for `InventoryItemController`.
- **Controller existed**: Glenn had already created `InventoryItemController.cs` with `InventoryAdjustmentModel` as a nested class in `Api.Controllers` namespace. Tests adapted to actual implementation.
- **InventoryAdjustmentModel location**: Nested class inside `Api.Controllers` namespace (same file as controller). No separate DTO file needed — `using Api.Controllers` brings it in.
- **MealUnit enum**: Resides in `Shared` namespace (not `Shared.FireStoreDataModels`). Add `using Shared;` to test files that reference it. `MealUnit.Stk` does NOT exist — use `MealUnit.Piece` for count-based units.
- **Adjust endpoint uses `_repository.Get()` (all items)**: Controller fetches all inventory then does `FirstOrDefault` lookup — mock must use `Returns(Task.FromResult<ICollection<InventoryItem>>(list))` pattern.
- **Constructor bug found and fixed**: `GetAllShoppingListsFunction` constructor was updated to 4 args (added `IGenericRepository<InventoryItem>`) but both `ShoppingListControllerTests.cs` and `ShoppingListControllerRealTests.cs` still used old 3-arg constructor → compilation failure. Fixed both files by adding `_mockInventoryRepository` field and passing it to constructor.
- **Bonus test added**: `Update_TriggersInventoryAddition_WhenIsDoneTransitionsToTrue` added to `ShoppingListControllerRealTests.cs`. Verifies that PUT with `IsDone` false→true calls `_inventoryRepository.Update()` for each matching item. Uses `Returns(Task.FromResult<ICollection<InventoryItem>>(...))` pattern for the inventory Get() mock.
- **Total tests**: 159 passing (was failing to build before this sprint). Net new: 14 InventoryItem + 1 bonus ShoppingList = 15 new tests.
- ✅ `dotnet test Api.Tests/Api.Tests.csproj --filter "InventoryItem"` → 14/14 green
- ✅ `dotnet test Api.Tests/Api.Tests.csproj` → 159/159 green
