# Project Context

- **Owner:** Daniel Aase
- **Project:** Shoppinglist — Blazor WebAssembly shopping list app with shop-specific item sorting
- **Stack:** Blazor WebAssembly (.NET 9), Syncfusion Blazor components, Azure Functions API, Google Cloud Firestore
- **Created:** 2026-03-22

## Core Context

### 2026-04-03 — Welcome page login flow: AllowAnonymous + RedirectToWelcome ✅ COMPLETE

**Pattern: Welcome page as auth entry point**
- `Landing.razor` at `/welcome` uses `@attribute [AllowAnonymous]` and `@layout LandingLayout`. This is the entry point for unauthenticated users.
- `RedirectToWelcome.razor` in `Client/Shared/` is a tiny utility component (no `@page`, no layout) that calls `Navigation.NavigateTo("/welcome")` in `OnInitialized`. Placed in `<NotAuthorized>` inside `App.razor`.
- `App.razor` `<NotAuthorized>` renders `<RedirectToWelcome />` — unauthenticated access to any route redirects to the welcome page.

**Infinite redirect guard pattern**
- The `[AllowAnonymous]` attribute on `Landing.razor` is the critical guard. Without it, `AuthorizeRouteView` would deny the welcome page itself → `<NotAuthorized>` → redirect to `/welcome` → loop.
- Both `Microsoft.AspNetCore.Components.Authorization` AND `Microsoft.AspNetCore.Authorization` must be in `_Imports.razor` — the first for auth components (`<AuthorizeView>`), the second for the `[AllowAnonymous]` attribute. Missing the second causes CS0246 build errors.
- Flow: `unauthenticated hit /anything` → `NotAuthorized` → `RedirectToWelcome` → `/welcome` (AllowAnonymous, renders) → user clicks login → AAD → redirect to `/` → `AuthorizeRouteView` → authenticated → shopping list. ✓

**staticwebapp.config.json**
- Add an explicit `{ "route": "/welcome", "allowedRoles": ["anonymous"] }` entry before the `/*` catch-all for clarity, even though `/*` already covers it. SWA evaluates routes top-to-bottom.

### 2026-04-02 — Login spinner still spinning after 5-second timeout fix ✅ COMPLETE

**Context:** The `/.auth/me` CancellationToken fix was in the code, but the app was still displaying the "Laster app..." spinner indefinitely in production.

**Suspects confirmed and fixed:**

- **CONFIRMED — Suspect 1 (root cause): Duplicate Syncfusion JS in `index.html`**
  - `<head>` loaded `_content/Syncfusion.Blazor.Core/scripts/syncfusion-blazor.min.js` (correct NuGet version)
  - `<body>` line 44 *also* loaded `https://cdn.syncfusion.com/blazor/20.4.38/syncfusion-blazor.min.js` (CDN, old version 20.4.38)
  - The CDN script overwrote the NuGet version with an older, incompatible one. This caused silent JS errors that prevented `blazor.webassembly.js` (loaded after) from booting correctly — the `#app` spinner never got replaced.
  - **Fix:** Removed the CDN `<script>` tag entirely. The commented-out duplicate of the NuGet version on the adjacent line was also removed. Only `_content/Syncfusion.Blazor.Core` remains.

- **CONFIRMED — Suspect 2: Missing `/*` anonymous catch-all in `staticwebapp.config.json`**
  - Routes array had explicit entries for known paths but no catch-all. In some SWA configurations, `/` itself (and deep-linked routes) could receive a 401, preventing Blazor from loading.
  - **Fix:** Added `{"route": "/*", "allowedRoles": ["anonymous"]}` as the final entry in the routes array (SWA evaluates routes top-to-bottom; last = lowest-priority catch-all).

- **CONFIRMED — Suspect 3: `MainLayout.OnInitializedAsync` blocking layout render**
  - `OnInitializedAsync` was `await`-ing `StartFastCorePreloadAsync()` (which itself awaits `PreloadCoreDataAsync()`) before returning. While Blazor renders before async completes, this meant the layout held its async path open for the full duration of all preload API calls — visually delaying `@Body` updates and making the page feel stuck.
  - **Fix:** Converted to synchronous fire-and-forget: `_ = Task.Run(async () => { ... })` + `return Task.CompletedTask`. Both preloads now run entirely in the background, never blocking layout render.

