# Project Context

- **Owner:** Daniel Aase
- **Project:** Shoppinglist — Blazor WebAssembly shopping list app with shop-specific item sorting
- **Stack:** Blazor WebAssembly (.NET 9), Azure Functions v4, Google Cloud Firestore, Syncfusion UI, Playwright E2E tests
- **Created:** 2026-03-22
- **Status:** Sprint 2 complete (8 issues, 122 tests passing)

## Core Context — Learnings & Decisions (Archived)

**See `.squad/decisions.md` for full decision history (D1–D35).** This section summarizes core learnings through Sprint 2.
<!-- Append new learnings below. Each entry is something lasting about the project. -->
- **PR #67 Triage (2026-04-23):** Daniel left 9-point feedback on PR #67. Triaged into 3 bugs (#68, #69, #70 — all frontend: inventory adjust, family edit, ingredient edit) and 6 features (#71–#76 — portion duplication, smart suggestions, basic item grouping, meal consumption tracking, meal-sourced tagging, purchase unit sizes). Bugs are P1 (block merge); features are P2 (backlog). Decision filed to `.squad/decisions/inbox/peter-pr67-triage.md`.
- **Smart Shopping List meta-feature identified:** Issues #73 + #75 + #76 are tightly coupled (basic items group + meal-source tagging + purchase units). Must be designed as a unit to avoid conflicting ShopItem/ShoppingListItem model changes.
- **PR #67 Round 2 Feedback (2026-04-23):** Daniel posted 3 follow-up comments after sprint review. (1) Bugs confirmed working ✅. (2) New requests: undo-consume on ukemeny (#81), unit-as-dropdown in vare-admin (#82), mobile responsiveness (#83). (3) Clarification: IsDone stock hook is correct at list level — N+1 is about writes within that operation, not trigger level. (4) Requested issues for all 4 sprint-review follow-up points → created #77 (IsBasic P1), #78 (transaction P3), #79 (N+1 P3), #80 (unused enums P4). Total 7 new issues created. Replied on PR in Norwegian.
- **PR #67 Round 3 Feedback (2026-04-24):** Daniel's latest comment: "We don't need a button to 'bytt' dinner — just select another in the list, and when dinner sets to 'eaten' that's when we lock it." UX simplification: remove the `🔄 Bytt` button, `_swappingDay` state, and swap-dropdown entirely. Regular `<select>` dropdown stays editable until `IsConsumed == true`. Created **#84** for this. Replied on PR.
- **Triage: Issues #84–#77 (2026-04-24):** Completed triage of 8 untriaged issues from PR #67 feedback cycle. **Assignments:** Blair (#84 UI simplify, #83 mobile UX, #82 dropdown UX), Glenn (#81 unconsume feature, #80 enum cleanup, #78 transaction safety, #77 P1 IsBasic bug), Ray (#79 N+1 Firestore optimization). **Priorities:** 1 P1 bug (#77), 6 P2 features/debt. Key insight: N+1 writes acceptable for v1 (typical lists 5–15 items); batch pattern is future optimization. Stock is explicitly approximate ("Estimert lager"). Summary filed to `.squad/decisions/inbox/peter-triage-summary-issues84-77.md`.
- **Inventory lifecycle pattern:** Plan → Shop → Stock → Cook → Deduct forms a closed loop across issues #74, #75, #76. Architecture spike recommended before implementation.
- **Issue #75 Design — StockBehaviour (2026-04-23):** Daniel's feedback flipped the framing from "tag meal-sourced items to stock" to "exclude staples from stock". Decided: single `StockBehaviour` enum (`Track`/`DoNotTrack`) on `ShopItem` (item master), NOT on `ShoppingListItem`. Drops `IsMealSourced` from original spec. Key insight: stock behaviour is a property of the item, not the shopping list row. Inventory is explicitly positioned as approximate ("Estimert lager") — trend accuracy, not exact counts. Decision filed to `.squad/decisions/inbox/peter-issue75-stockbehaviour.md`.
- **Sprint Review — PR #67 feedback sprint (2026-04-23):** All 3 bugs (#68, #69, #70) fixed. 6 features (#71, #73, #74, #75, #76) implemented. #72 (smart menu suggestion) skipped per Daniel. 220 unit tests passing. Key architectural additions: `StockBehaviour` enum on ShopItem, `IsConsumed` on DailyMeal, `IsLikelyNotNeeded` on ShoppingListItem, IsDone-hook for auto-stocking. Full inventory lifecycle now operational: Plan → Shop → Stock → Cook → Deduct.
- **BUG FOUND — `Varen.IsBasic` not populated in generated shopping lists:** `WeekMenuController.RunGenerateShoppingList()` constructs `ShopItemModel` with only Id/Name (line 265). `IsBasic` defaults to `false`, so the frontend grouping `i.Varen?.IsBasic == true` in `OneWeekMenuPage.razor` is dead code for generated lists. Only `IsLikelyNotNeeded` items reach the bottom group. Fix needed: populate `IsBasic` from ShopItem catalogue during aggregation, or fetch full ShopItem records.
- **Pattern: Request DTOs in controller file** — `ConsumeMealRequest`/`SwapMealRequest` defined at namespace level in `WeekMenuController.cs`. Acceptable for v1 (server-only deserialization). If frontend ever needs to construct these typed objects, move to Shared.
- **N+1 write pattern in IsDone-hook:** `ShoppingListController` iterates items and does individual Firestore writes per item when a list is completed. Acceptable for typical list sizes (5-15 items). Flag for optimization if lists grow large.
- **CRITICAL RULE — Azure Functions AuthorizationLevel**: ALL Azure Functions in this project MUST use `AuthorizationLevel.Anonymous`. Azure SWA does NOT inject function keys when proxying `/api/*` to the Functions backend. `AuthorizationLevel.Function` causes 401 responses → SWA redirects to `/welcome` (HTML) → client JSON parsers see `<` at byte 0 → `ExpectedStartOfValueNotFound` crash. (2026-03-28)
- **CRITICAL RULE — Staging Firestore guard**: `Api/Program.cs` must check BOTH `GOOGLE_CLOUD_PROJECT` AND `GOOGLE_APPLICATION_CREDENTIALS` before using production Firestore repos. Without both, `GoogleFireBaseGenericRepository` throws at constructor time, crashing the entire Functions host → all `/api/*` return HTML. The `useMemoryDb` fallback is the safety net for staging. (2026-03-28)
- **Startup crash signature**: `ManagedError: AggregateException (ExpectedStartOfValueNotFound, < LineNumber: 1 | BytePositionInLine: 0)` at `callEntryPoint` = an API endpoint called at startup is returning HTML instead of JSON. Check: (1) AuthorizationLevel.Function on any controller, (2) Functions host crash due to missing Firestore credentials. (2026-03-28)
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
## Core Context — PRD Synthesis & Initial Phase (2026-03-22 to 2026-03-23)

