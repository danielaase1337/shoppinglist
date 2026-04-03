# Meal Planning — Full Implementation Plan

**Author:** Peter (Lead/Architect)  
**Date:** 2026-04-03 (Revised — Full Scope + Refinements)  
**Supersedes:** Meal Planning v1 slim scope (2026-03-29)  
**Status:** ✅ FINALIZED (2026-04-03) — All phases locked, ready for Phase 1 implementation tickets  
**Issue:** #29 (Scoping: Meal Planning — Full Implementation)  
**Blocks:** All meal planning implementation tickets

---

## Context — Why This Document Exists

Daniel confirmed on 2026-04-03 (issue #29) that the slim v1 (text-history only) is superseded. The directive was: **"skip the slim v1, go for full implementation at once."** This document replaces the previous 2026-03-29 scope and defines the complete meal planning system.

---

## Overview & Goals

Build a complete meal planning system integrated with the existing shopping list app. The system must:

1. **Store structured meal recipes** with ingredients linked to existing `ShopItem` catalogue
2. **Plan meals week by week** using a Thursday-to-Thursday week (shopping day = Thursday)
3. **Generate shopping lists** from selected meals, deduplicating ingredients across meals
4. **Track pantry inventory** — what is in stock, updated automatically when shopping lists complete
5. **Suggest meals** that use up half-used ingredients (e.g. half a broccoli)
6. **Import existing history** from Google Keep documents (Daniel pastes, team cleans and imports)

---

## Business Rules

These are hard requirements, not preferences:

| Rule | Detail |
|------|--------|
| **Pizza Fridays** | Friday = **suggested** Pizza (auto-populated with top-scoring Pizza recipe, `IsSuggested = true`). User CAN override. `IsLocked` removed — see D36. |
| **Thursday shopping** | We shop on Thursdays. The planning week runs **Thursday to next Wednesday**. |
| **Fresh ingredient priority** | Ingredients flagged as "fresh/perishable" must be scheduled early in the week (Thursday/Friday/Saturday). The system must warn if a perishable ingredient appears late in the week. |
| **Ingredient matching** | Adding a meal with a half-portion ingredient (e.g. 1/2 broccoli) should trigger a suggestion to add another meal that also uses broccoli that week. |
| **Inventory deduction** | When a shopping list is marked Done, its items are added to pantry stock. Ingredients needed for a meal are deducted from stock before generating a shopping list. |
| **Stock thresholds** | Pantry items have a lower threshold. When stock drops below threshold, the item is flagged for reorder. |

---

## Unit System (Cross-Cutting Concern — Design First)

This is a **prerequisite** for meal ingredients and inventory. Must be designed before Phase 1 code starts.

### Supported Units

| Unit | Norwegian | Notes |
|------|-----------|-------|
| `gram` | gram | Weight — spices, butter, cheese |
| `kilogram` | kilogram | Weight — meat, vegetables |
| `liter` | liter | Volume — milk, cream, broth |
| `deciliter` | desiliter | Volume (common in Norwegian recipes) |
| `tablespoon` | spiseskje (ss) | Volume — oils, sauces |
| `tablespoon_half` | 1/2 ss | |
| `tablespoon_quarter` | 1/4 ss | |
| `teaspoon` | teskje (ts) | Volume — spices, baking |
| `teaspoon_half` | 1/2 ts | |
| `teaspoon_quarter` | 1/4 ts | |
| `piece` | stykk / stk | Countable — eggs, onions, cans |
| `piece_half` | 1/2 stk | e.g. half a broccoli |
| `piece_quarter` | 1/4 stk | |
| `pinch` | klype | Spices — "a pinch of salt" |
| `package` | pakke | Pre-packaged items |

### Implementation

```csharp
public enum MealUnit
{
    Gram,
    Kilogram,
    Liter,
    Deciliter,
    Tablespoon,
    TablespoonHalf,
    TablespoonQuarter,
    Teaspoon,
    TeaspoonHalf,
    TeaspoonQuarter,
    Piece,
    PieceHalf,
    PieceQuarter,
    Pinch,
    Package
}
```

- `MealUnit` lives in `Shared` — used by both `MealIngredient` and `InventoryItem`
- Display names (Norwegian) resolved via a `MealUnitExtensions.ToNorwegian()` helper
- Fractional quantities encoded as separate enum values (not decimal fields) — avoids floating-point UI issues
- Inventory stores quantities as `double` with `MealUnit` for type context

---

## Data Model

### New Entities

#### `MealRecipe` (Firestore + DTO)

```csharp
[FirestoreData]
public class MealRecipe : EntityBase
{
    [FirestoreProperty] public MealCategory Category { get; set; }
    [FirestoreProperty] public MealType MealType { get; set; }        // FreshCook | Frozen | PrePrepped | Takeout
    [FirestoreProperty] public int PopularityScore { get; set; }
    [FirestoreProperty] public DateTime? LastUsed { get; set; }
    [FirestoreProperty] public bool IsActive { get; set; }
    [FirestoreProperty] public bool IsFresh { get; set; }             // Contains perishable ingredients
    [FirestoreProperty] public int? PrepTimeMinutes { get; set; }     // null = unset; raw minutes for display
    [FirestoreProperty] public MealEffort Effort { get; set; }        // Quick | Normal | Weekend (for filtering)
    [FirestoreProperty] public int BasePortions { get; set; } = 4;    // How many people this recipe serves as written (D40)
    [FirestoreProperty] public ICollection<MealIngredient> Ingredients { get; set; }
}

public enum MealCategory { KidsLike, Fish, Meat, Vegetarian, Chicken, Pasta, Celebration, Other }

// Added 2026-04-03 (D38)
public enum MealType { FreshCook, Frozen, PrePrepped, Takeout }

// Added 2026-04-03 (D37)
public enum MealEffort
{
    Quick,    // ≤20 min — frozen, pre-prepped, takeout
    Normal,   // 20–45 min — typical weeknight
    Weekend   // 45+ min — lasagne, slow-cook, special meals
}
```

#### `MealIngredient` (embedded in MealRecipe — per D3)

```csharp
[FirestoreData]
public class MealIngredient
{
    [FirestoreProperty] public string ShopItemId { get; set; }
    [FirestoreProperty] public string ShopItemName { get; set; } // denormalized for display
    [FirestoreProperty] public double Quantity { get; set; }
    [FirestoreProperty] public MealUnit Unit { get; set; }
    [FirestoreProperty] public bool IsOptional { get; set; }
    [FirestoreProperty] public bool IsFresh { get; set; }        // Perishable flag per ingredient
    [FirestoreProperty] public bool IsBasic { get; set; }        // Base ingredient flag (D42) — e.g. oil, salt, butter assumed in stock
}
```

#### `WeekMenu` (Firestore + DTO)

```csharp
[FirestoreData]
public class WeekMenu : EntityBase
{
    [FirestoreProperty] public int WeekNumber { get; set; }
    [FirestoreProperty] public int Year { get; set; }
    [FirestoreProperty] public DateTime PlanningStartDate { get; set; }  // Always a Thursday
    [FirestoreProperty] public ICollection<DailyMeal> DailyMeals { get; set; }
    [FirestoreProperty] public DateTime? LastModified { get; set; }
}
```

#### `DailyMeal` (embedded in WeekMenu)

```csharp
[FirestoreData]
public class DailyMeal
{
    [FirestoreProperty] public DayOfWeek Day { get; set; }
    [FirestoreProperty] public string MealRecipeId { get; set; }       // ref — per D3
    [FirestoreProperty] public bool IsSuggested { get; set; }          // true = auto-filled (e.g. Pizza Friday), overridable — replaces IsLocked (D36)
    [FirestoreProperty] public ICollection<MealIngredient> CustomIngredients { get; set; }
    // Empty = use recipe defaults; populated = overrides for this day
}
```

#### `InventoryItem` (Firestore + DTO) — New collection

```csharp
[FirestoreData]
public class InventoryItem : EntityBase
{
    [FirestoreProperty] public string ShopItemId { get; set; }
    [FirestoreProperty] public string ShopItemName { get; set; }
    [FirestoreProperty] public double QuantityInStock { get; set; }
    [FirestoreProperty] public MealUnit Unit { get; set; }
    [FirestoreProperty] public double LowerThreshold { get; set; }        // Reorder point
    [FirestoreProperty] public DateTime? LastUpdated { get; set; }
    [FirestoreProperty] public string ItemCategory { get; set; }          // For grouping in UI
    [FirestoreProperty] public string? SourceMealRecipeId { get; set; }   // Added D38: links frozen stock back to original recipe
}
```

### Firestore Collection Keys (D4 convention)

| Entity | Collection |
|--------|-----------|
| `MealRecipe` | `mealrecipes` |
| `WeekMenu` | `weekmenus` |
| `InventoryItem` | `inventoryitems` |
| `FamilyProfile` | `familyprofiles` |
| `PortionRule` | `portionrules` |

`MealIngredient`, `DailyMeal`, and `FamilyMember` are **embedded** — no separate collections.

### Existing Entities Extended

- `ShoppingList`: No changes needed — generated lists use existing structure
- `ShopItem`: No changes — referenced by `ShopItemId` in ingredients

### New Entities (Phase 5 — Family Profile)

```csharp
// FamilyProfile (D39) — Firestore: familyprofiles
[FirestoreData]
public class FamilyProfile : EntityBase
{
    [FirestoreProperty] public ICollection<FamilyMember> Members { get; set; }
}

// FamilyMember — embedded in FamilyProfile (D3 convention)
[FirestoreData]
public class FamilyMember
{
    [FirestoreProperty] public string Name { get; set; }
    [FirestoreProperty] public AgeGroup AgeGroup { get; set; }
    [FirestoreProperty] public string? DietaryNotes { get; set; }
}

public enum AgeGroup { Adult, Child, Toddler }

// PortionRule (D39) — Firestore: portionrules
[FirestoreData]
public class PortionRule : EntityBase
{
    [FirestoreProperty] public string ShopItemId { get; set; }
    [FirestoreProperty] public AgeGroup AgeGroup { get; set; }
    [FirestoreProperty] public double QuantityPerPerson { get; set; }
    [FirestoreProperty] public MealUnit Unit { get; set; }
}
```

---

## Feature Phases

### Phase 1 — Meals + Ingredients CRUD

**Goal:** Database of meals with structured ingredients. Fast to add, easy to maintain.

**Scope:**
- `MealRecipe` CRUD — API endpoints + admin UI page
- `MealIngredient` management embedded in recipe editor
  - Autocomplete from existing `ShopItem` catalogue
  - Unit selector (all supported units)
  - Quantity + optional/fresh flags
- Category tagging (icons: kids, fish, meat, veg, chicken, pasta, celebration)
- **`MealType` tagging** — FreshCook / Frozen / PrePrepped / Takeout (D38)
- **`PrepTimeMinutes` + `MealEffort`** — raw prep time + Quick/Normal/Weekend bucket (D37)
- `PopularityScore` auto-increments when a meal is used in a week menu
- Import support: Admin "bulk import" endpoint that accepts a JSON array of meals (used for Google Keep import flow)

**API Endpoints:**
```
GET    /api/mealrecipes             All recipes sorted by PopularityScore
GET    /api/mealrecipe/{id}         Single recipe with embedded ingredients
POST   /api/mealrecipes             Create recipe
PUT    /api/mealrecipes             Update recipe (including ingredient list)
DELETE /api/mealrecipe/{id}         Soft delete (IsActive = false)
POST   /api/mealrecipes/import      Bulk import (for Google Keep migration)
```

**Pages:**
- `Client/Pages/Meals/MealManagementPage.razor` — List with search/filter by category
- `Client/Pages/Meals/OneMealRecipePage.razor` — Recipe editor with ingredient list

**Dependencies:** Unit system enum must be in `Shared` before coding starts.

---

### Phase 2 — Meal Planning + Week View

**Goal:** Plan a week of meals and generate a shopping list.

**Scope:**
- `WeekMenu` CRUD
- Week view: Thursday to Wednesday layout (7 days, Thursday first)
- **Friday slot: auto-populated with top-scoring Pizza recipe, `IsSuggested = true`, fully overridable** (D36 — `IsLocked` removed)
- Recipe picker per day — sorted by `PopularityScore`, filtered by category and `MealEffort`
- **Effort filter on picker:** "Show only Quick meals" for busy days
- **Frozen meal quick-select:** if Phase 4 inventory is live, `Frozen`-type inventory items appear as picks; selecting one deducts stock without generating shopping list ingredients
- "Generate shopping list" button: aggregates all ingredients, deducts inventory, creates `ShoppingListModel`
- Perishable warning: if a `IsFresh=true` ingredient appears on day 5+ (Tuesday/Wednesday), show warning
- Navigation: "Ukemeny" added to main nav (not admin dropdown — this is a primary feature)

**API Endpoints:**
```
GET    /api/weekmenus                               All week menus
GET    /api/weekmenu/{id}                           Single week menu (resolves recipe refs)
GET    /api/weekmenu/week/{weekNumber}/year/{year}  By week + year
POST   /api/weekmenus                               Create week menu
PUT    /api/weekmenus                               Update week menu
DELETE /api/weekmenu/{id}                           Delete
POST   /api/weekmenu/{id}/generate-shoppinglist     Generate ShoppingList from week
```

**Shopping List Generation Logic (D42 — Basevarer Feature):**
1. For each `DailyMeal`, resolve recipe ingredients (or custom overrides)
2. **Separate ingredients into two groups:**
   - **Primary:** Specific ingredients needed for this week's meals
   - **Basevarer (Suggested):** Ingredients marked `IsBasic=true` from all recipes in the week
3. Check `InventoryItem` stock — deduct available quantities from required amounts (primary only; basevarer pre-checked but user-selects)
4. Group remaining needed quantities by `ShopItemId`, sum across meals (primary and selected basevarer)
5. Output as `ShoppingListModel` with:
   - Name: "Uke {N} {YYYY}" + `LastModified = DateTime.UtcNow`
   - Primary items section + optional "Basevarer" section
   - Basevarer pre-checked in UI but NOT auto-added — user reviews and selects what to include before finalizing list

**Basevarer Workflow (User Experience):**
- User clicks "Generate shopping list"
- System shows two sections: "Ingredienser denne uken" (Primary) and "Basevarer — huk hva du trenger" (Suggested Base Items)
- Basevarer items are pre-checked with checkboxes — user can uncheck to exclude or add manually to checked items
- Final list includes only selected basevarer + all primary items
- Prevents "forgot oil/salt/butter" situations when recipes assume these are in stock

**Dependencies:** Phase 1 complete. Phase 4 inventory must exist for stock deduction — if Phase 4 not done yet, generate full list without deduction.

---

### Phase 3 — Ingredient Matching + "Use Up" Logic

**Goal:** Smart suggestions that prevent food waste by pairing meals around half-used ingredients.

**Scope:**
- After a meal is added to a week slot, scan all its ingredients for fractional quantities (`PieceHalf`, `PieceQuarter`, etc.)
- For each fractional ingredient, query other `MealRecipe` entries that also use that `ShopItemId`
- Suggest these meals as additions to the week: "You used 1/2 broccoli in [Meal X]. These meals also use broccoli: [Y, Z]"
- Suggestion is a nudge only — user dismisses or accepts
- Algorithm runs client-side on the already-loaded recipe data (no new API endpoint needed)

**Dependencies:** Phase 1 + Phase 2 complete. All recipes must have accurate ingredients to work well.

---

### Phase 4 — Inventory / Stock System

**Goal:** Track what is in the pantry. Auto-deduct when shopping lists complete.

**Scope:**
- `InventoryItem` CRUD — admin UI page (grouped by item category)
- Manual stock adjustment (add/remove quantities)
- Auto-update on shopping list completion: when `ShoppingList.IsDone` is set to `true`, trigger stock addition for all items in that list
  - `ShoppingListController.PUT` checks `IsDone` transition and calls inventory service
  - Units must match between shopping list item and inventory item (same `ShopItemId`)
- Lower threshold alerts: items below threshold shown in red in inventory view and flagged in shopping list generation
- Unit display in "meal recipe sizes" (gram, liter, tablespoon, piece)
- **"Cook double → freeze one" workflow (D38):**
  - User marks "Made double batch" when logging a `FreshCook` meal as cooked
  - System creates an `InventoryItem` with `Name = "{RecipeName} (frossen)"`, `QuantityInStock = 1`, `SourceMealRecipeId = recipeId`
  - No new Firestore collection — frozen meals are inventory entries
  - Frozen entries surface in Phase 2 week planner as `MealType = Frozen` quick-pick options

**API Endpoints:**
```
GET    /api/inventoryitems           All inventory items (grouped by category)
GET    /api/inventoryitem/{id}       Single item
POST   /api/inventoryitems           Add item to inventory
PUT    /api/inventoryitems           Update item (quantity, threshold)
DELETE /api/inventoryitem/{id}       Remove from inventory
POST   /api/inventoryitems/adjust    Bulk adjust quantities (e.g. after stock-take)
```

**Trigger hook on ShoppingList completion:**
```csharp
// In ShoppingListController.PUT — when IsDone transitions false -> true
if (!existing.IsDone && updated.IsDone)
{
    await _inventoryService.AddFromShoppingList(updated);
}
```

**Dependencies:** Phase 1 (unit system, ShopItem references). Can be built in parallel with Phase 2+3.

---

### Phase 5 — Family Profile + Portion Scaling

**Goal:** Store family composition and per-person portion rules. Apply them during shopping list generation so quantities reflect the actual household.

**Scope:**

**New entities:**
```csharp
// FamilyProfile — one per household (D39)
[FirestoreData]
public class FamilyProfile : EntityBase
{
    [FirestoreProperty] public ICollection<FamilyMember> Members { get; set; }
}

// FamilyMember — embedded in FamilyProfile (D3 convention)
[FirestoreData]
public class FamilyMember
{
    [FirestoreProperty] public string Name { get; set; }
    [FirestoreProperty] public AgeGroup AgeGroup { get; set; }
    [FirestoreProperty] public string? DietaryNotes { get; set; }
}

public enum AgeGroup { Adult, Child, Toddler }

// PortionRule — separate Firestore collection: portionrules
// "pasta = 100g per adult, 60g per child"
[FirestoreData]
public class PortionRule : EntityBase
{
    [FirestoreProperty] public string ShopItemId { get; set; }
    [FirestoreProperty] public AgeGroup AgeGroup { get; set; }
    [FirestoreProperty] public double QuantityPerPerson { get; set; }
    [FirestoreProperty] public MealUnit Unit { get; set; }
}
```

**Shopping list generation with scaling:**
1. Load `FamilyProfile` → count members by `AgeGroup` (e.g. 2 adults, 1 child, 1 toddler)
2. For each ingredient in aggregated list, check for a `PortionRule` matching `ShopItemId` + `AgeGroup`
3. If rule found: `totalQuantity = Σ (rule.QuantityPerPerson × memberCount[ageGroup])`
4. If no rule: use raw recipe quantity as-is

**Scaling mode (D41 — FINAL):** Semi-automatic. The system calculates `scalingFactor = FamilyMemberCount / MealRecipe.BasePortions` and pre-fills suggested scaled quantities. User can adjust any quantity before saving the list. If no `FamilyProfile` exists, recipe quantities are used as-is.

**Scaling workflow:**
1. User clicks "Generate shopping list" in the week planner
2. System loads `FamilyProfile.Members.Count` and each recipe's `BasePortions`
3. For each ingredient: `suggestedQty = ingredient.Quantity * (familyCount / basePortions)`
4. Review step shows suggested quantities — user can override any line item
5. User confirms → `ShoppingListModel` saved with finalised quantities

**API Endpoints:**
```
GET    /api/familyprofile          Get household profile
PUT    /api/familyprofile          Update profile (members list)
GET    /api/portionrules           All portion rules
POST   /api/portionrules           Add rule
PUT    /api/portionrules           Update rule
DELETE /api/portionrule/{id}       Delete rule
```

**Pages:**
- `Client/Pages/Family/FamilyProfilePage.razor` — family members + portion rules editor
- Navigation: Under **Admin** dropdown (setup data, not daily-use)

**Dependencies:** Phase 2 (shopping list generation exists). Phase 4 (unit system in place).

---

## Data Import Plan (Google Keep to Database)

Daniel has existing meal history as Google Keep text documents structured by week and day.

**Flow:**
1. Daniel pastes Google Keep content into a chat with a designated team member
2. Team member (Josh recommended — clean structured data task) parses the text into the bulk import JSON format
3. JSON is POSTed to `POST /api/mealrecipes/import` (Phase 1 endpoint)
4. Initial import creates `MealRecipe` entries with `Name` only — `Ingredients` empty (`IsActive=true`)
5. Follow-up: Daniel or team enriches recipes with ingredients over time via the UI

**Import JSON format:**
```json
[
  { "name": "Taco", "category": "KidsLike", "popularityScore": 10 },
  { "name": "Fisk med poteter", "category": "Fish", "popularityScore": 8 },
  { "name": "Pizza", "category": "KidsLike", "popularityScore": 52, "isActive": true }
]
```

Note: Pizza must be imported with a recognizable name so the system locks it on Fridays. Simplest approach: filter by `Name == "Pizza"` when building the week calendar Friday slot.

---

## What Is Deferred

| Feature | Reason |
|---------|--------|
| AI/ML-powered suggestions | Overkill — frequency + ingredient matching is sufficient |
| Multi-user / family isolation | Deferred to auth Phase 2 (D2 — OwnerId scope) |
| Nutritional information | Not requested, complex to source |
| Recipe sharing between users | Multi-user prereq not met |
| File upload for import | Manual paste + bulk import endpoint is sufficient |
| Mobile native app sync | Out of scope for web app |
| Meal ratings / star system | `PopularityScore` auto-increment serves this purpose |
| Season-based filtering | Nice-to-have, low ROI |
| Multi-family / guest profiles | Phase 5 covers single household; guest support deferred |
| Dietary restriction recipe filtering | `DietaryNotes` stored but not yet used to filter suggestions |
| MealEffort auto-derivation from PrepTime | Manual enum set for v1; auto-derive can be added later |

---

## Cross-Cutting Dependencies & Sequencing

```
Unit System (enum in Shared)
        |
        v
Phase 1: Meal CRUD + Import  (+ MealType, PrepTimeMinutes, MealEffort)
        |
        +------------------------------+
        v                              v
Phase 2: Week Planning          Phase 4: Inventory  (+ frozen workflow)
        |
        v
Phase 3: Ingredient Matching
        |
        v
Phase 5: Family Profile + Portion Scaling
```

- **Unit system** must be in `Shared` before any Phase 1 code
- **Phase 1** (meal CRUD) blocks Phase 2 and Phase 3
- **Phase 4** (inventory) can run in parallel with Phase 2 but must complete before stock-deduction and frozen meal features activate
- **Phase 3** is the last core feature — requires accurate ingredient data from real usage
- **Phase 5** can be built in parallel with Phase 3; activates in list generation when Phase 2 is complete

---

## Success Criteria (Full Implementation Complete)

- [ ] All meal recipes importable from Google Keep via bulk import endpoint
- [ ] Recipe editor makes it fast to add ingredients with unit selector + ShopItem autocomplete
- [ ] MealType (FreshCook/Frozen/PrePrepped/Takeout) settable per recipe
- [ ] PrepTimeMinutes and MealEffort (Quick/Normal/Weekend) settable per recipe
- [ ] Week view shows Thursday to Wednesday with Friday **suggested** as Pizza (overridable)
- [ ] Shopping list generated from week menu with correct ingredient aggregation
- [ ] Perishable warnings show when fresh ingredients scheduled late in week
- [ ] Inventory tracks stock, deducts when shopping list completed
- [ ] "Cook double → freeze one" creates frozen inventory entry
- [ ] Frozen meals appear as quick-pick options in week planner when in inventory
- [ ] Ingredient matching suggests complementary meals for half-used items
- [ ] FamilyProfile stores members + AgeGroup
- [ ] PortionRules scale ingredient quantities per person during list generation
- [ ] `BasePortions` set on all recipes; scaling factor derived as `FamilyMemberCount / BasePortions` (D40)
- [ ] Week planner review step shows suggested scaled quantities; user can override before saving (D41)
- [ ] All new API endpoints tested (target: 30+ new controller tests)
- [ ] E2E tests cover: add meal, plan week, generate list flow
- [ ] No regressions in existing 122 API + 61 client tests

---

## Approvals & Sign-Off

**Revised by:** Peter (Lead/Architect), 2026-04-03 (initial full scope)  
**Refined by:** Peter (Lead/Architect), 2026-04-03 (refinements: D36–D39 applied — Pizza Friday rule reversal, MealEffort/PrepTime, MealType/frozen workflow, Phase 5 FamilyProfile)  
**Finalized by:** Peter (Lead/Architect), 2026-04-03 (D40–D42: BasePortions, semi-automatic scaling, full scope locked)  
**Reason for revision:** Daniel directive (issue #29, 2026-04-03) — skip slim v1, go full implementation  
**Reason for refinement:** Daniel comment (issue #29, comment 4184857751) — 4 scope refinements  
**Reason for finalization:** Daniel comment (issue #29, comment 4184947107) — portion scaling confirmed semi-automatic  
**Reviewed by:** Daniel Aase (via GitHub comments)  
**Approved by:** Daniel Aase ✅
