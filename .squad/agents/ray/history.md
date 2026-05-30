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

### 2026-04-04 — Phase 1 Data Models for Meal Planning ✅ COMPLETE

**Summary:** Implemented all new data model files for meal planning Phase 1 per the authoritative spec in `peter/meal-planning-v1-scope.md`.

**New files created:**
- `Shared/Shared/MealUnit.cs` — `MealUnit` enum in `Shared` root namespace (15 values: Gram through Package)
- `Shared/Shared/MealUnitExtensions.cs` — `ToNorwegian()` extension method for display names
- `Shared/Shared/FireStoreDataModels/InventoryItem.cs` — New Firestore entity for pantry tracking
- `Shared/Shared/HandlelisteModels/InventoryItemModel.cs` — DTO mirror of InventoryItem

**Updated Firestore models:**
- `MealIngredient.cs` — Removed EntityBase inheritance; changed to ShopItemId/ShopItemName/Quantity(double)/Unit(MealUnit)/IsOptional/IsFresh/IsBasic fields per D3 (embedded, no standalone collection)
- `MealRecipe.cs` — Added MealType, IsFresh, PrepTimeMinutes, Effort (MealEffort), BasePortions fields
- `DailyMeal.cs` — Removed EntityBase inheritance; replaced WeekMenuId+DayOfWeek+MealRecipe with Day+MealRecipeId+IsSuggested per D3 (ref-based, not embedded)
- `WeekMenu.cs` — Replaced CreatedDate with PlanningStartDate (Thursday week anchor)
- `MealCategory.cs` — Added Chicken, Pasta, Celebration, Other; added MealType and MealEffort enums

**Updated DTO models:**
- `MealIngredientModel.cs` — Mirrors new MealIngredient (no EntityBase)
- `MealRecipeModel.cs` — Mirrors new MealRecipe with new fields
- `DailyMealModel.cs` — Mirrors new DailyMeal (no EntityBase, uses MealRecipeId reference)
- `WeekMenuModel.cs` — PlanningStartDate replaces CreatedDate
- `HandlelisteModels/MealCategory.cs` — Same enum additions as Firestore side

**Updated supporting files:**
- `Api/ShoppingListProfile.cs` — Added `InventoryItem ↔ InventoryItemModel` AutoMapper mapping
- `Repository/MemoryGenericRepository.cs` — Updated seed data to new MealIngredient structure; added `FirestoreMealType`/`FirestoreMealEffort` using aliases to resolve ambiguity from duplicate enums in both namespaces

**No change needed to `GoogleDbContext.cs`** — Convention-based naming already handles `InventoryItem → inventoryitems` automatically (D4).

**Validation:** `dotnet build Shared.csproj` clean; `dotnet test Api.Tests.csproj` — 110 passed, 0 failed.

**Key patterns confirmed:**
- `MealUnit` lives in root `Shared` namespace (shared across FireStoreDataModels and HandlelisteModels)
- Embedded classes (MealIngredient, DailyMeal) do NOT inherit EntityBase
- Root collection entities (MealRecipe, WeekMenu, InventoryItem) DO inherit EntityBase
- Duplicate enums in both namespaces (MealCategory, MealType, MealEffort) require using-aliases in files that import both namespaces simultaneously

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

### 2026-04-04 — Phase 5 Data Models: FamilyProfile, FamilyMember, PortionRule, AgeGroup ✅ COMPLETE

**Summary:** Created all Phase 5 entities for family profile and portion-rule support.

**New files created:**
- `Shared/Shared/AgeGroup.cs` — `AgeGroup` enum (Adult, Child, Toddler) in root `Shared` namespace alongside `MealUnit`
- `Shared/Shared/FireStoreDataModels/FamilyProfile.cs` — `FamilyMember` (embedded, no separate collection) + `FamilyProfile : EntityBase` (Firestore collection: `familyprofiles`)
- `Shared/Shared/HandlelisteModels/FamilyProfileModel.cs` — `FamilyMemberModel` + `FamilyProfileModel : EntityBase` with `IsValid()` guard
- `Shared/Shared/FireStoreDataModels/PortionRule.cs` — `PortionRule : EntityBase` (Firestore collection: `portionrules`); uses `AgeGroup` + `MealUnit`
- `Shared/Shared/HandlelisteModels/PortionRuleModel.cs` — `PortionRuleModel : EntityBase` with denormalised `ShopItemName` and `IsValid()` guard

