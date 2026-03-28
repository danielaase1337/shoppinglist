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
| D20: Branching Strategy | ✅ Implemented (New) | Daniel/Peter | Sprint 0 ✅ (2026-03-28) |

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here before implementation
- Block development if a P0/P1 decision is unresolved
- Archive resolved decisions to history.md when next session completes
