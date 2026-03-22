# Project Context

- **Owner:** Daniel Aase
- **Project:** Shoppinglist — Blazor WebAssembly shopping list app with shop-specific item sorting
- **Stack:** Blazor WebAssembly (.NET 9), Syncfusion Blazor components, Azure Functions API, Google Cloud Firestore
- **Created:** 2026-03-22

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
- UI components live under `Client/Pages/Shopping/` and `Client/Shared/`.
- `ISettings` / `ShoppingListKeysEnum` pattern handles all API URL construction — never hardcode URLs.
- `EntityBase.CssComleteEditClassName` drives CSS state: `"edit"`, `"completed"`, or `""`.
- New shopping items insert at position 0 (top of list) — enforced in `OneShoppingListPage.razor` and `OneFrequentListPage.razor`.
- Admin functions sit behind a CSS-only dropdown in `NewNavComponent.razor`.
- Current goals: auth UI flows and client-side performance improvements.

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
