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

## Issue #19 — Controller Test Pattern (Sprint 0)
- **Date**: 2025-01-27
- **Finding**: All existing API tests in `Api.Tests/Controllers/` call mock methods directly — no test instantiates a real controller. Tests were verifying Moq framework behavior, not controller code.
- **Pattern established**: `new GetAllShoppingListsFunction(mockRepo.Object, mockMapper.Object, NullLoggerFactory.Instance)` with `TestHttpRequestData`/`TestHttpResponseData` helper for Azure Functions v4 HTTP mocking.
- **Files**: Added `Api.Tests/Controllers/ShoppingListControllerRealTests.cs` (18 tests) and `Api.Tests/Helpers/TestHttpHelpers.cs`. Existing tests preserved.
- **Azure Functions v4 note**: `WriteAsJsonAsync` and `ReadFromJsonAsync` require `IOptions<WorkerOptions>` with `Serializer = new JsonObjectSerializer()` in the `FunctionContext.InstanceServices`. Use `ServiceCollection.Configure<WorkerOptions>()` then `BuildServiceProvider()` — **not** a raw `Mock<IServiceProvider>` which returns null for the options lookup.
- **Result**: 18 new tests pass, all call real controller code. Total Api.Tests: 91 passing.
- [2025-01-27] **CI test gap fixed (PR #34)**: Added `test` job to the Azure SWA workflow. Runs `Api.Tests` and `Client.Tests` individually via `dotnet test --no-build` after a `dotnet build` step. Playwright E2E excluded by targeting specific test projects (not solution filter). Workflow triggers broadened to `feature-*` and `squad/**` so the test job fires on squad PRs, not just main. `feature-add-dinnerlist` had to be pushed to remote before the PR base could be resolved.
