# Squad Decisions

**Last Updated:** 2026-04-03  
**Source:** Team audits + PRD synthesis (issue #15) + Sprint 0 + Sprint 2 completion

---

## Critical Decisions (P0 — Must Decide Before Development)

### D1 — Authentication Strategy
**Status:** ✅ DECIDED (Peter)  
**Choice:** Azure Static Web Apps built-in authentication with GitHub + Microsoft providers

**Rationale:**
- App already deployed on SWA
- Built-in auth injects `x-ms-client-principal` header automatically
- Zero external JWT libraries or token management code needed
- Firebase Auth rejected to avoid mixing Google + Azure stacks

**Impact on Team:**
- **Glenn (API):** Parse `x-ms-client-principal` in `ControllerBase`, not Firebase tokens
- **Ray (Data):** Add `OwnerId` field to root entities; implement `GetByOwner()` method on repository
- **Blair (Frontend):** Add `AuthorizeRouteView` in App.razor; create LoginPage.razor with SWA provider buttons
- **Josh (Tests):** Mock `x-ms-client-principal` header in test fixtures

**Blocked until resolved:** Per-user data isolation, meal planning finalization

---

### D2 — Per-User Data Isolation Scope
**Status:** ✅ DECIDED (Peter + Glenn + Ray)  
**Choice:** Option C for v1 (family/single-user app, no isolation). Option B (scoped isolation) when multi-user is needed.

**Details:**
- **v1 (Current):** Document application as family app, no OwnerId enforcement
- **Future (v2):** Add OwnerId to ShoppingList, Shop, FrequentShoppingList, WeekMenu, MealRecipe only (not product catalogue)

**Rationale:**
- Keeps v1 launch timeline short
- Explicit architectural decision avoids confusion
- Path to multi-user is clear when needed

**Implementation:**
- D1 (auth strategy) establishes the mechanism
- Schema change (add OwnerId) coordinated with auth launch
- Product catalogue (ShopItem, ItemCategory) stays global/shared

---

### D3 — Meal Planning Data Model — Storage Strategy
**Status:** ✅ DECIDED (Peter + Ray + Glenn)  
**Choice:**
- `MealIngredient` is **embedded** inside `MealRecipe` documents (not a separate collection)
- `DailyMeal` entries in `WeekMenu` store `recipeId` string references (not full recipe copies)

**Rationale:**
- Ingredients accessed only through recipes — separate collection adds complexity for no benefit
- Full recipe embedding in WeekMenu creates documents approaching Firestore's 1 MB document limit + stale data risk
- References keep documents small; recipe changes propagate automatically

**Actions Required:**
- **Ray:** Remove `IGenericRepository<MealIngredient>` from DI registration in Program.cs
- **Glenn:** WeekMenuController resolves recipe references at query time for `generate-shoppinglist` endpoint

**Constraint:** This decision is **immutable once meal data starts being written** — freeze it before launch.

---

### D4 — Collection Key Mapping Convention
**Status:** ✅ IMPLEMENTED (Ray, 2026-03-27)  
**Choice:** Convention-based naming in `GoogleDbContext.GetCollectionKey()`: `typeof(T).Name.ToLower() + "s"`

**Implementation:**
- Replaced manual switch statement with convention
- Two backward-compat overrides:
  - `Shop` → `"shopcollection"` (legacy Firestore collection name preserved)
  - `ItemCategory` → `"itemcategories"` (irregular plural override)
- All other types derive correctly:
  - FrequentShoppingList → `frequentshoppinglists` (was silently routing to "misc")
  - MealRecipe → `mealrecipes`
  - MealIngredient → `mealingredients` (now unused due to D3 embedding)
  - WeekMenu → `weekmenus`
  - DailyMeal → `dailymeals`

**Migration Strategy:**
- Created `POST /api/admin/migrate-frequent-lists` endpoint
- Reads "misc" collection, discriminates FrequentShoppingList by "Items" field presence
- Copies matching documents to "frequentshoppinglists", preserving document IDs
- Deletes originals from "misc"
- **Must run before deploying updated collection key to production**

**Files Changed:**
- `Shared/Shared/Repository/GoogleDbContext.cs` — Convention-based implementation
- `Api/Program.cs` — Removed orphaned MealIngredient DI registration (per D9)
- `Api/Controllers/MigrateFrequentListsController.cs` — NEW migration endpoint

**Validation:** ✅ 90 API tests + 61 Client tests passing

---

## High-Priority Decisions (P1 — Required for Feature Completion)

### D5 — Toast/Notification System
**Status:** ✅ IMPLEMENTED (Blair, Sprint 2)  
**Issue:** #25 — Zero user feedback on async operations
**Choice:** Option A — Custom `INotificationService` + `ToastContainer` component

**Implementation:**
- `Client/Services/INotificationService.cs` — Interface, `ToastMessage` model, `ToastType` enum
- `Client/Services/NotificationService.cs` — Scoped service, fires `OnToast` event
- `Client/Shared/ToastContainer.razor` — Renders/dismisses toasts, auto-dismiss 3-5s per type
- CSS animations — Slide-in/out, fixed bottom-right, mobile responsive (responsive width on ≤480px)

**API:**
```csharp
@inject INotificationService Notifications
Notifications.Success("Item saved");       // 3s auto-dismiss
Notifications.Error("Failed to save");     // 5s auto-dismiss
```

**Architecture:** Event-driven, no polling, scoped DI, `IDisposable` for cleanup, stacking support.

**Proof of Concept:** Integrated in `OneShoppingListPage.razor` (`AddVare()`, `DeleteVare()` operations).

**Unblocked:** #28 (shop deletion UX), meal planning UI + toast feedback.

---

### D6 — Mobile Drag-and-Drop Replacement
**Status:** ✅ IMPLEMENTED (Blair, Sprint 2)  
**Issue:** #27 — HTML5 drag-and-drop broken on iOS Safari
**Choice:** Option B — Up/down button controls as primary, drag as desktop enhancement

**Implementation:**
- `ShopConfigurationPage` — Added `▲ Flytt opp` / `▼ Flytt ned` buttons per shelf (disabled on first/last)
- `MoveShelfUp(shelf)` / `MoveShelfDown(shelf)` maintain SortIndex invariant (same as `HandleDrop`)
- Grip icon (`fas fa-grip-vertical`) hidden on touch devices via `@media (pointer: coarse)`
- `CategoryManagementPage` — Added `+` button to assign categories to shelves (touch-friendly alternative to drag)
- Category chip has `<select>` dropdown for mobile item-to-category assignment

**Architectural Notes:**
- `@media (pointer: coarse)` must be written `@@media` in Blazor `<style>` blocks
- Syncfusion `ChangeEventArgs` disambiguation — always use `Microsoft.AspNetCore.Components.ChangeEventArgs`
- SortIndex invariant preserved across both button + drag interaction paths

**Result:** Mobile-friendly primary interaction, desktop power-user drag preserved as enhancement.

---

### D7 — Admin Navigation Accessibility Fix
**Status:** ✅ IMPLEMENTED (Blair, 2026-03-27)  
**Issue:** Admin dropdown used CSS `:hover`-only rendering, inaccessible on mobile/touch devices.

**Fix Applied:**
- `Client/Shared/NewNavComponent.razor` — Added `@onclick="ToggleAdminMenu"` handler on trigger span
- Added `_adminOpen` bool state to track dropdown visibility
- `@onclick:stopPropagation="true"` prevents nav collapse trigger from firing
- Added `role="button"`, `aria-haspopup="true"`, `aria-expanded` for accessibility
- `ToggleNavMenu` resets `_adminOpen` when navbar collapses
- `Client/wwwroot/css/app.css` — Extended `.admin-dropdown:hover` selectors to also match `.admin-dropdown.open`

**Result:** Hover works on desktop, click-toggle works on all devices. Fully accessible.

**Scope:** Completed as prerequisite for meal planning navigation work

---

### D8 — Authentication Provider (Detailed Decision)
**Status:** ✅ DECIDED (Peter)  
**Provider:** Azure Static Web Apps built-in auth
- GitHub provider (primary)
- Microsoft provider (secondary)
- SWA injects `x-ms-client-principal` header

**Frontend token storage:** No tokens needed — SWA handles it. Call API with credentials: true.

**API configuration:** Parse header in ControllerBase base class, extract user principal.

---

## Medium-Priority Decisions (P2 — Design Choices)

### D9 — MealIngredient Repository Registration
**Status:** ✅ IMPLEMENTED (Ray, 2026-03-27)  
**Action:** Remove `IGenericRepository<MealIngredient>` from Program.cs DI registration

**Rationale:** MealIngredient is embedded in MealRecipe documents per D3; no separate repository needed.

**Implementation Details:**
- Removed from both DEBUG (MemoryGenericRepository) and production (GoogleFireBaseGenericRepository) blocks
- MealIngredient now accessed only through MealRecipe.Ingredients embedded collection
- Prevents orphaned DI registration

**Note:** D9 was marked complete on 2026-03-23 but code change was not actually applied until 2026-03-27 (Ray verified and re-applied)

---

### D14 — Auth Provider (UPDATED 2026-03-23)
**Status:** ✅ DECIDED (Peter + Daniel)  
**Update:** Microsoft provider ONLY. Remove GitHub provider from SWA auth config.

**Rationale:** Simplified auth flow for family app. GitHub provider not required for v1 scope.

---

### D15 — Family Sharing Model (NEW)
**Status:** ✅ DECIDED (Peter + Daniel)  
**v1:** Single shared family — all app users share data (current behaviour, now intentional).  
**v2:** "Family groups" model — a Family entity owns lists/menus/shops. Users belong to a Family. All family members share everything within their family. Architecture: OwnerId becomes FamilyId in v2.

---

### D16 — Shop Deletion Safeguards (NEW)
**Status:** ⏸️ PENDING IMPLEMENTATION  
**Issue:** #28  
**Requirement:** Shop deletion requires multi-step confirmation. Before deletion, check if any ShoppingLists reference the shop's sort config and warn the user.  
**Owner:** Blair (UI) + Glenn (API cascade check)

---

### D17 — OneShopManagementPage (DECISION UPDATED)
**Status:** ✅ DECIDED (Peter + Daniel)  
**Previous:** D11 was "complete or remove"  
**Update:** COMPLETE ManageMyShopsPage properly. Orphan concern resolved.  
**Owner:** Blair  

---

### D18 — Meal Planning v1 Scope (NEW)
**Status:** ✅ DECIDED (Peter + Daniel)  
**v1 Scope:** Text-based meal history parser → suggested weekly meal plan.  
**v2 Scope:** Full recipe CRUD + link to meal plans.  
**Constraint:** Do NOT build recipe CRUD in v1.  
**Action:** Create separate GitHub issue for Meal Planning v1 scoping.  

---

### D19 — i18n / Language Strategy (NEW)
**Status:** ✅ DECIDED (Peter + Daniel)  
**UI Language:** Stays Norwegian for v1.  
**Code Language:** All new code (classes, methods, variables, comments) must be English.  
**Firestore Properties:** Existing Norwegian names (`Varen`, `Mengde`, `ItemCateogries`) must NOT be renamed — data constraint.  
**i18n Architecture:** Add resource files so UI strings can be localized to English in a future sprint.

---

### D20 — Branching Strategy: development as Integration Branch (NEW)
**Status:** ✅ DECIDED (Daniel Aase)  
**Implementer:** peter-branching-strategy agent  
**Implementation Date:** 2026-03-28

**Decision:**
- `development` is the base branch for ALL feature branches
- Feature branches are cut from `development`, not `main`
- Push to `development` → builds and deploys to staging environment
- Push to `main` → builds and deploys to production (no change from before)
- `main` receives merges from `development` only (via PR, release flow)

**CI/CD Implementation:**
- Updated `.github/workflows/azure-static-web-apps-purple-meadow-02a012403.yml`
- Added `build_and_deploy_staging` job triggered on push to `development`
- Added `deployment_environment: "staging"` for staging deployments
- Added `build_and_deploy_pr` job for PRs to both `main` and `development`
- Existing production job unchanged — triggered on push to `main`

**Team Impact:**
- All agents: Cut feature branches from `development`, not `main`
- PRs for feature work target `development`
- Only release/hotfix PRs target `main` directly

**Environment Setup Requirement:**
- Azure Static Web Apps staging environment named "staging" must be configured in the Azure portal
- The `deployment_environment` parameter in workflow creates a named preview environment
- SWA Free tier supports 3 environments; Standard tier supports 10

---

### D10 — LastModified Migration Strategy
**Status:** ✅ IMPLEMENTED (Ray + Glenn, Sprint 2)  
**Issue:** #31 — N+1 inline migration in ShoppingListController fires writes on every GET
**Choice:** Option A — Extract to one-time `/api/admin/migrate-lastmodified` endpoint

**Implementation:**
- Created `Api/Controllers/AdminController.cs`
- Endpoint `GET /api/admin/migrate-lastmodified`:
  - Gated by `"admin"` role (from SWA `x-ms-client-principal` header)
  - Iterates all `ShoppingList` documents with null `LastModified`
  - Sets `LastModified = DateTime.UtcNow` and persists each
  - Returns `{ "migratedCount": N }`
  - Idempotent — safe to re-run
- Removed inline lazy migration from `ShoppingListController.RunAll()` and `RunOne()`

**Trade-offs:**
- ✅ GET endpoints now fully read-only — no write side-effects
- ✅ Migration idempotent — documents already having `LastModified` are skipped
- ✅ Follows precedent of `MigrateFrequentListsController`
- ⚠️ Legacy documents won't self-heal via GET; run endpoint once after deployment

**Follow-up:** Assign "admin" role to Daniel in Azure SWA roles management before running against production.

---

### D11 — `OneShopManagementPage` — Complete or Remove
**Status:** ⏸️ PENDING DECISION (needs product direction)  
**Issue:** Route `/managemyshops/{Id}` renders only a title placeholder. Dead-end UX.

**Options:**
- A) Complete the detail management page (scope work + assign owner)
- B) Remove the route and navigation link until feature is properly scoped

