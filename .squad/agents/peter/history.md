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
- **`Index.razor` is protected at a higher level:** Because `App.razor` has `<AuthorizeRouteView>` with `<NotAuthorized><RedirectToWelcome /></NotAuthorized>` and `Program.cs` has `FallbackPolicy = DefaultPolicy`, adding `<AuthorizeView>` to `Index.razor` would be redundant. Don't gold-plate.


## Learnings — Meal Planning Full Scope Pivot (2026-04-03)

- **Slim v1 was thrown away — correctly:** The text-history + frequency-suggestions scope (WeekMenuText entity) was superseded before a single line of implementation code was written. Scoping separately from implementation gave us the flexibility to pivot cleanly. This validates the "scoping ticket before implementation ticket" pattern.
- **Business rules belong in scope doc, not just in backlog:** Pizza Fridays, Thursday-Thursday week, and fresh ingredient perishability are hard rules that shape every phase of the data model. Capturing them explicitly in the scope doc before architecture starts prevents accidental omission.
- **Unit system is a cross-cutting concern — freeze it first:** MealUnit enum is used by MealIngredient (Phase 1) and InventoryItem (Phase 4). Designing it once in Shared before any coding starts prevents the coordination cost of retrofitting units across two phases mid-sprint.
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
