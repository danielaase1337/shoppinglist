# Decision: D6 — Mobile Drag-and-Drop Replacement (Implemented)

**Author:** Blair  
**Date:** 2026-03-28  
**Issue:** #27 — Replace drag-and-drop with up/down buttons for mobile  
**Status:** ✅ IMPLEMENTED

---

## Decision

Implemented **Option B** from D6: up/down button controls as primary interaction, HTML5 drag-and-drop preserved as desktop enhancement only.

---

## What Was Done

### ShopConfigurationPage (`/shopconfig/{Id}`)
- Added `▲ Flytt opp` / `▼ Flytt ned` buttons to each shelf row (primary reorder interaction).
- `MoveShelfUp(shelf)` / `MoveShelfDown(shelf)` methods maintain SortIndex in the same way as the existing `HandleDrop`.
- Up button `disabled` on first shelf; down button `disabled` on last shelf.
- Grip icon button (`fas fa-grip-vertical`) given class `drag-handle-desktop` — hidden via `@@media (pointer: coarse)`.
- Available-category chips inside each shelf gained a `+` click button (`AssignCategoryToShelf`) as a mobile alternative to dragging categories onto shelves.

### CategoryManagementPage (`/categories`)
- Each item chip (blue badge) now contains a compact `<select>` dropdown pre-set to the item's current category.
- Changing the select calls `MoveItemToCategoryById` — same optimistic UI + rollback pattern as the existing `HandleItemDrop`.
- Uncategorized items have a `-- Velg kategori --` placeholder option.
- On touch devices (`pointer: coarse`), chip `cursor` is set to `default` (no grab cursor).

---

## Architectural Notes

- **`@media (pointer: coarse)` in Blazor `<style>` blocks must be written `@@media`** — single `@` is parsed as a Blazor directive and causes CS0103 compile error.
- **`ChangeEventArgs` ambiguity**: `Syncfusion.Blazor.Navigations` exports its own `ChangeEventArgs`. Always fully qualify as `Microsoft.AspNetCore.Components.ChangeEventArgs` when using `@onchange` in pages that import Syncfusion.
- SortIndex invariant preserved: `MoveShelfUp/Down` rebuilds the full ordered list and re-assigns sequential indexes (same pattern as `HandleDrop`).

---

## Rejected Approaches

- **JS touch polyfill**: Adds external JS dependency, fragile, defeats accessibility goal.
- **Removing drag entirely**: Desktop power users rely on drag for fast reordering. Keeping it as enhancement is correct.