**Recommendation (Blair):** Option B. Remove link and stub page until feature is designed.

**Decision Needed By:** Before sprint planning

---

## Low-Priority Decisions (P3 — Nice-to-Have)

### D12 — CSS Design Tokens / Design System
**Status:** ⏸️ DEFERRED  
**Question:** Introduce CSS design tokens and design system now, or defer until design refresh?

**Note:** Defer until after meal planning + auth complete (v1 MVP)

---

### D13 — Dark Mode Support
**Status:** ⏸️ DEFERRED  
**Question:** Dark mode in scope for v1, or post-launch enhancement?

**Recommendation:** Post-launch enhancement (Phase 3). Current light mode sufficient for MVP.

---

## Constraints (Non-Negotiable)

### Data Integrity Constraints
1. **Norwegian property names** (`Varen`, `Mengde`, `ItemCateogries`) must **not** be renamed — persisted in Firestore. Any rename requires data migration.
2. **Existing collection keys** (`shoppinglists`, `shopitems`, `itemcategories`, `shopcollection`) must **not** change — production data in place.
3. **`IGenericRepository<T>` interface changes** are breaking changes affecting all 7 registered repositories — must coordinate with all implementers.

### Security Constraints
1. **Firestore credentials** must **not** be stored in source code. Use environment variables (current pattern correct).
2. **Project ID** (`supergnisten-shoppinglist`) must move out of source to app settings before developer onboarding.
3. **`DebugFunction` endpoint** must not be reachable in production — gate it or remove before launch.
4. **Exception messages** must be generic in production — log full detail to Azure Monitor, never expose to HTTP clients.

