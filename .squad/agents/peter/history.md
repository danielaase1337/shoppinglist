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
