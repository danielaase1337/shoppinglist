# Team Decisions Archive

---

## Phase 1 Meal Planning — Data Models (Ray)
**Date:** 2026-04-04  
**Author:** Ray (Firebase Expert)  
**Status:** ✅ COMPLETE

### D1: MealUnit Location
**Decision:** `MealUnit` enum defined in root `Shared` namespace (not FireStoreDataModels/HandlelisteModels).  
**Rationale:** Cross-cutting concern used by both MealIngredient and InventoryItem. Avoids duplication.  
**Implication:** Any file using MealUnit must `using Shared;` explicitly.

### D2: Embedded vs Root Entities
**Decision:** MealIngredient and DailyMeal are **embedded** (not root entities). Neither inherits EntityBase.  
**Rationale:** Historical data integrity — MealIngredient belongs to MealRecipe, DailyMeal to WeekMenu. Hard-deleting parent orphans children.  
**Implication:** No `IGenericRepository<MealIngredient>` registration. Accessed only through parent entity.

### D3: MealIngredient Quantity as Double + Enum
**Decision:** Quantity = `double`, Unit = `MealUnit` enum (not int with implicit "1 piece" assumption).  
**Rationale:** Recipes need fractional quantities (0.5 kg, 1.5 cups). MealUnit enum supports standardized conversions.  
**Implication:** MemoryGenericRepository and AutoMapper must handle double serialization.

### D4: InventoryItem Collection Key
**Decision:** InventoryItem collection = `inventoryitems` (automatic via GoogleDbContext D4 convention).  
**Rationale:** Consistency with naming — ShopItem → `shopitems`, MealRecipe → `mealrecipes`, InventoryItem → `inventoryitems`.  
**Implication:** No manual collection key registration needed.

### D5: Enum Duplication in Dual Namespaces
**Decision:** MealCategory, MealType, MealEffort defined in **both** FireStoreDataModels and HandlelisteModels.  
**Rationale:** Mirrors existing pattern (ShoppingListItem duplicated). Allows files to use either namespace without wildcard imports.  
**Implication:** Files using both namespaces must use `using` aliases to disambiguate (e.g., `using FireStoreMealCategory = Shared.FireStoreDataModels.MealCategory`).

---

## Phase 1 Meal Planning — API Controller (Glenn)
**Date:** 2026-03-29  
**Author:** Glenn (Backend Dev)

### D-G1: Soft Delete Pattern
**Decision:** DELETE on MealRecipe sets `IsActive = false` and calls `_repository.Update()`. No hard delete.  
**Rationale:** Soft delete preserves historical data — WeekMenu/DailyMeal history depends on MealRecipe integrity.  
**Implication:** GET /api/mealrecipes returns all records (including IsActive=false). Client must filter if needed.

### D-G2: AuthorizationLevel.Anonymous
**Decision:** All MealRecipeController Functions use `AuthorizationLevel.Anonymous`.  
**Rationale:** Consistent with ShopsItemsController. Auth enforcement at Azure Static Web Apps edge (D2 architecture).  
**Implication:** No Function-level API keys. SWA routing handles identity.

### D-G3: Bulk Import Function Naming
**Decision:** Bulk import endpoint = `[Function("mealrecipesimport")]` with route `mealrecipes/import`.  
**Rationale:** Azure requires unique Function names. `mealrecipes` taken by collection handler. Suffix `import` clarifies intent.  
**Implication:** URL is `/api/mealrecipes/import` (POST), distinct from single-record handlers.

### D-G4: InventoryItem Registration Deferred
**Decision:** InventoryItem repository NOT registered in Program.cs (Phase 1).  
**Rationale:** Ray's model finalized too late for Glenn to safely register. Deferred to Phase 4 (when needed for planner).  
**Implication:** InventoryItem available in Shared only. No API endpoints yet.

---

## Phase 1 Meal Planning — Frontend Pages (Blair)
**Date:** 2026-04-04  
**Author:** Blair (Frontend Dev)

### Decision 1: MealUnit Namespace Scoping
**Choice:** Added `@using Shared` **per-page** (not _Imports.razor).  
**Rationale:** MealUnit in root Shared namespace. Global using risks conflicts with other Shared.* types.  
**Impact:** Each meal/inventory page must add one line. Future-proof against namespace pollution.

### Decision 2: Enum Binding Pattern
**Choice:** String-backed enum bindings — `@bind="@_categoryStr"` + `Enum.TryParse` in Save().  
**Rationale:** Blazor `@bind` on cross-namespace enums can fail at runtime. String intermediaries are explicit and safe.  
**Impact:** New enum-bound selects (MealType, MealEffort) follow this pattern throughout meal pages.