- **Logout redirect corrected:**
  - `NewNavComponent.razor` had `post_logout_redirect_uri=/` on the logout link. Per the 2026-03-29 learning, redirecting to `/` after logout lands on a protected Blazor route → auth check → spinner.
  - **Fix:** Changed to `post_logout_redirect_uri=/.auth/login/aad`. Logout now flows directly to the AAD login page, bypassing Blazor entirely.

**Key lesson:** The 5-second CancellationToken in `SwaAuthenticationStateProvider` was correct and not the issue. The spinner was caused by the CDN Syncfusion script conflicting with the NuGet version, silently preventing Blazor from initialising at all. The other three fixes are defence-in-depth.

### 2026-03-29 — Strip SWA-level auth, Blazor owns everything ✅ COMPLETE
- **Root cause of infinite spinner**: `/.auth/me` had no timeout — if SWA gateway was slow/unreachable, `GetAuthenticationStateAsync` hung forever and `<Authorizing>` never resolved. Fix: `CancellationTokenSource(TimeSpan.FromSeconds(5))` passed to `GetFromJsonAsync`.
- **SWA `/*` → `authenticated` + `responseOverrides.401 → /welcome` was a redirect loop**: SWA blocked the Blazor app's own assets, then redirected to `/welcome`, which triggered Blazor auth again. Removed both. SWA now serves everything anonymously; Blazor `FallbackPolicy = DefaultPolicy` is the single auth gate.
- **Logout redirect**: Changed `post_logout_redirect_uri` from `/welcome` (removed route) to `/`. With SWA open and Blazor FallbackPolicy active, landing on `/` unauthenticated correctly shows the `<NotAuthorized>` login page in `App.razor`.
- **Nav logout**: Replaced `<LoginDisplay />` component reference in `NewNavComponent.razor` with an inline `<AuthorizeView>` block showing username + logout link at the bottom of the nav `<ul>`. Cleaner and co-located with the nav structure.
- **Key insight**: Two auth systems (SWA gateway + Blazor middleware) fighting is a common Blazor-on-SWA pitfall. SWA auth is only useful for server-side route protection of APIs; for Blazor WASM the correct pattern is SWA fully anonymous + Blazor FallbackPolicy.

- `[AllowAnonymous]` in Blazor WASM requires `@using Microsoft.AspNetCore.Authorization` — **not** `Microsoft.AspNetCore.Components.Authorization` (which covers auth components). Both namespaces must be in `_Imports.razor` when using the attribute on a page. Mixing them up causes CS0246 build errors and a completely broken app.
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
`[AllowAnonymous]` in Blazor WASM requires `@using Microsoft.AspNetCore.Authorization` — **not** `Microsoft.AspNetCore.Components.Authorization` (which covers auth components). Both namespaces must be in `_Imports.razor` when using the attribute on a page. Mixing them up causes CS0246 build errors and a completely broken app.

- For SWA logout links, always use `post_logout_redirect_uri=/.auth/login/aad` (not `/`) — redirecting to `/` on a protected route causes a 401 that lands on the Blazor loading spinner. Pointing directly to the AAD login page bypasses Blazor entirely and gives a clean logout → login UX.

- `{request.path}` does NOT work in SWA `responseOverrides` — it is only substituted in `routes`. Using it in the 401 redirect caused post-login redirects to unresolvable paths, showing Blazor NotFound. Use plain `/.auth/login/aad` for 401 redirects; SWA handles the post-login redirect automatically.
- UI components live under `Client/Pages/Shopping/` and `Client/Shared/`.
- `ISettings` / `ShoppingListKeysEnum` pattern handles all API URL construction — never hardcode URLs.
- `EntityBase.CssComleteEditClassName` drives CSS state: `"edit"`, `"completed"`, or `""`.
- New shopping items insert at position 0 (top of list) — enforced in `OneShoppingListPage.razor` and `OneFrequentListPage.razor`.
- Admin functions sit behind a CSS-only dropdown in `NewNavComponent.razor`.
- Current goals: auth UI flows and client-side performance improvements.

