# Test Coverage & Quality Audit — shoppinglist

**Agent:** Josh  
**Date:** 2025-07-14  
**Scope:** Api.Tests, Client.Tests, Client.Tests.Playwright

---

## Executive Summary

The project has **~118 tests** across three test projects. The overall test quality is **mixed**: `Client.Tests` has genuinely useful service tests, but `Api.Tests` has a fundamental structural defect that renders its tests nearly worthless as coverage instruments. The Playwright suite contains debug/inspection tests that always pass regardless of application state. Critical business logic—FrequentShoppingListController, all Blazor page components, MealRecipes E2E flows—has zero test coverage.

---

## 1. Test Infrastructure

### 1.1 Api.Tests
- **File:** `Api.Tests/Api.Tests.csproj`
- **Framework:** xunit 2.9.2, Moq 4.20.72, AutoMapper 12.0.0
- **Additional packages:** Microsoft.AspNetCore.Mvc.Testing 9.0.0, Microsoft.Azure.Functions.Worker 2.0.0, coverlet.collector 6.0.2
- **Structure:** Single `Controllers/` folder — 5 test files mirroring `Api/Controllers/`
- **No shared fixtures** — each test class creates its own Mock and Mapper in constructor
- **No test data files or fixtures**

### 1.2 Client.Tests
- **File:** `Client.Tests/Client.Tests.csproj`
- **Framework:** xunit 2.9.2, Moq 4.20.72, coverlet.collector 6.0.2
- **Structure:** 4 test files at the root level (no folder organisation)
- **Test doubles:** `Mock<HttpMessageHandler>` for HttpClient via `Moq.Protected`; `Mock<IDataCacheService>` for service-layer tests
- **IDisposable** pattern used in `DataCacheServiceTests` to dispose HttpClient

### 1.3 Client.Tests.Playwright (E2E)
- **File:** `Client.Tests.Playwright/Client.Tests.Playwright.csproj`
- **Framework:** xunit 2.9.2, Microsoft.Playwright.MSTest 1.40.0
- **Structure:** `PlaywrightFixture.cs` (shared Chromium context) + `Tests/` folder with 4 files
- **Hardcoded BaseUrl:** `https://localhost:7072` in every test file
- **Requires:** Running Blazor WASM dev server and running Azure Functions API — no in-process hosting
- **No Page Object Model** — selectors scattered across test files

---

## 2. API Test Coverage (Api.Tests)

### ⚠️ CRITICAL DEFECT — Tests Do Not Exercise Controller Code

Every test class in `Api.Tests` suffers the same fundamental flaw: the controller (`_controller`) is instantiated in the constructor but **never called in any test method**. All `[Fact]` methods invoke `_mockRepository.Object.Get()`, `_mockRepository.Object.Insert()`, etc. directly — they test Moq's return-value behaviour, not the controller's HTTP handling logic.

**Example from `ShoppingListControllerTests.cs` (line 65):**
```csharp
// Act
var result = await _mockRepository.Object.Get();  // ← calls the MOCK, not the controller
```

The actual `GetAllShoppingListsFunction.RunAll()` / `RunOne()` methods — containing migration logic, HTTP status code decisions, null guards, AutoMapper calls — are **never invoked** in any test.

### Test Count by File

| Test File | [Fact] | [Theory]/[InlineData] | Total Tests |
|---|---|---|---|
| `ShoppingListControllerTests.cs` | 14 | 4 Theory (11 cases) | ~25 |
| `ShopsControllerTests.cs` | 10 | 0 | 10 |
| `ShopsItemsControllerTests.cs` | 12 | 0 | 12 |
| `MealRecipeControllerTests.cs` | 7 | 0 | 7 |
| `ShopItemCategoryControllerTests.cs` | 10 | 1 Theory (5 cases) | 15 |
| **Total** | | | **~69** |

### What Is Actually Tested (despite the flaw)