**Updated files:**
- `Api/ShoppingListProfile.cs` — added `FamilyProfile ↔ FamilyProfileModel`, `FamilyMember ↔ FamilyMemberModel`, `PortionRule ↔ PortionRuleModel` AutoMapper entries
- `Api/Program.cs` — added `IGenericRepository<FamilyProfile>` and `IGenericRepository<PortionRule>` in both dev (Memory) and production (Firestore) blocks
- `Shared/Shared/Repository/GoogleDbContext.cs` — added `public const string FamilyProfiles = "familyprofiles"` and `public const string PortionRules = "portionrules"` as named constants (convention-based `GetCollectionKey()` already derives them correctly without overrides)

**Key patterns:**
- `FamilyMember` is embedded in `FamilyProfile` — no standalone Firestore collection, no standalone DI registration (same as MealIngredient/DailyMeal)
- `AgeGroup` lives in root `Shared` namespace, identical to `MealUnit` pattern
- `PortionRuleModel` denormalises `ShopItemName` for display without separate fetch

**Validation:** `dotnet build Shared.csproj` — 0 errors, 46 warnings (pre-existing). `dotnet build Api.csproj` — 0 errors, 59 warnings (pre-existing). ✅

### Firestore Meal Seeding — SeedMealRecipesController ✅ COMPLETE

**Date:** 2026-04-05

**Finding:** The Firestore infrastructure for MealRecipe was fully correct — `[FirestoreData]`/`[FirestoreProperty]` attributes on model, `GoogleFireBaseGenericRepository<MealRecipe>` registered in production Program.cs, `GetCollectionKey()` convention giving `mealrecipes`, AutoMapper mappings in place, and a full CRUD controller including bulk import. Zero code gaps in the storage layer.

**The actual gap:** Production Firestore `mealrecipes` collection starts empty. The 50+ canonical family dinner recipes exist only in `MemoryGenericRepository` seed data (development-only). No mechanism existed to populate Firestore on first deployment.

**Fix:** Created `Api/Controllers/SeedMealRecipesController.cs` — a `POST /api/tools/seed-meal-recipes` endpoint that:
1. Reads `mealrecipes` collection via `IGenericRepository<MealRecipe>.Get()`
2. If not empty: returns `{ alreadySeeded: true, count: N }` — safe to re-call
3. If empty: inserts all 57 canonical meal recipes with correct Category/MealType/Effort/PopularityScore; IDs auto-generated by Firestore
4. Returns count of inserted records

**Existing import endpoint** (`POST /api/mealrecipes/import`) also available for custom batch import.

**Validation:** `dotnet build Api.csproj` — 0 errors. `dotnet test Api.Tests.csproj` — 159 passed, 0 failed.

**Lesson:** `GoogleFireBaseGenericRepository.Insert()` always auto-generates the Firestore document ID (overwrites any pre-set `entity.Id`). Seeding via Insert is idempotent only if guarded by an empty-collection check first.

### 2026-04-05 — Staging Firestore Seeding ✅ COMPLETE

**Task:** Seed the staging Firestore `mealrecipes` collection from `dinners.txt` family dinner history.

**Result:**
- Called `POST /api/tools/seed-meal-recipes` → collection was empty → seeded **61 canonical recipes** (the hardcoded seed list in `SeedMealRecipesController.BuildSeedRecipes()`).
- Parsed `dinners.txt` (~850 diary entries) for unique, canonical dinner names not already covered by the 61. Excluded: family visits ("morfar", "mormor", "bestebo"), events ("juletrefest", "grillfest"), vague entries ("rester", "grøt" already covered), and place names ("Bergen", "Eiksetra").
- Built 35 additional recipes and imported via `POST /api/mealrecipes/import`.
- **Total in Firestore after seeding: 96 recipes** (61 canonical + 35 from dinners.txt).