- **`@media` in Blazor `<style>` blocks must be written `@@media`** — single `@` is parsed as a Blazor directive and causes `CS0103: The name 'media' does not exist` at compile time. This affects both `@media` and any other CSS at-rules using `@`.
- **`ChangeEventArgs` ambiguity with Syncfusion** — `Syncfusion.Blazor.Navigations` exports its own `ChangeEventArgs`. Any page that `@using` Syncfusion and uses `@onchange` on a native `<select>` must fully qualify: `Microsoft.AspNetCore.Components.ChangeEventArgs`. Otherwise CS0104 ambiguous reference error.
- **SfAutoComplete pattern for ShopItemModel**: `TValue="string"`, `TItem="ShopItemModel"`, `DataSource="@_shopItems"`, `@bind-Value="@_newIngName"`, `FilterType="FilterType.Contains"`, `Highlight`. Field settings: `<AutoCompleteFieldSettings Value="Name" Text="Name" />`. Event handler: `Syncfusion.Blazor.DropDowns.ChangeEventArgs<string, ShopItemModel>`. Check `args.ItemData != null` to distinguish catalogue selection (use `args.ItemData.Id`) from free-form text (derive slug from `args.Value`). Always reset both the name field and the id field in form reset.
- **ShopItems loading pattern**: Start `Http.GetFromJsonAsync<List<ShopItemModel>>(Settings.GetApiUrl(ShoppingListKeysEnum.shopItems))` as a task at the top of `OnInitializedAsync` before any other awaits, so it runs in parallel with recipe loading. Await it after other tasks. Use `_shopItemsLoaded = true` flag to show spinner/disable autocomplete until ready. Enum key is `ShoppingListKeysEnum.shopItems` (lowercase s).

