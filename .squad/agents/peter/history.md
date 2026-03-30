# Project Context

- **Owner:** Daniel Aase
- **Project:** Shoppinglist — Blazor WebAssembly shopping list app with shop-specific item sorting
- **Stack:** Blazor WebAssembly (.NET 9), Azure Functions v4, Google Cloud Firestore, Syncfusion UI, Playwright E2E tests
- **Created:** 2026-03-22

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
- App uses a dual-model pattern: `FireStoreDataModels` (with Firestore attributes) and `HandlelisteModels` (DTOs). AutoMapper with `.ReverseMap()` bridges them.
- Core shop-specific sorting runs client-side in `OneShoppingListPage.razor` via `SortShoppingList()`.
- Norwegian property names (`Varen`, `Mengde`, `ItemCateogries`) must be preserved — backward compatibility with Firestore data.
- Current goals: add authentication/security and improve performance.
- Auth provider changed to Microsoft-only — GitHub dropped per Daniel (2026-03-23)
- Family sharing model clarified: v2 uses FamilyId group ownership, NOT individual OwnerId scoping
- Meal planning v1 is text-history based suggestion engine, NOT recipe CRUD — separate scoping issue required
- i18n: Norwegian UI is intentional for v1; add resource file architecture for future English localization
- Norwegian Firestore property names (Varen, Mengde, ItemCateogries) are permanent data constraints
- Shop deletion requires safeguard UX (multi-step confirm + dependency check)
- ManageMyShopsPage (D11): decision is to COMPLETE the page, not remove it
- Sprint 0 (P0 bugs) successfully completed: 6 merged PRs (#34-#38) closed issues #16-#21 (2026-03-23)
- **REGRESSION FOUND (2026-03-23):** Frequent lists invisible due to collection key fix NOT merged to main. PR #35 has fix, but `main` branch still uses hardcoded switch defaulting FrequentShoppingList to `misc` collection. Fix committed in bb35ee2 but not cherry-picked to production branch.

### 2026-03-27 — Regression Fixes Complete ✅
- **D7 (Admin Nav Accessibility):** ✅ IMPLEMENTED by Blair. Converted CSS `:hover`-only dropdown to Blazor `@onclick` toggle with `_adminOpen` state. Added ARIA attributes. Updated app.css to support both hover and click-toggle. Frequent lists now accessible on mobile.
- **D4 (Collection Key Convention):** ✅ IMPLEMENTED by Ray. Replaced hardcoded switch in `GoogleDbContext.GetCollectionKey()` with convention: `typeof(T).Name.ToLower() + "s"`. Two backward-compat overrides: `Shop → shopcollection` (legacy), `ItemCategory → itemcategories` (irregular plural). Created `POST /api/admin/migrate-frequent-lists` endpoint for one-time data recovery from "misc" collection. Migration must run before D4 merge to main.
- **D9 (MealIngredient DI):** ✅ RE-APPLIED by Ray. Removed orphaned `IGenericRepository<MealIngredient>` DI registration (was marked complete 2026-03-23 but not actually applied). Now correctly implements D3 (embedding strategy).
- **Verification:** All tests passing (90 API + 61 Client). No regressions introduced.

## PRD Synthesis — 2026-03-22

### Key Architectural Decisions
- **AD-1:** Auth via Azure SWA built-in authentication (GitHub + Microsoft providers). No Firebase Auth — avoids mixing cloud ecosystems.
- **AD-2:** Hybrid data isolation — shared product catalogue (ShopItem, ItemCategory), per-user lists/shops/menus via `OwnerId` field.
- **AD-3:** MealIngredient stored embedded in MealRecipe, not as separate collection. Remove standalone MealIngredient repository.
- **AD-4:** WeekMenu uses recipe ID references, not full MealRecipe embedding. Prevents document bloat.
- **AD-5:** Convention-based collection key fallback to prevent future `"misc"` collection bugs.
- **AD-6:** Error message scrubbing in production — generic messages to callers, full details to ILogger.

### Critical Bugs Found
1. `GetCollectionKey()` maps 5 entity types (FrequentShoppingList, MealRecipe, MealIngredient, WeekMenu, DailyMeal) to `"misc"` — active data corruption risk.
2. WeekMenu/DailyMeal have no DI registration — entire feature is unwired.
3. All 65 API tests call mocks directly, not controller methods — zero controller code tested.
4. No tests run in CI — GitHub Actions builds but never runs `dotnet test`.
5. `ShopItemCategoryController.RunOne` has no try/catch — unhandled exceptions leak to callers.
6. `ShopsController` uses `.Result` instead of `await` — potential deadlocks.

### GitHub Issue Filed
- **Issue #15**: PRD: Shoppinglist App — Next Evolution (Auth, Meal Planning, UI, Performance)
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
