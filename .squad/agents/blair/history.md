# Project Context

- **Owner:** Daniel Aase
- **Project:** Shoppinglist — Blazor WebAssembly shopping list app with shop-specific item sorting
- **Stack:** Blazor WebAssembly (.NET 9), Syncfusion Blazor components, Azure Functions API, Google Cloud Firestore
- **Created:** 2026-03-22

## Core Context

### Frontend Architecture
- **UI components** in `Client/Pages/Shopping/` and `Client/Shared/`; new pages in `Client/Pages/Meals/`
- **URL routing** via `ISettings` + `ShoppingListKeysEnum` — never hardcode API paths
- **Auth pattern:** `SwaAuthenticationStateProvider` (reads `/.auth/me` from SWA), `<AuthorizeView>`, `<AuthorizeRouteView>` in App.razor
- **SfAutoComplete pattern:** `TValue="string"`, `TItem="ShopItemModel"`, check `args.ItemData != null` for selection vs free-form
- **Loading pattern:** Parallel task launch in `OnInitializedAsync`, flag-based spinners, await all at end
- **Navigation:** Admin dropdown (Blazor-toggled, accessible) contains: Hyppige Lister, Lager (inventory), Middager, Ukemeny, Håndter butikker, Administrer varer, Administrer kategorier

### Phase Completions
- **Phase 1 (Meals):** MealRecipe CRUD pages + category icons ✅ (2026-04-04)
- **Phase 2 (WeekMenus):** 7-day planner, Friday pizza auto-suggest, generate list preview ✅ (2026-04-07)
- **Phase 3 (Use-Up):** Fractional ingredient matching, session-scoped dismissal, amber suggestion panel ✅ (2026-04-08)
- **Phase 4 (Inventory):** Pantry admin, +1/-1 adjustments, frozen-meal shortcut, soft delete ✅ (2026-04-08)
- **Phase 5 (Portion):** FamilyProfilePage (members + rules), client-side scaling in generate-list flow ✅ (2026-04-09)

### Key Extensions & Helpers
- `MealUnit.ToNorwegian()` — extension for unit display throughout
- `AgeGroupLabel()` — static helper for AgeGroup localization (Adult/Barn/Småbarn)
- `GetCategoryIcon()` — static helper for meal category emojis
- `NaturalSortComparer` — existing, correct, no changes needed

### Cross-Agent Integration Patterns
- **Moq ICollection:** Requires explicit `Task.FromResult<ICollection<T>>(list)` type parameter
- **Hard delete (FamilyProfile):** No IsActive → `repository.Delete()` direct call
- **Soft delete (PortionRule, InventoryItem):** IsActive present → Get → set false → Update
- **Denormalisation:** DTOs can carry lookup names (ShopItemName, RecipeName) if stored at sync time
- **Client-side scaling:** Portion adjustments applied after API response (no new endpoint)

## Learnings & Tech Details

### 2026-05-29 — Meal Ingredient Inline Edit (#70) ✅ COMPLETE

**Inline edit pattern for meal ingredients on `OneMealRecipePage.razor`**
- Switched ingredient edit state from page-only `_editingIngredient == ing` checks to the shared `EditClicked` / `CssComleteEditClassName` pattern on `MealIngredientModel`.
- Kept display rows read-only and rendered a scoped inline editor row beneath the ingredient; this avoids accidental checkbox mutation without persistence and fits table layout better than styling `<tr class="edit">` directly.
- Existing recipes persist ingredient edits through `PUT` to `Settings.GetApiUrl(ShoppingListKeysEnum.MealRecipes)` because ingredients are embedded in the recipe payload; there is no separate meal-ingredient endpoint.
- Added scoped CSS in `OneMealRecipePage.razor.css` for the inline editor highlight so the shared `edit` class can be reused without relying on the global block-style `.edit` treatment.

### 2026-04-27 — Edit-Vare UI Simplification ✅ COMPLETE

