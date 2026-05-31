# Blair Decision — Consume/Unconsume Meal UI (#74)

**Date:** 2026-05-31  
**Author:** Blair  
**Issue:** #74

## API URL Strategy

No new `ShoppingListKeysEnum` values were added for consume/unconsume.

**Decision:** Append `/consume` and `/unconsume` as path suffixes to `Settings.GetApiUrlId(ShoppingListKeysEnum.WeekMenu, id)`.

**Rationale:**
- The same pattern already exists in `OneWeekMenuPage.razor` for `generate-shoppinglist`:  
  `$"{Settings.GetApiUrlId(ShoppingListKeysEnum.WeekMenu, _menu.Id)}/generate-shoppinglist"`
- Adding `WeekMenuConsume`/`WeekMenuUnconsume` enum values would require both a new enum entry and a URL mapping in `ISettings` for what is effectively a sub-action of an existing resource.
- Glenn's #80 cleanup explicitly removed `WeekMenuConsume` and `WeekMenuSwap` unused enum values — adding them back for trivial path suffixes would reintroduce the same noise.
- If consume/unconsume URLs ever change, the single `WeekMenu` entry in `ISettings` is the correct anchor point.

## CSS Consumed State

- `tr.meal-consumed td` → `opacity: 0.55` for global dim
- `tr.meal-consumed td.day-consume` → `opacity: 1` (override) so "Angre" button stays clearly visible
- `tr.meal-consumed td.day-label` → `text-decoration: line-through; color: #6c757d` for explicit crossed-out day label
- Dropdown `disabled` (not hidden) when consumed — preserves recipe name in the row without layout shift