### Decision 3: Ingredient List Ordering
**Choice:** New ingredients **append to end** (not insert at position 0).  
**Rationale:** Shopping list rule (D-blair-1) doesn't apply to recipes. Ingredients are structured (protein → sides → spices). Users expect intentional order.  
**Impact:** Overrides general "insert at 0" convention **only for ingredients**. Shopping/frequent items still insert at top.

### Decision 4: Soft-Delete UX
**Choice:** Delete on list page calls DELETE API and reflects inactive state locally. Delete on detail page navigates away.  
**Rationale:** List page keeps inactive recipes visible (avoids full re-fetch). Detail page navigation avoids editing inactive recipes.  
**Impact:** DELETE endpoint must return 200 OK with soft-deleted recipe (not 404).

---

## Phase 1 Meal Planning — Unit Tests (Josh)
**Date:** 2026-04-04  
**Author:** Josh (QA Engineer)

### Decision 1: Real Controller Tests
**Context:** Pre-existing tests called `_mockRepository.Object.Get()` directly — tested mock, not controller.  
**Decision:** Replaced all 6 stub tests. New 12 tests call `_controller.RunAll(req)` / `_controller.RunOne(req, id)`.  
**Impact:** Tests now verify actual controller logic (routing, mapping, LastModified, error codes).

### Decision 2: MealCategory Aliases in Tests
**Context:** MealCategory enum defined in both namespaces — ambiguity in test code.  
**Decision:** Added file-level aliases:
```csharp
using FireStoreMealCategory = Shared.FireStoreDataModels.MealCategory;
using DtoMealCategory = Shared.HandlelisteModels.MealCategory;
```
**Impact:** Enables use of both enums in same test file without `global::` prefixes.

### Decision 3: BulkImport Fully Tested
**Context:** RunImport handler not initially in controller.  
**Decision:** Test 9 (BulkImport_CreatesAllRecipes) written to spec — fully implemented and passing.  
**Impact:** Verifies deserialization, insert loop, AutoMapper application for list of 9 recipes.

### Decision 4: Model Validation Tests
**Context:** MealRecipeModel has IsValid method.  
**Decision:** Added Tests 11 & 12 for empty name → invalid, name set → valid.  
**Impact:** Ensures business rules (non-empty name) are testable.

---

## Cross-Team Decisions

