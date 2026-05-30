# Blair — Inventory adjust endpoint key

## Context
`InventoryItemsPage.razor` previously built the bulk adjust URL by concatenating `"/adjust"` onto the inventory collection endpoint.

## Decision
Add a dedicated client key for the bulk endpoint:
- `ShoppingListKeysEnum.InventoryItemsAdjust`
- `ISettings["inventoryitemsadjust"] = "api/inventoryitems/adjust"`

## Why
Blair's frontend rule is to route all API URLs through `ISettings` instead of hardcoding endpoint fragments inside Razor pages. This keeps custom action endpoints discoverable and reduces future breakage when routes change.
