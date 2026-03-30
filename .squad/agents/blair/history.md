# Project Context

- **Owner:** Daniel Aase
- **Project:** Shoppinglist — Blazor WebAssembly shopping list app with shop-specific item sorting
- **Stack:** Blazor WebAssembly (.NET 9), Syncfusion Blazor components, Azure Functions API, Google Cloud Firestore
- **Created:** 2026-03-22

## Learnings

- `[AllowAnonymous]` in Blazor WASM requires `@using Microsoft.AspNetCore.Authorization` — **not** `Microsoft.AspNetCore.Components.Authorization` (which covers auth components). Both namespaces must be in `_Imports.razor` when using the attribute on a page. Mixing them up causes CS0246 build errors and a completely broken app.

- For SWA logout links, always use `post_logout_redirect_uri=/.auth/login/aad` (not `/`) — redirecting to `/` on a protected route causes a 401 that lands on the Blazor loading spinner. Pointing directly to the AAD login page bypasses Blazor entirely and gives a clean logout → login UX.

- `{request.path}` does NOT work in SWA `responseOverrides` — it is only substituted in `routes`. Using it in the 401 redirect caused post-login redirects to unresolvable paths, showing Blazor NotFound. Use plain `/.auth/login/aad` for 401 redirects; SWA handles the post-login redirect automatically.
- UI components live under `Client/Pages/Shopping/` and `Client/Shared/`.
- `ISettings` / `ShoppingListKeysEnum` pattern handles all API URL construction — never hardcode URLs.
- `EntityBase.CssComleteEditClassName` drives CSS state: `"edit"`, `"completed"`, or `""`.
- New shopping items insert at position 0 (top of list) — enforced in `OneShoppingListPage.razor` and `OneFrequentListPage.razor`.
- Admin functions sit behind a CSS-only dropdown in `NewNavComponent.razor`.
- Current goals: auth UI flows and client-side performance improvements.

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
