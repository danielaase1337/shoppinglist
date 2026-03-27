# UI/UX Audit: Shopping List Blazor WebAssembly Application

**Agent:** Blair  
**Date:** 2025-07-09  
**Scope:** Full client-side audit of `Client/` — components, CSS, navigation, state, accessibility, performance  

---

## 1. Component Architecture

### Component Hierarchy

```
App.razor
└── MainLayout.razor                          (layout shell, triggers background preload)
    ├── NewNavComponent.razor                 (top navbar)
    └── @Body → routed page components
        ├── Index.razor                       (root redirect, /  )
        ├── Pages/Shopping/
        │   ├── ShoppingListMainPage.razor    (/shoppinglist)
        │   ├── OneShoppingListPage.razor     (/shoppinglist/{id})
        │   ├── FrequentListsPage.razor       (/frequent-lists)
        │   ├── OneFrequentListPage.razor     (/frequent-lists/{id})
        │   ├── ManageMyShopsPage.razor       (/managemyshops)
        │   ├── OneShopManagmentPage.razor    (/managemyshops/{id})  ← STUB
        │   ├── ShopConfigurationPage.razor   (/shopconfig/{id})
        │   ├── ItemManagementPage.razor      (/items)
        │   └── CategoryManagementPage.razor  (/categories)
        └── Shared/
            ├── LoadingComponent.razor        (3-dot animated spinner)
            ├── ConfirmDelete.razor           (modal — UNUSED in main flows)
            ├── ShoppingListComponents/
            │   ├── OneShoppingListItemComponent.razor  (list item row — PARTIALLY USED)
            │   └── ListSummaryFooter.razor             (filter bar + delete-all)
```

### Responsibilities & Data Flow

| Component | Data Source | State Owner | Notes |
|---|---|---|---|
| `MainLayout` | BackgroundPreloadService | Layout-level | Triggers parallel preload on every navigation |
| `ShoppingListMainPage` | DataCacheService | Page-local | Cache-first, then API fallback |
| `OneShoppingListPage` | DataCacheService + direct HTTP | Page-local | Most complex page; manages shop sort, frequent list import, modal |
| `FrequentListsPage` | DataCacheService | Page-local | List CRUD |
| `OneFrequentListPage` | DataCacheService + direct HTTP | Page-local | Item management with autocomplete |
| `ManageMyShopsPage` | DataCacheService + direct HTTP | Page-local | Shop CRUD; add button missing (commented out) |
| `ShopConfigurationPage` | Direct HTTP only | Page-local | Drag-and-drop shelf/category management; NOT using cache |
| `ItemManagementPage` | DataCacheService | Page-local | Inline edit, search/filter, JS confirm dialog |
| `CategoryManagementPage` | DataCacheService + direct HTTP | Page-local | Drag-and-drop category-item mapping |
| `ListSummaryFooter` | Props + EventCallbacks | Parent-owned | Stateful filter UI (allSelected/activeSelected/doneSelected) lives here |
| `OneShoppingListItemComponent` | `[Parameter]` + EventCallback | Parent-owned | Checkbox does NOT save; parent must detect change |
| `LoadingComponent` | None | None | CSS-only |
| `ConfirmDelete` | Props + EventCallback | Parent-controlled | Exists but only ItemManagementPage uses JS `confirm()` instead |

### Architecture Issues