### 2026-03-27 — D7 Navigation Accessibility Fix ✅ COMPLETE
- **D7 (Admin Nav Accessibility):** ✅ IMPLEMENTED. Converted CSS `:hover`-only dropdown to Blazor `@onclick` toggle. Root cause: commit 30e7e83 moved "Hyppige Lister" into admin dropdown but no click handler was added. Desktop browsers could hover, but mobile/touch couldn't. Fix applied: Added `_adminOpen` bool state in `NewNavComponent.razor`. `@onclick="ToggleAdminMenu"` on trigger span. `@onclick:stopPropagation="true"` prevents nav collapse. Added `role="button"`, `aria-haspopup="true"`, `aria-expanded` attributes. `ToggleNavMenu` resets `_adminOpen` when navbar collapses. Extended `app.css` `.admin-dropdown:hover` selectors to also match `.admin-dropdown.open` class. Result: Both hover (desktop) and click-toggle (mobile/all) work. Fully accessible. **Frequent lists now visible on all devices.**
- **Secondary issue (Ray's domain):** `GoogleDbContext.GetCollectionKey()` silently maps `FrequentShoppingList` to `"misc"` collection. All existing production data lives in `"misc"`. D4 implementation includes migration strategy but must run migration endpoint before D4 merge to main, otherwise data becomes invisible.

### 2026-03-23 — Frequent Lists Regression Investigation
- **Root cause**: Commit `30e7e83` moved "Hyppige Lister" into the admin dropdown but the dropdown was CSS `:hover`-only — inaccessible on mobile/touch. D7 was not shipped alongside the nav refactor.
- **Fix applied**: `NewNavComponent.razor` converted to Blazor `@onclick` toggle (`_adminOpen` bool + `ToggleAdminMenu()`). `@onclick:stopPropagation="true"` prevents the nav collapse handler from firing on the trigger. `ToggleNavMenu` resets `_adminOpen` when navbar collapses. Added `aria-haspopup`/`aria-expanded`. `app.css` extended to `.admin-dropdown.open` so both hover and click-toggle work. **D7 is now ✅ done.**
- **Secondary bug (Ray's domain)**: `GoogleDbContext.GetCollectionKey()` returns `"misc"` for `FrequentShoppingList` — no dedicated Firestore collection. All production data for frequent lists lives in `"misc"`. D4 implementation must include a data migration before the correct collection key is set, otherwise existing data goes dark.

### 2026-04-04 — Auth-aware nav and Index.razor branching ✅ COMPLETE

**Auth-aware nav pattern (`NewNavComponent.razor`)**
- Wrapped all nav links (Handlelister + Admin dropdown) and the existing logout block inside a single `<AuthorizeView><Authorized>` block.
- The pre-existing inner `<AuthorizeView>` around username/logout was removed and its content merged into the outer `<Authorized>` block — avoids double-wrapping.
- Added `<NotAuthorized>` block with a `🔒 Logg inn` link pointing to `/.auth/login/aad?post_login_redirect_uri=/`.
- Brand logo and hamburger toggler remain outside `<AuthorizeView>` — always visible.
- No CSS classes changed; all existing `@onclick` handlers preserved.

**Index.razor auth branching pattern**
- Wrapped `<ShoppingListMainPage>` in `<AuthorizeView><Authorized>`. Unauthenticated users get `<RedirectToWelcome />` in `<NotAuthorized>`.
- `RedirectToWelcome` is at `Client/Shared/RedirectToWelcome.razor` — tiny component that calls `Navigation.NavigateTo("/welcome")` in `OnInitialized`.
- `@code` block was empty — no data loading affected.
- No `[AllowAnonymous]` was present on Index.razor — nothing to remove.
- `Microsoft.AspNetCore.Components.Authorization` already in `_Imports.razor` — no import changes needed.
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

### 2026-03-28 — Staging Auth Loop Fix ✅ COMPLETE

**Root causes identified and fixed (3 issues):**

1. **`ClaimsIdentity` unauthenticated when `IdentityProvider` is null** — `ClaimsIdentity.IsAuthenticated` only returns `true` when `authenticationType` is non-null/non-empty. `principal.IdentityProvider` from SWA's `/.auth/me` can be null, causing FallbackPolicy to reject authenticated users. **Fix:** `principal.IdentityProvider ?? "aad"` in `SwaAuthenticationStateProvider.cs`.

2. **Missing `<Authorizing>` template caused blank screen** — While `/.auth/me` was in-flight, `AuthorizeRouteView` rendered nothing (Blazor replaced the index.html spinner, leaving a blank screen). Users saw it as "stuck loading". **Fix:** Added `<Authorizing>` spinner template in `App.razor`.

3. **401 redirect to `/welcome` (non-existent Blazor page)** — `staticwebapp.config.json` pointed 401 errors at `/welcome`, but no `Welcome.razor` page exists. After SWA served `index.html`, Blazor hit a dead route. **Fix:** Changed 401 redirect to `/.auth/login/aad` (direct AAD login — no Blazor page needed). Removed dead `/welcome` anonymous route entry.

**Key learning:** Never use `new ClaimsIdentity(claims, authenticationType)` without guaranteeing `authenticationType` is non-null — even if claims are populated, `IsAuthenticated` will be `false`. Always use a fallback: `provider ?? "aad"`.

**New branching strategy is in effect as of 2026-03-28:**
- `development` is now the base branch for ALL feature branches
- Cut new branches from `development`, not `main`
- Merging into `development` triggers a **staging** deployment (Azure SWA staging environment)
- Only `main` deploys to **production** — never push features directly to `main`
- PRs for feature work target `development`; only release PRs target `main`

**CI/CD updated:** `.github/workflows/azure-static-web-apps-purple-meadow-02a012403.yml` now has three separate jobs: production (main), staging (development), and PR previews.


## Learnings

### 2026-04-03 — Complete ManageMyShopsPage (#32) ✅ COMPLETE

**Completed `OneShopManagmentPage.razor` (`/managemyshops/{Id}`):**

- Replaced title stub with a full shop detail page.
- Inline name editing: click edit → input bound to `_editName`, Enter/Escape shortcuts, save fires PUT to `api/shops`.
- Shelf list: ordered by `SortIndex`, shows category badge tags, up/down reorder buttons (same pattern as `ShopConfigurationPage` from #27). "Lagre rekkefølge" button persists via PUT.
- Stats card: shelf count + total category count (computed properties `ShelfCount`/`CategoryCount`).
- Two-step delete: "Slett butikk" → dependency check → confirm/cancel panel (same pattern as #28 in ManageMyShopsPage). Navigates to `/managemyshops` after confirmed deletion via `NavigationManager`.
- Loading state via `<LoadingComponent />`, error state if API returns null/throws.
- Link to `/shopconfig/{id}` for full shelf/category configuration.
- Back navigation arrow to `/managemyshops`.
- Mobile-responsive with `@@media` at 576px (note: must use `@@media` not `@media` in Blazor `<style>` blocks).
- Fixed broken nav link in `ManageMyShopsPage.razor`: `GetItemNavLink` was returning `oneShop/{id}` (no matching route); corrected to `managemyshops/{id}`.
- **Key learning**: Always verify nav link targets against actual `@page` routes — the old stub had a routing mismatch that silently produced dead links.

### 2026-04-01 — Toast Notification System (#25)

**Implemented D5 (Option A): custom INotificationService + ToastContainer.**

- INotificationService interface with Success/Error/Warning/Info methods and OnToast event
- NotificationService implementation — event-driven, no dependencies on HttpClient or Blazor
- ToastContainer.razor — subscribes to OnToast, manages active toasts list, handles auto-dismiss with CSS leave animation, implements IDisposable to prevent memory leaks
- CSS in pp.css — fixed bottom-right position, slide-in/out keyframe animation, stacking via flex-column, mobile responsive (full-width ≤ 480px)
- Registered as AddScoped<INotificationService, NotificationService>() in Program.cs
- <ToastContainer /> added to MainLayout.razor outside the main layout div so it renders above all content
- Wired proof-of-concept in OneShoppingListPage.razor: item added (Success) and item deleted (Success/Error)

**Key pattern:** Use vent Action<ToastMessage> on the service + IDisposable on ToastContainer for clean subscribe/unsubscribe. Auto-dismiss with Task.Delay + CSS transition avoids Timer complexity.

**Unblocked:** #28 (shop deletion safeguards) can now proceed.

### 2026-04-02 — Shop Deletion Safeguards (#28) ✅ COMPLETE

**Implemented two-step delete flow in `ManageMyShopsPage.razor`:**

- Red `btn-danger` trash button appears next to each shop in non-edit mode — visually distinct from edit/config actions.
- First click → `InitiateDelete()`: calls `GET api/shop/{id}/dependencies`, sets `_deletingShopId`, shows inline `shop-delete-panel`.
- Panel shows a spinner while checking; then either "⚠️ Denne butikken brukes av X handleliste(r) for sortering." or the simpler "Er du sikker?" variant.
- Second click on red "Ja, slett butikken" → `ConfirmDelete()`: calls DELETE, removes from list, fires `INotificationService.Success("Butikk slettet")`.
- On any HTTP error or exception: `INotificationService.Error("Kunne ikke slette butikken")`.
- "Avbryt" button resets `_deletingShopId = null` → panel disappears.
- Dependencies endpoint is gracefully handled: if not yet deployed (404/error), `catch` sets `DependencyCount = 0` and the flow continues with the simple confirmation.
- `ShopDependencyResult` model declared as a private inner class in the page — no Shared model needed for this page-local DTO.
- URL constructed as `Settings.GetApiUrlId(ShoppingListKeysEnum.Shop, id) + "/dependencies"` — no new enum key required.
- CSS: `shop-delete-panel` with red left-border + fade-in animation added to `app.css`.

### 2026-04-03 — i18n Resource File Architecture (#30) ✅ COMPLETE

**Implemented D19 (i18n Architecture): `Microsoft.Extensions.Localization` infrastructure.**

**Package added:**
- `Microsoft.Extensions.Localization` 10.0.5 to `Client/Client.csproj`

**Files created:**
- `Client/Resources/SharedResources.cs` — empty marker class; carries full JSDoc comment with usage pattern and key naming convention
- `Client/Resources/SharedResources.nb-NO.resx` — 10 Norwegian strings (v1 default; all current hardcoded UI strings for ShoppingListMainPage)
- `Client/Resources/SharedResources.en.resx` — identical keys with `TODO` values; English translation template

**Files modified:**
- `Client/Program.cs` — added `using BlazorApp.Client.Resources` and `builder.Services.AddLocalization(opts => opts.ResourcesPath = "Resources")`
- `Client/Pages/Shopping/ShoppingListMainPage.razor` — converted to use `@inject IStringLocalizer<SharedResources> L`. All 3 visible hardcoded strings (`Handlelister`, `Navn på listen?`, `Det finnes ingen handlelister!`) replaced with `@L["key"]`. Added `aria-label` attributes to all 5 action buttons using localized strings (accessibility bonus).

**Squad docs:**
- `.squad/decisions/inbox/blair-i18n-pattern.md` — full convention doc: key naming, usage pattern, service registration, activation instructions, what NOT to localize

**Key learnings:**
- `AddLocalization()` in Blazor WASM works at the service level; `.resx` files are embedded resources resolved by `IStringLocalizer<T>` against the marker class namespace. ResourcesPath maps the physical folder.
- The culture must be set explicitly via `CultureInfo.DefaultThreadCurrentUICulture` for v1 (no browser auto-detect, no UI switcher — config flag only per D19).
- `using Microsoft.Extensions.Localization` must be in the `@using` block of each page that injects `L`; it is NOT in `_Imports.razor` to avoid polluting all pages until they are migrated.
- Key naming convention: `{PageOrScope}_{DescriptiveName}` — page prefix avoids collisions; `Common_` for shared strings.

### 2026-04-04 — Fix: Remove Category from Shelf (Issue #44) ✅ COMPLETE

**Root cause:** Firestore does NOT store document IDs inside embedded array maps by default. When a `Shop` document is loaded, the `ItemCategory` objects nested inside `Shelf.ItemCateogries` have `Id = null`. `GetUnusedCategories()` compares by `Id`, so the null IDs never match anything in `AvailableCategories` — resulting in categories that disappear after removal instead of appearing in the available list.

**Fix:** Added `NormalizeEmbeddedCategoryIds()` called right after `OnParametersSetAsync` loads both `CurrentShop` and `AvailableCategories`. It walks all embedded categories in every shelf; if `Id` is null/empty, it finds the matching entry in `AvailableCategories` by `Name` (case-insensitive) and backfills the ID. All downstream logic (`GetUnusedCategories()`, `AssignCategoryToShelf` duplicate-guard) stays unchanged because they already use ID comparison correctly — they just needed valid IDs.

**Key learnings:**
- **Firestore embedded array items never get a document ID** — only top-level documents get `DocumentSnapshot.Id`. For objects stored inside arrays, only `[FirestoreProperty]`-tagged fields are round-tripped. If data was saved before `Id` was reliably set, embedded items arrive with `Id = null`.
- **Normalize on load, not on use** — fixing IDs once in `OnParametersSetAsync` keeps all other methods clean. Matching by `Name` is safe here because category names are unique within the catalogue.
- **The bug also caused a silent duplicate** — `AssignCategoryToShelf` checked `c.Id == category.Id`; with null IDs the guard always passed, so assigning an already-embedded category would add a duplicate to the shelf. Normalization fixes this too.
- **File changed:** `Client/Pages/Shopping/ShopConfigurationPage.razor` — added 18-line `NormalizeEmbeddedCategoryIds()` method + one call-site in `OnParametersSetAsync`.

### 2026-04-04 — Active Filter Double-Check Bug Fix (#45) ✅ COMPLETE

**Root cause:** Two cooperating issues caused the "next item also gets checked" bug:
1. **No `@key` on `<li>` in the `@foreach` loop** — without a key, Blazor diffs by position. After `FilterList` removes the just-checked item from `ThisShoppingListItems`, the `<li>` at the original DOM index now holds a different item. The browser's already-captured change event fires on the recycled DOM node, marking the wrong item.
2. **`FilterList` called synchronously inside `VareCheckChanged`** — the list was rebuilt mid-render, before Blazor had a chance to process the current event cycle cleanly.

**Fix applied (`OneShoppingListPage.razor`):**
- Added `@key="vare.Id"` on the `<li>` element — Blazor now tracks items by identity, not position, so nodes are never recycled for different items.
- Added `await Task.Yield()` before `FilterList(activeListFiler)` — defers list rebuild to after the current render cycle, eliminating the race between DOM event rebinding and list reconstruction.
- Removed stale `Console.WriteLine("bvalue")` debug statement.

**Key rules for all future list renders:**
- **Always `@key` repeated elements** — `@foreach` without `@key` is a Blazor anti-pattern whenever the list can change order or size during events. Use the item's stable unique identifier (`Id` from `EntityBase`).
- **Never rebuild a rendered list synchronously inside an event handler that triggered from that list** — use `await Task.Yield()` to yield to the render cycle first.
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

