# Project Context

- **Owner:** Daniel Aase
- **Project:** Shoppinglist — Blazor WebAssembly shopping list app with shop-specific item sorting
- **Stack:** Blazor WebAssembly (.NET 9), Azure Functions v4, Google Cloud Firestore, Syncfusion UI, Playwright E2E tests
- **Created:** 2026-03-22
- **Status:** Sprint 2 complete (8 issues, 122 tests passing)

## Core Context — Learnings & Decisions (Archived)

**See `.squad/decisions.md` for full decision history (D1–D35).** This section summarizes core learnings through Sprint 2.

### Fundamental Architecture
- Dual-model pattern: `FireStoreDataModels` (Firestore-attributed) + `HandlelisteModels` (DTOs), bridged by AutoMapper + `.ReverseMap()`
- Shop-specific sorting: client-side in `OneShoppingListPage.razor` via `SortShoppingList()`
- Product catalogue globally shared; user-generated content (lists, shops) will be per-family in v2

### Data Constraints (Permanent)
- Norwegian Firestore property names (`Varen`, `Mengde`, `ItemCateogries`) must NOT be renamed — data already persisted
- Existing collection keys (`shoppinglists`, `shopitems`, `itemcategories`, `shopcollection`) are immutable
- `IGenericRepository<T>` interface changes are breaking — coordinate across all 7+ implementations