- **`OneShopManagmentPage.razor`** — Dead stub page at `/managemyshops/{id}`. Renders `<h3>OneShopManagmentPage</h3>` and a broken event binding (`onclick="@AddItem"`). Navigating from `ManageMyShopsPage` via shop name link routes here, giving users a blank page.
- **`OneShoppingListItemComponent` is bypassed** — `OneShoppingListPage` renders its own full inline `<li>` markup instead of using `OneShoppingListItemComponent`, which exists but only used in no active page flow. The component's `VareCheckd` method does not persist changes; duplicating this logic inline in the page fixes the bug but creates drift.
- **`ConfirmDelete.razor` is never invoked** — Pages either skip confirmation (delete list, delete shop, frequent list item delete) or call `JSRuntime.InvokeAsync<bool>("confirm", ...)` directly (ItemManagementPage). The reusable component is dead code.
- **`MainLayout` blocks render** — `OnInitializedAsync` `await`s both `StartFastCorePreloadAsync()` and `StartPreloadingAsync()`. `StartFastCorePreloadAsync` is a full parallel API fetch; this means the layout does not render until all 4 API calls complete on cold start.

---

## 2. Syncfusion Integration

### Components Used

| Syncfusion Component | Location | Purpose |
|---|---|---|
| `SfDialog` | `OneShoppingListPage` (line 10–51) | "Add item" modal with category autocomplete |
| `SfTextBox` | `OneShoppingListPage` (line 17–18) | Item name input inside modal |
| `SfAutoComplete<string, ShopItemModel>` | `OneShoppingListPage` (line 115–128) | Primary item-add input |
| `SfAutoComplete<string, ShopItemModel>` | `OneFrequentListPage` (line 21–33) | Item-add for frequent list |
| `SfDropDownList<string, ShopModel>` | `OneShoppingListPage` (line 77–88) | Shop-sort selector |
| `SfDropDownList<string, FrequentShoppingListModel>` | `OneShoppingListPage` (line 93–103) | Import-from-frequent-list selector |

### Binding Patterns

- All Syncfusion inputs use **`ValueChange` event** (not `@bind-Value` + `@onchange`) to react to selection.
- `SfDropDownList` uses `@bind-Index` (integer index) for shop selection, accessed later via `AvailableShops.ElementAt(shopIndex.Value)` — **fragile**: index-based access breaks if `AvailableShops` is an unordered `ICollection` returned in different order after cache expiry.
- `SfAutoComplete` correctly uses `FilterType.Contains` and `Highlight` for all inputs.
- All Syncfusion components are globally registered via `AddSyncfusionBlazor()` in `Program.cs` and imported globally in `_Imports.razor`.

### Issues & Potential Improvements

1. **`SfDialog` is conditionally rendered** (`@if (newVareModel != null)`) — the dialog is destroyed and recreated when `newVareModel` is set/cleared. This works but loses dialog animation state and may cause Syncfusion to re-initialise its JS interop unnecessarily. Prefer `@bind-Visible` with a persistent dialog and empty form reset.

2. **`SfTextBox` without `@bind-Value` validation** — The `SfTextBox` inside the modal uses `@bind-Value=@newVareModel.Name` but there is no validation indicator shown for empty name. The save button silently returns if `!newVareModel.IsValid()`.

3. **Shop sort uses index instead of ID** — `@bind-Index="@shopIndex"` is the Syncfusion DropDownList index binding. The event handler `OnShopSelectionChanged` reads `args.ItemData` but the actual sort logic uses `AvailableShops.ElementAt(shopIndex.Value)`. `SelectedShop` is also separately tracked but `shopIndex` drives the sort. These two references should be unified.

4. **Add button on primary autocomplete has no `@onclick` handler** — `OneShoppingListPage` line 131:
   ```razor
   <button class="btn btn-add-item btn-lg" title="Legg til vare">
       <i class="fas fa-plus"></i>
   </button>
   ```
   This button does nothing. Items are only added via `ValueChange` on the autocomplete. The button is visually prominent but non-functional, which is confusing UX.

5. **Syncfusion license key is hardcoded** in `Program.cs` line 12 — not a UX issue but a security concern.

6. **No loading/disabled states on Syncfusion inputs** during API calls — while an item is being added via `AddVare()`, the autocomplete remains active and could fire another `ValueChange` event, queuing duplicate items.

---

## 3. CSS & Styling

### CSS File Inventory