- **AutoMapper configuration** — bidirectional mapping for all entities (genuinely useful)
- **Model property validation** — basic property assignments on domain models
- **Repository mock return values** — confirm Moq works as expected (no production value)
- **`MealRecipeModel.IsValid()`** — one real model method tested

### Coverage Gaps — API (All HIGH priority)

| Gap | Affected File | Priority |
|---|---|---|
| `FrequentShoppingListController` — no test file exists | `Api/Controllers/FrequentShoppingListController.cs` | 🔴 HIGH |
| `DebugFunction` — no tests | `Api/Controllers/DebugFunction.cs` | 🟡 LOW |
| `ShoppingListController.RunAll()` GET with migration logic | `ShoppingListController.cs:55-73` | 🔴 HIGH |
| `ShoppingListController.RunAll()` POST sets `LastModified` | `ShoppingListController.cs:81-82` | 🔴 HIGH |
| `ShoppingListController.RunAll()` PUT updates `LastModified` | `ShoppingListController.cs:102` | 🔴 HIGH |
| `ShoppingListController.RunOne()` GET with migration | `ShoppingListController.cs:138-144` | 🔴 HIGH |
| `ShoppingListController.RunOne()` DELETE path | `ShoppingListController.cs:151-159` | 🔴 HIGH |
| HTTP 400/404/500 response shapes never asserted | All controllers | 🔴 HIGH |
| `MealRecipeController` popularity sort in GET | `MealRecipeController.cs:52` | 🟠 MEDIUM |
| `FrequentShoppingListController` validation — PUT without ID returns 400 | `FrequentShoppingListController.cs:74-79` | 🔴 HIGH |
| `ShopsController.RunAll()` / `RunOne()` HTTP layer | `ShopsController.cs` | 🔴 HIGH |

### Mocking Strategy Assessment

The mock strategy uses a **class-level `Mock<IGenericRepository<T>>`** with per-test `Setup()` calls. This is a valid and idiomatic Moq approach. However, the mock is used to bypass — rather than support — testing of the actual controller code. Recommended fix: create a real `HttpRequestData` via `ServiceCollection`/`FunctionContext` pattern (as supported by `Microsoft.Azure.Functions.Worker` test helpers) and invoke `RunAll()`/`RunOne()` directly.

---

## 3. Client Test Coverage (Client.Tests)

### 3.1 DataCacheServiceTests
- **File:** `Client.Tests/DataCacheServiceTests.cs`
- **Test count:** 27 tests
- **Quality:** ✅ Genuinely tests the `DataCacheService` implementation

| Scenario Group | Tests | Quality |
|---|---|---|
| `GetItemsAsync` — first call, cache hit, force refresh, HTTP failure | 4 | ✅ Good |
| `GetCategoriesAsync` — first call, cache hit | 2 | ✅ Good |
| `GetShopsAsync` — first call, cache hit | 2 | ✅ Good |
| `GetShoppingListsAsync` — first call, cache hit | 2 | ✅ Good |
| `GetShoppingListAsync` — first call, cache hit | 2 | ✅ Good |
| `GetShopAsync` — first call, cache hit | 2 | ✅ Good |
| Cache invalidation — per-entity + `InvalidateAllCaches` | 6 | ✅ Good |
| `PreloadActiveShoppingListsAsync` — active only, none active | 2 | ✅ Good |
| `PreloadCoreDataAsync` — parallel, subsequent cache use, partial failure | 3 | ✅ Good |
| Network error — no caching of failed results | 1 | ✅ Good |

**Coverage gaps for DataCacheService:**
- `GetFrequentListsAsync` / `GetFrequentListAsync` — **not tested** 🔴 HIGH
- `InvalidateFrequentListsCache` / `InvalidateFrequentListCache` — **not tested** 🔴 HIGH
- Cache TTL expiry (5-minute TTL is implemented but never tested) 🟠 MEDIUM
- Concurrent calls — no thread-safety test 🟡 LOW

### 3.2 BackgroundPreloadServiceTests
- **File:** `Client.Tests/BackgroundPreloadServiceTests.cs`
- **Test count:** 4 tests
- **Quality:** ✅ Tests real service behaviour including idempotency and exception swallowing

