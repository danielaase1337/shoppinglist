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

## Deferred Decisions

### Phase 2+: InventoryItem API
**Status:** Awaiting finalization of recipe → stock feature scope.  
**Blocker:** DI registration deferred (no endpoints defined yet).

### Phase 2+: Frontend IsActive Filtering
**Status:** Awaiting frontend pagination/filter work.  
**Approach:** Client-side filter for now. Consider backend query optimization in Phase 3.

### Future: Enum Consolidation
**Ray recommendation:** Consolidate MealCategory/MealType/MealEffort to single namespace.  
**Impact:** Would simplify file-level aliases. Lower priority than Phase 2 features.