| File | Size / Purpose |
|---|---|
| `wwwroot/css/app.css` | ~750 lines — global styles, all custom classes |
| `Shared/MainLayout.razor.css` | 82 lines — page layout, sidebar, responsive |
| `Shared/NewNavComponent.razor.css` | Empty (1 line) — no scoped nav styles |
| `Shared/LoadingComponent.razor.css` | 45 lines — overlay and dot animation |
| `Pages/Shopping/ItemManagementPage.razor` | Inline `<style>` block ~70 lines |
| `Pages/Shopping/CategoryManagementPage.razor` | Inline `<style>` block ~100+ lines |
| `Pages/Shopping/ShopConfigurationPage.razor` | Inline `<style>` block (inferred from page size) |
| `wwwroot/css/Oldapp.css` | Legacy file, unused |

### CSS Architecture Issues

1. **Inline `<style>` blocks in page components** — `ItemManagementPage`, `CategoryManagementPage`, and `ShopConfigurationPage` each embed `<style>` tags directly in the `.razor` file. These are injected into `<head>` globally (Blazor WASM does not scope inline styles). Class names like `.card-item`, `.editing`, `.items-container` defined in `ItemManagementPage.razor` can conflict with identically-named classes in other pages or `app.css`.

2. **`.editing` class defined twice** — `app.css` defines `.todo-list li.editing` (line-item context). `ItemManagementPage` redefines `.editing` as a blue-border input class. These overlap in context when both are loaded.

3. **`body` font declared twice** — `app.css` has two `body` rules: one at the top with system-ui stack (line ~3) and one further down with `'Helvetica Neue'` at `11px font-size` (legacy TodoMVC origin). The second rule overrides the first, resulting in `11px` body font — very small.

4. **`CssComleteEditClassName` typo baked into the model** — `EntityBase.cs` and `ShoppingListBaseModel.cs` expose `CssComleteEditClassName` (missing 'p'). This typo propagates to every `class="@item.CssComleteEditClassName"` in all list-rendering pages. Fixing requires a coordinated rename.

5. **`child-horizontal-stack` selector missing `.`** — `app.css` defines `child-horizontal-stack { width: 100px; }` (no dot prefix) — this targets an HTML element named `child-horizontal-stack`, which does not exist. Dead rule.

6. **`.editButtonStyle` defined twice** — Declared at line ~52 and again at line ~130 in `app.css` with different values. Second declaration wins. Redundant.

7. **`MainLayout.razor.css` contains `.sidebar` styles** — The layout uses a horizontal navbar (`NewNavComponent`), not a sidebar. The sidebar gradient (`linear-gradient(180deg, rgb(5, 39, 103) 0%, #3a0647 70%)`) and 250px sidebar styles are orphaned, from the default Blazor template.

8. **Bootstrap partial dependency** — `bootstrap.min.css` is referenced and bootstrap grid classes (`row`, `col`, `col-2`, `card`, `btn-group`, `btn-sm`) are used throughout. However, `app.css` redefines many of these (`.row`, `.card`, `.btn-*`), causing unpredictable override order depending on load sequence.

9. **`!important` overuse in Syncfusion overrides** — The Syncfusion override section in `app.css` (lines ~370–540) uses `!important` on virtually every property. This makes future theming or component updates very difficult.

### Color & Visual Design

- Background: `linear-gradient(135deg, #667eea 0%, #764ba2 100%)` — good visual identity.
- Cards use `backdrop-filter: blur(10px)` — beautiful but unsupported in Firefox < 103 and older Safari without prefix.
- Card hover `transform: translateY(-2px)` on shopping list cards makes entire list items feel clickable even when they're not — could confuse users.

---

## 4. Navigation UX

### Nav Structure

