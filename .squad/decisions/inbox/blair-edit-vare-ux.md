# Decision: Edit-Vare UX — Checkbox + Combined Pakkestørrelse

**Author:** Blair  
**Date:** 2026-04-27  
**Branch:** mealplanningv2  

## Context

The extended edit row in `ItemManagementPage.razor` had four col-3 fields:
`IsBasic` checkbox, `StockBehaviour` select, `StandardPurchaseQuantity` number input, `StandardPurchaseUnit` text input.
This was visually cluttered and the select for a two-value enum was heavy.

## Decisions Made

### D-BLAIR-01: StockBehaviour → single checkbox "Spor lager"
A dropdown for a binary enum (Track / DoNotTrack) is excessive. A labelled checkbox is immediately legible.
Binding via `@onchange` inline lambda; no computed property needed.

### D-BLAIR-02: Pakkestørrelse as input-group (number + SfComboBox)
Showing purchase quantity and unit as two separate inputs gave no visual relationship between them.
Merging into `<div class="input-group">` with the existing `_unitOptions` list (same as the Unit field above)
makes the pairing obvious and consistent. `AllowCustom="true"` preserves free-text unit entry.

### D-BLAIR-03: 3×col-4 layout
Three equal columns (IsBasic / Spor lager / Pakkestørrelse) are cleaner than four unequal ones.
Each column has a single, clear purpose.

### D-BLAIR-04: Bottom Lagre/Avbryt row in expanded edit section
Top action buttons remain icon-only (space-efficient in the item row).
A second row at the bottom of the expanded section adds text-labelled buttons for discoverability,
following the pattern established in PR #87.

### D-BLAIR-05: Read-mode pkg-hint
Small muted `pk: 400g` label below the unit badge when pakkestørrelse is set.
Guarded by `StandardPurchaseQuantity > 0 && !string.IsNullOrEmpty(StandardPurchaseUnit)` — shows nothing for items without package info.
