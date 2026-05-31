# Glenn Decision — MealRecipe IsActive Legacy Migration

- **Date:** 2026-05-31
- **Requested by:** Daniel Aase
- **Related issue:** #72a

## Context
`MealRecipe.IsActive` was added after some Firestore meal documents already existed. Those legacy documents deserialize with `IsActive = false` because C# bool defaults to false when the field is missing. `WeekMenuController.SuggestMenu()` filtered on `m.IsActive`, which excluded every legacy meal and returned an empty suggestion list.

## Decision
1. In `Api/Controllers/WeekMenuController.cs`, do **not** filter suggested meals by `IsActive` yet; only require a non-null `Id`.
2. In `Api/Controllers/MealRecipeController.cs` GET-all, lazily migrate legacy meal documents by setting `IsActive = true` and updating the repository before mapping.

## Rationale
There is currently no meal deactivation UI, so `IsActive = false` cannot be treated as a user-authored state in normal workflows. Removing the gate restores suggestions immediately, while lazy migration heals Firestore data over time so a future deactivation workflow can safely reintroduce the filter.

## Follow-up
When a real meal deactivation workflow ships, re-enable explicit `IsActive` filtering in suggestion flows and distinguish migrated legacy data from intentionally deactivated meals if needed.
