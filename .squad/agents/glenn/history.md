# Project Context

- **Owner:** Daniel Aase
- **Project:** Shoppinglist — Blazor WebAssembly shopping list app with shop-specific item sorting
- **Stack:** Azure Functions v4 (.NET 9), AutoMapper, repository pattern, Google Cloud Firestore, .NET DI
- **Created:** 2026-03-22

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
- Controllers follow a function-per-endpoint pattern: `[Function("shopitems")]` (collection) and `[Function("shopitem")]` (single, `Route = "shopitem/{id}"`).
- `Api/Program.cs` has `#if DEBUG` blocks — MemoryGenericRepository in debug, GoogleFireBaseGenericRepository in production.
- `LastModified = DateTime.UtcNow` must be set on all POST and PUT operations. GET operations do lazy migration for null values.
- `Api/ShoppingListProfile.cs` holds all AutoMapper mappings with `.ReverseMap()`.
- Current goals: implement authentication middleware and secure all API endpoints.

### Security Audit — 2025-01-29
- **CRITICAL BUG**: `GoogleDbContext.GetCollectionKey()` only maps 4 entity types. `FrequentShoppingList`, `MealRecipe`, `MealIngredient`, `WeekMenu`, and `DailyMeal` all resolve to `"misc"` in Firestore production — data corruption bug.
- **Auth inconsistency**: `ShoppingListController` and `MealRecipeController` use `AuthorizationLevel.Function`; all other controllers are `AuthorizationLevel.Anonymous`. No user-level auth exists anywhere.
- **No per-user data isolation**: `IGenericRepository<T>.Get()` returns all records with no user filtering. `EntityBase` has no `OwnerId` field.
- **Exception messages leaked to callers**: `GetErroRespons(e.Message, req)` pattern exposes internal error text.
- **ShopsController** uses `.Result` blocking call on `ReadFromJsonAsync` — deadlock risk under load.
- **WeekMenuController is missing entirely**: DI registration for `WeekMenu`/`DailyMeal` also absent.
- **AutoMapper** does not ignore UI-state properties (`EditClicked`, `CssComleteEditClassName`) in reverse mapping direction.
- **Azure SWA auth path**: Parse `x-ms-client-principal` header injected by the SWA runtime — no external JWT library needed.
- Full findings written to `.squad/agents/glenn/api-findings.md`.
