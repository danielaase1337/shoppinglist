# D-Glenn-Unconsume: Unconsume Endpoint Design Decisions

**Author:** Glenn  
**Date:** 2026-04-24  
**Issue:** #81  
**PR:** #85

---

## Decision 1 — Reuse ConsumeMealRequest for unconsume

**Choice:** Unconsume endpoint accepts the same `ConsumeMealRequest` body (`DayOfWeek` + `MealRecipeId`) as the consume endpoint.

**Rationale:**
- Caller already has this data when rendering the "undo" button (same page, same context)
- No new DTO required — reduces surface area
- Symmetric API: consume and unconsume have identical request shapes

---

## Decision 2 — No upper-clamp on inventory restore

**Choice:** `QuantityInStock += ingredient.Quantity` with no maximum cap.

**Rationale:**
- Consume deducts stock, unconsume restores exactly what was deducted
- Clamping at 0 only makes sense for deduction (can't go negative)
- Restoring above original stock is acceptable (edge case: user manually adjusted stock between consume and unconsume — accept the slight over-count rather than silently lose stock data)

**Alternative rejected:** Cap at pre-consume value — impossible to know without storing a snapshot, adds complexity for a rare edge case.

---

## Decision 3 — Silent skip when inventory entry missing

**Choice:** If no `InventoryItem` exists for an ingredient's `ShopItemId`, skip silently (same behaviour as consume endpoint).

**Rationale:**
- Unconsume should never fail due to a missing inventory entry
- Consistent with consume logic — if deduct skips missing entries, restore must also skip them
- No data to restore if there was no deduction recorded

---

## Decision 4 — Route pattern

**Choice:** `PUT /api/weekmenu/{weekMenuId}/unconsume` with Function name `"weekmenuunconsume"`.

**Rationale:**
- Mirrors `PUT /api/weekmenu/{weekMenuId}/consume` exactly
- Azure Functions route uniqueness requires distinct Function names
- `PUT` is correct: modifying existing state (IsConsumed flag), not creating a new resource