### Architecture: Soft Delete is Standard
**Applies to:** All entities with historical references (MealRecipe, InventoryItem, future extensions)  
**Pattern:** DELETE sets IsActive=false, calls Update(), returns 200 OK with updated record.  
**Client Responsibility:** Filter IsActive=true when displaying lists (not API's job in v1).

### Architecture: LastModified Auto-Set
**Applies to:** All POST/PUT operations  
**Pattern:** Automatic `DateTime.UtcNow` on create/update — no client override.  
**Testing:** Verify LastModified is set/updated in unit tests (Josh model).

### Architecture: Enum Duplication for Dual-Namespace
**Applies to:** Enums used across FireStore ↔ DTO boundaries  
**Pattern:** Define in both namespaces, use file-level aliases where both imported.  
**Migration:** Future refactor could consolidate to single source (Ray's recommendation).

### Frontend: String-Backed Enum Bindings
**Applies to:** `<select>` with enums in different namespaces  
**Pattern:** `@bind="@_enumStr"` + `Enum.TryParse` in handler — avoid direct `@bind="@entity.EnumProp"`.  
**Rationale:** Prevents cross-namespace binding failures.

---

## Phase 2 Meal Planning — WeekMenuController API (Glenn)
**Date:** 2026-04-04  
**Author:** Glenn (Backend Dev)  
**Status:** ✅ IMPLEMENTED

### D1 — Four Function handlers (not three)
WeekMenu needs extra lookup route (`weekmenu/week/{weekNumber}/year/{year}`) beyond standard collection/single split. Distinct Azure Function (`weekmenubyweek`) avoids route conflicts with `{id}`.

### D2 — `weekmenubyweek` does full Get() scan
`IGenericRepository<T>.Get()` has no predicate overload. Loads all records and filters in-memory. Acceptable for current dataset sizes; Firestore query index needed if hot path.

### D3 — Generate shopping list is preview-only
`weekmenugenerateshoppinglist` returns `ShoppingListModel` without persisting. Caller decides whether to save as ShoppingList or FrequentList. Keeps API composable, avoids side-effects.

### D4 — Single `_mealRepository.Get()` with in-memory dict
Fetches all active recipes once into `Dictionary<string, MealRecipe>` (O(1) lookup). Only active recipes included. Avoids N+1 fetches per DailyMeal.

### D5 — CustomIngredients take precedence
If DailyMeal has `CustomIngredients.Any()`, uses those exclusively. Recipe defaults only when no overrides. DailyMeals with missing MealRecipeId/ingredients silently skipped.

### D6 — Quantity aggregation uses Math.Ceiling to int
`Mengde` is `int`. Aggregated `double` quantities ceiled to avoid under-ordering (e.g., 1.5 + 0.5 = 2).

### D7 — No new DI registrations
`IGenericRepository<WeekMenu>` already registered (Phase 1). No changes needed.

### D8 — Auto-generated Name on POST
Client sends empty/null Name → controller sets `"Uke {WeekNumber} {Year}"`. Ensures meaningful display name.

---

## Phase 2 Meal Planning — WeekMenuPages Frontend (Blair)
**Date:** 2026-04-07  
**Author:** Blair (Frontend Dev)  
**Status:** ✅ IMPLEMENTED

### D1 — `DailyMealModel.MealRecipeName` added
Was missing from Shared model. Added as plain `string` property for denormalized display in planner. Avoids recipe lookups on every render.

### D2 — `select` (not SfAutoComplete) for recipe picker
7-row planner uses `<select>` with `@onchange`, not SfAutoComplete. Syncfusion instances are too heavy; standard select is performant and gives side-effects cleanly.

### D3 — Thursday-first week order
`WeekOrder` static `DayOfWeek[]` starts Thursday (Norwegian meal-planning convention). No localization library needed.

### D4 — Friday auto-suggestion: KidsLike OR pizza
On page load, if Friday unset, top-scoring active recipe with `Category == KidsLike` OR name contains "pizza" (case-insensitive) pre-selected with `IsSuggested = true`. Override clears flag.

### D5 — "Generer handleliste" hidden for unsaved menus
Button renders only when `!IsNew` (has Id). Prevents API calls with non-existent ids.

### D6 — Generate shopping list: inline card, not modal
Rendered below save buttons, not in JS modal. Consistent with app's no-modal infrastructure; simpler implementation.

### D7 — Nav placement: "Ukemeny" under Admin, after "Middager"
Planning/admin concern grouped logically with meal features.

### D8 — `@using Shared` per-page
Follows Phase 1 pattern. Avoids namespace collision in _Imports.razor.

---

## Phase 2 Meal Planning — Unit Tests (Josh)
**Date:** 2026-04-04  
**Author:** Josh (QA Engineer)  
**Status:** ✅ IMPLEMENTED

### D1 — Real Controller Tests (WeekMenuControllerTests)
All 16 tests call actual controller methods (`_controller.RunAll(req)` / `_controller.RunOne(req, id)`). No mock-only testing.

### D2 — Moq ICollection<T> Type Inference Bug
`_mockRepo.Setup(r => r.Get()).ReturnsAsync(new List<T>())` silently returns null for parameterless `Get()` returning `Task<ICollection<T>>`. **Fix**: use `Returns(Task.FromResult<ICollection<T>>(list))` with explicit type parameter. Applies to any mocked `Get()` returning interface collection.

### D3 — Soft-delete pattern verified
Mock setup ensures `_repository.Delete()` never called (verified with `Times.Never`). Tests document correct soft-delete: Get → IsActive=false → Update.

---

## Phase 2 UI — ShopItem Autocomplete (Blair)
**Date:** 2026-04-04  
**Author:** Blair (Frontend Dev)  
**Status:** ✅ IMPLEMENTED

### Decision: Replace plain text input with SfAutoComplete for ingredients
Reuses ShopItems catalogue, links ingredients to known item IDs (enables future shop sorting). Free-form entry preserved: null `ItemData` → slug-ID from text.

### Implementation: Parallel ShopItems loading
`_shopItems` loaded in `OnInitializedAsync` parallel to recipe fetch. `_shopItemsLoaded` flag shows spinner until ready. `_newIngShopItemId` tracks real catalogue ID separately from display text.

### Pattern: Exact match to OneShoppingListPage lines 115-128
SfAutoComplete configuration mirrors existing autocomplete usage. Event handler type: `Syncfusion.Blazor.DropDowns.ChangeEventArgs<string, ShopItemModel>`.

---

## Deferred Decisions

### Phase 3+: InventoryItem API
**Status:** Awaiting finalization of recipe → stock feature scope.  
**Blocker:** DI registration deferred (no endpoints defined yet).

### Phase 3+: Frontend IsActive Filtering
**Status:** Awaiting frontend pagination/filter work.  
**Approach:** Client-side filter for now. Consider backend query optimization.

### Future: Enum Consolidation
**Ray recommendation:** Consolidate MealCategory/MealType/MealEffort to single namespace.  
**Impact:** Would simplify file-level aliases. Lower priority than Phase 2 features.

### Phase 3+: Meal Suggestion Algorithm (AI Foreslå Meny)
**Status:** Deferred pending API spec refinement.  
**Scope:** Suggestion endpoint, category weighting, recent-meal exclusion.

---

## Dinner Seed Data — MemoryGenericRepository (Glenn)
**Date:** 2026-04-08  
**Author:** Glenn (Backend Dev)  
**Status:** ✅ IMPLEMENTED

### D1 — Family Dinner Parsing
**Decision:** Replace 5-item placeholder MealRecipe list with 62 real unique meals from `Api.Tests/Helpers/dinners.txt`.  
**Rationale:** Development mode needs realistic recipe data for testing meal planning. dinners.txt contains family's actual dinner history (723 entries).  
**Parsing logic:** Strip day-prefix, normalize case, merge near-duplicates (laks variants distinct), filter noise (rester, ferie, events).  
**Result:** 62 meaningful dishes extracted with zero placeholder artifacts.

### D2 — Category Distribution
**Distribution achieved:**
- **KidsLike (14):** pizza, taco, pannekaker, pølse og potetmos, nuggets, fiskeburger, pølsegnocchi, nachos, etc.
- **Fish (11):** laks, fiskeboller, fiskegrateng, fiskekaker, salmalaks, laks i pita, hvit fisk, fiskesuppe, torsk, fiskepakke, kveite
- **Meat (12):** lapskaus, kjøttkaker, biff, spareribs, bulgogi, kjøttboller, fårikål, raspeballer, vossakorv, benløse fugler, nakkekoteletter, finnebiff
- **Vegetarian (6):** tomatsuppe, linsegryte, marokkansk bønnegryte, veggisburger, gulrotsuppe, quinoaburger
- **Chicken (8):** kylling gong bao, tikka masala, kyllingform, kyllingsuppe, kyllinglår, kyllingklubber, hønsefrikassé, kalkun
- **Pasta (4):** lasagne, spagetti og kjøttsaus, pastaform, one pot pasta
- **Celebration (3):** pinnekjøtt, ribbe, medisterkaker
- **Other (3):** wok, enchiladas, drunken noodles

### D3 — Effort Classification
- **Quick (≤20 min):** grøt, pannekaker, pølse og potetmos, nuggets, fiskeburger, pølsegnocchi, fiskepinner
- **Weekend (45+ min):** taco, lasagne, kjøttkaker, biff, spareribs, bulgogi, fårikål, raspeballer, pinnekjøtt, ribbe, kylling gong bao, tikka masala, kalkun
- **Normal (20–45 min):** all others

### D4 — MealType Classification
- **Frozen (1 entry):** fiskepinner (pre-frozen product)
- **FreshCook (61 entries):** everything else

### D5 — Seed Record Properties
- `IsActive = true`
- `BasePortions = 4` (family meal standard)
- `LastModified = DateTime.UtcNow`
- `Id = Guid.NewGuid().ToString()` (regenerated per dev restart)
- `PopularityScore` reflects real-world family frequency (pizza=100, taco=90, laks=85, ..., drunken noodles=34)

### D6 — Code Pattern for 62 Entries
**Old pattern:** One verbose `new MealRecipe { ... }` object per item + `if (x is TEntity e) await Insert(e)`.  
**New pattern:** `List<MealRecipe> recipes = new() { ... }; foreach (var r in recipes) await Insert(r as TEntity);`  
**Reason:** 62 entries in verbose pattern would be unmaintainable.

### Impact
- Development mode has realistic test data for meal planning feature validation
- Popularity scores enable sorting/suggestion testing
- Production Firestore unaffected (DEBUG-only path)

---

## mealplanningv2 PR Delivery (Peter)
**Date:** 2026-06-XX  
**Author:** Peter (Lead)  
**Requested by:** Daniel Aase  
**Status:** PR #67 OPENED

### Summary
Complete 5-phase meal planning system delivered to `main` via PR #67.

### PR Details
- **URL:** https://github.com/danielaase1337/shoppinglist/pull/67
- **Branch:** `mealplanningv2` → `main`
- **Title:** feat: Full meal planning system — Phases 1–5
- **Test Suite:** 177+ tests passing, 0 build errors

### Issues Closed (20 total)
#29, #46–#62, #66 (all Phase 1–5 implementation issues).

### Architecture Decisions Embedded
- OwnerId isolation (#66): FamilyProfile structure in place; per-user scoping deferred to auth hardening sprint (per D2).
- Moq pattern documented: `Task<ICollection<T>>` requires explicit generic parameter in `.ReturnsAsync()`.
- Soft-delete standard across all meal-related entities.
- String-backed enum bindings for cross-namespace safety (Blazor limitation).

### Next Steps
1. Daniel reviews and merges PR #67
2. Issues #64 (variety engine) and #65 (Google Keep import) tracked for future sprint
3. OwnerId scoping tracked as follow-on to auth hardening sprint
