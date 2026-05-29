# Squad Decisions

**Last Updated:** 2026-05-29  
**Source:** Phase 6+ onwards (active decisions). Archived entries moved to decisions-archive.md.  
**Note:** Archived decisions (Phases 1–6, older than 30 days) preserved in decisions-archive.md for reference.

---
## Phase 6+: Recent Decisions (Issues #81–#84) — 2026-04-24

### D30 — Issue #81 Unconsume Endpoint (Backend)
**Status:** ✅ IMPLEMENTED (Glenn, 2026-04-24)  
**Component:** Azure Functions `PUT /api/weekmenu/{id}/unconsume`

**Decision:**
- Reuse `ConsumeMealRequest` shape (DayOfWeek + MealRecipeId) — no separate request type needed
- **No upper-bound clamp** on inventory restore: `stock += quantity` (allows out-of-order operation tolerance)
- Skip inventory update if `InventoryItem` missing (opt-in pattern)
- Set `IsConsumed = false` and `LastModified = DateTime.UtcNow`

**Rationale:**
- Mirror `ConsumeMeal` logic exactly in reverse
- No clamp prevents data loss if operations called out of sequence
- Opt-in inventory keeps transaction minimal

**Tests:**
- ✅ `Unconsume_SetsIsConsumedFalse_ReturnsOk`
- ✅ `Unconsume_ReversesInventoryDeduction`
- ✅ `Unconsume_Returns404_WhenMenuNotFound`
- ✅ 162 API tests passing (0 failures)

**Configuration:**
- `ShoppingListKeysEnum.WeekMenuUnconsume = 23`
- `ISettings: "weekmenuunconsume" → "api/weekmenu"`

---

### D30.1 — Issue #81 Unconsume UI (Frontend)
**Status:** ✅ IMPLEMENTED (Blair, 2026-04-24)  
**Component:** OneWeekMenuPage.razor — "↩ Angre" button

**Decision:**
- No confirmation dialog (per Daniel: "keep it simple")
- Button appears only when `IsConsumed == true`
- Calls backend `PUT /api/weekmenu/{id}/unconsume` endpoint
- Silently logs errors if endpoint unavailable (graceful degradation)

**Rationale:**
- Single-action reversal unlikely to trigger accidentally
- Reduces UI clutter
- Consistent with Daniel's simplicity direction

---

### D31 — Issue #82 Unit Field UI Component
**Status:** ✅ IMPLEMENTED (Blair, 2026-04-24)  
**Component:** ItemManagementPage.razor — Unit input

**Decision:**
- Use `SfComboBox` (not `SfDropDownList`) with `AllowCustom="true"`
- Predefined unit list: Stk, kg, g, L, dl, ml, pk, boks, pose (hardcoded, no API call)
- Applied to both add-new-item form AND inline edit row

**Rationale:**
- `SfComboBox` allows both list selection and custom input (e.g., "flaske", "glass")
- `SfDropDownList` enforces strict selection only (too rigid)
- Units stable; no need for server data source
- Reduces typos while allowing exceptions

---

### D32 — Issue #83 Mobile Responsiveness
**Status:** ✅ IMPLEMENTED (Blair, 2026-04-24)  
**Component:** OneWeekMenuPage.razor, FamilyProfilePage.razor, InventoryPage.razor

**Decision:**
- Use Bootstrap responsive breakpoints (`col-12 col-md-N`, `col-12 col-sm-N`)
- Add single `@media (max-width: 576px)` block **only** for week-planner table (icon column hidden)
- Apply `table-responsive` wrapper to both tables in FamilyProfilePage (horizontal scroll)
- Prioritize content > buttons on small screens

**Rationale:**
- Minimal custom CSS; Bootstrap handles 80% of cases
- Week planner needs surgical control for icon column (mobile: icon visible in dropdown name, so no loss)
- Content accessibility critical for shopping use case (on-phone while shopping)

**Breakpoints Tested:** 576px (mobile), 768px (tablet), 992px (desktop)

---

### D33 — Issue #84 Meal Selection UX Unification
**Status:** ✅ IMPLEMENTED (Blair, 2026-04-24)  
**Component:** OneWeekMenuPage.razor — merged dropdown handler

**Decision:**
- Remove "🔄 Bytt" button entirely
- Merge `OnSwapMealSelected` logic into single `OnMealSelected` handler
- Regular dropdown stays editable until `IsConsumed == true`
- Handler flow: update local state → call `PUT /weekmenu/{id}/swap` if menu saved (not new)

**Rationale:**
- Two-button flow was redundant (selection intent already clear from dropdown)
- Single handler reduces state management complexity (`_swappingDay` field removed)
- Consumed days locked via plain text display (no explicit `disabled` needed)
- `WeekMenuSwap = 22` enum kept (endpoint still used, just no dedicated button)

**Code Reduction:** ~30 lines removed (swap dropdown, state field, old handler)

---