**Scope & Architecture:**
- Auth via Azure SWA (Microsoft AAD provider) — no Firebase Auth
- Hybrid data isolation: shared catalogue (ShopItem, ItemCategory), per-user lists via OwnerId
- MealIngredient embedded in MealRecipe (no separate repo)
- WeekMenu uses recipe ID references (no embedding)
- Convention-based Firestore collection keys to prevent "misc" corruption
- Error scrubbing: generic messages to callers, full details to ILogger

**P0 Bugs Found & Fixed (Sprint 0):**
- ✅ GetCollectionKey() mapped 5 entity types to "misc" — fixed by Ray (D4 convention)
- ✅ WeekMenu/DailyMeal DI registration missing — fixed by Ray (D9)
- ✅ 65 API tests called mocks, not controllers — fixed by Josh (real controller tests)
- ✅ No CI test execution — fixed by Josh (added dotnet test step)
- ✅ ShopItemCategoryController missing try/catch — fixed by Glenn
- ✅ ShopsController used .Result blocking call — fixed by Glenn

**GitHub Issues (#15-#33):** Issue #15 synthesized PRD into 18 sub-tickets across 7 sprints (P0 bugs, Auth, UI, Shop, Meal, i18n, Data, Testing). All P0 (#16-#21) completed and merged.

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

## 2026-04-24–2026-04-26 — Package Size Feature Design (D36) ✅ COMPLETE

### Design Phase (Peter)
- **Scope:** Enable package-aware shopping list generation. Recipe needs 400g chicken → chicken comes in 500g packages → buy 1 package (not raw 400g).
- **Discovery:** StandardPurchaseQuantity (double) and StandardPurchaseUnit (string) already exist on ShopItem from issue #76. No new data model changes needed.
- **3-part solution identified:**
  1. **Unit bridge (Glenn):** MealUnitExtensions methods for compatibility check & normalization
  2. **Backend calculation (Glenn):** WeekMenuController.RunGenerateShoppingList() package conversion with stock comparison
  3. **Display layer (Blair):** Format `{Mengde} × {qty}{unit}` in UI
- **Design document filed:** `.squad/decisions/inbox/peter-package-size-design.md` → merged as **D36** to main decisions.md
- **Key insight:** Package size lives on ShopItem (item master), not ShoppingListItem. Fallback to Math.Ceiling when data unavailable or units incompatible.

### Implementation Coordination (Peter coordinating Glenn + Blair)
- **Glenn's PR #89:** MealUnitExtensions (4 methods) + WeekMenuController update with aggregation tuple extended to carry MealUnit. Pipeline order CRITICAL: stock comparison → package conversion (both mutate Mengde). 26 unit tests + 3 integration tests. **211 total, 0 failures.**
- **Blair's PR #88:** OneShoppingListItemComponent display + OneWeekMenuPage FormatQuantity() helper. Package info when StandardPurchaseQuantity > 0.
- **Blair's PR #87 (bonus):** SfComboBox pattern enforcement (D31 update) + UX fixes (OK/Avbryt buttons, Norwegian labels, "Er alltid hjemme" clarification).
- **Parallel work streams:** No blocking dependencies between Glenn and Blair. Both can merge independently.
- **Decisions merged:** D36, D36.1, D36.2, D36.3 (sub-decisions), D31 update, D31.1 (label fix) → all to main decisions.md.

### Test Coverage
- Unit: MealUnitExtensionsTests (11 for compatibility + normalization)
- Unit: ShoppingListExtensionsTests (26 for package calculation)
- Integration: WeekMenuControllerTests (3 scenarios: with package, without, incompatible units)
- **Total test delta:** 162 → 211 tests (49 new), **0 failures**
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

---

## 2026-04-04 — Basevarer (Base Ingredients) Feature Added to Phase 2 ✅

**Context:** Daniel Aase posted comment on #29 (2026-04-03 19:56:59 UTC) identifying a gap in meal planning — certain ingredients assumed to be "in stock" (oil, salt, butter) may be out of stock, causing incomplete shopping trips.

**Peter's Response:** Acknowledged and scoped as minimal Phase 2 delta. Posted acknowledgment comment: https://github.com/danielaase1337/shoppinglist/issues/29#issuecomment-4185006585

**Changes Made:**

1. **Updated `MealIngredient` data model:**
   - Added `IsBasic: bool` property (default `false`)
   - Set during recipe creation to flag base ingredients (oil, salt, butter, flour, sugar, baking powder, etc.)

2. **Updated Phase 2 shopping list generation logic:**
   - Separates ingredients into two groups: **Primary** (needed for this week's meals) and **Basevarer** (suggested base items)
   - Inventory deduction applies only to primary items
   - Basevarer items output as pre-checked checkboxes in review UI
   - User selects which basevarer to include before finalizing the list

3. **Updated `ShoppingListModel` (Shared):**
   - Added `BaseIngredients: ICollection<ShoppingListItemModel>` property
   - Backward compatible (nullable)

4. **Phase 2 UI workflow:**
   - Review modal shows two sections: "Ingredienser denne uken" (Primary) and "Basevarer — huk hva du trenger" (Suggested Base Items)
   - Basevarer pre-checked with checkboxes — user uncheck to exclude or leave checked to add
   - Prevents "forgot the oil" situations

5. **Decision Record:** Filed `.squad/decisions/inbox/peter-basevarer-delta.md` with full design, implementation details, and testing plan.

6. **Scope Document:** Updated `.squad/agents/peter/meal-planning-v1-scope.md` Phase 2 section with basevarer workflow.

**Impact Assessment:**
- ✅ Minimal delta (no new entities, no new collections)
- ✅ Backward compatible (defaults to `false`, nullable output)
- ✅ Unblocks meal planning Phase 1 implementation
- ✅ Team-relevant decision recorded

**Status:** Phase 2 scope now finalized. Ready for Phase 1 implementation tickets (D43 pending Daniel approval on overall direction).
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
- **Index.razor is protected at a higher level:** Because `App.razor` has `<AuthorizeRouteView>` with `<NotAuthorized><RedirectToWelcome /></NotAuthorized>` and `Program.cs` has `FallbackPolicy = DefaultPolicy`, adding `<AuthorizeView>` to `Index.razor` would be redundant. Don't gold-plate.


## Learnings — Issue Triage Sprint (2026-05-29)

- **P1 bug in data layer blocks frontend:** Issue #77 (IsBasic not populated in generated shopping lists) reveals that backend aggregation logic must populate all fields needed for frontend UI logic. The generateShoppingList endpoint constructs minimal DTOs; frontend grouping assumes IsBasic is set. **Fix strategy:** Populate from catalogue during aggregation or fetch full records.
- **N+1 writes acceptable for v1 (but flagged):** IsDone-hook does individual Firestore writes per item. Acceptable for typical list sizes (5–15 items). Flagged as #79 (batch optimization future). Decision: prioritize correctness and UX over batch efficiency in v1.
- **Stock is explicitly approximate ("Estimert lager"):** This is the correct framing for a family app with deferred deduction (consume at cook time, not shop time). Partial state (meal marked eaten but inventory update fails) is acceptable. Removes pressure for transaction guarantees.
- **UI simplification is a valid triage outcome:** Issue #84 (remove swap button) is not a bug fix, but a UX simplification that unblocks better meal selection flow. Sometimes the right answer is to remove complexity rather than add features.
- **8 issues triaged in 1 session:** Issues #84–#77. Assignments split 3 UI / 4 backend / 1 Firebase. **Key decision:** P1 bug (#77) must be fixed before PR #67 merge. Team capacity clear.

**Learnings — Issue #77 Root Cause Analysis**
- `WeekMenuController.RunGenerateShoppingList()` (line 265) constructs ShopItemModel with only Id/Name
- IsBasic defaults to false → frontend grouping logic `i.Varen?.IsBasic == true` is dead code for generated lists
- Only `IsLikelyNotNeeded` items appear in bottom group (handled separately via API)
- **Lesson:** Frontend grouping logic that depends on API response fields must be validated end-to-end. Add test: generate list → verify field population.



- **Slim v1 was thrown away — correctly:** The text-history + frequency-suggestions scope (WeekMenuText entity) was superseded before a single line of implementation code was written. Scoping separately from implementation gave us the flexibility to pivot cleanly. This validates the "scoping ticket before implementation ticket" pattern.
- **Business rules belong in scope doc, not just in backlog:** Pizza Fridays, Thursday-Thursday week, and fresh ingredient perishability are hard rules that shape every phase of the data model. Capturing them explicitly in the scope doc before architecture starts prevents accidental omission.
- **Unit system is a cross-cutting concern — freeze it first:** MealUnit enum is used by MealIngredient (Phase 1) and InventoryItem (Phase 4). Designing it once in Shared before any coding starts prevents the coordination cost of retrofitting units across two phases mid-sprint.

## Learnings — Package Size Feature Design (2026-04-24)

- **#76 purchase unit fields already exist on ShopItem:** `StandardPurchaseQuantity` (double) and `StandardPurchaseUnit` (string) were added in the #76 sprint by Ray. Both Firestore and DTO models have them. ItemManagementPage already has edit fields for them. The data model layer is DONE.
- **Key gap is package-aware calculation in generate-shoppinglist:** Current `WeekMenuController.RunGenerateShoppingList()` uses `Math.Ceiling(totalQuantity)` for Mengde. It does NOT use `StandardPurchaseQuantity` to calculate how many packages to buy. This is the core missing logic.
- **Unit compatibility is the hard problem:** MealIngredient uses `MealUnit` enum (Gram, Kilogram, etc.) while ShopItem uses `StandardPurchaseUnit` string ("kg", "stk", "l"). These must be bridged via `MealUnitExtensions.ToNorwegian()` for comparison, or a new conversion helper.
- **Smallest correct solution:** Add package calculation ONLY in `RunGenerateShoppingList()` — no new models, no new endpoints, no new pages. Just math: `packagesNeeded = ceil(totalQuantity / packageSize)` when units are compatible.
- **Migration is a non-issue:** `StandardPurchaseQuantity` defaults to 0, code already treats 0 as "not set". Existing items gracefully degrade to current behavior (ceil of raw quantity).
- **Phase 4 (Inventory) can parallel-track with Phase 2 (Week Planning):** The inventory deduction step in shopping list generation degrades gracefully — if Phase 4 is not done when Phase 2 ships, generation simply produces a full list without stock deduction. This is an explicit design decision, not a bug.
- **Ingredient matching is client-side, not an API concern:** The "use up half broccoli" logic reads the already-loaded recipe catalogue in memory. No new endpoint. No round-trip. Keep computation where the data is.
- **Import flow: JSON bulk endpoint beats file upload UI:** For a one-time Google Keep import, building a JSON bulk POST endpoint is 20x faster than a file-upload-and-parse UI. Joshua handles the text-to-JSON conversion manually. One-time effort does not justify a general-purpose import tool.
- **IsDone trigger for inventory must guard against double-fire:** The ShoppingList.PUT endpoint must check `!existing.IsDone && updated.IsDone` before calling the inventory service. A plain `if (updated.IsDone)` re-adds stock on every subsequent PUT to an already-completed list.

## Learnings — Meal Planning Scope Refinements (2026-04-03)

- **Hard rules aren't always hard — revisit them before implementation:** "Pizza Friday is locked" was in the scope doc for less than an hour before Daniel revised it to "suggested default, overridable." The `IsSuggested` flag pattern is more honest than `IsLocked`: it preserves intent (pizza on Fridays) without removing agency. Always validate lock semantics with the product owner before they bake into a data model.
- **Two properties for one concept (PrepTime + Effort):** Capturing a raw number (`PrepTimeMinutes`) alongside a bucketed enum (`MealEffort`) serves two different consumers — display (show "25 min") and filtering ("show only Quick"). Don't collapse them; they're orthogonal concerns.
- **Frozen meals belong in inventory, not a separate entity:** The instinct to create a "FrozenMeal" entity is wrong. A frozen lasagne is just an `InventoryItem` that knows which recipe created it (`SourceMealRecipeId`). Reusing the inventory model keeps the data model lean and the Phase 4 build necessary for two features at once.
- **Phase coupling is sometimes intentional:** The frozen meal workflow requires both Phase 1 (MealType on recipe) and Phase 4 (inventory). This is documented explicitly — not treated as a dependency problem, but as a feature that degrades gracefully when Phase 4 is not yet live.
- **FamilyProfile is a separate concern — never embed it in a meal entity:** Portion rules apply across all meals, not to any specific recipe. Storing them separately (own collection, own admin page) keeps meal recipes portable and household-agnostic. This is the same logic that keeps `ShopItem` global even though ShoppingLists are per-family.
- **Open questions should stay open in the scope doc:** Daniel hasn't answered the automatic vs manual portion scaling question yet. Forcing a decision before the product owner responds is gold-plating. Mark it as pending, ship the rest of Phase 5, and wire up the scaling mode when the answer arrives.
- **Decisions compound: D36 reversal shows scope docs must be living documents:** A scope doc that can't be edited is a liability. The meal planning scope doc now tracks its own revision history. Future sprints should treat scope refinements as first-class updates, not footnotes.

## 2026-04-03 — Full Meal Planning Scope Locked ✅ (D40–D42)

**Triggered by:** Daniel comment on issue #29 (comment 4184947107) — portion scaling confirmed semi-automatic.

### Decisions Filed

- **D40:** `BasePortions` (int, default 4) added to `MealRecipe`. Represents the recipe's intended serving size as written. Set during import; editable in recipe UI. Firestore property: `BasePortions`.
- **D41:** Portion scaling is **semi-automatic** — suggested, overridable. Formula: `scalingFactor = FamilyMemberCount / BasePortions`. Week planner shows pre-filled scaled quantities in a review step before list is saved. User can adjust any line. If no `FamilyProfile`, quantities used as-is.
- **D42:** Full meal planning scope **finalized** across all 5 phases. No open questions remain. Ready for Phase 1 GitHub implementation tickets on Daniel's go-ahead.

### Scope Document Updated
- `meal-planning-v1-scope.md`: Status changed to ✅ FINALIZED, `BasePortions` added to `MealRecipe` model, Phase 5 scaling section updated with workflow, success criteria updated, approvals section signed off.
- Decision record: `.squad/decisions/inbox/peter-portion-scaling-final.md`

### Key Learning
- **Semi-automatic = the right default for household apps:** "Suggested but overridable" threads the needle between convenience and flexibility. Guest dinners, special occasions, or just "we're extra hungry tonight" all require override capability. Automatic-only would frustrate; manual-only would be ignored. The IsSuggested/override pattern generalises well across this app.

## 2026-04-XX — Meal Planning GitHub Issues Created ✅

**Triggered by:** Daniel Aase request to create concrete implementation tickets from finalized scope.

### Labels Created
- `meal-planning`, `phase-1`, `phase-2`, `phase-3`, `phase-4`, `phase-5`, `backend`, `frontend`, `shared`

### Milestones Created
- Meal Planning Phase 1 (milestone #1)
- Meal Planning Phase 2 (milestone #2)
- Meal Planning Phase 3 (milestone #3)
- Meal Planning Phase 4 (milestone #4)
- Meal Planning Phase 5 (milestone #5)

### Phase 1 Issues Created (13 total — ready to start immediately)

| Issue | Title |
|-------|-------|
| #46 | [Shared] Add MealUnit enum to Shared project |
| #47 | [Shared] Add MealRecipe and MealIngredient Firestore data models |
| #48 | [Shared] Add MealRecipeModel and MealIngredientModel DTO models |
| #49 | [Shared] Extend AutoMapper profile for MealRecipe ↔ MealRecipeModel mapping |
| #50 | [Api] Register MealRecipe repository in Program.cs |
| #51 | [Api] Create MealRecipeController with CRUD endpoints |
| #52 | [Client] Add MealRecipe enum keys to ShoppingListKeysEnum |
| #53 | [Client] Add MealRecipe URL mappings to ISettings |
| #54 | [Client] Create MealManagementPage — recipe list and management |
| #55 | [Client] Create OneMealRecipePage — recipe detail and ingredient editor |
| #56 | [Client] Add Meals link to Admin navigation dropdown |
| #57 | [Api.Tests] Add MealRecipeController unit tests |
| #58 | [Client.Tests] Add MealManagementPage component tests |

### Phase 2–5 Issues Created (8 total — queued for future sprints)

| Issue | Title |
|-------|-------|
| #59 | [Phase 2] WeekMenu + DailyMeal models and CRUD API |
| #60 | [Phase 2] WeekMenu UI + shopping list generation from meals |
| #61 | [Phase 3] InventoryItem model and pantry CRUD API |
| #62 | [Phase 3] Inventory deduction on shopping list completion + Pantry UI |
| #63 | [Phase 4] Ingredient matching suggestions and perishable scheduling warnings |
| #64 | [Phase 4] Meal variety suggestion engine with category balancing |
| #65 | [Phase 5] Google Keep meal history import tool |
| #66 | [Phase 5] Family profile support — OwnerId isolation for meal planning |

**Summary comment posted on #29.** Manifest filed at `.squad/decisions/inbox/peter-phase1-issues-created.md`.

## 2026-06-XX — mealplanningv2 PR Opened + Issues Closed ✅

**Requested by:** Daniel Aase

### PR Created

- **PR #67:** feat: Full meal planning system — Phases 1–5
- **URL:** https://github.com/danielaase1337/shoppinglist/pull/67
- **Branch:** `mealplanningv2` → `main`
- **Test suite:** 177+ passing, 0 build errors

### Issues Closed (20 total)

All Phase 1–5 implementation issues commented with delivery summary and closed referencing PR #67:

- **#29** — Meal planning scoping (superseded by full v2 implementation)
- **#46–#58** — All Phase 1 MealRecipe issues (shared models, DTOs, AutoMapper, controller, client pages, tests)
- **#59–#60** — Phase 2 WeekMenu issues (models + UI with Thu–Wed calendar + generate shopping list)
- **#61–#62** — Phase 4 Inventory issues (InventoryItem CRUD + IsDone→stock hook + InventoryItemsPage)
- **#63** — Phase 3 Ingredient matching suggestions (client-side use-up / fractional matching)
- **#66** — Phase 5 Family profile (portion scaling delivered; OwnerId isolation deferred per D2)

### Issues Left Open (not in this PR)

- **#64** — Meal variety suggestion engine with category balancing — future sprint
- **#65** — Google Keep meal history import tool — future sprint

### Key Observations

- The original v1 scope (text-history WeekMenuText entity) was correctly abandoned before implementation. The full recipe CRUD + WeekMenu + Inventory + FamilyProfile system is what shipped.
- OwnerId isolation (#66) is partially addressed structurally but not enforced — remains on hold per D2 v1 decision until auth hardening sprint.
- Moq pattern finding: `Task<ICollection<T>>` in `.ReturnsAsync()` requires explicit generic parameter — documented in test files and PR description.
- Decision record filed: `.squad/decisions/inbox/peter-pr-opened.md`
