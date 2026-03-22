# Project Context

- **Owner:** Daniel Aase
- **Project:** Shoppinglist — Blazor WebAssembly shopping list app with shop-specific item sorting
- **Stack:** Google Cloud Firestore, `GoogleFireBaseGenericRepository<T>`, `MemoryGenericRepository<T>` (debug), Shared models
- **Created:** 2026-03-22

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
- Dual-model pattern is mandatory: `Shared/FireStoreDataModels/` (with `[FirestoreData]`/`[FirestoreProperty]`) and `Shared/HandlelisteModels/` (DTOs).
- Norwegian property names (`Varen`, `Mengde`, `ItemCateogries`) preserved for Firestore backward compatibility — do not rename.
- Every entity inherits `EntityBase` which includes `LastModified` (DateTime?) with `[FirestoreProperty]`.
- Lazy migration: GET endpoints check for null `LastModified` and backfill with `DateTime.UtcNow`.
- Current goals: explore Firebase Authentication integration and Firestore query performance improvements.

### 2025-01-22 — Full Data Architecture Review (data-findings.md)
- **CRITICAL BUG:** `GoogleDbContext.GetCollectionKey()` only maps 4 types. `FrequentShoppingList`, `MealRecipe`, `MealIngredient`, `WeekMenu`, and `DailyMeal` all fall through to `"misc"` — they will corrupt each other in production.
- **CRITICAL BUG:** `WeekMenu` and `DailyMeal` are not registered in `Program.cs` DI — the week menu feature is entirely unwired.
- **DESIGN ISSUE:** `MealIngredient` is both embedded inside `MealRecipe.Ingredients[]` AND has its own `IGenericRepository<MealIngredient>` registered in DI. These two storage strategies are contradictory. The standalone repo writes to a "misc" collection that nothing reads.
- **DESIGN ISSUE:** `MealCategory` enum is duplicated in both `Shared.FireStoreDataModels` and `Shared.HandlelisteModels` namespaces. AutoMapper maps between two identical enums — unnecessary.
- **DATA MODEL:** `ShoppingListItem` does not inherit `EntityBase` — it has no `Id`. Items within a list cannot be individually targeted for update.
- **DATA MODEL:** `ShoppingList.ListId` is redundant with the inherited `Id` field from `EntityBase`.
- **ANTI-PATTERN:** `ShopItem` is fully embedded (denormalised) in `ShoppingListItem.Varen`, `FrequentShoppingItem.Varen`, and `MealIngredient.ShopItem`. Renames do not propagate. At 5 levels of nesting in WeekMenu, document size limits become a real risk.
- **PERFORMANCE:** `IGenericRepository.Get()` always reads entire collections — no filtering, no pagination, no field-level querying. All sorting and filtering is in-memory on the API.
- **PERFORMANCE:** Inline `LastModified` migration in `ShoppingListController.RunAll()` fires one write per document on every GET — should be extracted to a one-time migration script.
- **SECURITY:** Zero user isolation. No `OwnerId`/`UserId` on any entity. Adding Firebase Auth requires adding ownership fields + migration + query changes.
- `ShelfCategory` class with `GetDefaults()` is dead code — never stored in Firestore, never used by any controller or model.
- `GoogleDbContext` stores collection state as mutable properties (`Collection`, `CollectionKey`) — safe only because `AddTransient` creates a new instance per injection, but the design is fragile.
