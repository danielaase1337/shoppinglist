# Project Context

- **Owner:** Daniel Aase
- **Project:** Shoppinglist — Blazor WebAssembly shopping list app with shop-specific item sorting
- **Stack:** Google Cloud Firestore, `GoogleFireBaseGenericRepository<T>`, `MemoryGenericRepository<T>` (debug), Shared models
- **Created:** 2026-03-22

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-03-28 — Issue #31: LastModified Migration Data Layer Analysis ✅ COMPLETE
- `IGenericRepository<T>.Get()` (no args) performs a full Firestore collection scan via `GetSnapshotAsync()` — no pagination. This is sufficient for the migration endpoint; no new interface method is needed.
- `GoogleFireBaseGenericRepository.Update()` uses `SetAsync(entity)` — full document overwrite, one write per call, no batching. Acceptable for this project's scale (personal/family lists). True Firestore `WriteBatch` would require bypassing `IGenericRepository` entirely.
- `MemoryGenericRepository` is in full parity: `Get()` returns all items, `Update()` replaces by ID. Migration works identically in DEBUG mode.
- Inline migration in `ShoppingListController` lives in **two places**: `RunAll` (GET /api/shoppinglists, lines ~54–70) and `RunOne` (GET /api/shoppinglist/{id}, lines ~138–144). Glenn removes both blocks after migration endpoint is deployed.
- Migration endpoint must be **idempotent** (`if (!LastModified.HasValue)` guard) and **partial-failure tolerant** (re-runnable; returns migrated/skipped counts).
- No code changes made to repositories — analysis only. No commit from Ray on this issue.
- Dual-model pattern is mandatory: `Shared/FireStoreDataModels/` (with `[FirestoreData]`/`[FirestoreProperty]`) and `Shared/HandlelisteModels/` (DTOs).
- Norwegian property names (`Varen`, `Mengde`, `ItemCateogries`) preserved for Firestore backward compatibility — do not rename.
- Every entity inherits `EntityBase` which includes `LastModified` (DateTime?) with `[FirestoreProperty]`.
- Lazy migration: GET endpoints check for null `LastModified` and backfill with `DateTime.UtcNow`.
- Current goals: explore Firebase Authentication integration and Firestore query performance improvements.
- `GetCollectionKey()` was found still in the old manual switch form despite history recording Sprint 0 PR #35 as merged — the fix was not actually applied to the file. Always verify file state against history claims.
- Convention-based key mapping (D4): only two overrides needed — `Shop → shopcollection` (legacy name) and `ItemCategory → itemcategories` (irregular plural). All other types derive correctly from `TypeName.ToLower() + "s"`.
- `MealIngredient` standalone DI registration was still present in Program.cs despite D9/PR #37 claiming it was removed. Removed it again.
- Migration strategy for "misc" → correct collection: use `DocumentSnapshot.ToDictionary()` + `SetAsync(Dictionary)` to copy documents preserving the document ID. Discriminate FrequentShoppingList by presence of the `"Items"` field (unique to that type among entities that landed in misc).

### 2026-03-27 — D4 Collection Key Convention & D9 DI Fix ✅ COMPLETE
- **FIXED D4 (Collection Key Convention):** Replaced `GetCollectionKey()` hardcoded switch with convention-based naming: `typeof(T).Name.ToLower() + "s"`. Two backward-compat overrides preserved: `Shop → "shopcollection"` (legacy Firestore collection name) and `ItemCategory → "itemcategories"` (irregular English plural). All other types now derive correctly: FrequentShoppingList → frequentshoppinglists (was misc), MealRecipe → mealrecipes, WeekMenu → weekmenus, etc.
- **FIXED D9 (MealIngredient DI):** Removed `IGenericRepository<MealIngredient>` from Program.cs (both DEBUG and production blocks). MealIngredient is embedded in MealRecipe per D3; no standalone collection needed. Orphaned registration prevented.
- **NEW: Migration Endpoint:** Created `Api/Controllers/MigrateFrequentListsController.cs` with `POST /api/admin/migrate-frequent-lists`. Strategy: read "misc" collection, identify FrequentShoppingList documents by presence of "Items" field (unique discriminator), copy to "frequentshoppinglists" preserving document IDs, delete originals. Returns JSON with migrated/skipped counts. **Must run once before deploying D4 to main.**
- **Validation:** Added `CollectionKeyTests.cs` (8 tests) verifying all known types map to correct collections and no future type falls through to "misc". ✅ 90 API tests + 61 Client tests passing.
- **Files changed:** `Shared/Shared/Repository/GoogleDbContext.cs`, `Api/Program.cs`, `Api/Controllers/MigrateFrequentListsController.cs` (NEW).

