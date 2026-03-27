# Project Context

- **Owner:** Daniel Aase
- **Project:** Shoppinglist — Blazor WebAssembly shopping list app with shop-specific item sorting
- **Stack:** Blazor WebAssembly (.NET 9), Azure Functions v4, Google Cloud Firestore, Syncfusion UI, Playwright E2E tests
- **Created:** 2026-03-22

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
- App uses a dual-model pattern: `FireStoreDataModels` (with Firestore attributes) and `HandlelisteModels` (DTOs). AutoMapper with `.ReverseMap()` bridges them.
- Core shop-specific sorting runs client-side in `OneShoppingListPage.razor` via `SortShoppingList()`.
- Norwegian property names (`Varen`, `Mengde`, `ItemCateogries`) must be preserved — backward compatibility with Firestore data.
- Current goals: add authentication/security and improve performance.
- Auth provider changed to Microsoft-only — GitHub dropped per Daniel (2026-03-23)
- Family sharing model clarified: v2 uses FamilyId group ownership, NOT individual OwnerId scoping
- Meal planning v1 is text-history based suggestion engine, NOT recipe CRUD — separate scoping issue required
- i18n: Norwegian UI is intentional for v1; add resource file architecture for future English localization
- Norwegian Firestore property names (Varen, Mengde, ItemCateogries) are permanent data constraints
- Shop deletion requires safeguard UX (multi-step confirm + dependency check)
- ManageMyShopsPage (D11): decision is to COMPLETE the page, not remove it
- Sprint 0 (P0 bugs) successfully completed: 6 merged PRs (#34-#38) closed issues #16-#21 (2026-03-23)
- **REGRESSION FOUND (2026-03-23):** Frequent lists invisible due to collection key fix NOT merged to main. PR #35 has fix, but `main` branch still uses hardcoded switch defaulting FrequentShoppingList to `misc` collection. Fix committed in bb35ee2 but not cherry-picked to production branch.

### 2026-03-27 — Regression Fixes Complete ✅
- **D7 (Admin Nav Accessibility):** ✅ IMPLEMENTED by Blair. Converted CSS `:hover`-only dropdown to Blazor `@onclick` toggle with `_adminOpen` state. Added ARIA attributes. Updated app.css to support both hover and click-toggle. Frequent lists now accessible on mobile.
- **D4 (Collection Key Convention):** ✅ IMPLEMENTED by Ray. Replaced hardcoded switch in `GoogleDbContext.GetCollectionKey()` with convention: `typeof(T).Name.ToLower() + "s"`. Two backward-compat overrides: `Shop → shopcollection` (legacy), `ItemCategory → itemcategories` (irregular plural). Created `POST /api/admin/migrate-frequent-lists` endpoint for one-time data recovery from "misc" collection. Migration must run before D4 merge to main.
- **D9 (MealIngredient DI):** ✅ RE-APPLIED by Ray. Removed orphaned `IGenericRepository<MealIngredient>` DI registration (was marked complete 2026-03-23 but not actually applied). Now correctly implements D3 (embedding strategy).
- **Verification:** All tests passing (90 API + 61 Client). No regressions introduced.

## PRD Synthesis — 2026-03-22

### Key Architectural Decisions
- **AD-1:** Auth via Azure SWA built-in authentication (GitHub + Microsoft providers). No Firebase Auth — avoids mixing cloud ecosystems.
- **AD-2:** Hybrid data isolation — shared product catalogue (ShopItem, ItemCategory), per-user lists/shops/menus via `OwnerId` field.
- **AD-3:** MealIngredient stored embedded in MealRecipe, not as separate collection. Remove standalone MealIngredient repository.
- **AD-4:** WeekMenu uses recipe ID references, not full MealRecipe embedding. Prevents document bloat.
- **AD-5:** Convention-based collection key fallback to prevent future `"misc"` collection bugs.
- **AD-6:** Error message scrubbing in production — generic messages to callers, full details to ILogger.

### Critical Bugs Found
1. `GetCollectionKey()` maps 5 entity types (FrequentShoppingList, MealRecipe, MealIngredient, WeekMenu, DailyMeal) to `"misc"` — active data corruption risk.
2. WeekMenu/DailyMeal have no DI registration — entire feature is unwired.
3. All 65 API tests call mocks directly, not controller methods — zero controller code tested.
4. No tests run in CI — GitHub Actions builds but never runs `dotnet test`.
5. `ShopItemCategoryController.RunOne` has no try/catch — unhandled exceptions leak to callers.
6. `ShopsController` uses `.Result` instead of `await` — potential deadlocks.

### GitHub Issue Filed
- **Issue #15**: PRD: Shoppinglist App — Next Evolution (Auth, Meal Planning, UI, Performance)
- URL: https://github.com/danielaase1337/shoppinglist/issues/15

## PRD Decomposition — 2026-03-23

### Sub-Tickets Created (18 total from PRD #15)
- **Sprint 0 (P0 bugs):** #16 (collection keys, Ray), #17 (DI registration, Ray), #18 (CI tests, Josh), #19 (test audit, Josh), #20 (try/catch, Glenn), #21 (.Result deadlock, Glenn)
- **Sprint 1 (Auth):** #22 (SWA config, Glenn), #23 (auth middleware, Glenn), #24 (auth UI, Blair)
- **Sprint 2 (UI):** #25 (toast system, Blair), #26 (nav accessibility, Blair), #27 (mobile drag, Blair)
- **Sprint 3 (Shop):** #28 (shop deletion safeguards, Blair + Glenn)
- **Sprint 4 (Meal):** #29 (scoping only — NOT implementation, Peter)
- **Sprint 5 (i18n):** #30 (resource file architecture, Blair)
- **Sprint 6 (Data):** #31 (LastModified migration, Ray + Glenn), #32 (ManageMyShopsPage, Blair)
- **Sprint 7 (Testing):** #33 (controller test rewrite, Josh)

### Key Blocking Dependencies
- Sprint 0 blocks ALL feature sprints
- Auth chain: #22 → #23 → #24 (serial)
- Toast (#25) blocks shop deletion UX (#28) and future meal planning UI
- Nav fix (#26) blocks adding meal planning navigation entries
- CI tests (#18) and audit (#19) block full test rewrite (#33)
- Meal scoping (#29) blocks all meal implementation — Daniel must align on v1 first

### Labels Created on GitHub
- Squad labels: `squad:peter`, `squad:blair`, `squad:glenn`, `squad:ray`, `squad:josh`
- Priority labels: `P0`, `P1`, `P2`, `P3`