| Test | Scenario |
|---|---|
| `StartFastCorePreloadAsync_CallsDataCachePreloadCoreData` | Happy path |
| `StartFastCorePreloadAsync_DataCacheThrows_DoesNotPropagateException` | Error resilience |
| `StartPreloadingAsync_CallsDataCachePreloadActiveShoppingLists` | Background task fires |
| `StartPreloadingAsync_CalledMultipleTimes_OnlyStartsOnce` | Idempotency guard |
| `StartPreloadingAsync_DataCacheThrows_DoesNotPropagateException` | Error resilience in background |

**Gap:** The 2-second `Task.Delay` inside `StartPreloadingAsync` makes the test brittle — timing-dependent assertions with a 5-second timeout.

### 3.3 NaturalSortComparerTests
- **File:** `Client.Tests/NaturalSortComparerTests.cs`
- **Test count:** 9 tests (including Theory groups = 30+ cases)
- **Quality:** ✅ Excellent coverage of sorting logic

| Test Group | Cases |
|---|---|
| Week numbers (Uke N) | 6 InlineData cases |
| Text-only alphabetical | 4 InlineData cases |
| Mixed text + numbers | 6 InlineData cases |
| Identical strings | 3 InlineData cases |
| Null handling | 3 assertions in 1 Fact |
| Case insensitivity | 3 InlineData cases |
| Real-world weekly shopping list scenario | 7-item sort |
| Complex version strings | 5-item sort |
| Pure numeric strings | 5-item sort |

**Minor gap:** Very long strings with large numbers (>10 digits) could overflow `int.TryParse` — untested.

### 3.4 ShoppingListSortingTests
- **File:** `Client.Tests/ShoppingListSortingTests.cs`
- **Test count:** 3 tests
- **Quality:** ✅ Good — directly mirrors the `SortShoppingLists()` LINQ expression from `ShoppingListMainPage.razor`

| Test | Coverage |
|---|---|
| Active-first, completed-last with different dates | Partition ordering |
| Same-date tie-breaking with natural sort | Tie-break ordering |
| Mixed active/completed with date ordering within groups | Group stability |

**Gap:** No test for lists where `LastModified == null` (DateTime.MinValue fallback) with mixed null/non-null dates.

---

## 4. E2E Test Coverage (Client.Tests.Playwright)

### 4.1 Test File Overview

| File | Tests | Has Real Assertions? |
|---|---|---|
| `ShoppingListSortingTests.cs` | 5 | Partial — some only check page length |
| `NavigationTests.cs` | 5 | Partial |
| `DebugTests.cs` | 2 | ❌ Always `Assert.True(true)` |
| `PageInspectionTests.cs` | 3 | ❌ Always `Assert.True(true)` |

### 4.2 ShoppingListSortingTests Assessment

| Test | Assertion Quality | Issue |
|---|---|---|
| `OneShoppingListPage_WhenShopSelected_ShouldSortItemsByShelfOrder` | 🟠 Weak | Only checks page content length > 500 or contains "Ukeshandel" |
| `OneShoppingListPage_WhenNoShopSelected_ShouldShowUnsortedList` | 🟠 Weak | Page length > 200 chars |
| `OneShoppingListPage_SyncfusionComponents_ShouldBeInteractive` | 🟡 Cosmetic | `Assert.True(true, "...completed successfully")` |
| `ShoppingListMainPage_ShouldSortByLastModified_NewestFirst` | 🟠 Weak | Checks for "Handlelister" heading only |
| `ShoppingListMainPage_NaturalSorting_ShouldHandleWeekNumbers` | 🟡 Cosmetic | `Assert.True(true, "Natural sorting logic is in place")` |

**Core problem:** None of the sorting E2E tests actually verify the ORDER of items on screen. They check that the page loads and has content, not that items are sorted correctly.

### 4.3 NavigationTests Assessment

