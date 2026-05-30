# Glenn decision note — Issue #77 IsBasic fix

## Context
`WeekMenuController.RunGenerateShoppingList()` already uses AutoMapper when a `ShopItem` record is found, but generated lists still lost `IsBasic` behaviour in two cases:
1. `MealIngredient.IsBasic` items were filtered out during aggregation, so they never reached the generated shopping list.
2. The fallback inline `ShopItemModel` (used when the catalogue lookup misses) still defaulted `IsBasic` to `false`.

## Decision
- Keep `IsBasic` ingredients in the generated shopping list so the frontend can group them under the collapsed “Basisvarer / Trolig ikke nødvendig” section.
- Continue using the existing catalogue lookup + AutoMapper path for full `ShopItem` metadata (`IsBasic`, `ItemCategory`, `Unit`, `StockBehaviour`, purchase-size fields).
- When the catalogue lookup misses, preserve `MealIngredient.IsBasic` on the fallback `ShopItemModel` instead of defaulting to `false`.

## Why
This matches the frontend contract in `OneWeekMenuPage.razor`, fixes issue #77 without adding new Firestore queries, and keeps generated shopping lists resilient when meal data references a stale or missing catalogue entry.
