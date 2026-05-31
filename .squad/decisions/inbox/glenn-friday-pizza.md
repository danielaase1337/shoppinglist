# Glenn Decision — Friday Pizza Suggestion Rule

- **Date:** 2026-05-31
- **Requested by:** Daniel Aase
- **Related issue:** Week menu Friday pizza rule

## Context
`WeekMenuController.SuggestMenu()` builds a weekend meal pool for the client WeekOrder `[Thursday, Friday, Saturday, Sunday, Monday, Tuesday, Wednesday]`. Friday is slot index `1`, but the existing logic filled slots `1`, `2`, and `3` from the weekend pool in popularity order, so Friday could end up with any weekend meal.

## Decision
1. In `Api/Controllers/WeekMenuController.cs`, check the weekend pool for a meal whose `Name` contains `"pizza"` case-insensitively before filling weekend slots.
2. If such a meal exists, place it into weekend pick index `0` so it maps to Friday slot `1`, then remove it from the weekend pool before choosing Saturday and Sunday.
3. If no pizza meal exists, preserve the previous fallback behavior and fill Friday/Saturday/Sunday from the weekend pool and weekday fallback unchanged.

## Rationale
This delivers the explicit product expectation that Fridays should suggest pizza, while keeping the existing week ordering, weekend/weekday split, and fallback behavior intact.

## Follow-up
If more fixed day-specific meal rules are added later, keep them in the weekend/weekday pick stage before final slot assembly so the client WeekOrder remains stable.
