# Squad Decisions

**Last Updated:** 2026-03-23  
**Source:** Team audits + PRD synthesis (issue #15) + Sprint 0 completion + Daniel feedback

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

### D5 — Toast/Notification System Requirement
**Status:** ⏸️ PENDING DECISION (needs design choice)  
**Issue:** Zero user feedback on async operations. No toasts, spinners, or error messages.

**Constraint:** Must implement before building meal planning UI or auth UI (both have async operations)

**Options:**
- A) Implement custom `INotificationService` + `ToastContainer` component
- B) Integrate third-party library (e.g., CxListen, MudBlazor notifications)

**Recommendation (Blair):** Option A (custom). Simpler, faster, avoids dependency. Takes ~1 day.

**Decision Needed By:** Before Sprint 1 UI/UX work

---

### D6 — Mobile Drag-and-Drop Replacement
**Status:** ⏸️ PENDING DECISION (needs design choice)  
**Issue:** HTML5 drag-and-drop broken on iOS Safari. ShopConfigurationPage and CategoryManagementPage unusable on iPhone/iPad.

**Options:**
- A) Add JS polyfill for touch drag events
- B) Provide up/down button controls as primary interaction; drag as desktop enhancement

**Recommendation (Blair):** Option B. Simpler, accessible, reliable cross-platform. Button controls work everywhere.

**Decision Needed By:** Before mobile redesign sprint

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
**Status:** ⏸️ PENDING DECISION  
**Issue:** Current N+1 inline migration in ShoppingListController fires writes on every GET for un-migrated docs

**Options:**
- A) Extract to one-time `GET /api/admin/migrate` endpoint, run once, disable
- B) Accept self-correction over time (all lists migrated as users visit them)

**Recommendation (Ray):** Option A. Reduces wasted write operations and improves GET response time.

**Decision Needed By:** Before launch (low urgency)

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