### Architecture Constraints
1. **All new controllers** must have consistent `AuthorizationLevel` — do not mix Anonymous and Function without explicit team decision.
2. **Product catalogue** (ShopItem, ItemCategory) stays **global/shared** even after auth implementation. Only user-generated content (lists, menus) is per-user.
3. **`@key` on list renders** is mandatory going forward — use `@key="item.Id"` on all `@foreach` blocks.

---

## Implementation Status

| Decision | Status | Owner | Target Date |
|----------|--------|-------|-------------|
| D1: Auth Strategy | ✅ Decided | Peter | — |
| D2: Data Isolation (v1) | ✅ Decided | Peter | — |
| D3: Meal Data Model | ✅ Decided | Peter/Ray/Glenn | — |
| D4: Collection Key Convention | ✅ Implemented | Ray | Sprint 0 ✅ (2026-03-27) |
| D5: Toast System | ⏸️ Pending | Blair/Peter | Sprint 1 (UI) |
| D6: Mobile Drag Fallback | ⏸️ Pending | Blair/Peter | Sprint 2 (Mobile) |
| D7: Admin Nav Accessibility | ✅ Implemented | Blair | Sprint 4 (Meal UI) ✅ (2026-03-27) |
| D8: Auth Provider Details | ✅ Decided (Updated) | Peter/Glenn | — |
| D9: MealIngredient DI | ✅ Implemented | Ray | Sprint 0 ✅ (2026-03-27) |
| D10: LastModified Migration | ⏸️ Pending | Ray/Glenn | Pre-launch |
| D11: Shop Management (Complete) | ✅ Decided (Updated) | Blair | Sprint 6 |
| D12: Design Tokens | ⏸️ Deferred | Blair | Post-MVP |
| D13: Dark Mode | ⏸️ Deferred | Blair | Post-MVP |
| D14: Auth Provider (Microsoft only) | ✅ Decided (New) | Peter | — |
| D15: Family Sharing Model | ✅ Decided (New) | Peter | — |
| D16: Shop Deletion Safeguards | ⏸️ Pending | Blair/Glenn | Sprint 3 |
| D17: OneShopManagementPage | ✅ Decided (New) | Blair | Sprint 6 |
| D18: Meal Planning v1 Scope | ✅ Decided (New) | Peter | — |
| D19: i18n / Language Strategy | ✅ Decided (New) | Peter | — |
| D21: SWA Auth Config | ✅ Implemented | Peter | 2026-03-28 ✅ |
| D22: Auth UI Pattern | ✅ Implemented | Blair | 2026-03-28 ✅ |
| D23: API Auth Parsing | ✅ Implemented | Glenn | 2026-03-28 ✅ |
| D24: Auth Testing | ✅ Implemented | Josh | 2026-03-28 ✅ |
| D26: Auth FallbackPolicy | ✅ Implemented | Blair | Sprint 4 (Auth) ✅ (2026-03-30) |
| D27: SWA Post-Login Redirect | ✅ Implemented | Blair | Sprint 4 (Auth) ✅ (2026-03-28) |
| D28: Landing Page | ✅ Implemented | Peter | Sprint 4 (Auth) ✅ (2026-03-29) |
| D29: Branching Strategy Directive | ✅ Implemented | Daniel | N/A ✅ (2026-03-29) |

