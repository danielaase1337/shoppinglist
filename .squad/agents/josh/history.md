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