| Test | Assertion Quality |
|---|---|
| `HomePage_ShouldLoadSuccessfully` | 🟢 Checks title and nav presence |
| `NavigationPages_ShouldLoadCorrectly` (3 routes Theory) | 🟠 Only checks no "unhandled error" + content length |
| `ShoppingListMainPage_ShouldShowShoppingLists` | 🟠 Fuzzy content check with OR logic |
| `OneShoppingListPage_WithValidId_ShouldLoadCorrectly` | 🟠 Checks "Ukeshandel" or "test" |
| `AdminPage_ShouldLoadWithoutAuthentication` | 🟢 Checks for "Admin" and "Database"/"Test Data" |

### 4.4 DebugTests & PageInspectionTests

Both files consist entirely of diagnostic/inspection tests that **always pass** (`Assert.True(true)`). They write output to `Console.WriteLine` for developer inspection but provide zero regression protection. These tests should either be:
- Converted to real assertions and moved to appropriate test classes, or
- Deleted from the test suite (they add CI noise without safety)

### 4.5 Critical E2E Gaps

| Missing Scenario | Priority |
|---|---|
| Add item to shopping list end-to-end | 🔴 HIGH |
| Mark item as done within a shopping list | 🔴 HIGH |
| Create new shopping list and verify it appears sorted | 🔴 HIGH |
| Delete shopping list | 🔴 HIGH |
| Select shop and verify items ARE sorted by shelf order | 🔴 HIGH |
| FrequentListsPage — any E2E coverage | 🔴 HIGH |
| ManageMyShopsPage — add/edit shop | 🟠 MEDIUM |
| ItemManagementPage — CRUD operations | 🟠 MEDIUM |
| Error state — API unavailable | 🟠 MEDIUM |

---

## 5. Test Quality Assessment

### 5.1 Test Naming

| Project | Pattern Used | Assessment |
|---|---|---|
| Api.Tests | `MethodName_Condition_ExpectedResult` | ✅ Clear but misleading (names imply testing controller, but test the mock) |
| Client.Tests | `MethodName_Condition_ExpectedResult` | ✅ Consistent and accurate |
| Client.Tests.Playwright | `PageName_Condition_ExpectedBehavior` | ✅ Clear intent, poor execution |

### 5.2 Test Independence

- **Api.Tests:** ✅ Independent — each test class uses fresh `Mock<>` instances per constructor
- **Client.Tests:** ✅ Independent — `DataCacheServiceTests` implements `IDisposable`; each test uses fresh `DataCacheService` instance
- **Playwright:** ⚠️ Shared `PlaywrightFixture` creates a shared `IBrowser` but a new `IBrowserContext` per `CreatePageAsync()` call. Pages are closed in `finally` blocks — good isolation.

### 5.3 Assertion Specificity

| Test File | Avg Assertion Quality |
|---|---|
| `ShoppingListControllerTests` | 🟠 Medium — Assert.Equal on mock return values (not controller output) |
| `DataCacheServiceTests` | 🟢 High — verifies HTTP call counts and cache identity |
| `NaturalSortComparerTests` | 🟢 High — exact sort order, sign checks |
| `ShoppingListSortingTests` | 🟢 High — exact index assertions |
| `BackgroundPreloadServiceTests` | 🟢 High — `Times.Once` verification |
| Playwright tests | 🔴 Low — mostly length > N and substring contains |

### 5.4 Test Data Quality

- Api.Tests uses **Norwegian-language test data** ("Ukeshandel", "Melk", "Rema 1000") — realistic and domain-appropriate ✅
- Client.Tests uses same language and data patterns ✅
- Playwright tests hardcode `test-list-1` ID (relying on `MemoryGenericRepository` test seed data)
- No shared test data factory or builder pattern — data construction repeated across test classes

---

## 6. Coverage Metrics (Estimated)

### Api.Tests — Effective Line Coverage