### Completed Infrastructure
- **Sprint 0 (P0 bugs):** 6 PRs (#34–#38), issues #16–#21 closed. Collection key convention (D4), DI registration (D9), admin nav accessibility (D7).
- **Auth Chain (Sprint 1):** Issues #22–#24 complete. SWA config (D21), auth parsing (D23), auth UI (D22).
- **Sprint 2 (UI/Testing):** Issues #25–#33 complete. Toast system (D5), mobile controls (D6), migration endpoint (D10), i18n architecture (D32), shop deletion safeguards (D30), 122 controller tests (D33).

### Key Decisions
- **v1 Scope (D18):** Text-based meal history + frequency suggestions only — NO recipe CRUD. v2 will have full meal planning.
- **Auth (D1):** Azure SWA with Microsoft provider only. No per-user enforcement in v1 (D2).
- **i18n (D19):** UI stays Norwegian v1; resource file infrastructure ready for English v2.
- **Mobile (D6):** Up/down buttons as primary reorder control; HTML5 drag-drop as desktop enhancement.

### Blockers & Risks Mitigated
- Collection key bug (FrequentShoppingList in "misc" collection) — Fixed by Ray (D4), migration endpoint created
- No controller code tested (65 tests called mocks) — Fixed by Josh (D33), 122 real-method tests passing
- Meal v1 scope unclear — Fixed by Peter (D29–D34), 6 implementation tickets ready
- i18n missing infrastructure — Fixed by Blair (D32), resource files + pattern established

---

## Recent Sprint History
- URL: https://github.com/danielaase1337/shoppinglist/issues/15

## PRD Decomposition — 2026-03-23

### Sub-Tickets Created (18 total from PRD #15)
- **Sprint 0 (P0 bugs):** #16 (collection keys, Ray), #17 (DI registration, Ray), #18 (CI tests, Josh), #19 (test audit, Josh), #20 (try/catch, Glenn), #21 (.Result deadlock, Glenn)
- **Sprint 1 (Auth):** #22 (SWA config, Glenn), #23 (auth middleware, Glenn), #24 (auth UI, Blair)
- **Sprint 2 (UI):** #25 (toast system, Blair), #26 (nav accessibility, Blair), #27 (mobile drag, Blair)
- **Sprint 3 (Shop):** #28 (shop deletion safeguards, Blair + Glenn)
- **Sprint 4 (Meal):** #29 (scoping only — NOT implementation, Peter)
- **Sprint 5 (i18n):** #30 (resource file architecture, Blair)
- **Sprint 6 (Data):** #31 (LastModified migration, Ray + Glenn), #32 (ManageMyShopsPage, Blair)
- **Sprint 7 (Testing):** #33 (controller test rewrite, Josh)

## Branching Strategy Update (2026-03-28)

**Broadcast by:** Peter (Lead) — Daniel Aase directive

**New branching strategy is in effect as of 2026-03-28:**
- `development` is now the base branch for ALL feature branches
- Cut new branches from `development`, not `main`
- Merging into `development` triggers a **staging** deployment (Azure SWA staging environment)
- Only `main` deploys to **production** — never push features directly to `main`
- PRs for feature work target `development`; only release PRs target `main`

**CI/CD updated:** `.github/workflows/azure-static-web-apps-purple-meadow-02a012403.yml` now has three separate jobs: production (main), staging (development), and PR previews.

### 2026-03-28 — Auth SWA Config Complete ✅

- **`Client/wwwroot/staticwebapp.config.json`:** Added Microsoft (AAD) auth provider, route protection (`/*` → authenticated), 401 → login redirect, `/api/*` kept anonymous for v1, Blazor fallback routing preserved.
- **`Api/local.settings.example.json`:** Added `AAD_CLIENT_ID` and `AAD_CLIENT_SECRET` placeholders for local dev onboarding.
- **Decision filed:** `.squad/decisions/inbox/peter-auth-swa-config.md` — documents Azure portal steps Daniel must take (Entra app registration, redirect URI, app settings).
- **Issue #22 (SWA config) unblocked** → Glenn (#23 auth middleware) and Blair (#24 auth UI) can proceed.
- `/api/*` left anonymous at SWA level intentionally — D2 v1 scope, no OwnerId enforcement. Phase 2 handles API-level auth.

### Key Blocking Dependencies
- Sprint 0 blocks ALL feature sprints
- Auth chain: #22 → #23 → #24 (serial)
- Toast (#25) blocks shop deletion UX (#28) and future meal planning UI
- Nav fix (#26) blocks adding meal planning navigation entries
- CI tests (#18) and audit (#19) block full test rewrite (#33)
- Meal scoping (#29) blocks all meal implementation — Daniel must align on v1 first

### Labels Created on GitHub
- Squad labels: `squad:peter`, `squad:blair`, `squad:glenn`, `squad:ray`, `squad:josh`
- Priority labels: `P0`, `P1`, `P2`, `P3`

## 2026-06-01 — Landing Page for Signed-Out Users ✅

- **Landing page implemented:** Client/Pages/Landing.razor at /welcome with LandingLayout (no preload service).
- **SWA config updated:** /welcome made anonymous; 401 redirect changed from /.auth/login/aad to /welcome.
- **Sign-out flow fixed:** LoginDisplay.razor now redirects to /welcome instead of straight back to AAD login.
- **Key learnings:** MainLayout cannot be used for unauthenticated pages (calls authenticated API in preload). Always use a minimal layout. SWA anonymous route must appear before /* catch-all rule.

## 2026-06-XX — Sprint Closure: Auth Chain Verification ✅

**Issues Closed:**
- **#22 (Auth: Configure staticwebapp.config.json)** — CLOSED ✅
  - Work was pre-existing (committed 2026-03-28 in b2e32fe)
  - Microsoft (AAD) provider configured, routes guarded, 401→/welcome redirect, /api/* anonymous per D2 v1 scope
  
- **#23 (Auth: Parse x-ms-client-principal header)** — CLOSED ✅
  - Work was pre-existing (committed 2026-03-28 in 0c91d2e)
  - ClientPrincipal.cs + AuthExtensions.cs + ControllerBase helpers fully implemented
  - Safe parsing: handles null/malformed headers, never throws
  
- **#24 (Auth: LoginPage + AuthorizeRouteView)** — CLOSED ✅
  - Work was pre-existing (committed 2026-03-28 in 40ecc0f, refined 2026-03-30 in 5984891)
  - App.razor has AuthorizeRouteView with NotAuthorized block
  - LoginDisplay.razor integrated in nav, shows user + logout
  - SwaAuthenticationStateProvider reads /.auth/me endpoint
  - Program.cs has AddAuthorizationCore(FallbackPolicy=DefaultPolicy), full auth cascade

**Auth chain complete** — All three blocking issues verified and closed. Ready for downstream features (#25, #26, #28).

## 2026-XX-XX — Sprint 2 Planning Complete ✅

**Sprint 2 Scope:** 8 issues (2 P1, 6 P2) focused on UI/UX polish + testing infrastructure + data layer foundation.

**P1 Issues (Blocking):**
- **#33 (API controller test rewrite)** — Josh owner. Currently 65 tests mock everything, zero controller code executed. Refactor to test actual controller methods. Unblocks Sprint 7 + catches auth regressions.
- **#25 (Toast/notification system)** — Blair owner. Implements D5 Option A (custom INotificationService). Blocks #28 shop deletion UX. Estimated 1 day.

**P2 Issues (High-Value Feature Work):**
- **#27 (Mobile drag-and-drop → buttons)** — Blair owner. Implements D6 Option B. Replaces broken iOS drag with up/down button controls (accessible, reliable).
- **#28 (Shop deletion safeguards)** — Blair+Glenn owners. Implements D16. Multi-step confirm + cascade check for dependency safety. **Depends on #25.**
- **#29 (Meal planning v1 scoping)** — Peter owner. SCOPING ONLY (no code). Capture v1 requirements (text history parser + weekly suggestions). Aligns with D18. Must finalize scope with Daniel before v1 implementation.
- **#30 (i18n resource architecture)** — Blair owner. Implements D19. Add `.resx` files + LocalizationService for future English localization. UI stays Norwegian for v1.
- **#31 (LastModified migration endpoint)** — Ray+Glenn owners. Implements D10 Option A. Extract inline lazy migration from ShoppingListController → one-time admin endpoint. Improves GET performance.
- **#32 (ManageMyShopsPage completion)** — Blair owner. Implements D17 COMPLETE decision. Shelf/category list + reorder controls. Currently stub with title only. Uses #27 buttons. Moderate scope.

**Out of Sprint 2:**
- Auth chain (#22-#24) already complete + merged
- Sprint 0 bugs already complete
- Meal planning v1 implementation deferred until scope finalized (separate sprint)
- Sprint 3+ issues deferred

**Critical Dependencies:**
- #25 (toast) must complete before #28 (shop deletion UX) starts
- #27 (buttons) must complete before #32 (ManageMyShopsPage) starts
- #33 (tests) must complete before Sprint 7
- No cross-sprint blocking

**Team Effort Estimate:** 30-37 team-days (realistic for 2-week sprint, 5 developers, 80 available team-days)

**Technical Concerns Mitigated:**
- #33: Josh+Glenn establish test pattern on 1 controller first, then scale
- #25: aria-live accessibility + queue-based notification design required
- #28: Unit test cascade check before UI integration
- #32: Reuse #27 patterns for reorder controls
- #29: Explicitly scoping-only, separate implementation issue created

**Deliverables:**
- `.squad/agents/peter/sprint2-plan.md` — Detailed sprint roadmap (dependencies, owners, technical concerns)
- `.squad/decisions/inbox/peter-sprint2-scope.md` — Formal decision document (scope rationale, capacity analysis, approval gates)

**Next Steps:**
- Daniel reviews sprint plan + decision
- Daniel confirms scope + approves sprint start
- Peter creates GitHub Projects sprint board
- Team begins work in recommended sequence

## 2026-03-29 — Meal Planning v1 Scoping Complete ✅

**Issued by:** Daniel Aase (issue #29 — scoping-only ticket)

**Scope Document:** `.squad/agents/peter/meal-planning-v1-scope.md` — Comprehensive 12-section decision document

**v1 Definition (FINAL):**
- **NOT recipe CRUD** — text-based meal history viewer only
- Data model: `WeekMenuText` entity with `Dictionary<string, string> DailyMeals` (one key per day, free text)
- Firestore collection: `weekmenutexts` (follows D4 convention)
- UI: 2 pages (`WeekMenuTextListPage`, `WeekMenuTextPage`)
- Suggestion engine: Simple frequency-based picker from historical entries (no AI)
- No shopping list integration (v1 standalone)

**Implementation Tickets Ready (6 total):**
1. Backend data model + API controller (Ray + Glenn, 3-4 days)
2. Suggestion algorithm (Glenn, 2-3 days)
3. Frontend list page (Blair, 2-3 days)
4. Frontend editor page (Blair, 3-4 days, depends #25 toast)
5. Navigation integration (Blair, 0.5 days)
6. E2E tests (Josh, 1-2 days)

**Total Effort:** 13-16 team-days (2 weeks), can run Tickets 1-3 in parallel

**v1 vs v2 Boundary (CLEAR):**
- v1: Text entries + suggestions only
- v2 (future): Recipe CRUD + ingredients + shopping list generation
- Completely separate entities (`WeekMenuText` vs `WeekMenu`/`DailyMeal`/`MealRecipe`)
- No data migration path between v1 and v2

**Constraints Respected:**
- ✅ D3 (MealIngredient embedding) — N/A for v1
- ✅ D4 (Collection convention) — `weekmenutexts` automatic
- ✅ D18 (Meal v1 scope) — Confirmed text history + suggestions
- ✅ D19 (i18n) — UI Norwegian, code English

**Blocking Dependency:**
- #25 (Toast system) — Required for week editor user feedback

**All 6 Scope Questions Answered:**
1. Data format: Flat Dictionary with one key per day
2. Storage: Single entity per week with text dictionary
3. Suggestion: Frequency-based random picker from history
4. UI: 2 pages (list overview + week editor)
5. Import: Manual text entry only (file upload v2+)
6. Integration: Standalone (shopping list v2+)

**Next Steps (Blocked on Daniel):**
- Daniel reviews scope document
- Daniel approves 6 implementation tickets
- Peter creates GitHub issues from ticket definitions
- Team begins Sprint 4 implementation (Tickets 1-3 can start immediately after approval)

## 2026-04-03 — Sprint 2 Complete ✅

**Lead:** Scribe (documentation), Peter (verification)

**Scope:** 8 issues (UI/UX polish + testing + data layer foundation)

### Issues Closed (All Verified)

| # | Owner(s) | Component | Status |
|---|----------|-----------|--------|
| **#25** | Blair | Frontend | ✅ Toast/Notification System — Custom INotificationService + ToastContainer |
| **#27** | Blair | Frontend | ✅ Mobile Controls — Up/down buttons replacing drag-drop |
| **#28** | Blair + Glenn | Frontend + API | ✅ Shop Deletion Safeguards — Dependency check endpoint + 2-step UI |
| **#31** | Ray + Glenn | Data + API | ✅ LastModified Migration — Admin endpoint, inline migration removed |
| **#29** | Peter | Architecture | ✅ Meal v1 Scoping — Text history + suggestions, 6 tickets ready |
| **#32** | Blair | Frontend | ✅ ManageMyShopsPage — Shelf/category management, deletion, stats |
| **#30** | Blair | Frontend | ✅ i18n Architecture — Resource files + IStringLocalizer pattern |
| **#33** | Josh | Testing | ✅ API Controller Tests — 122 passing, real methods, no mocks |

### Highlights by Developer

**Blair (Frontend, 5 issues):**
- **#25 Toast System:** Event-driven service with auto-dismiss (3-5s per type), stacking, mobile-responsive CSS
- **#27 Mobile Buttons:** Up/down shelf reorder + category `+` assignment button (touch-friendly)
- **#32 ManageMyShopsPage:** Full implementation with inline shelf editor, category assignment, stats dashboard
- **#30 i18n Architecture:** Resource files (Norwegian + English template), page-scoped key convention, ShoppingListMainPage proof-of-concept

**Glenn (API Backend, 2 issues):**
- **#28 API Endpoint:** `GET /api/shop/{id}/dependencies` for cascade safety check
- **#31 Migration Endpoint:** `GET /api/admin/migrate-lastmodified` with admin role gating, idempotent design

**Ray (Data Layer, 1 issue):**
- **#31 Data Analysis:** Verified repository interface sufficient for migration; no code changes needed

**Josh (Testing, 1 issue):**
- **#33 Test Rewrite:** 122 passing tests with real controller instantiation, TestHttpFactory pattern, all error paths

**Peter (Architecture, 1 issue):**
- **#29 Meal v1 Scope:** Text-based weekly meal planner, frequency suggestions, 6 implementation tickets defined

### Decision Documents Merged

| Doc | Scope | Implementation |
|-----|-------|-----------------|
| **D5** | Toast System | INotificationService + ToastContainer (Option A) |
| **D6** | Mobile Controls | Up/down buttons as primary, drag as enhancement (Option B) |
| **D10** | Migration | One-time admin endpoint (Option A) |
| **D18** | Meal v1 | Text history + frequency suggestions (text-based only) |
| **D19** | i18n | Resource file architecture + IStringLocalizer pattern |
| **D30** | Shop Dependencies | Dependency check endpoint for safe deletion |
| **D31-D35** | Sprint 2 | New decisions for all completed features + analysis |

### Metrics

| Metric | Value |
|--------|-------|
| Issues Closed | 8 |
| Story Points | 46 |
| API Tests | 122 ✅ |
| Client Tests | 61 ✅ |
| E2E Tests | 20+ ✅ |
| Decision Docs | 7 merged (D31-D35 new) |
| Code Quality | No regressions |
| Downstream Unblocks | 6 meal planning tickets ready |

### Key Learnings

1. **Test Infrastructure Critical:** Moving from mock-based to real-controller tests caught potential regressions.
2. **Decision Isolation:** Scoping meal v1 separately prevented architecture creep — v1 launches without recipe CRUD.
3. **Mobile-First CSS:** Touch detection (`pointer: coarse`) simpler than JS polyfills for UI visibility.
4. **i18n Early:** Resource file setup upfront saves rework when English needed later.
5. **Admin Endpoint Pattern:** Extracting lazy migration follows SRP — repeatable for future migrations.
6. **Data Layer Analysis:** Ray's audit confirmed existing interfaces sufficient; no premature abstraction.

### Downstream Unblocks

- ✅ **Meal Planning v1 Implementation:** 6 tickets ready (1-3 parallel, then 4-5, then 6)
- ✅ **i18n English Localization:** Resource files ready for translation
- ✅ **Admin Panel Development:** Migration endpoint pattern established
- ✅ **Shop Management UI:** Full shop detail page complete + deletion safeguards

### Sprint 3 Prep

- **Meal Planning v1 Implementation:** 6 tickets can begin immediately
- **Auth Phase 2:** Per-user data isolation (OwnerId → FamilyId scoping)
- **Performance:** Caching strategies for frequently accessed shops/shelves

**Status:** ✅ COMPLETE — Ready for Daniel review + production deployment

## Learnings — PR #43 Review (2026-04-03)

- **i18n in Blazor WASM:** `AddLocalization` alone is not enough. Must set `CultureInfo.DefaultThreadCurrentUICulture` explicitly — otherwise defaults to browser/OS culture and `IStringLocalizer` returns raw key names. Always pin culture in `Program.cs` for single-language apps.
- **SWA anonymous routes are order-sensitive:** Every route that must be accessible unauthenticated — including the landing/welcome page — must be listed BEFORE the `/*` catch-all rule. The 401 `responseOverride` redirect must point to the landing page (`/welcome`), not directly to `/.auth/login/aad`.
- **Firestore embedded objects may lose document IDs:** When categories are embedded as arrays inside shop/shelf documents in Firestore, the Firestore document ID is NOT automatically stored as a property. Any UI logic comparing embedded `Id` to top-level collection `Id` must verify the Id is correctly serialized into the embedded object's `Id` field (e.g., via `[FirestoreDocumentId]` or explicit Id assignment before saving).
- **Pre-existing build environment issue:** `Shared.csproj` fails to build locally with `MSB3492 / CoreCompile` error. This is an environment-level issue (locked cache file) and pre-dates PR #43. Does not affect CI/CD pipeline builds.

## Learnings — Auth-Aware Nav + Index Assessment (2026-06-XX)

- **Nav auth wrapping — consolidate, don't scatter:** The pre-existing nav had the logout block in its own `<AuthorizeView>` while nav links were unprotected. The correct pattern is a **single** `<AuthorizeView>` wrapping ALL authenticated nav content. Scattered wrappers create gaps and are harder to reason about.
- **`<NotAuthorized>` in nav should be a single login CTA:** Unauthenticated users need exactly one action — log in. `🔐 Logg inn → /.auth/login/aad?post_login_redirect_uri=/` is sufficient. No need for descriptive text or multiple links in the nav.
- **Index.razor null-check anti-pattern — check before changing:** Task 2 was assigned to remove a null-check from `Index.razor`, but reading the file first revealed it was already clean (just `<ShoppingListMainPage>`). Always read the file first; don't apply changes based on assumptions about "current state."
- **Auth defence-in-depth:** The auth chain has four layers (SWA config → `AuthorizeRouteView` → `FallbackPolicy` → `<AuthorizeView>` in nav). Each layer is independent — no single point of failure. This is the correct architecture for Blazor WASM on Azure SWA.
- **`Index.razor` is protected at a higher level:** Because `App.razor` has `<AuthorizeRouteView>` with `<NotAuthorized><RedirectToWelcome /></NotAuthorized>` and `Program.cs` has `FallbackPolicy = DefaultPolicy`, adding `<AuthorizeView>` to `Index.razor` would be redundant. Don't gold-plate.