### 2026-03-23 — Sprint 0 Bug Fixes (Issues #16 and #17) ✅ COMPLETED
- **FIXED #16:** `GetCollectionKey()` replaced with convention-based naming: `typeof(T).Name.ToLower() + "s"`. Two backward-compat special cases kept: `Shop` → `shopcollection` (legacy collection name) and `ItemCategory` → `itemcategories` (irregular plural; convention would produce `itemcategorys`).
- **FIXED #17:** `WeekMenu` DI registration was missing — added to both DEBUG (MemoryGenericRepository) and production (GoogleFireBaseGenericRepository) blocks. `MealIngredient` standalone repository registration removed per D3/D9 (it's embedded in MealRecipe, not a root document).
- **TEST COVERAGE:** Added `CollectionKeyTests` (8 unit tests) in `Api.Tests/Repository/` — verifies all known entity types map to expected Firestore collection names and that no future type falls through to `"misc"`.
- **LESSON:** `typeof(T).Name.ToLower() + "s"` is simple but not universally correct for English plurals. Irregular plurals (category → categories, not categorys) require explicit overrides. Keep the switch slim: add a new case only when the convention breaks.
- **LESSON:** `git stash` without `--include-untracked` does not stash new untracked files. Untracked test files from other branches can leak into the working tree and confuse test counts.
- **DESIGN DECISION:** Convention-based collection naming auto-derives keys for new types (MealRecipe → mealrecipes, WeekMenu → weekmenus) without code changes. Only two override cases are needed because they violate the convention (Shop's legacy collection name + ItemCategory's irregular plural). Daniel reviewed this approach on PR #35 — confirmed the convention produces correct keys for all existing types.
- **PRs #35 + #37 merged** (`squad/16-collection-key-fix` + `squad/17-di-registration`)

## Branching Strategy Update (2026-03-28)

**Broadcast by:** Peter (Lead) — Daniel Aase directive

**New branching strategy is in effect as of 2026-03-28:**
- `development` is now the base branch for ALL feature branches
- Cut new branches from `development`, not `main`
- Merging into `development` triggers a **staging** deployment (Azure SWA staging environment)
- Only `main` deploys to **production** — never push features directly to `main`
- PRs for feature work target `development`; only release PRs target `main`

**CI/CD updated:** `.github/workflows/azure-static-web-apps-purple-meadow-02a012403.yml` now has three separate jobs: production (main), staging (development), and PR previews.

### 2025-01-22 — Full Data Architecture Review (data-findings.md)
- **CRITICAL BUG:** `GoogleDbContext.GetCollectionKey()` only maps 4 types. `FrequentShoppingList`, `MealRecipe`, `MealIngredient`, `WeekMenu`, and `DailyMeal` all fall through to `"misc"` — they will corrupt each other in production.
- **CRITICAL BUG:** `WeekMenu` and `DailyMeal` are not registered in `Program.cs` DI — the week menu feature is entirely unwired.
- **DESIGN ISSUE:** `MealIngredient` is both embedded inside `MealRecipe.Ingredients[]` AND has its own `IGenericRepository<MealIngredient>` registered in DI. These two storage strategies are contradictory. The standalone repo writes to a "misc" collection that nothing reads.
- **DESIGN ISSUE:** `MealCategory` enum is duplicated in both `Shared.FireStoreDataModels` and `Shared.HandlelisteModels` namespaces. AutoMapper maps between two identical enums — unnecessary.
- **DATA MODEL:** `ShoppingListItem` does not inherit `EntityBase` — it has no `Id`. Items within a list cannot be individually targeted for update.
- **DATA MODEL:** `ShoppingList.ListId` is redundant with the inherited `Id` field from `EntityBase`.
- **ANTI-PATTERN:** `ShopItem` is fully embedded (denormalised) in `ShoppingListItem.Varen`, `FrequentShoppingItem.Varen`, and `MealIngredient.ShopItem`. Renames do not propagate. At 5 levels of nesting in WeekMenu, document size limits become a real risk.
- **PERFORMANCE:** `IGenericRepository.Get()` always reads entire collections — no filtering, no pagination, no field-level querying. All sorting and filtering is in-memory on the API.
- **PERFORMANCE:** Inline `LastModified` migration in `ShoppingListController.RunAll()` fires one write per document on every GET — should be extracted to a one-time migration script.
- **SECURITY:** Zero user isolation. No `OwnerId`/`UserId` on any entity. Adding Firebase Auth requires adding ownership fields + migration + query changes.
- `ShelfCategory` class with `GetDefaults()` is dead code — never stored in Firestore, never used by any controller or model.
- `GoogleDbContext` stores collection state as mutable properties (`Collection`, `CollectionKey`) — safe only because `AddTransient` creates a new instance per injection, but the design is fragile.