**New recipes added (35):**
KidsLike: Thaiburger, Grillpølser, Pølsefest, Pølsehorn, Pølsegrateng, Pizzaboller, McDonalds
Fish: Laks og erterisotto, Laks og ris, Fiskekaker i brun saus, Kremet fiskesuppe med pesto, Hvit fisk curry, Pastaform med laks, Sommerruller med laks, Sushi
Meat: Grillkjøtt, Grillspyd, Biff wok, Bakt potet og biff, Hjort og rotgrønnsaker, Elg, Skinkesteik, Sommerkoteletter, Falukorv
Vegetarian: Stekt søtpotet i tortilla, Spinatpannekaker med kikerter, Grønn hverdagspizza
Chicken: Kylling og ris, Tom ka gai, Høne
Pasta: Pasta bolognese, Pastamiddag i panne
Celebration: Raclette
Other: Kebab, Sprø thaiboller

**Lesson:** The import endpoint (`POST /api/mealrecipes/import`) does not deduplicate — calling it twice would create duplicates. Always check existing collection state before importing. The seed endpoint (`POST /api/tools/seed-meal-recipes`) is already guarded: it's a no-op if the collection is non-empty.

**Lesson:** `MealType` enum in both namespaces uses `Takeout = 3` (not `Takeaway`). This tripped up initial classification — always verify enum values from source files, not from memory or spec documents.

### 2026-04-05 — ShopItem Extensions (refs #73, #75, #76) ✅ COMPLETE

**Summary:** Extended `ShopItem` (Firestore) and `ShopItemModel` (DTO) with four new properties spanning three issues. All done in one cohesive commit to avoid merge conflicts on the same model files.

**New enum created:**
- `Shared/Shared/StockBehaviour.cs` — `StockBehaviour { Track, DoNotTrack }` in root `Shared` namespace (same pattern as `MealUnit`, `AgeGroup` — shared between both model hierarchies without duplication)

**Properties added to `ShopItem` + `ShopItemModel`:**
- `IsBasic` (bool, default false) — #73: marks staple/always-stocked items
- `StockBehaviour` (StockBehaviour, default Track) — #75: per Peter's decision, lives on ShopItem not ShoppingListItem
- `StandardPurchaseQuantity` (double, default 0) — #76: purchase pack size (0 = not set)
- `StandardPurchaseUnit` (string, default null) — #76: purchase unit label ("kg", "stk", "l"; null = not set)

**AutoMapper:** `CreateMap<ShopItem, ShopItemModel>().ReverseMap()` handles all four fields by name convention. No profile changes needed.

**Admin UI:** `ItemManagementPage.razor` edit form extended with checkbox (IsBasic), dropdown (StockBehaviour), number input (StandardPurchaseQuantity), text input (StandardPurchaseUnit). Second row below existing name/category/unit row, visible only when EditClicked. EnableEdit/CancelEdit updated to save/restore all four fields.

**Firestore migration:** None required. All four properties have safe zero-value defaults — missing fields on existing documents deserialise to false/Track/0/null respectively.

**Build validation:** Shared ✅ 0 errors, Api ✅ 0 errors, Client ✅ 0 errors.

**Lesson:** `StockBehaviour` enum belongs in root `Shared` namespace (not `Shared.FireStoreDataModels`) so both model hierarchies can use it without cross-namespace coupling or duplication. Follow the `MealUnit`/`AgeGroup` pattern for any new enum that needs to be shared across Firestore and DTO models.


## Orchestration Log — 2026-04-04T05:12:37Z
**Phase 1 Meal Planning — Data Models ✅ COMPLETE**
- Created MealUnit enum (root Shared namespace) + MealUnitExtensions
- Created InventoryItem (Firestore) + InventoryItemModel (DTO)
- Updated MealIngredient: removed EntityBase, double qty+MealUnit, IsFresh, IsBasic
- Updated MealRecipe: MealType, IsFresh, PrepTimeMinutes, Effort, BasePortions
- Updated DailyMeal: removed EntityBase, MealRecipeId string ref, IsSuggested
- Updated WeekMenu: PlanningStartDate (Thursday anchor)
- Extended MealCategory enum: Chicken, Pasta, Celebration, Other
- Added MealType, MealEffort enums (both namespaces)
- Mirror all changes in HandlelisteModels DTOs
- Updated MemoryGenericRepository: seed data + using aliases for enum ambiguity
- Updated ShoppingListProfile: InventoryItem AutoMapper entry
- GoogleDbContext: no changes (D4 convention handles InventoryItem → inventoryitems)
- ✅ Build clean, 110 API tests pass