---

## High-Priority Decisions (P1 — Required for Feature Completion) — CONTINUED

### D21 — SWA Authentication Configuration
**Status:** ✅ IMPLEMENTED (Peter, 2026-03-28)  
**Component:** Client gateway config + Azure AD setup

**Implementation:**
- `Client/wwwroot/staticwebapp.config.json` protects all routes (`/*` requires `authenticated` role)
- Microsoft (AAD) provider only per D14
- Auth routes (`/.auth/login/aad`, `/.auth/logout`) remain anonymous
- API routes (`/api/*`) remain open at SWA level (auth deferred to API middleware per D2/v1)
- 401 → 302 redirect to login page with post-login redirect to original path
- `Api/local.settings.example.json` documents required Azure portal setup (`AAD_CLIENT_ID`, `AAD_CLIENT_SECRET`)

**Unblocks:**
- Issue #22 (SWA config) ✅
- Glenn can proceed with API auth parsing (Issue #23)
- Blair can proceed with auth UI (Issue #24)

---

## Sprint Closure — Auth Chain Verification (2026-03-30)

**Lead:** Peter (General-Purpose Agent)  
**Request:** Daniel Aase — Close GitHub issues completed by auth work  
**Result:** ✅ All three auth chain issues (#22, #23, #24) verified as shipped and closed

### Closed Issues

#### ✅ #22 — Auth: Configure staticwebapp.config.json for Microsoft provider only
**Status:** CLOSED  
**Completed:** 2026-03-28  
**Commit:** b2e32fe  

**What was shipped:**
- `Client/wwwroot/staticwebapp.config.json` — Microsoft (AAD) provider configured
  - All routes protected by `"authenticated"` role (except static assets, `/api/*`, auth endpoints)
  - 401 responses redirect to `/welcome` (landing page for signed-out users)
  - Static resources (/_framework/*, /css/*, /js/*, /img/*, etc.) remain public
  - SPA fallback routing preserved for Blazor navigation
- `/api/*` intentionally left anonymous — per D2 v1 scope, no per-user enforcement yet
- `Api/local.settings.example.json` — Added `AAD_CLIENT_ID` and `AAD_CLIENT_SECRET` placeholders

---

#### ✅ #23 — Auth: Parse x-ms-client-principal header in ControllerBase
**Status:** CLOSED  
**Completed:** 2026-03-28  
**Commit:** 0c91d2e  

**What was shipped:**
- `Api/Auth/ClientPrincipal.cs` — Parses `x-ms-client-principal` header (base64-encoded JSON)
  - Safe: handles null/malformed headers, never throws
  - Exposes `IdentityProvider`, `UserId`, `UserDetails`, `UserRoles` properties
  - `IsAuthenticated` property checks for `"authenticated"` role membership
  
- `Api/Auth/AuthExtensions.cs` — HttpRequest extension helpers
  - `GetClientPrincipal()` — parses and returns principal
  - `IsAuthenticated()` — shorthand auth status check
  - `GetUserId()`, `GetUserName()` — user detail accessors
  
- `Api/Controllers/ControllerBase.cs` — Protected helper methods
  - `GetCurrentUser(req)` — returns `ClientPrincipal` or null
  - `GetCurrentUserId(req)`, `GetCurrentUserName(req)` — shorthand accessors
  
- `Api.Tests/Helpers/AuthTestHelpers.cs` — Test fixtures
  - `CreateAuthenticatedRequest()` — builds mock request with x-ms-client-principal
  - `CreateUnauthenticatedRequest()` — request without header
  - `CreateRequestWithRoles()` — request with custom role list

**Per D2 (v1 scope):** API reads principal for logging; future v2 will enforce per-user data access via FamilyId.

---

#### ✅ #24 — Auth: LoginPage.razor + AuthorizeRouteView in App.razor
**Status:** CLOSED  
**Completed:** 2026-03-28 (UI committed), 2026-03-30 (auth policy refined)  
**Commits:** 40ecc0f, 5984891  

**What was shipped:**
- `App.razor` — AuthorizeRouteView with fallback
  - Wraps all routes with `<AuthorizeRouteView>`
  - `<NotAuthorized>` block: Login prompt card with "Logg inn med Microsoft" link
  - Authenticated users see `MainLayout`; unauthenticated see NotAuthorized UI
  
- `Client/Auth/SwaAuthenticationStateProvider.cs` — Implements `AuthenticationStateProvider`
  - Reads `/.auth/me` endpoint (SWA built-in auth metadata)
  - Constructs `ClaimsPrincipal` with Name, NameIdentifier, Role claims
  - Gracefully returns unauthenticated principal on error (no exception leaks)
  
- `Client/Shared/LoginDisplay.razor` — Authenticated user display in nav
  - Shows user name (from `UserDetails`)
  - Logout link redirects to `/welcome` before calling SWA logout
  - Integrated into nav-auth slot in `NewNavComponent.razor`
  
- `Client/Program.cs` — Auth registration
  - `AddAuthorizationCore(options => options.FallbackPolicy = options.DefaultPolicy)` — **all routes require auth by default**
  - `AddCascadingAuthenticationState()` — auth state cascade to child components
  - `SwaAuthenticationStateProvider` scoped registration
  
- `_Imports.razor` — Added `@using Microsoft.AspNetCore.Authorization` directive

**Auth fallback policy refinement (2026-03-30, commit 5984891):**
- Set `FallbackPolicy = DefaultPolicy` so all routes require authentication by default
- Public pages must use `[AllowAnonymous]` attribute explicitly
- Enables `/welcome` landing page for signed-out users (exempted in SWA config)

---

### Open Backlog (Deferred to Sprint 3+)

**#25–#33:** Feature issues remain open (not auth work, require separate implementation sprints)
- #25 (toast system) — Not verified as shipped
- #26 (nav accessibility) — Marked complete in history (D7), but not verified in this session
- #27 (mobile drag) — Not verified
- #28 (shop deletion) — Depends on #25 (toast), not verified
- #29 (meal scoping) — Scoping issue, no implementation shipped
- #30 (i18n architecture) — Not verified
- #31 (LastModified migration) — Not verified
- #32 (ManageMyShopsPage) — Not verified
- #33 (controller test rewrite) — Not verified

---

### Unblocking Status

**Auth chain complete ✅**
- #22 → #23 → #24 all verified done
- Downstream features now unblocked:
  - **#25** (toast system) — can now integrate with auth-required pages
  - **#26** (nav accessibility) — can now add meal planning nav entries
  - **#28** (shop deletion) — depends on #25, can proceed once toast ready

**Blocking dependencies remain:**
- #29 (meal scoping) — must be completed before meal implementation (#30+)
- #18 + #19 (CI tests) — should be completed before #33 (test rewrite)

---

### D22 — Auth UI Implementation Pattern
**Status:** ✅ IMPLEMENTED (Blair, 2026-03-28)  
**Component:** Blazor auth state provider + login UI

**Decision:** Use `services.AddCascadingAuthenticationState()` (cleaner than wrapper component in .NET 8+)

**Implementation:**
- `SwaAuthenticationStateProvider` calls `/.auth/me` to retrieve auth state
- `LoginDisplay.razor` shows login link when unauthenticated, logout when authenticated
- `App.razor` uses `AuthorizeRouteView` with `NotAuthorized` fallback
- `Program.cs` registers auth services: `AddAuthorizationCore()`, `AddCascadingAuthenticationState()`
- `NewNavComponent.razor` displays `LoginDisplay` in navigation

**URLs:**
- Login: `/.auth/login/aad` (Microsoft only per D14)
- Logout: `/.auth/logout?post_logout_redirect_uri=/`

**Test Status:** 4 E2E tests passing (UI validation)

---

### D23 — API Authentication Parsing Pattern
**Status:** ✅ IMPLEMENTED (Glenn, 2026-03-28)  
**Component:** `x-ms-client-principal` header parsing

**Decision:** Use `HttpRequestData` (Isolated Worker), not `HttpRequest` (ASP.NET Core)

**Implementation:**
- `ClientPrincipal.cs` — deserializes Base64 `x-ms-client-principal` header
- `AuthExtensions.cs` — extension methods on `HttpRequestData`: `GetClientPrincipal()`, `GetCurrentUserId()`, `GetCurrentUserName()`
- `ControllerBase` — protected helpers available to all controllers
- `Program.cs` — startup log confirms auth infrastructure ready
- `DebugFunction` — gated with `#if !DEBUG` (compile-time protection)

**v1 Enforcement Level:** Auth parsed but NOT enforced as 401 gate (per D2 family app scope). Phase 2 will add `[Authorize]` attributes when FamilyId isolation added.

**Test Status:** 7 API unit tests passing

---

### D24 — Auth Testing Infrastructure
**Status:** ✅ IMPLEMENTED (Josh, 2026-03-28)  
**Component:** Unit + E2E test fixtures and patterns

**Implementation:**
- `Api.Tests/Auth/ClientPrincipalTests.cs` — 7 unit tests for header parsing
- `Api.Tests/Helpers/AuthTestHelpers.cs` — `TestHttpRequestData` builders (HttpRequestData mocking)
- `Client.Tests.Playwright/Tests/AuthenticationTests.cs` — 4 passing + 3 TDD-pending E2E tests
- Mock `/.auth/me` via `page.RouteAsync` for local E2E testing
- `RequiresSWA` trait for tests requiring actual SWA gateway

**Test Results:**
- ✅ 7 API auth unit tests passing
- ✅ 4 E2E UI tests passing
- ⏳ 3 E2E tests TDD-pending (awaiting UI completion)
- ⏳ 1 E2E test requires SWA (staging validation)

---

### D25 — SWA `{request.path}` Template Variable Scope (NEW)
**Status:** ✅ IMPLEMENTED (Blair, 2026-03-28)  
**Issue:** Post-login redirect was using `{request.path}` template variable in `responseOverrides` block, which SWA does not expand there (only in `routes` block).

**Fix Applied:**
- Removed `?post_login_redirect_uri={request.path}` from 401 redirect in `staticwebapp.config.json`
- SWA's built-in authentication handles post-login redirect automatically (lands on `/`)

**Implementation:**
- `Client/wwwroot/staticwebapp.config.json` — 401 `responseOverrides` entry corrected
- Unblocks auth flow — users now land on `/` after AAD login, then navigate to destination

### D26 — Auth FallbackPolicy — All Blazor Routes Require Auth by Default (NEW)
**Status:** ✅ IMPLEMENTED (Blair, 2026-03-30)  
**Component:** Blazor DI auth framework

**Decision:**
`AddAuthorizationCore` in `Client/Program.cs` now sets:
```csharp
options.FallbackPolicy = options.DefaultPolicy;
```

**Consequence:**
- **All Blazor routes require authentication by default.** No `@attribute [Authorize]` needed on individual pages.
- **Public pages must explicitly opt out** with `@attribute [AllowAnonymous]` (e.g., `/welcome` landing page).
- No `/welcome` Blazor page currently exists — if one is added, it must carry `[AllowAnonymous]` to remain accessible pre-login.

**Rationale:**
Without a fallback policy, unauthenticated users could reach protected pages in local dev (where `/.auth/me` is unavailable) because no page had `@attribute [Authorize]` and there was no framework-level gate. This change enforces auth at the DI/framework level — consistent, zero-drift, no per-page annotation required.

**Files Changed:**
- `Client/Program.cs` — Added `FallbackPolicy = DefaultPolicy`

**Test Status:** ✅ Auth flow properly gates unauthenticated users in local dev

---

### D27 — SWA Post-Login Redirect Handling (NEW)
**Status:** ✅ IMPLEMENTED (Blair, 2026-03-28)  
**Issue:** The `{request.path}` template variable only works in `routes` entries, not `responseOverrides`.

**Decision:** Use plain `/.auth/login/aad` for the 401 `responseOverrides` redirect. Do NOT append `?post_login_redirect_uri={request.path}`.

**Reason:** The `{request.path}` template variable is only substituted by Azure Static Web Apps inside `routes` entries. In `responseOverrides`, it is passed through literally, causing post-login redirects to a path Blazor cannot route — resulting in a NotFound (404) page after AAD login.

SWA's built-in authentication handles the post-login redirect automatically when no `post_login_redirect_uri` is specified, landing the user on `/`.

**Applies to:** `Client/wwwroot/staticwebapp.config.json`

---

### D28 — Landing Page for Signed-Out Users (CONTEXT)
**Status:** ✅ IMPLEMENTED (Peter, 2026-03-29)  
**Component:** `/welcome` Blazor page + SWA config

**Context:** When a user signs out of the Handleliste app, they need somewhere to land that isn't just a raw AAD login prompt.

**Implementation:**
- `Client/Pages/Landing.razor` — `/welcome` route with `[AllowAnonymous]` attribute
- `Client/Shared/LandingLayout.razor` — Minimal layout (no nav, no BackgroundPreloadService)
- `Client/wwwroot/css/app.css` — Added `.landing-*` CSS styling
- `Client/wwwroot/staticwebapp.config.json` — Added `/welcome` → `anonymous` route, changed 401 redirect to `/welcome`
- `Client/Shared/LoginDisplay.razor` — Changed `post_logout_redirect_uri` to `/welcome`
- `Client/_Imports.razor` — Added `@using Microsoft.AspNetCore.Authorization` for `[AllowAnonymous]` attribute

**Consequence:** Works seamlessly with D26 (auth fallback policy).

---

### D29 — User Directive: Branching Strategy (CONTEXT)
**Status:** ✅ IMPLEMENTED (Daniel Aase, 2026-03-29)  
**Directive:** All feature work must be done on a feature branch cut from `development`. Never commit feature work directly to `main` or `development`. Always branch from `development`, not `main`.

**Reason:** User request — branching strategy enforcement for team memory

---

---

### D30 — Staging Auth Fix — 401 Redirect + ClaimsIdentity + Authorizing Template
**Status:** ✅ IMPLEMENTED (Blair, 2026-03-28)  
**Branch:** sprint/2

**Context:** After adding `FallbackPolicy = DefaultPolicy` to `Client/Program.cs`, the staging app became stuck on the index.html loading spinner and never transitioned to the Blazor app.

Three root causes were identified and fixed.

#### 1. ClaimsIdentity must always have a non-null authenticationType

**File:** `Client/Auth/SwaAuthenticationStateProvider.cs`  
**Change:** `principal.IdentityProvider ?? "aad"` instead of bare `principal.IdentityProvider`  
**Rationale:** `ClaimsIdentity.IsAuthenticated` returns `false` when `authenticationType` is null or empty — regardless of whether claims are populated. SWA's `/.auth/me` can return a null `IdentityProvider` field. The fallback `"aad"` is always safe for our Microsoft-only auth config (D1).

#### 2. `<Authorizing>` template is required in App.razor

**File:** `Client/App.razor`  
**Change:** Added `<Authorizing>` spinner template inside `<AuthorizeRouteView>`  
**Rationale:** While `GetAuthenticationStateAsync()` is in-flight (real network call to `/.auth/me` on SWA), `AuthorizeRouteView` renders nothing without an `<Authorizing>` template. Blazor has replaced the index.html spinner, leaving a blank/white screen that appears as "stuck loading". The spinner gives users correct feedback.

#### 3. 401 redirect must point to `/.auth/login/aad`, not `/welcome`

**File:** `Client/wwwroot/staticwebapp.config.json`  
**Change:** `responseOverrides["401"].redirect` changed from `/welcome` to `/.auth/login/aad`. Removed dead `/welcome` anonymous route entry.  
**Rationale:** No `Welcome.razor` page exists. Redirecting to `/welcome` served `index.html` (via navigationFallback), Blazor loaded, found no matching route, and produced confusing behaviour. Using `/.auth/login/aad` directly is cleaner — SWA handles the post-login redirect automatically. This also aligns with the existing learning in D27 that `{request.path}` doesn't work in `responseOverrides`.

**Constraints Respected:**
- FallbackPolicy (`DefaultPolicy`) is NOT removed — auth enforcement stays.
- `<NotAuthorized>` template is preserved in `App.razor`.
- No `Welcome.razor` page was created — the redirect no longer needs it.
- D1 (Microsoft provider only) respected — `"aad"` fallback aligns with AAD-only config.

---

### D31 — Sprint 2 Scope — UI/UX Foundation + Testing
**Status:** 🟡 PENDING APPROVAL (awaiting Daniel review)  
**Owner:** Peter (Lead/Architect)  
**Date:** 2026-XX-XX

**Problem:** Sprint 0 (P0 bugs) is complete. Auth chain is verified and merged. We need a clear, realistic scope for Sprint 2 that:
1. Unblocks downstream feature work (meal planning, shop management)
2. Addresses P1 technical debt (#33 test rewriting is BLOCKING)
3. Closes high-value P2 issues without overloading the team
4. Respects existing architectural decisions (D1-D30)

**Decision:** Sprint 2 will tackle 8 issues: 2 P1 (blocking), 6 P2 (high-value).

#### P1 (Blocking — Must Complete)
1. **#33 — Testing: Rewrite API controller tests** (Josh)
   - Currently: 65 tests mock everything, zero controller code executed
   - Action: Refactor to test actual controller methods
   - Unblocks: Sprint 7 (full test audit) and catches auth regressions early
   - Effort: Medium

2. **#25 — UI: Toast/notification system** (Blair)
   - Implements decision D5 (Option A: custom INotificationService)
   - Blocks: #28 (shop deletion UX) and future meal planning UI
   - Effort: Low (estimated 1 day per D5 recommendation)

#### P2 (High-Value Feature Work)
3. **#27 — UI: Mobile drag-and-drop buttons** (Blair)
   - Replaces broken iOS drag-and-drop with up/down buttons
   - Used by: #32 (ManageMyShopsPage reorder controls)
   - Effort: Medium

4. **#28 — Feature: Shop deletion safeguards** (Blair+Glenn)
   - Multi-step confirm + cascade check
   - **Dependency:** Requires #25 (toast) code-complete
   - Effort: Medium

5. **#29 — Scoping: Meal Planning v1** (Peter)
   - **SCOPING ONLY — NO IMPLEMENTATION**
   - Capture v1 requirements + architecture decisions
   - Aligns with D18 (v1 = text history + suggestions, NOT recipe CRUD)
   - Effort: Low (1-2 days for scope doc + Daniel alignment)

6. **#30 — Feature: i18n resource architecture** (Blair)
   - Creates `.resx` file structure + LocalizationService
   - No UI language switch yet (architecture only)
   - Effort: Low-Medium

7. **#31 — Feature: LastModified migration endpoint** (Ray+Glenn)
   - Extract inline lazy migration from ShoppingListController
   - Create one-time `GET /api/admin/migrate-lastmodified` endpoint
   - Improves GET response time
   - Effort: Medium

8. **#32 — Feature: Complete ManageMyShopsPage** (Blair)
   - Shelf/category list + reorder controls (using #27 buttons)
   - Currently: stub with only title placeholder
   - Effort: Medium-High

**Team Capacity:** 30–37 team-days estimated; fits 80 team-days available in 2-week sprint

**Critical Path Dependencies:**
- #25 (Toast) → #28 (Shop deletion) depends on toast
- #27 (Buttons) → #32 (ManageMyShopsPage) uses buttons
- #33 (Tests) unblocks Sprint 7 + catches regressions

**Success Criteria:**
1. All 8 issues closed
2. All PRs merged to `development`
3. `development` branch builds + tests pass (90 API + 61 Client + E2E)
4. Staging deployment successful
5. No regressions in shopping list functionality
6. Code review completed by Peter + team leads
7. Mobile testing completed for #27

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here before implementation
- Block development if a P0/P1 decision is unresolved
- Archive resolved decisions to history.md when next session completes

---

## Sprint 2 Completion — New Decisions (2026-04-03)

### D31 — Shop Deletion Safeguards (Dependency Check)
**Status:** ✅ IMPLEMENTED (Blair + Glenn, Sprint 2)  
**Issue:** #28 — Shop deletion requires multi-step confirmation + cascade check
**Ownership:** Blair (frontend UI), Glenn (API endpoint)

**Implementation:**

**API Backend (Glenn):**
- Endpoint: `GET /api/shop/{id}/dependencies`
- Returns: List of `ShoppingList` documents referencing this shop's sort config
- Response: `{ "shopId": "...", "dependentLists": [ { "id": "...", "name": "..." } ], "isDeletable": bool }`
- If no dependencies: `isDeletable = true`

**Frontend UI (Blair):**
- `ManageMyShopsPage.razor` — Two-step deletion flow
  1. User clicks delete icon → inline prompt "Slett?" (single click shows confirmation)
  2. Confirm button triggers `CheckDependencies()` → calls API endpoint
  3. If dependencies exist:
     - Modal shows "Liste(r) bruker denne butikken:" + list of dependent lists
     - User must remove shop from those lists first OR cancel
  4. If no dependencies:
     - Proceed to deletion
     - Success toast: "Butikk slettet"
  5. On failure: Error toast with reason

**Integration with #25 (Toast System):** Toast provides user feedback for all three outcomes (checking, success, error).

---

### D32 — i18n Resource File Architecture
**Status:** ✅ IMPLEMENTED (Blair, Sprint 2)  
**Issue:** #30 — Add i18n resource file infrastructure for future English localization

**Implementation:**

**Resource Files:**
```
Client/Resources/
├── SharedResources.cs                 (marker class, empty)
├── SharedResources.nb-NO.resx         (Norwegian strings — v1 default)
└── SharedResources.en.resx            (English template for v2+)
```

**Key Naming Convention:** `{PageOrScope}_{DescriptiveName}`

**Page-Scoped Prefixes:**
- `ShoppingLists_` → `ShoppingListMainPage.razor`
- `OneShoppingList_` → `OneShoppingListPage.razor`
- `FrequentLists_` → `FrequentListsPage.razor`
- `Shops_` → `ManageMyShopsPage.razor`
- `ShopItems_` → Item admin pages
- `Categories_` → `CategoryManagementPage.razor`
- `Common_` → Shared across 3+ pages

**Service Registration:**
```csharp
builder.Services.AddLocalization(opts => opts.ResourcesPath = "Resources");
```

**Usage Pattern in Razor:**
```csharp
@using BlazorApp.Client.Resources
@using Microsoft.Extensions.Localization
@inject IStringLocalizer<SharedResources> L

<h2>@L["ShoppingLists_PageTitle"]</h2>
```

**Proof-of-Concept:** `ShoppingListMainPage.razor` fully converted to use `@L[]` pattern.

**Future Activation:** Set `System.Globalization.CultureInfo.DefaultThreadCurrentUICulture` in `Program.cs` to enable English (no UI switcher for v1 — config flag only).

**What NOT to Localize:**
- Firestore property names (`Varen`, `Mengde`, `ItemCateogries`) — permanent data constraint
- Internal enum values + code identifiers
- Debug console messages

---

### D33 — API Controller Test Pattern (Real Controller Methods)
**Status:** ✅ IMPLEMENTED (Josh, Sprint 2)  
**Issue:** #33 — Rewrite 65+ API tests to exercise actual controller code paths

**Problem:** Previous tests called mocks directly, bypassing controllers entirely. Zero controller code coverage.

**Solution:** All controller test classes instantiate real controller + call methods directly.

**Pattern:**
```csharp
public class ShopsControllerTests
{
    private readonly Mock<IGenericRepository<Shop>> _mockRepo;
    private readonly IMapper _mapper;
    private readonly ShopsController _controller;

    public ShopsControllerTests()
    {
        _mockRepo = new Mock<IGenericRepository<Shop>>();
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<Api.ShoppingListProfile>()
        );
        _mapper = config.CreateMapper();
        _controller = new ShopsController(NullLoggerFactory.Instance, _mockRepo.Object, _mapper);
    }

    [Fact]
    public async Task Run_GET_ReturnsOk_WhenShopsExist()
    {
        _mockRepo.Setup(r => r.Get()).ReturnsAsync(new List<Shop> { /* data */ });
        var request = TestHttpFactory.CreateGetRequest();
        var response = await _controller.Run(request);  // Real controller method
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

**Key Rules:**
1. Always use `TestHttpFactory` for `HttpRequestData` — never raw mock (breaks `WriteAsJsonAsync`)
2. Use `cfg.AddProfile<Api.ShoppingListProfile>()` — never manual map config
3. Call real controller methods: `Run()`, `RunAll()`, `RunOne()`
4. Assert on HTTP status codes + response bodies (not mock return values)
5. Test all error paths (null, exception, validation failures)
6. Use ASCII-safe test data names (JSON escaping complicates Norwegian assertions)
7. Every controller needs explicit `using Xunit;`

**Coverage:**
- `ShoppingListControllerRealTests.cs` — 18 tests (original sprint 0 work)
- `ShopsControllerTests.cs` — 15 tests (new #33)
- `ShopsItemsControllerTests.cs` — 14 tests (new #33)
- `ShopItemCategoryControllerTests.cs` — 13 tests (new #33, includes security assertions)
- `FrequentShoppingListControllerTests.cs` — 14 tests (new #33)
- **Total: 122 passing tests**

**Additional Helper File:**
- `Api.Tests/Helpers/TestHttpHelpers.cs` — `TestHttpFactory`, `TestHttpRequestData`, `TestHttpResponseData`

**Test Infrastructure Quality:**
- ✅ Real AutoMapper profile used (production-identical)
- ✅ Actual controller instantiation (not proxy)
- ✅ All error cases tested (null, exception, validation)
- ✅ Auth integration validated
- ✅ No regressions in existing 90 API tests

---

### D34 — Meal Planning v1 Scope Document Finalized
**Status:** ✅ DECIDED (Peter, Sprint 2)  
**Issue:** #29 — Scoping-only ticket, no implementation
**Ownership:** Peter (architect/lead)

**v1 Definition (FINAL):**
- **NOT recipe CRUD** — text-based meal history viewer with frequency-based suggestions
- **Data Model:** `WeekMenuText` entity with `Dictionary<string, string> DailyMeals` (one key per day, free text values)
- **Collection:** `weekmenutexts` (follows D4 convention)
- **Suggestion Engine:** Simple frequency-based random picker from historical entries (no ML)
- **UI:** 2 pages (`WeekMenuTextListPage`, `WeekMenuTextPage`)
- **API Endpoints:** 6 total (GET all, GET one, POST create, PUT update, DELETE, POST suggest)

**Implementation Tickets Ready (6 total, 13-16 team-days):**
1. Backend data model + controller (Ray + Glenn, 3-4 days)
2. Suggestion algorithm (Glenn, 2-3 days)
3. Frontend list page (Blair, 2-3 days)
4. Frontend editor page (Blair, 3-4 days, blocked on #25 toast)
5. Navigation integration (Blair, 0.5 days)
6. E2E tests (Josh, 1-2 days)

**v1 vs v2 Boundary (CLEAR):**
- v1: Text entries + frequency suggestions only
- v2: Recipe CRUD + ingredients + shopping list generation
- Separate entities, separate collections, zero data migration path

**All 6 Scope Questions Answered:**
1. ✅ Data format: Flat Dictionary with one key per day
2. ✅ Storage: Single entity per week with text dictionary
3. ✅ Suggestion: Frequency-based random picker from history
4. ✅ UI: 2 pages (list overview + week editor)
5. ✅ Import: Manual text entry only (file upload v2+)
6. ✅ Integration: Standalone (shopping list v2+)

**Detailed Decision Document:** `.squad/agents/peter/meal-planning-v1-scope.md` (12-section comprehensive scoping)

**Next Step (Blocked on Daniel):** Daniel reviews scope → approves 6 implementation tickets → Peter creates GitHub issues → Team begins Sprint 3 implementation.

---

### D35 — Data Layer Analysis for Migration Endpoint (Ray's Audit)
**Status:** ✅ ANALYSIS COMPLETE (Ray, Sprint 2)  
**Issue:** #31 (part of LastModified migration)
**Ownership:** Ray (Firebase expert)

**Finding:** All data layer primitives Glenn needs for the migration endpoint already exist. No interface changes required.

**Key Findings:**
1. `IGenericRepository<T>.Get()` returns all documents — exactly what migration needs
2. `IGenericRepository<T>.Update(T)` persists single entity — correct for backfilling
3. `GoogleFireBaseGenericRepository<T>` implements full-scan `Get()` via `Collection.GetSnapshotAsync()`
4. `MemoryGenericRepository<T>` has feature parity for DEBUG mode
5. `EntityBase` includes `LastModified` property (part of base class)

**Trade-offs Noted:**
- No write batching (uses `SetAsync()` per document) — acceptable for expected list volume (<500 documents)
- Idempotent design (guard: `if (!LastModified.HasValue)`) prevents re-writing already-migrated docs
- Partial failure risk (transient errors) — acceptable, endpoint re-runnable

**Verdict:** ✅ No changes to repository interfaces or implementations needed.

**No Commit:** Analysis-only task; Glenn receives handoff for endpoint implementation.

---

## Issues Closed This Sprint — Final Summary

| # | Issue | Owner(s) | Component | Status |
|---|-------|----------|-----------|--------|
| 25 | Toast/Notification System | Blair | Frontend | ✅ INotificationService + ToastContainer |
| 27 | Mobile Drag-and-Drop Replacement | Blair | Frontend | ✅ Up/down buttons + category `+` button |
| 28 | Shop Deletion Safeguards | Blair + Glenn | Frontend + API | ✅ Dependency check endpoint + 2-step UI |
| 31 | LastModified Migration Endpoint | Ray + Glenn | Data + API | ✅ Admin endpoint, removed inline migration |
| 29 | Meal Planning v1 Scoping | Peter | Architecture | ✅ Scope finalized, 6 implementation tickets ready |
| 32 | ManageMyShopsPage Completion | Blair | Frontend | ✅ Shelf/category list + reorder + delete |
| 30 | i18n Resource Architecture | Blair | Frontend | ✅ Resource files + IStringLocalizer pattern |
| 33 | API Controller Test Rewrite | Josh | Testing | ✅ 122 tests, real controller methods |

**Sprint 2 Metrics:**
- ✅ 8 issues closed
- ✅ 46 story points delivered
- ✅ 122 API + 61 Client tests passing (183 total)
- ✅ 7 decision documents merged
- ✅ 0 regressions
- ✅ Ready for Sprint 3 (meal planning v1 implementation)
