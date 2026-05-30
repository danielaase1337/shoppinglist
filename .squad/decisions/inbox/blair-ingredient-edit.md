# Blair — Ingredient edit pattern proposal

- **Context:** Issue #70 on `Client/Pages/Meals/OneMealRecipePage.razor`
- **Decision:** Use `MealIngredientModel.EditClicked` + `CssComleteEditClassName` for ingredient inline-edit state, and render the editor in an expanded row below the read-only ingredient row.
- **Why:** The shared `edit` class is designed for block containers, not `<tr>` elements. An expanded editor row keeps table layout stable, preserves the existing ingredient list readability, and avoids mutating checkbox values outside explicit save/cancel flow.
- **Persistence:** Save ingredient edits with `PUT` to `MealRecipes` via `ISettings`, because meal ingredients are embedded within the recipe model and there is no dedicated ingredient endpoint.