```
Navbar (NewNavComponent.razor)
├── Brand: "The Aase-broen's" → href=""  (home, no active route)
├── NavLink: "Handlelister" → /shoppinglist
└── Admin dropdown (CSS hover-only)
    ├── "Hyppige Lister" → /frequent-lists
    ├── "Håndter butikker" → /managemyshops
    ├── "Administrer varer" → /items
    └── "Administrer kategorier" → /categories
```

### Navigation Issues

1. **CSS-only dropdown** — The Admin dropdown (`admin-dropdown-menu`) uses `opacity:0; visibility:hidden` → `opacity:1; visibility:visible` on `:hover`. This means:
   - **Not keyboard accessible** — no `tabindex`, no `:focus-within` trigger. Users navigating by keyboard cannot open the dropdown at all.
   - **Not touch-friendly** — hover state doesn't trigger on mobile. Admin items are completely unreachable on touchscreens.
   - The `aria-expanded="false"` attribute on the mobile toggler button is never updated (`collapseNavMenu` toggles the CSS class but the attribute stays `false`).

2. **Mobile hamburger menu closes on any click inside** — The `@onclick="ToggleNavMenu"` is on the outer collapse `<div>`, meaning clicking any NavLink or dropdown item inside the open menu also triggers `ToggleNavMenu`, which works but is implicitly fragile.

3. **NavLink vs `<a>` inconsistency** — Top-level "Handlelister" uses `<NavLink>` (gets `active` class). Admin items use `<NavLink class="dropdown-item">` (also correct). Brand link uses `<a>` without NavLink — minor but inconsistent.

4. **No active state indicator for current section** — The Admin dropdown trigger (`<span class="nav-link dropdown-trigger">`) is not a `NavLink`, so no active CSS class is applied when on admin pages. Users cannot tell which section they're in from the navbar.

5. **`OneShopManagmentPage` as broken navigation target** — Clicking a shop name in `ManageMyShopsPage` navigates to `/managemyshops/{id}` which renders the stub page. This is a dead-end in the navigation flow. Users are given no feedback. They should be redirected to `/shopconfig/{id}`.

6. **No back-navigation or breadcrumbs** — Detail pages (`/shoppinglist/{id}`, `/frequent-lists/{id}`, `/shopconfig/{id}`) have no back button or breadcrumb. Users must use browser back button or re-navigate via the navbar.

7. **App root `/` has no redirect** — `Index.razor` is at `/` but its content is unknown (file not examined due to small size). If it doesn't redirect to `/shoppinglist`, the landing page may be empty.

---

## 5. State Management

### Patterns Used

**DataCacheService** (Scoped singleton per browser session):
- In-memory TTL cache (5 min for lists, 10 min for detail objects)
- All pages call `DataCache.GetXxxAsync()` — cache-first, API fallback
- Pages call `DataCache.InvalidateXxxCache()` after mutations and re-fetch

**Direct HttpClient** (bypassing cache):
- `ShopConfigurationPage` uses `Http.GetFromJsonAsync<>()` directly — no caching, no invalidation
- `OneShoppingListPage.UpdateShoppinglist()` uses `Http.PutAsJsonAsync()` directly — invalidates shopping list cache manually

**EventCallbacks**:
- `ListSummaryFooter` → parent via `FilterList` and `DeleteAllCompletedClikced` (typo in event name)
- `OneShoppingListItemComponent` → parent via `DeleteVareCallback`

### Anti-Patterns & Issues

1. **`async void` in event handler** — `FrequentListsPage.OnKeyUpNewListHandler` is declared `async void` (line 173):
   ```csharp
   private async void OnKeyUpNewListHandler(KeyboardEventArgs e)
   ```
   `async void` swallows exceptions silently and cannot be awaited. Should be `async Task`.

2. **`MainLayout.OnInitializedAsync` blocks layout render** — `await BackgroundPreload.StartFastCorePreloadAsync()` fetches 4 parallel API endpoints. Until these complete, `@Body` does not render. On slow connections this is a significant cold-start delay with no progress indicator visible (loading screen is inside `Body`).