| Controller | Estimated Coverage | Reason |
|---|---|---|
| `GetAllShoppingListsFunction` | ~5% | Only AutoMapper config tested; no HTTP path exercised |
| `ShopsController` | ~5% | Same issue |
| `ShopsItemsController` | ~5% | Same issue |
| `MealRecipeController` | ~5% | Same issue |
| `ShopItemCategoryController` | ~5% | Same issue |
| `FrequentShoppingListController` | **0%** | No test file |
| `DebugFunction` | **0%** | No tests |
| `ShoppingListProfile` (AutoMapper) | ~90% | Tested via mapper config in multiple classes |

### Client.Tests — Effective Line Coverage

| Source | Estimated Coverage | Reason |
|---|---|---|
| `DataCacheService` | ~75% | Most methods tested; FrequentList methods untested; TTL untested |
| `BackgroundPreloadService` | ~85% | Both public methods tested; timing-dependent |
| `NaturalSortComparer` | ~95% | Thorough case coverage |
| `ShoppingListMainPage` sorting logic | ~90% | Unit tested via LINQ extraction in ShoppingListSortingTests |
| All Blazor `.razor` page components | **~0%** | No Blazor component tests (bUnit not used) |
| `ISettings` implementations | **~0%** | Not tested |

### Client.Tests.Playwright — Functional Workflow Coverage

| User Workflow | Coverage |
|---|---|
| App loads / navigation works | 🟠 Smoke-level |
| View shopping list | 🟠 Smoke-level |
| Shopping list sorting (shop-based) | 🔴 Not verified (assertion too weak) |
| Add/edit/delete items | 🔴 None |
| Frequent shopping lists | 🔴 None |
| Shop management | 🔴 None |
| Category/item management | 🔴 None |

---

## 7. Mock / Test Double Strategy

### 7.1 Repository Mocks (Api.Tests)

```
Mock<IGenericRepository<T>>
  ├── .Setup(r => r.Get()).ReturnsAsync(...)
  ├── .Setup(r => r.Get(id)).ReturnsAsync(...)
  ├── .Setup(r => r.Insert(It.IsAny<T>())).ReturnsAsync(...)
  ├── .Setup(r => r.Update(It.IsAny<T>())).ReturnsAsync(...)
  └── .Setup(r => r.Delete(id)).ReturnsAsync(...)
```

**Assessment:** Correct use of Moq for IGenericRepository. However, these mocks are called *directly* rather than being injected into the controller under test — this is the root cause of the coverage failure.

### 7.2 HttpClient Mock (Client.Tests)

```
Mock<HttpMessageHandler>
  └── .Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ...)
```

**Assessment:** ✅ Correct and idiomatic Moq pattern for mocking `HttpMessageHandler`. The `ItExpr.Is<>` URL matching is clear and maintainable.

### 7.3 Firestore Mock

**Not mocked in tests.** The `GoogleFireBaseGenericRepository` implementation is never referenced in any test. The `MemoryGenericRepository` (in-memory implementation used for E2E tests via the client dev server) serves as a Firestore stand-in for Playwright tests.

### 7.4 Test Implementation Completeness

| Mock | Completeness | Notes |
|---|---|---|
| `IGenericRepository<T>` | ~80% | All CRUD operations mocked; no exception paths set up |
| `HttpMessageHandler` | ~85% | Happy path + HTTP 500; no timeout/`HttpRequestException` for all methods |
| `IDataCacheService` | ~60% | Only `PreloadCoreDataAsync` and `PreloadActiveShoppingListsAsync` mocked in BackgroundPreloadTests |
| `ISettings` | ~40% | Only `GetApiUrl` + `GetApiUrlId` mocked; other methods not covered |

---

## 8. Missing Tests — Prioritized

### 🔴 HIGH Priority (Critical paths without coverage)

1. **Fix Api.Tests to call actual controllers**
   - File to create/fix: `Api.Tests/Controllers/ShoppingListControllerTests.cs` (and all others)
   - Pattern: Use `FunctionContext` + `HttpRequestData` to call `RunAll()` / `RunOne()` directly
   - Coverage impact: Would immediately add ~50-60% effective line coverage across all controllers

