# Peter — Sprint Review Follow-ups

**Date:** 2026-04-23  
**Author:** Peter (Lead / Architect)  
**Context:** Review of PR #67 feedback sprint (issues #68–#76)

---

## Follow-up Items

### FU-1: `Varen.IsBasic` not populated in generated shopping lists (SHOULD FIX BEFORE MERGE)

**File:** `Api/Controllers/WeekMenuController.cs` — `RunGenerateShoppingList()` (line 263-268)

**Problem:** `ShopItemModel` is constructed with only `Id` and `Name`. The `IsBasic` flag from the ShopItem catalogue is never set, so it defaults to `false`. The frontend grouping logic in `OneWeekMenuPage.razor` (line 182: `i.Varen?.IsBasic == true`) is dead code — no items from generated lists will ever match.

**Impact:** The "Basisvarer" part of the collapsible bottom group label ("Basisvarer / Trolig ikke nødvendig") is misleading — only `IsLikelyNotNeeded` items (from stock comparison) appear there. Items where the ShopItem master has `IsBasic=true` are NOT grouped there.

**Fix options:**
1. Load all ShopItems in `generate-shoppinglist` and use the full object for `Varen` (adds one more repository call but produces correct data)
2. Carry `IsBasic` through the aggregation loop from ShopItem catalogue
3. Accept the current behaviour and rename the group to just "Trolig ikke nødvendig"

**Recommendation:** Option 1 — fetch ShopItems to populate full `Varen`. Keeps frontend logic correct without special handling.

---

### FU-2: No transaction guarantee in ConsumeMeal

**File:** `Api/Controllers/WeekMenuController.cs` — `ConsumeMeal()`

**Behaviour:** WeekMenu is updated (IsConsumed=true) before inventory deduction loop. If a Firestore write fails mid-loop, the menu shows consumed but some inventory items are not deducted.

**Decision:** Accept for v1. Inventory is explicitly approximate ("Estimert lager"). No Firestore transaction support in the generic repository pattern. Revisit if users report inconsistencies.

---

### FU-3: Unused enum values WeekMenuConsume / WeekMenuSwap

**File:** `Client/Common/ShoppingListKeysEnum.cs`

**Behaviour:** Enum values 21/22 exist but the URL is constructed from `WeekMenu` base + string append. The values serve as documentation / future refactor hooks.

**Decision:** No action needed. Note as minor tech debt.

---

### FU-4: #72 (Smart Menu Suggestion) remains open

Skipped per Daniel's request. The Phase 2 Friday auto-suggestion (top KidsLike recipe) remains the only suggestion mechanism. Full suggestion engine (history scan, category rotation, freshness) is parked.