**Simplified edit-vare extended row in ItemManagementPage.razor**
- `StockBehaviour` select → plain `<input type="checkbox">` with `@onchange` event handler: `item.StockBehaviour = (bool)e.Value! ? StockBehaviour.Track : StockBehaviour.DoNotTrack`. No computed property needed — inline lambda is clean and safe.
- `StandardPurchaseQuantity` + `StandardPurchaseUnit` merged into a single "Pakkestørrelse" `<div class="input-group input-group-sm">` containing a number `<input>` (max-width: 5rem) and `SfComboBox` with `AllowCustom="true"`. The combo has `CssClass="pkg-unit-combo"` to remove the double-border via scoped CSS (`border-left: 0; border-radius: 0 4px 4px 0`).
- Layout changed from 4×col-3 to 3×col-4 for visual balance.
- "Basisvare" label updated to "Er alltid hjemme".
- Bottom Lagre/Avbryt row duplicated under the extended section (text buttons); top row keeps icon-only buttons for compactness.
- Read-mode: added small `.pkg-hint` span (block, 0.75em, 70% opacity) showing `pk: 400g` when both quantity > 0 and unit set.
- Created `ItemManagementPage.razor.css` (first scoped CSS for this page) for `pkg-unit-combo` and `pkg-hint` rules. Used `::deep` for the Syncfusion wrapper element.
- `G29` format specifier for quantity display to strip trailing zeros (consistent with PR #88 pattern).

### 2026-04-26 — Pakkestørrelse-visning (PR #88) ✅ COMPLETE

**Package size display in shopping list items**
- `OneShoppingListItemComponent.razor`: replaced plain `<input @bind-value="Mengde" />` with a `<span class="qty-display">` wrapper containing the input (narrowed to 3rem when package info present) plus a `<span class="pkg-size-label">` showing `× {qty}{unit}` (e.g. `× 500g`).
- `StandardPurchaseQuantity` check: `> 0` (double) and `StandardPurchaseUnit` not null/empty. Format: `{Mengde} × {qty.ToString("G29")}{unit}` — `G29` avoids trailing zeros for clean display.
- Created `OneShoppingListItemComponent.razor.css` (first scoped CSS for this component) for `qty-display` flexbox and `pkg-size-label` muted styling.
- `OneWeekMenuPage.razor`: extracted `FormatQuantity(ShoppingListItemModel)` static helper with same logic; applied to both normal-items table and staple/bottom-items table in the generated-list preview.
- `git add` on tracked `.razor` files can silently not stage if the files show as "modified" but aren't picked up — always verify with `git diff HEAD` after the add. Use `git commit --amend` to fix without a new commit.


### 2026-04-24–2026-04-26 — UX Fixes & SfComboBox Unification (PR #87) ✅ COMPLETE

**SfComboBox Pattern Enforcement (D31 update):**
- Meal selection dropdown in OneWeekMenuPage: replaced plain `<select>` with `SfComboBox + AllowFiltering="true"`. Built-in name search, no `FilteringEventArgs` wiring. DataSource as stable `List<T>` field to avoid re-render churn.
- StandardPurchaseUnit field in ItemManagementPage: replaced plain text input with `SfComboBox` using existing `_unitOptions` static list + `AllowCustom="true"`. Consistency with Unit field above. Prevents typos in unit values that break inventory matching.
- Note: `ComboBoxTemplates` does NOT support `ValueTemplate` — only `ItemTemplate`, `HeaderTemplate`, `FooterTemplate`, `GroupTemplate`, `NoRecordsTemplate`, `ActionFailureTemplate`.

**UX Improvements:**
- OK/Avbryt (Confirm/Cancel) buttons on edit-vare modal (previously only save button).
- Norwegian labels throughout ("Bestillingsmengde", "Bestillingsenhet" for StandardPurchaseQuantity/Unit fields).
- "Er alltid hjemme" clarification label for IsBasic checkbox (was "Basisvare", not self-explanatory).
- `<hr class="my-3" />` separators in ItemManagementPage for visual grouping.

**Decision D31 merged** to `decisions.md` with SfComboBox pattern enforcement and unit field UI component specification.




**FIX 1 — SfComboBox med AllowFiltering i OneWeekMenuPage**
- Replaced plain `<select>` with `SfComboBox TValue="string" TItem="MealRecipeModel"` + `AllowFiltering="true"` — native search-as-you-type without extra filter event wiring.
- `ComboBoxEvents` inner component used for `ValueChange` instead of `@onchange` — async-safe pattern: `ValueChange="@(async e => await OnMealSelected(meal, e.Value))"`.
- `ComboBoxTemplates` accepts: `ItemTemplate`, `FooterTemplate`, `HeaderTemplate`, `GroupTemplate`, `NoRecordsTemplate`, `ActionFailureTemplate` — **`ValueTemplate` is NOT supported** (causes RZ9996 build error). Remove it.
- Category icons shown via `<ItemTemplate>` only; selected value displays plain `Name` from `ComboBoxFieldSettings`.
- DataSource must be a stable `List<T>` reference — computed once after loading into `_sortedRecipes` field. Avoids Syncfusion re-render thrash on every component render cycle.
- `CssClass` parameter on SfComboBox adds class to root wrapper div — `font-style: italic` does NOT inherit to inner `<input>`; must add `.week-planner-table .suggested-meal .e-input-group input.e-input` rule in `.razor.css`.

**FIX 2 — Bottom OK/Avbryt buttons in ItemManagementPage edit row**
- Simple duplication: added a `<div class="row mt-2">` after the extended edit block with the same `SaveItem`/`CancelEdit` callbacks.
- Top buttons stay as icon-only (space-efficient); bottom buttons include text ("OK" / "Avbryt") for discoverability.

**FIX 3 — UX labels + divider in edit vare**
- Added `<hr class="my-2" />` as visual divider between main row (name/category/unit) and extended row (advanced fields).
- All extended fields now have `<label class="form-label form-label-sm mb-1 text-muted">` above them.
- `IsBasic` sub-label changed from "Basisvare" (cryptic) to "Er alltid hjemme" (explains meaning).
- `StockBehaviour` now labelled "Lagersporing" above the select.
- `StandardPurchaseUnit` free-text input replaced with `SfComboBox` (same `_unitOptions` list as existing unit field) — consistent with D31 pattern.
- `align-items-end` on the extended row grid so labels + controls bottom-align cleanly.

### 2026-04-24 — Issues #84 #82 #83 #81 ✅ COMPLETE & INTEGRATED

**#84 — Remove Bytt button, unify dropdown handler (updated 2026-04-24)**
- Confirmed handler merge complete; reduced state management complexity
- `WeekMenuSwap = 22` enum retained (endpoint still active, button removed)
- **Decision D33 merged** to `decisions.md`

**#82 — Unit field → SfComboBox (updated 2026-04-24)**
- Confirmed `SfComboBox` with `AllowCustom="true"` on both add form and inline edit
- Static unit list: Stk, kg, g, L, dl, ml, pk, boks, pose (hardcoded)
- **Decision D31 merged** to `decisions.md`

**#83 — Mobile responsiveness pass (updated 2026-04-24)**
- Week planner: `@media (max-width: 576px)` hides day-icon column; icon visible in dropdown name
- FamilyProfilePage + InventoryPage: `col-12 col-md-N` breakpoints + `table-responsive` wrappers
- Breakpoints tested: 576px, 768px, 992px
- **Decision D32 merged** to `decisions.md`

**#81 — Undo "spist" button (updated 2026-04-24)**
- Added `↩ Angre` button below consumed day rows; no confirmation dialog
- Calls `PUT /api/weekmenu/{id}/unconsume` (Glenn's backend, now complete)
- Button disappears when day re-edits (IsConsumed = false re-enables dropdown)
- Frontend ready; backend integrated via Decision D30
- **Decisions D30 + D30.1 merged** to `decisions.md`

---

### 2026-04-12 — Issues #84 #82 #83 #81 ✅ COMPLETE

**#84 — Remove Bytt button, unify dropdown handler (`OneWeekMenuPage.razor`)**
- Removed `_swappingDay` field and the entire swap-specific dropdown branch; the regular dropdown now handles both "first-set" and "change" via a single `async Task OnMealSelected`.
- Merged handler: updates local state (MealRecipeId, MealRecipeName, IsSuggested=false, UseUpSuggestions) then calls `PUT /weekmenu/{id}/swap` if the menu is saved (not new).
- Removed `OnSwapMealSelected` entirely; also cleared `_swappingDay = null` reference from `ConsumeDay`.
- Consumed days lock the dropdown by showing plain text instead — no explicit `disabled` needed.
- `WeekMenuSwap` enum value kept (it's still used conceptually via the merged handler); just no longer has a dedicated button.

**#82 — Unit field → SfComboBox in ItemManagementPage**
- Replaced free-text `<input>` for Unit in both the add-new-item form and the inline edit row with `<SfComboBox TValue="string" TItem="string" AllowCustom="true">`.
- `DataSource="@_unitOptions"` — static readonly list: Stk, kg, g, L, dl, ml, pk, boks, pose.
- `@bind-Value` works identically to the old `@bind-value` on `<input>`.
- `SfComboBox` is in `Syncfusion.Blazor.DropDowns` which is globally imported via `_Imports.razor` — no per-page `@using` needed.
- `SfDropDownList` does NOT support custom entry; use `SfComboBox` with `AllowCustom="true"` for "dropdown + freeform" pattern.

**#83 — Mobile responsiveness pass**
- Week planner: added `@@media (max-width: 576px)` block in `<style>` (note double `@@` to escape Razor processing). Hides `day-icon` column, reduces font/padding on narrow screens.
- MealManagementPage: search bar changed from fixed `col-6/col-4/col-2` to `col-12 col-md-6/col-md-4/col-md-2` — stacks vertically on mobile.
- FamilyProfilePage: both tables wrapped in `<div class="table-responsive">` for horizontal scrolling; add-member and add-rule form rows use `col-12 col-sm-N` breakpoints.

**#81 — Undo "spist" button**
- Added `WeekMenuUnconsume = 23` to `ShoppingListKeysEnum.cs` and `"weekmenuunconsume": "api/weekmenu"` to `ISettings.cs`.
- For consumed day rows, added `↩ Angre` button alongside the ✅ checkmark.
- `UnconsumeDay` calls `PUT /weekmenu/{id}/unconsume` with `{ DayOfWeek }`, flips `meal.IsConsumed = false`, calls `StateHasChanged()` — re-enables the dropdown automatically since consumed check controls which branch renders.
- Pattern mirrors `ConsumeDay` exactly.



**#71 — Duplicate portion rule (`FamilyProfilePage.razor`)**
- Added 📋 duplicate button alongside existing delete button on each active PortionRule row.
- Clicking sets `_duplicatingRule = source` and opens an inline `table-warning` row immediately below, pre-filled with the source's ShopItem (display-only, no autocomplete), Quantity, and Unit. AgeGroup defaults to Adult so the user changes it.
- Save POSTs a new `PortionRuleModel` to the `PortionRules` endpoint; on success inserts the returned record at `sourceIndex + 1` in `_portionRules` so the list order reflects the duplicate position.
- Cancel sets `_duplicatingRule = null` — no API call.
- **Key pattern**: `_duplicatingRule == rule` reference equality inside the `@foreach` renders the inline row right after the matched source row. Same `table-warning` pattern as member edit from #69.
- **ShopItem locked on duplicate**: name displayed as `<strong>` text, not an autocomplete — the item is already resolved; only group + amount need changing.

### 2026-04-10 — Bugs #68, #69, #70 Fixed ✅ COMPLETE

**#68 — Inventory +/-1 buttons (`InventoryItemsPage.razor`)**
- Root cause: `Adjust()` sent `new { Id, Delta }` but API `inventoryitemsadjust` expects `List<InventoryAdjustmentModel>` with property `QuantityDelta` (not `Delta`).
- Fix: changed request to `new List<object> { new { Id = item.Id, QuantityDelta = delta } }` and added `StateHasChanged()` after local quantity update.

**#69 — Family member edit (`FamilyProfilePage.razor`)**
- Root cause: member table had only a delete button; no edit affordance.
- Fix: added `_editingMember` reference + 3 edit form fields. `StartEditMember()` copies current values; `SaveEditMember()` writes back and calls `UpdateProfile()` (PUT). Member rows now show pencil + delete; edit row highlights in `table-warning` with Name input, AgeGroup select, DietaryNotes input, save/cancel.
- Pattern: reference-based tracking (FamilyMemberModel has no Id/EntityBase).

**#70 — Ingredient edit (`OneMealRecipePage.razor`)**
- Root cause: ingredient rows were display-only for Quantity and Unit; only flag checkboxes were live-bound.
- Fix: added `_editingIngredient` reference + 5 edit fields. `StartEditIngredient()` snapshots current values; `SaveEditIngredient()` updates ingredient and (for existing recipes) immediately calls PUT on recipe endpoint. Pencil edit button added alongside delete button; edit row in `table-warning`.
- Pattern: same reference-based inline edit as #69. `IsNew` guard prevents spurious PUT on new recipes.

**General pattern reminder:** When a model doesn't inherit EntityBase (no `Id`), use object reference equality for edit tracking (`_editingX == item`), not string Id comparison.
`[AllowAnonymous]` in Blazor WASM requires `@using Microsoft.AspNetCore.Authorization` — **not** `Microsoft.AspNetCore.Components.Authorization` (which covers auth components). Both namespaces must be in `_Imports.razor` when using the attribute on a page. Mixing them up causes CS0246 build errors and a completely broken app.

- For SWA logout links, always use `post_logout_redirect_uri=/.auth/login/aad` (not `/`) — redirecting to `/` on a protected route causes a 401 that lands on the Blazor loading spinner. Pointing directly to the AAD login page bypasses Blazor entirely and gives a clean logout → login UX.

- `{request.path}` does NOT work in SWA `responseOverrides` — it is only substituted in `routes`. Using it in the 401 redirect caused post-login redirects to unresolvable paths, showing Blazor NotFound. Use plain `/.auth/login/aad` for 401 redirects; SWA handles the post-login redirect automatically.
- UI components live under `Client/Pages/Shopping/` and `Client/Shared/`.
- `ISettings` / `ShoppingListKeysEnum` pattern handles all API URL construction — never hardcode URLs.
- `EntityBase.CssComleteEditClassName` drives CSS state: `"edit"`, `"completed"`, or `""`.
- New shopping items insert at position 0 (top of list) — enforced in `OneShoppingListPage.razor` and `OneFrequentListPage.razor`.
- Admin functions sit behind a CSS-only dropdown in `NewNavComponent.razor`.
- Current goals: auth UI flows and client-side performance improvements.

- **SfAutoComplete pattern for ShopItemModel**: `TValue="string"`, `TItem="ShopItemModel"`, `DataSource="@_shopItems"`, `@bind-Value="@_newIngName"`, `FilterType="FilterType.Contains"`, `Highlight`. Field settings: `<AutoCompleteFieldSettings Value="Name" Text="Name" />`. Event handler: `Syncfusion.Blazor.DropDowns.ChangeEventArgs<string, ShopItemModel>`. Check `args.ItemData != null` to distinguish catalogue selection (use `args.ItemData.Id`) from free-form text (derive slug from `args.Value`). Always reset both the name field and the id field in form reset.
- **ShopItems loading pattern**: Start `Http.GetFromJsonAsync<List<ShopItemModel>>(Settings.GetApiUrl(ShoppingListKeysEnum.shopItems))` as a task at the top of `OnInitializedAsync` before any other awaits, so it runs in parallel with recipe loading. Await it after other tasks. Use `_shopItemsLoaded = true` flag to show spinner/disable autocomplete until ready. Enum key is `ShoppingListKeysEnum.shopItems` (lowercase s).

### 2026-03-27 — D7 Navigation Accessibility Fix ✅ COMPLETE
- **D7 (Admin Nav Accessibility):** ✅ IMPLEMENTED. Converted CSS `:hover`-only dropdown to Blazor `@onclick` toggle. Root cause: commit 30e7e83 moved "Hyppige Lister" into admin dropdown but no click handler was added. Desktop browsers could hover, but mobile/touch couldn't. Fix applied: Added `_adminOpen` bool state in `NewNavComponent.razor`. `@onclick="ToggleAdminMenu"` on trigger span. `@onclick:stopPropagation="true"` prevents nav collapse. Added `role="button"`, `aria-haspopup="true"`, `aria-expanded` attributes. `ToggleNavMenu` resets `_adminOpen` when navbar collapses. Extended `app.css` `.admin-dropdown:hover` selectors to also match `.admin-dropdown.open` class. Result: Both hover (desktop) and click-toggle (mobile/all) work. Fully accessible. **Frequent lists now visible on all devices.**
- **Secondary issue (Ray's domain):** `GoogleDbContext.GetCollectionKey()` silently maps `FrequentShoppingList` to `"misc"` collection. All existing production data lives in `"misc"`. D4 implementation includes migration strategy but must run migration endpoint before D4 merge to main, otherwise data becomes invisible.

### 2026-03-23 — Frequent Lists Regression Investigation
- **Root cause**: Commit `30e7e83` moved "Hyppige Lister" into the admin dropdown but the dropdown was CSS `:hover`-only — inaccessible on mobile/touch. D7 was not shipped alongside the nav refactor.
- **Fix applied**: `NewNavComponent.razor` converted to Blazor `@onclick` toggle (`_adminOpen` bool + `ToggleAdminMenu()`). `@onclick:stopPropagation="true"` prevents the nav collapse handler from firing on the trigger. `ToggleNavMenu` resets `_adminOpen` when navbar collapses. Added `aria-haspopup`/`aria-expanded`. `app.css` extended to `.admin-dropdown.open` so both hover and click-toggle work. **D7 is now ✅ done.**
- **Secondary bug (Ray's domain)**: `GoogleDbContext.GetCollectionKey()` returns `"misc"` for `FrequentShoppingList` — no dedicated Firestore collection. All production data for frequent lists lives in `"misc"`. D4 implementation must include a data migration before the correct collection key is set, otherwise existing data goes dark.
- **Key files**: `Client/Shared/NewNavComponent.razor`, `Client/wwwroot/css/app.css`, `Shared/Shared/Repository/GoogleDbContext.cs`

### 2026-03-22 — Full UI Audit (ui-findings.md)
- **Zero user feedback on async ops** — no toast system, no spinners, no error display. This is the #1 UX gap and blocks feeling trustworthy.
- **`OneShopManagementPage` is a stub** — only renders its heading. Shop delete button is also commented out. Two dead affordances in the Admin area.
- **Drag-and-drop is iOS-broken** — both `ShopConfigurationPage` and `CategoryManagementPage` use HTML5 drag events, which don't work on mobile Safari. Need touch-friendly fallback (up/down buttons).
- **Admin dropdown is hover-only** — no keyboard or touch support. Must be converted to a Blazor-toggled element with ARIA.
- **`@key` missing on all list `@foreach` loops** — Blazor can't diff efficiently; entire lists re-render on sort/reorder.
- **`OneShoppingListItemComponent` is unused** — items are rendered inline in the page. Component exists but isn't wired up.
- **CSS has 50+ `!important` overrides** — all fighting Syncfusion defaults. No CSS custom properties / design tokens.
- **Body font-size is 11px** — legacy holdover; every component overrides it. Baseline should be 16px.
- **No auth UI at all** — no login page, no session indicator, no `AuthorizeRouteView`. When auth is added, requires changes to `App.razor`, `MainLayout`, `NewNavComponent`, and all `HttpClient` calls.
- **Meal planning requires ~4 new pages + 4 shared components + nav entries + 4 enum keys** — full scope documented in `ui-findings.md` section 7.
- **`DataCacheService` + `BackgroundPreloadService` pattern is excellent** — preserve this; it makes the app feel fast.
- **`CategoryManagementPage` optimistic UI with rollback is best-in-class** — use as reference pattern for new pages.
- **`NaturalSortComparer` is correct and well-tested** — no changes needed.

### 2026-03-28 — Auth UI Implementation ✅ COMPLETE

**Decisions respected:** D1, D8, D14 (Microsoft provider ONLY via `aad` SWA path)

**Files created:**
- `Client/Auth/SwaAuthenticationStateProvider.cs` — Custom `AuthenticationStateProvider` that calls `/.auth/me` (SWA built-in endpoint). Returns unauthenticated state on error (graceful local dev handling).
- `Client/Shared/LoginDisplay.razor` — `<AuthorizeView>` component: shows `👤 username + Logg ut` link when authenticated; shows `Logg inn` link when not.

**Files modified:**
- `Client/App.razor` — `<RouteView>` → `<AuthorizeRouteView>` with `<NotAuthorized>` fallback card.
- `Client/Program.cs` — Added `using BlazorApp.Client.Auth`, `AddAuthorizationCore()`, `AddCascadingAuthenticationState()`, `AddScoped<AuthenticationStateProvider, SwaAuthenticationStateProvider>()`.
- `Client/Shared/NewNavComponent.razor` — Added `<div class="navbar-nav ml-auto nav-auth"><LoginDisplay /></div>` inside collapsible section.
- `Client/wwwroot/css/app.css` — Added `.nav-auth`, `.nav-user`, `.nav-login`, `.nav-logout` styles (pill-shaped, white on dark nav).

**Patterns used:**
- `AddCascadingAuthenticationState()` (.NET 8 service registration pattern — no wrapper component needed in App.razor)
- Login URL: `/.auth/login/aad` (Microsoft/Azure AD — D14 compliant, no GitHub)
- Logout URL: `/.auth/logout?post_logout_redirect_uri=/`
- Auth state is read-only from SWA; no token storage in client

**Note for integration:** Peter's `staticwebapp.config.json` gate + this client-side `<AuthorizeRouteView>` are two layers of defence. SWA gateway is the real enforcer; Blazor gives a nice "Ikke logget inn" fallback UX.



### 2026-03-28 — Auth FallbackPolicy Fix
- **Change:** `Client/Program.cs` — `AddAuthorizationCore()` expanded to set `options.FallbackPolicy = options.DefaultPolicy`. This makes ALL Blazor routes require authentication by default without needing `@attribute [Authorize]` on every page.
- **Why:** Previously, pages had no `[Authorize]` attribute and no fallback policy, so unauthenticated users in local dev (where `/.auth/me` is unavailable) could reach shopping list pages. The fallback policy closes this gap at the framework level.
- **No `/welcome` page exists** — `Client/Pages/Welcome.razor` does not exist. The SWA 401 redirect target in `staticwebapp.config.json` may point to `/welcome`, but no Blazor page for it was created (out of scope). Any genuinely public page added in future must carry `@attribute [AllowAnonymous]`.

**Broadcast by:** Peter (Lead) — Daniel Aase directive

**New branching strategy is in effect as of 2026-03-28:**
- `development` is now the base branch for ALL feature branches
- Cut new branches from `development`, not `main`
- Merging into `development` triggers a **staging** deployment (Azure SWA staging environment)
- Only `main` deploys to **production** — never push features directly to `main`
- PRs for feature work target `development`; only release PRs target `main`

**CI/CD updated:** `.github/workflows/azure-static-web-apps-purple-meadow-02a012403.yml` now has three separate jobs: production (main), staging (development), and PR previews.

### 2026-04-04 — Phase 1 Meal Planning Frontend ✅ COMPLETE

**Files created:**
- `Client/Pages/Meals/MealManagementPage.razor` — `/meals` list page: search, category filter, popularity display, edit/delete nav, `GetCategoryIcon()` helper
- `Client/Pages/Meals/OneMealRecipePage.razor` — `/meals/new` + `/meals/{Id}` editor: full form with all spec fields, ingredient table + add-ingredient form, POST/PUT/DELETE

**Files modified:**
- `Client/Common/ShoppingListKeysEnum.cs` — Added `MealRecipes=11`, `MealRecipe=12`, `WeekMenus=13`, `WeekMenu=14`, `InventoryItems=15`, `InventoryItem=16`
- `Client/Common/ISettings.cs` — Added 6 new URL mappings in the dictionary
- `Client/Shared/NewNavComponent.razor` — Added "Middager" link under Admin dropdown
- `Shared/Shared/HandlelisteModels/MealRecipeModel.cs` — (Already updated by Ray) Confirmed correct; no changes needed.

**Patterns followed:**
- `@using Shared` added per-page for `MealUnit` (lives in `Shared` namespace, not `Shared.HandlelisteModels`)
- `ISettings.GetApiUrl` / `GetApiUrlId` for all API calls — no hardcoded URLs
- String-backed enum bindings for `<select>` elements (Blazor-safe pattern)
- `LoadingComponent` while data null (matches ShoppingListMainPage pattern)
- New items appended at end of Ingredients list (ingredients are ordered by user intent, not auto-sorted)
- `LastModified = DateTime.UtcNow` set on Save

**Build status:** Client pages compile correctly. `Shared` project has pre-existing errors in `MemoryGenericRepository.cs` (Ray's domain): ambiguous `MealType`/`MealEffort` references (same enums in both Firestore and HandlelisteModels namespaces) + dummy data uses old `MealIngredient` properties (`Id`, `Name`, `ShopItem`, `StandardQuantity`) before Ray's model update. Client will build clean once Ray resolves those.

### 2026-04-07 — Phase 2 WeekMenu Pages ✅ COMPLETE

**Files created:**
- `Client/Pages/Meals/WeekMenuListPage.razor` — `/weekmenus` list page: ISO week auto-calc, active/inactive badge, days-planned count, sort by active→year→week
- `Client/Pages/Meals/OneWeekMenuPage.razor` — `/weekmenus/new` + `/weekmenus/{Id}`: Thursday-first 7-day planner, `<select>` per day (not SfAutoComplete), Friday KidsLike/pizza auto-suggestion, generate-shoppinglist inline preview, POST/PUT/navigate

**Files modified:**
- `Shared/Shared/HandlelisteModels/DailyMealModel.cs` — Added `MealRecipeName` property (was missing; needed for denormalized display)
- `Client/Shared/NewNavComponent.razor` — Added "Ukemeny" nav link under Admin dropdown (after "Middager")

**Patterns followed:**
- `ISettings.GetApiUrl` / `GetApiUrlId` for all API calls
- `@using Shared` per-page for `MealCategory` enum (not _Imports.razor)
- `select` with `@onchange` instead of SfAutoComplete for 7-row planner (perf)
- `LoadingComponent` while data loading
- `LastModified = DateTime.UtcNow` on save
- Generate shopping list inline card (no JS modal — no modal infrastructure in app)
- "Generer handleliste" button hidden until menu is saved (has Id)

**Build status:** ✅ 0 errors, warnings only (pre-existing)
**Integration:** Fully integrated with WeekMenuController API and 16 passing unit tests


### 2026-04-08 — Phase 3 Use-Up Suggestions + Phase 4 Inventory Page ✅ COMPLETE

**Files modified:**
- `Client/Pages/Meals/OneWeekMenuPage.razor` — Phase 3: added `UseUpSuggestion` record, `_useUpSuggestions` list, `_dismissedIngredientIds` set. `OnMealSelected` now calls `UpdateUseUpSuggestions()`. New methods: `UpdateUseUpSuggestions`, `AddToNextEmptySlot`, `DismissSuggestion`. Suggestion panel renders below the planner table, dismissable per ingredient. Added `.use-up-panel` CSS with amber left border.
- `Client/Shared/NewNavComponent.razor` — Added "Lager" nav link under Admin dropdown (after "Ukemeny")

**Files created:**
- `Client/Pages/Meals/InventoryItemsPage.razor` — `/inventory` admin page: loads inventory + recipes + shop items in parallel. Table with grouped/sorted rows, low-stock 🔴/🟢 status, inline edit on row click, +1/-1 adjust buttons (POST /api/inventoryitems/adjust), soft delete. Add-item form with SfAutoComplete (ShopItems catalogue). "🧊 Legg til frossen middag" shortcut creates item from recipe with `(frossen)` suffix and `SourceMealRecipeId`.

**Patterns followed:**
- `ShoppingListKeysEnum.InventoryItems`/`InventoryItem` — already present, confirmed
- `ISettings` URL mappings for `inventoryitem`/`inventoryitems` — already present, confirmed
- `MealUnit.ToNorwegian()` extension used for unit display
- SfAutoComplete pattern identical to `OneMealRecipePage`
- `LoadingComponent` while items null
- `LastModified = DateTime.UtcNow` on all mutations

**Use-up algorithm:**
- Runs purely client-side on `_recipes` list — no API calls
- Triggers on every `OnMealSelected` call
- Scans ingredients with `Quantity < 1.0` across all currently-selected recipes
- For each fractional ingredient, finds other active, non-selected recipes using the same `ShopItemId`
- Dismissed suggestions are tracked in `_dismissedIngredientIds` (session-scoped `HashSet`)

**Build status:** ✅ 0 errors, 79 warnings (all pre-existing)


**Phase 1 Meal Planning — Meal Pages Frontend ✅ COMPLETE**
- Created MealManagementPage.razor (/meals): list, search, category filter, popularity score, soft-delete with inactive label
- Created OneMealRecipePage.razor (/meals/{id}, /meals/new): create/edit form, ingredient management (autocomplete ShopItem, qty+unit, freshness flags)
- Updated ShoppingListKeysEnum: +6 values (MealRecipes, MealRecipe, WeekMenus, WeekMenu, InventoryItems, InventoryItem)
- Updated ISettings: +6 API URL mappings
- Updated NewNavComponent: added "Middager" link under Admin dropdown
- Decision: @using Shared per-page (not _Imports.razor) for MealUnit namespace safety
- Decision: String-backed enum bindings on <select> (not direct enum bind) for cross-namespace stability
- Decision: Ingredients append to end (not position 0) to preserve recipe order
- Decision: Soft-delete UX — list reflects IsActive=false, detail navigates away
- ✅ Pages compile clean, ready for integration


### 2026-04-09 — Phase 5 FamilyProfilePage + Portion Scaling ✅ COMPLETE

**Files created:**
- `Client/Pages/Meals/FamilyProfilePage.razor` — `/familyprofile` admin page: create/view/manage family profile (one household), member list with AgeGroup Norwegian labels (Voksen/Barn/Småbarn) + dietary notes, add/remove members (PUT profile on each change), portion rules table with SfAutoComplete (ShopItems), add rule form (AgeGroup + qty + MealUnit), delete rule per row. All loads in parallel on init.

**Files modified:**
- `Client/Common/ShoppingListKeysEnum.cs` — Added `FamilyProfiles=17`, `FamilyProfile=18`, `PortionRules=19`, `PortionRule=20`
- `Client/Common/ISettings.cs` — Added 4 new URL mappings: `familyprofile`, `familyprofiles`, `portionrule`, `portionrules`
- `Client/Shared/NewNavComponent.razor` — Added "Familieprofil" link under Admin dropdown (after "Lager")
- `Client/Pages/Meals/OneWeekMenuPage.razor` — Phase 5 portion scaling: `_familyProfile` + `_portionRules` loaded in parallel on init; `ApplyPortionScaling()` called after `GenerateShoppingList` response; `_scalingApplied` bool; "📐 Mengder tilpasset familieprofil" note shown in generated list preview when scaling was applied; silent skip when no profile loaded.

**Patterns followed:**
- SfAutoComplete pattern identical to `OneMealRecipePage` (TValue="string", TItem="ShopItemModel", FilterType.Contains)
- String-backed enum bindings for `<select>` (AgeGroup, MealUnit)
- `MealUnit.ToNorwegian()` extension used for unit display in both table and dropdown
- `ISettings.GetApiUrl` / `GetApiUrlId` for all API calls — no hardcoded URLs
- `LoadingComponent` while data loading
- `LastModified = DateTime.UtcNow` on all mutations
- Parallel task loading (shopItemsTask + portionRulesTask started before profile GET)
- Profile assumed single-household: `profiles?.FirstOrDefault()`
- Scaling silently skipped when no family profile or no portion rules

**Build status:** ✅ 0 errors, 33 warnings (all pre-existing)


### 2026-04-12 — Issues #73 and #74 ✅ COMPLETE

**#73 — Staple items group in generated shopping list (OneWeekMenuPage.razor)**
- The generated list preview now splits items into two groups:
  - **Normal items** (top): IsBasic == false && !IsLikelyNotNeeded
  - **Bottom group**: IsBasic == true || IsLikelyNotNeeded
- Bottom group label: "Basisvarer / Trolig ikke nødvendig" (covers both IsBasic and future #76 flag)
- Bottom group is collapsible via _staplesExpanded bool toggle (no Syncfusion, plain button)
- Each bottom-group item shows a 🗑 delete button (RemoveGeneratedItem() — mutates list in place)
- Added IsLikelyNotNeeded stub property to ShoppingListItemModel (Shared) for #76 — Glenn fills logic
- "Lagre som handleliste" button text simplified to "Lagre liste"

**#74 — Mark as consumed + swap (OneWeekMenuPage.razor)**
- Added IsConsumed to DailyMealModel (Shared) — serialises to/from Firestore and API
- Table rows gain two action cells: **Spist** (✅) and **Bytt** (🔄)
- Consumed rows get .consumed-day CSS (opacity 0.55) and "✅ Spist" label replaces dropdown
- Swap mode: clicking 🔄 toggles _swappingDay == day; the meal cell swaps to an inline <select> — selecting triggers OnSwapMealSelected() which calls PUT .../swap and updates local state
- ConsumeDay() calls PUT .../consume with { DayOfWeek, MealRecipeId }; on success sets meal.IsConsumed = true and clears swap state

**Enum / Settings additions**
- WeekMenuConsume = 21, WeekMenuSwap = 22 added to ShoppingListKeysEnum
- Dictionary entries in Settings: "weekmenuconsume" -> "api/weekmenu", "weekmenuswap" -> "api/weekmenu"
- In page, URLs constructed via: Settings.GetApiUrlId(WeekMenu, id) + "/consume" (same pattern as generate-shoppinglist)

**Key patterns re-confirmed:**
- Never use Syncfusion for simple toggles; plain <button> + bool state is sufficient
- DayOfWeek? nullable works cleanly as "which day is in swap mode" tracker
- Shared model stubs with default alse are safe for Firestore (missing field → false)