3. **Cache invalidation is one-way** — When `ShopConfigurationPage` saves shelf/category changes via direct `Http.PutAsJsonAsync`, it does NOT call `DataCache.InvalidateShopsCache()`. Other pages (e.g., `OneShoppingListPage`'s shop sort dropdown) will show stale shop data until the 5-minute TTL expires.

4. **Shopping list items mutate shared cached object** — `OneShoppingListPage.SortShoppingList()` does `item.Varen.ItemCategory.SortIndex = sortIndex` in-place on objects from the cache. This mutates the cached `ShopItemModel` objects globally, potentially affecting other pages that hold the same cached item references.

5. **`VareMengdeFocusLost()` saves entire list on blur** — Every time a quantity input loses focus, the entire shopping list is PUT to the API. This fires even if nothing changed, creating unnecessary network traffic.

6. **`DeleteAllClicked()` calls individual DELETE per item** — Iterates `completed` items and calls `Http.DeleteAsync()` per item sequentially inside a loop (no `Task.WhenAll`). For lists with many completed items this is slow.

7. **`ListSummaryFooter` filter state is local** — The filter state (`allSelected`, `activeSelected`, `doneSelected`) lives inside `ListSummaryFooter`. If the parent re-renders (e.g., after `StateHasChanged()`), the child re-renders but does NOT reset filter state — visual filter buttons may mismatch the actual filtered list displayed by the parent.

8. **`CancelEdit(item)` in `ItemManagementPage` uses single shared backup** — `originalItemName`, `originalItemUnit`, `originalItemCategory` are single fields. If two items enter edit mode simultaneously (technically possible via rapid clicks before `StateHasChanged`), editing the second item overwrites the first item's backup.

---

## 6. User Workflows

### Workflow: Create a Shopping List

1. Navigate to `/shoppinglist` (ShoppingListMainPage)
2. Type name in `<input class="new-todo">` → press Enter or click ✓ button
3. List appears at top (sorted: active first, newest first)

**UX Issues:**
- After adding, `_newListName` is cleared but the input has no focus restoration. Users must click back into the input.
- No inline validation — empty name silently does nothing (returns early, no error message).
- The ✓ button is styled as `btn-outline-link` but the click handler is on an `<i>` inside: `<i class="fas fa-check" @onclick="@(f => AddList())">`. The entire button area is not clickable — only the icon pixel area fires.

### Workflow: Add Items to a Shopping List

1. Navigate to `/shoppinglist/{id}`
2. Items-add section is **collapsed by default** — user must click the `fa-chevron-down` toggle button to reveal shop-sort and import dropdowns
3. Primary autocomplete (`SfAutoComplete`) is always visible at bottom of card header
4. Type item name → select from dropdown → item added automatically via `ValueChange`
5. The `+` button next to the autocomplete **does nothing** (no `@onclick`)

**UX Issues:**
- The hidden "Legg til vare" dialog (`SfDialog`) is only accessible by clicking on an existing item's name. This is completely undiscoverable — there is no label, tooltip, or instruction pointing users to this interaction.
- Items are added via `ValueChange` automatically, but after adding, `autoValue` is set to `null` (not `string.Empty`). This may leave the Syncfusion autocomplete in an inconsistent state.
- New items are inserted at index 0 (top of list), which is good for immediate visibility.
- No success/failure feedback to the user after adding an item.

### Workflow: Sort by Shop

1. In `OneShoppingListPage`, expand the collapsed section (chevron toggle)
2. Select a shop from `SfDropDownList` ("Velg butikk for sortering")
3. `OnShopSelectionChanged` fires → `SortShoppingList()` runs

**UX Issues:**
- Sort happens automatically on shop selection — no explicit "Sort" button, which is good. But there is a commented `// TODO - lag notifikasjon til bruker om at butikk må velges først` (line 562) when no shop is selected. This never happens because selection fires immediately.
- If an item's category has no match in the selected shop's shelves, it silently stays at sort index 0 (top). Users see these items but have no indication they're unsorted.
- After sort, `FilterList(activeListFiler)` is called, which is correct.
- Shop sort mutates `ItemCategory.SortIndex` on the shared model objects — **not undoable**.

### Workflow: Mark Items Done

- Checkbox `@onchange` calls `VareCheckChanged` → sets `vare.IsDone` → `FilterList()` → `UpdateShoppinglist()`
- Completed items get CSS class `completed` (strikethrough via `CssComleteEditClassName`)
- `ListSummaryFooter` shows remaining count and filter buttons

**UX Issues:**
- No optimistic UI — the list does not visually update until after the API PUT completes.
- Actually, `FilterList(activeListFiler)` is called before `UpdateShoppinglist`, so the item moves out of the "active" filter view immediately. This is good for perceived performance but the API call could fail silently.
- No undo for checking off items.

### Workflow: Manage Frequent Lists

1. `/frequent-lists` → create/edit/delete list names
2. `/frequent-lists/{id}` → add items (autocomplete), set standard quantity (number input, saves on `onfocusout`), delete items
3. Back in `/shoppinglist/{id}` → "Importer fra hyppig liste" → select list → click import button

**UX Issues:**
- Import button (`disabled="@(frequentListIndex == null || frequentListIndex < 0)"`) is disabled until a list is selected. But the condition checks `frequentListIndex < 0` while the initial value is `null` — the disabled logic is correct, but could be simplified.
- Duplicate items during import update existing item quantity (`existingItem.Mengde += frequentItem.StandardMengde`) — this is good but there is no user-visible message about what was added vs updated.
- `SaveListChanges()` in `OneFrequentListPage` is called on every quantity input blur (`@onfocusout="@SaveListChanges"`). Each individual blur fires a full PUT.

---

## 7. Accessibility

### Critical Issues (WCAG 2.1 Level A/AA Failures)

1. **Keyboard-inaccessible Admin dropdown** (WCAG 2.1.1, 4.1.2)
   - `NewNavComponent.razor` lines 21–39: The admin dropdown menu uses CSS `:hover` only. No `:focus-within`, no `button` trigger element, no `aria-expanded`, no keyboard event handler. The dropdown and all 4 admin pages are unreachable by keyboard.

2. **Icon-only buttons without accessible labels** (WCAG 1.3.1, 4.1.2)
   - Nearly every action button uses only a Font Awesome icon with no text label, `aria-label`, or `title`:
     - Edit button: `<button @onclick="..."><i class="fas fa-edit"></i></button>` — no label
     - Delete button: `<button @onclick="..."><i class="fas fa-times"></i></button>` — no label
     - Checkbox items are unlabeled — `<input type="checkbox" checked="@vare.IsDone" @onchange="...">` with no `<label>` association
   - Exception: Some buttons have `title` attributes (e.g., `ShopConfigurationPage`, `OneShoppingListPage` chevron button)

3. **`aria-expanded` not updated on hamburger toggle** (WCAG 4.1.2)
   - `NewNavComponent.razor` line 10: `aria-expanded="false"` is a static attribute. `ToggleNavMenu()` toggles `collapseNavMenu` (CSS class) but never updates `aria-expanded`. Screen readers will always announce "collapsed".

4. **`ConfirmDelete` modal lacks focus management** (WCAG 2.4.3)
   - When the modal shows (`display: block`), focus is not moved into the modal. Users can tab past it. No `aria-modal="true"`, no focus trap.

5. **`LoadingComponent` is not announced** (WCAG 4.1.3)
   - Loading spinner has no `role="status"`, no `aria-label`, and no `aria-live` region. Screen readers have no way to announce that content is loading.

6. **List filter links have no accessible state** (WCAG 4.1.2)
   - `ListSummaryFooter` filter `<a>` tags get class `selected` but no `aria-current="page"` or `aria-pressed` equivalent.

### Moderate Issues (WCAG AA)

7. **Color contrast — small body text at 11px** — Due to the double `body` rule in `app.css`, body font-size is `11px`. WCAG SC 1.4.3 requires 4.5:1 contrast for small text. At 11px, text is below the 14pt (18.67px) normal-text threshold, requiring high contrast ratios that must be verified against the purple gradient background.

8. **Drag-and-drop only UI** — `ShopConfigurationPage` and `CategoryManagementPage` both use drag-and-drop as the primary (only) mechanism for reordering shelves/assigning categories. No keyboard-accessible alternative exists (WCAG 2.1.1).

9. **Double-click to edit** — Multiple pages (`OneShoppingListPage`, `OneFrequentListPage`, `ShopConfigurationPage`, `ItemManagementPage`, `CategoryManagementPage`) use `@ondblclick` to enter edit mode. This is not keyboard accessible and not intuitive on mobile.

---

## 8. Performance

### Rendering Bottlenecks

1. **`GetFilteredItems()` called multiple times per render** — `ItemManagementPage` calls `GetFilteredItems()` three times in the template:
   ```razor
   <span class="badge badge-info">@GetFilteredItems().Count() varer</span>
   ...
   @if (GetFilteredItems().Any())
   ...
   @foreach (var item in GetFilteredItems().OrderBy(i => i.Name))
   ```
   Each call recomputes the LINQ filter from scratch. With large item lists this is O(3n) per render. Should be computed once and stored in a field, recomputed only on `searchText` or `filterCategoryId` change.

2. **`GetItemsInCategory()` called per render in `CategoryManagementPage`** — Every render of the categories list calls `GetItemsInCategory(category.Id)` for the item count badge, the items grid, and the empty-zone check per category. For C categories with I items each, this is O(C × I) per render cycle.

3. **`GetUnusedCategories()` repeated per shelf in `ShopConfigurationPage`** — The method is called inside the `@foreach (var shelf in ...)` loop, recomputing the "unused categories" set for every shelf row on every render. Should be computed once.

4. **`MainLayout.OnInitializedAsync` is `await`ed** — Blocks initial render until 4 parallel API calls complete. The app shows nothing (not even the navbar) until this resolves. The correct pattern is fire-and-forget with `StateHasChanged()` after completion:
   ```csharp
   // Anti-pattern (current):
   await BackgroundPreload.StartFastCorePreloadAsync();
   
   // Better pattern:
   _ = BackgroundPreload.StartFastCorePreloadAsync().ContinueWith(_ => InvokeAsync(StateHasChanged));
   ```

5. **`card:hover` transform on every list item** — `app.css` applies `transform: translateY(-2px)` on every `.card:hover`. Every shopping list page wraps items in `.card` containers, triggering GPU compositing on hover for the entire card area, not just interactive elements.

6. **`SortShoppingList()` mutates global item category objects** — After shop-sort, the cached `ShopItemModel` objects have their `ItemCategory.SortIndex` permanently changed in memory. This is not reset when navigating away. If the user visits another shopping list in the same session, items will appear pre-sorted by the previously selected shop's layout.

7. **`DeleteAllClicked()` is sequential** — Deletes completed items one by one:
   ```csharp
   foreach (var item in completed)
   {
       var url = await Http.DeleteAsync(...);  // sequential
       ThisShoppingList.ShoppingItems.Remove(item);
   }
   ```
   Should use `Task.WhenAll(completed.Select(item => Http.DeleteAsync(...)))`.

8. **Background preload races with `OnParametersSetAsync`** — `BackgroundPreloadService.StartPreloadingAsync()` fires after a 2-second delay (`await Task.Delay(2000)`). If a user navigates to `/shoppinglist/{id}` before 2 seconds, the page's `OnParametersSetAsync` runs concurrently with background preloading of all active lists. Both hit the same `DataCacheService` simultaneously — the service has no locking/semaphore, so concurrent writes to `_cachedShoppingListDetails` dictionary are not thread-safe (though WebAssembly is single-threaded, so actual data corruption is unlikely but race conditions in cache population exist).

---

## Summary & Prioritised Recommendations

### Priority 1 — Critical Bugs

| # | Issue | File | Fix |
|---|---|---|---|
| P1-1 | Add item `+` button has no `@onclick` | `OneShoppingListPage.razor` line 131 | Add `@onclick="AddCurrentItem"` handler or route through `SfAutoComplete` only |
| P1-2 | `OneShopManagmentPage` is a dead stub | `OneShopManagmentPage.razor` | Redirect `/managemyshops/{id}` to `/shopconfig/{id}` or implement the page |
| P1-3 | `async void` event handler | `FrequentListsPage.razor` line 173 | Change to `async Task` |
| P1-4 | `ShopConfigurationPage` saves without cache invalidation | `ShopConfigurationPage.razor` | Call `DataCache.InvalidateShopsCache()` after PUT |
| P1-5 | Sort mutates cached item objects | `OneShoppingListPage.razor` `SortShoppingList()` | Apply sort indices to a local copy, not global cache objects |

### Priority 2 — UX Blockers

| # | Issue | File | Fix |
|---|---|---|---|
| P2-1 | Admin dropdown keyboard/mobile inaccessible | `NewNavComponent.razor` | Replace with `<button>` trigger + `@onclick` toggle + `:focus-within` CSS |
| P2-2 | Double-click-to-edit undiscoverable | Multiple pages | Add visible edit button or tooltip "double-click to rename" |
| P2-3 | No feedback after add/save/delete operations | Multiple pages | Add toast notifications or inline status messages |
| P2-4 | `MainLayout` blocks render on cold start | `MainLayout.razor` | Fire preload without `await`, show spinner until ready |
| P2-5 | `ConfirmDelete.razor` is dead code | Multiple pages | Either wire it up consistently or use JS confirm (current inconsistency) |

### Priority 3 — Accessibility

| # | Issue | Files | Fix |
|---|---|---|---|
| P3-1 | Icon-only buttons have no labels | All pages | Add `aria-label` to every icon-only button |
| P3-2 | Checkboxes unassociated with labels | All list pages | Wrap in `<label>` or add `aria-label` |
| P3-3 | `aria-expanded` not updated on nav toggle | `NewNavComponent.razor` | Bind: `aria-expanded="@(!collapseNavMenu)"` |
| P3-4 | LoadingComponent not announced | `LoadingComponent.razor` | Add `role="status" aria-label="Laster..."` |
| P3-5 | Drag-and-drop without keyboard fallback | `ShopConfigurationPage`, `CategoryManagementPage` | Add up/down arrow buttons for reorder |
| P3-6 | Filter links lack `aria-current` | `ListSummaryFooter.razor` | Add `aria-current="true"` to selected filter |

### Priority 4 — CSS & Code Quality

| # | Issue | Fix |
|---|---|---|
| P4-1 | Double `body` font-size rule (11px override) | Remove/consolidate duplicate body rules in `app.css` |
| P4-2 | Inline `<style>` blocks in pages | Extract to `PageName.razor.css` scoped files |
| P4-3 | `CssComleteEditClassName` typo | Rename across model + all pages (single refactor) |
| P4-4 | Orphaned sidebar styles in `MainLayout.razor.css` | Remove `.sidebar` styles not used in current layout |
| P4-5 | `GetFilteredItems()` called 3× per render | Compute once per render cycle, cache in field |
| P4-6 | `DeleteAllClicked()` sequential deletes | Use `Task.WhenAll()` for parallel deletion |
| P4-7 | `VareMengdeFocusLost()` saves on every blur | Debounce or only save if value changed |