### D34 — IsBasic Population Bug Audit
**Status:** ✅ COMPLETED (Glenn, 2026-04-24)  
**Component:** WeekMenuController.RunGenerateShoppingList (Issue #77 fix verification)

**Finding:**
- Scanned all `Api/Controllers/` for inline `new ShopItemModel` constructions bypassing AutoMapper
- **One bug found:** `WeekMenuController` line 265 — `Varen = new ShopItemModel { Id = kvp.Key, Name = kvp.Value.ShopItemName }`
- **Fix applied:** Replaced with `_mapper.Map<ShopItemModel>(shopItem)` + graceful fallback
- **Result:** IsBasic, StockBehaviour, StandardPurchaseQuantity, StandardPurchaseUnit now populate correctly

**Other Controllers Audited — No Issues:**
- ShoppingListController, MealRecipeController, ShopsItemsController, ShopItemCategoryController, InventoryItemController — all use AutoMapper correctly

**Recommendation:**
- Add linting rule: "Never construct `ShopItemModel` inline — always use `_mapper.Map<ShopItemModel>()`"
- Fallback pattern acceptable only when source may be unavailable

---

### D35 — StockBehaviour on ShopItem (Issue #75)
**Status:** ✅ DECIDED (Peter, 2026-04-24)  
**Component:** ShopItem + ShopItemModel + IsDone hook

**Decision:**
- Add `StockBehaviour` enum on `ShopItem`, not on `ShoppingListItem`
- Enum values: `Track` (default), `DoNotTrack`
- When `ShoppingList.IsDone → true`: iterate items where `Varen.StockBehaviour == Track`, upsert InventoryItem, increment `QuantityInStock`

**Rationale:**
- Item-level, not row-level: "Don't track bread" is about bread itself, not a shopping trip
- Simplest change: 1 enum file + 1 property + 1 filter
- No per-list decision fatigue for users
- Inventory explicitly approximate ("Estimert lager"); auto-stock is additive, auto-deduct is subtractive, drift expected

**Impact:**
| Team | Action |
|------|--------|
| Ray | Add `StockBehaviour` enum + property to ShopItem/ShopItemModel, update AutoMapper |
| Glenn | Implement IsDone hook filter |
| Blair | Add toggle to item admin page; label inventory "Estimert lager" |
| Josh | Tests: Track items stocked, DoNotTrack skipped |

**Alternatives Rejected:**
1. `IsMealSourced` on ShoppingListItem + `ExcludeFromStock` on ShopItem — overlapping concerns
2. `StockBehaviour` on ShoppingListItem — per-row flexibility adds decision fatigue

---

| Decision | Status | Owner | Target Date |
|----------|--------|-------|-------------|
| D30: Unconsume Backend | ✅ Implemented | Glenn | 2026-04-24 ✅ |
| D30.1: Unconsume Frontend | ✅ Implemented | Blair | 2026-04-24 ✅ |
| D31: Unit Dropdown | ✅ Implemented | Blair | 2026-04-24 ✅ |
| D32: Mobile CSS | ✅ Implemented | Blair | 2026-04-24 ✅ |
| D33: UX Unification | ✅ Implemented | Blair | 2026-04-24 ✅ |
| D34: IsBasic Audit | ✅ Completed | Glenn | 2026-04-24 ✅ |
| D35: StockBehaviour | ✅ Decided | Peter | Next Sprint |

---

## Phase 7: Package Size Feature Sprint (Issues #76–#88–#89) — 2026-04-26

### D36 — Package Size Feature Design (Architecture)
**Status:** ✅ DESIGNED (Peter, 2026-04-24)  
**Related:** Issue #76 (purchase unit sizes already implemented)

**Context:**
`ShopItem` already has `StandardPurchaseQuantity` (double) and `StandardPurchaseUnit` (string) from sprint #76. The feature enables package-aware shopping list generation: recipe needs 400g chicken → chicken comes in 500g packages → buy 1 package.

**Components (3-part solution):**
1. **Unit Bridge (Glenn):** `MealUnitExtensions` — compatibility check between `MealUnit` enum and `StandardPurchaseUnit` string
2. **Backend Calculation (Glenn):** `WeekMenuController.RunGenerateShoppingList()` — package conversion with stock comparison
3. **Display Layer (Blair):** Format `{Mengde} × {qty}{unit}` in shopping list UI

**Architecture Decisions:**
- Package size lives on `ShopItem` (item master), not `ShoppingListItem`
- Fallback: Math.Ceiling when package data unavailable or units incompatible
- No new entities or API endpoints needed

---

### D36.1 — Package Unit Compatibility & Normalization
**Status:** ✅ IMPLEMENTED (Glenn, 2026-04-24)  
**Component:** `Shared/MealUnitExtensions.cs` — 4 new public methods

**Methods:**

| Method | Purpose |
|--------|---------|
| `IsCompatibleWith(MealUnit, string)` | Check if ingredient unit and purchase unit are same dimension |
| `NormalizeToBaseUnit(MealUnit, double)` | Convert ingredient quantity to base unit (gram, dl, stk) |
| `NormalizePurchaseUnitToBase(string, double)` | Convert purchase unit to base unit |
| `CalculatePackagesNeeded(...)` | Final package count; returns null on incompatibility |

**Supported Units:**
- Weight: `g`, `gram`, `kg`, `kilogram` → base: grams
- Volume: `dl`, `deciliter`, `l`, `liter` → base: deciliters
- Count: `stk`, `pcs`, `pakke`, `pk` → base: count

**Fallback Conditions (null returned):**
- `StandardPurchaseQuantity <= 0`
- `StandardPurchaseUnit` null/empty/unknown
- Demand unit and package unit incompatible (e.g., grams vs. stk)
- `UnitMismatch = true` (same ShopItemId used with different MealUnit across meals)

---

### D36.2 — Package Conversion in RunGenerateShoppingList
**Status:** ✅ IMPLEMENTED (Glenn, 2026-04-24)  
**Component:** `Api/Controllers/WeekMenuController.cs` — shopping list generation

**Decision: Pipeline Order (CRITICAL)**
Stock comparison (raw-unit subtraction of `QuantityInStock`) **must run before** package conversion. Both mutate `item.Mengde`.

**Canonical Pipeline:**
1. Aggregate raw ingredient quantities per `ShopItemId`
2. Apply stock comparison → subtract `QuantityInStock`, set `IsLikelyNotNeeded`
3. Apply package conversion → update `Mengde` to package count

**Aggregation Tuple (extended):**
```csharp
var aggregated = new Dictionary<string, (double Quantity, MealUnit Unit, string ShopItemName, ShopItem ShopItem)>();
```

**Impact:**
- `item.Mengde` becomes a **package count** when StandardPurchaseQuantity is configured and units compatible
- Falls back to raw quantity (existing behavior) when package data unavailable or incompatible
- No per-trip configuration — fully automatic

**Tests:** 3 new integration tests + 26 unit extension tests (211 total, 0 failures)

---

### D36.3 — Package Size Display Format
**Status:** ✅ IMPLEMENTED (Blair, 2026-04-24)  
**Component:** `OneShoppingListItemComponent` + `OneWeekMenuPage` preview

**Decision:**
- Format: `{Mengde} × {StandardPurchaseQuantity}{StandardPurchaseUnit}` (e.g., "2 × 500g")
- G29 number format avoids trailing zeros (500 not 500.0)
- Applied in: live shopping list + week menu preview
- Fallback: plain `Mengde.ToString()` when package info not set
- Style: `pkg-size-label` muted to keep numeric focus

**Rationale:**
- Compact, mobile-friendly — fits on one line
- User sees both editable (Mengde) and informational (package size) data simultaneously
- No modal/separate view needed

**Related:** `FormatQuantity()` helper for consistency across components

---

### D31 — SfComboBox Pattern (UX Consistency Update)
**Status:** ✅ IMPLEMENTED (Blair, 2026-04-24)  
**Component:** Multiple Blazor components

**Update to D31 (existing pattern):**

**1. Meal Selection in OneWeekMenuPage**
- Replaced plain `<select>` with `SfComboBox` + `AllowFiltering="true"`
- Built-in name search (no `FilteringEventArgs` wiring needed)
- DataSource as stable `List<T>` field (avoid re-render churn)
- Note: `ComboBoxTemplates` does not support `ValueTemplate` — use `ItemTemplate` only

**2. StandardPurchaseUnit in ItemManagementPage**
- Replaced plain `<input type="text">` with `SfComboBox TValue="string" TItem="string"`
- Uses existing `_unitOptions` static list + `AllowCustom="true"`
- Identical to pattern used for `Unit` field above
- Enforces D31 consistency, prevents unit value typos

**Rationale:** Consistency (D31), discoverability, prevents typos breaking inventory matching

---

### D31.1 — "Er alltid hjemme" Label Clarification
**Status:** ✅ IMPLEMENTED (Blair, 2026-04-24)  
**Component:** ItemManagementPage.razor — IsBasic checkbox

**Change:** Sub-label updated from "Basisvare" to "Er alltid hjemme" for clarity.

**Context:** `IsBasic` enum means item is always-in-stock and should appear collapsed in generated shopping lists. Previous label was not self-explanatory.

**Implementation:** Display-only, no model changes.

---

## Implementation Status (Phase 7 — Updated)

| Decision | Status | Owner | Target Date |
|----------|--------|-------|-------------|
| D36: Package Size Design | ✅ Designed | Peter | 2026-04-24 ✅ |
| D36.1: Unit Compatibility | ✅ Implemented | Glenn | PR #89 ✅ (2026-04-24) |
| D36.2: RunGenerateShoppingList | ✅ Implemented | Glenn | PR #89 ✅ (2026-04-24) |
| D36.3: Display Format | ✅ Implemented | Blair | PR #88 ✅ (2026-04-24) |
| D31 Update: SfComboBox | ✅ Implemented | Blair | PR #87 ✅ (2026-04-24) |
| D31.1 Label Clarification | ✅ Implemented | Blair | PR #87 ✅ (2026-04-24) |