2. **Create `FrequentShoppingListControllerTests.cs`**
   - File: `Api.Tests/Controllers/FrequentShoppingListControllerTests.cs` (missing)
   - Scenarios: GET all, POST (valid + null body), PUT (with/without ID), GET by ID (found/404), DELETE (success/failure)

3. **DataCacheService — FrequentList methods**
   - File: `Client.Tests/DataCacheServiceTests.cs`
   - Missing: `GetFrequentListsAsync`, `GetFrequentListAsync`, `InvalidateFrequentListsCache`, `InvalidateFrequentListCache`

4. **E2E: Verify actual sort order on screen**
   - File: `Client.Tests.Playwright/Tests/ShoppingListSortingTests.cs`
   - Missing: Read the ordered list items from the DOM and assert specific order based on seed data

5. **E2E: Core shopping list workflows**
   - File: New `Client.Tests.Playwright/Tests/ShoppingListWorkflowTests.cs`
   - Scenarios: Add item, check off item, delete item, add new list, mark list as done

### 🟠 MEDIUM Priority

6. **ShoppingListController migration logic test**
   - Scenario: GET when a `ShoppingList` has `LastModified == null` triggers `Update()` (migration path in `RunAll` lines 55-73)

7. **NaturalSortComparer — large integer overflow**
   - Input strings with numbers > `int.MaxValue` — `int.TryParse` fails silently, falls through to string compare

8. **BackgroundPreloadService — race condition timing**
   - Replace `Task.Delay(5000)` timeout with a more deterministic synchronisation mechanism

9. **`ShoppingListSortingTests` — null LastModified**
   - Test `OrderBy(f => f.IsDone).ThenByDescending(f => f.LastModified ?? DateTime.MinValue)` when some items have `LastModified == null`

10. **E2E: ManageMyShopsPage shelf ordering**
    - New file: `Client.Tests.Playwright/Tests/ShopManagementTests.cs`

### 🟡 LOW Priority

11. **Remove or convert DebugTests.cs and PageInspectionTests.cs**
    - These tests always pass and provide no regression protection
    - Either add real assertions or delete

12. **Blazor component tests via bUnit**
    - Add `bunit` NuGet package to Client.Tests
    - Test `ShoppingListMainPage` component rendering, `OneShoppingListPage` item display, `LoadingComponent` display state

13. **DataCacheService — TTL cache expiry**
    - Test that data is re-fetched after 5 minutes (can inject `IClock` or use a time-advance pattern)

14. **`ShopsController` — missing `Get(int)` overload in tests**
    - Current test calls `Get(1)` (integer) but production `IGenericRepository` uses `Get(object id)` — type mismatch risk

---

## 9. Recommendations Summary

### Immediate Actions (Sprint-level)

1. **Fix all Api.Tests controller test classes** to invoke actual `RunAll()`/`RunOne()` methods using `Microsoft.Azure.Functions.Worker` test helpers. This is the single highest-impact change.
2. **Add `FrequentShoppingListControllerTests.cs`** — this is a complete coverage gap for a production controller.
3. **Strengthen Playwright sorting assertions** — use `page.Locator("ul li").AllTextContentsAsync()` to assert ordered lists.
4. **Delete or properly convert `DebugTests.cs` and `PageInspectionTests.cs`** — they pollute the test suite.
5. **Add FrequentList tests to `DataCacheServiceTests.cs`**.

### Medium-Term Actions

6. Add **bUnit** to `Client.Tests` for Blazor component unit testing.
7. Introduce a **Page Object Model** in `Client.Tests.Playwright` to reduce selector duplication.
8. Replace the hardcoded `BaseUrl` in every Playwright test with a shared constant or environment variable.
9. Add `data-testid` attributes to key Blazor components to stabilise E2E selectors.
10. Add a **test data builder** pattern to reduce setup duplication across test classes.

---

*Generated by Josh (exploration agent) — 2025-07-14*
