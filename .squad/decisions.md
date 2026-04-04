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

## Phase 4: Inventory Management (P1 — Feature Implementation)

### D-Phase4-Inventory-1: InventoryItem IsActive Property
**Status:** ✅ IMPLEMENTED (Glenn, 2026-04-08)  
**Component:** Shared models (Firestore + DTO), soft-delete pattern

**Decision:**
- Add `IsActive: bool` property to both `InventoryItem` and `InventoryItemModel`
- Default to `true` in constructors; inherited from `EntityBase` precedent
- Enables soft-delete filtering and inactive inventory display

**Rationale:**
- Mirrors PortionRule pattern (Phase 5)
- Allows historical tracking without hard delete
- GET endpoints filter `IsActive == true` automatically

---

### D-Phase4-Inventory-2: InventoryItemController Endpoints
**Status:** ✅ IMPLEMENTED (Glenn, 2026-04-08)  
**Component:** Azure Functions controller

**Endpoints:**
| Function | Methods | Route | Behavior |
|----------|---------|-------|----------|
| `inventoryitems` | GET, POST, PUT | `/api/inventoryitems` | GET filters IsActive=true, orders by Name |
| `inventoryitem` | GET, DELETE | `/api/inventoryitem/{id}` | DELETE soft-deletes (IsActive→false) |
| `inventoryitemsadjust` | POST | `/api/inventoryitems/adjust` | Bulk delta adjustments, clamps to 0 |

**Key Logic:**
- **Soft delete:** GET existing → set `IsActive=false` → `LastModified=UtcNow` → `Update()`
- **Bulk adjust:** Fetches all inventory **once** before loop (avoids N+1)
- **Quantity clamping:** Delta will not reduce stock below 0
- **InventoryAdjustmentModel:** Public nested class in controller for JSON deserialization

---

### D-Phase4-Inventory-3: ShoppingList IsDone→Inventory Hook
**Status:** ✅ IMPLEMENTED (Glenn, 2026-04-08)  
**Component:** ShoppingListController.GetAllShoppingListsFunction

**Behavior:**
When a shopping list PUTs with `IsDone: false → true` transition:
1. Fetch existing list state from repo **before** update
2. Detect transition: `existing != null && !existing.IsDone && updatedList.IsDone`
3. Load all active inventory items (once)
4. For each `ShoppingListItem` where `Varen != null`:
   - Find matching inventory by `ShopItemId + IsActive`
   - Increment `QuantityInStock += Mengde`
5. No auto-create: Missing inventory silently skipped

**Rationale:**
- Closes the loop: shopping → inventory tracking
- User-managed inventory (no auto-creation)
- One-shot operation on IsDone transition only

---

### D-Phase4-Inventory-4: InventoryItemsPage Frontend UI
**Status:** ✅ IMPLEMENTED (Blair, 2026-04-08)  
**Component:** New page at `/inventory`, SfAutoComplete + quick-adjust buttons

**Features:**
- Row-level name click → inline edit (no modal)
- +1/−1 buttons → `POST /api/inventoryitems/adjust` (optimistic UI)
- Frozen meal shortcut: creates inventory item with `Name = "{RecipeName} (frossen)"`, `SourceMealRecipeId = recipe.Id`
- SfAutoComplete for ShopItem selection (parallel data loading)
- Nav integration: "Lager" link in Admin dropdown

**Design Decisions:**
- **Optimistic adjust:** Update local `QuantityInStock` before API response (fast UX)
- **Frozen meal ShopItemId:** Uses `recipe.Id` as proxy (no real ShopItem). Migration path clear for future real links.
- **No IsActive badge:** IsActive filter handled server-side; UI shows active items only

---

### D-Phase4-Inventory-5: Phase 3 Ingredient Use-Up Matching
**Status:** ✅ IMPLEMENTED (Blair, 2026-04-08)  
**Component:** OneWeekMenuPage.razor suggestion panel

**Algorithm:**
- Runs on recipe selection; no new API calls (uses loaded `_recipes`)
- Suggests fractional ingredients (`Quantity < 1.0`) only
- Excludes recipes already in week plan (`selectedIds` set)
- Dismissal: session-scoped `HashSet<string>` (no persistence)
- "Add to next slot" finds first empty day in `WeekOrder`, silently ignores if full

**UI:**
- Amber left border panel (#ffc107, Bootstrap warning colour)
- Between week planner table and generated list card
- Dismissable per ingredient

---

### D-Phase4-Inventory-6: IsActive Patch Finding
**Status:** ✅ DOCUMENTED (Josh, 2026-04-07)  
**Component:** Cross-agent discovery

**Finding:**
InventoryItem model was missing `IsActive` property despite soft-delete requirement. Discovered during test seeding. Glenn added to both Firestore + DTO models.

**Lesson:** Pre-check model completeness against controller implementation before writing tests.

---

## Phase 5: Family Profile & Portion Scaling (P1 — Feature Implementation)

### D-Phase5-DataModels-1: AgeGroup Enum & Namespace
**Status:** ✅ IMPLEMENTED (Ray, 2026-04-04)  
**Component:** Shared/AgeGroup.cs, root Shared namespace

**Enum Values:**
- `Unknown = 0`
- `Adult = 1`
- `Child = 2`
- `SmallChild = 3` (toddler)

**Rationale:**
- Enums shared between Firestore + DTO namespaces (no duplication needed)
- Root Shared namespace mirrors MealUnit precedent
- Simplifies cross-model references

---

### D-Phase5-DataModels-2: FamilyProfile & FamilyMember Structure
**Status:** ✅ IMPLEMENTED (Ray, 2026-04-04)  
**Component:** Firestore + DTO models, embedded FamilyMember

**Decision:**
- `FamilyMember` is **embedded** in `FamilyProfile`, not a root collection
- No separate DI registration or Firestore collection for FamilyMember
- Mirrors MealIngredient→MealRecipe pattern

**Structure:**
- `FamilyProfile: EntityBase` → `Members: ICollection<FamilyMember>`
- `FamilyMember` (embedded): Name, AgeGroup, DietaryNotes
- `FamilyProfileModel` and `FamilyMemberModel` in DTO namespace

**Rationale:**
- Single household assumption (v1) doesn't require separate collection
- Embedding reduces Firestore round-trips
- Clear path to multi-household in v2 (promote to root collection)

---

### D-Phase5-DataModels-3: PortionRule Model with Denormalisation
**Status:** ✅ IMPLEMENTED (Ray, 2026-04-04)  
**Component:** Firestore + DTO models

**Structure:**
- `ShopItemId: string` (required)
- `AgeGroup: enum` (required)
- `QuantityPerPerson: float` (e.g., 0.5 for half a can per person)
- `IsActive: bool` (inherited from EntityBase)
- `PortionRuleModel` also includes `ShopItemName: string` (denormalised)

**Rationale:**
- Denormalisation avoids UI fetch for item names
- Mirrors `MealIngredientModel` pattern (existing precedent)
- Server-side stores names at sync time; client cache responsibility

---

### D-Phase5-Controllers-1: FamilyProfileController Hard Delete
**Status:** ✅ IMPLEMENTED (Glenn, 2026-04-08)  
**Component:** Azure Functions

**Endpoints:**
| Function | Methods | Route |
|----------|---------|-------|
| `familyprofiles` | GET, POST, PUT | `/api/familyprofiles` |
| `familyprofile` | GET, DELETE | `/api/familyprofile/{id}` |

**Behavior:**
- **GET all:** Ordered by Name ascending (single-household assumption, but collection interface)
- **POST:** Sets `LastModified = DateTime.UtcNow`
- **DELETE:** Hard delete via `_repository.Delete(id)` (no IsActive property exists)

**Rationale:** FamilyProfile has no IsActive field → hard delete is only option (no soft-delete path).

---

### D-Phase5-Controllers-2: PortionRuleController Soft Delete
**Status:** ✅ IMPLEMENTED (Glenn, 2026-04-08)  
**Component:** Azure Functions

**Endpoints:**
| Function | Methods | Route |
|----------|---------|-------|
| `portionrules` | GET, POST, PUT | `/api/portionrules` |
| `portionrule` | GET, DELETE | `/api/portionrule/{id}` |

**Behavior:**
- **GET all:** Filters `IsActive == true`, ordered by ShopItemId then AgeGroup (enum ordinal)
- **POST:** Sets `LastModified = DateTime.UtcNow` AND `IsActive = true` (override client)
- **DELETE:** Soft delete — Get → `IsActive=false` → `LastModified=UtcNow` → Update

**Rationale:** PortionRule has IsActive → soft delete preserves history.

---

### D-Phase5-Frontend-1: FamilyProfilePage Structure
**Status:** ✅ IMPLEMENTED (Blair, 2026-04-09)  
**Component:** `/familyprofile` page, two sections

**Section 1 — Family Members:**
- Loads `GET /api/familyprofiles`, takes `FirstOrDefault()` (single-household)
- "Create profile" button if none exists
- List: Name, AgeGroup (Norwegian label), DietaryNotes
- Add member form: Name, AgeGroup select, DietaryNotes textarea
- Remove button → modify Members collection → PUT profile
- All mutations immediately PUT profile back

**Section 2 — Portion Rules:**
- Loads `GET /api/portionrules` (shows active only)
- Table: ShopItemName, AgeGroup (Norwegian), QuantityPerPerson, Unit
- Add rule form: SfAutoComplete for ShopItem, AgeGroup select, qty input, MealUnit select
- Delete button → `DELETE /api/portionrule/{id}`

**Nav:** "Familieprofil" (oi-people icon) in Admin dropdown after "Lager"

---

### D-Phase5-Frontend-2: Portion Scaling in OneWeekMenuPage
**Status:** ✅ IMPLEMENTED (Blair, 2026-04-09)  
**Component:** Client-side scaling after generate-shoppinglist response

**Flow:**
1. `_familyProfile` and `_portionRules` loaded in parallel on init
2. User generates shopping list via `generate-shoppinglist` endpoint
3. `ApplyPortionScaling()` runs after response:
   - For each ShoppingItem, find matching rules by ShopItemId
   - For each AgeGroup in rules, multiply `rule.QuantityPerPerson × memberCount`
   - Sum across all age groups
   - Set `item.Mengde = (int)Math.Ceiling(scaledQty)`
4. Generated list preview shows "📐 Mengder tilpasset familieprofil" if scaling applied
5. Silently skipped if no profile or no rules

**Rationale:**
- Client-side only (no new API endpoint)
- Consistent with existing client-side sorting pattern
- `_scalingApplied` resets per generate call (prevents stale note)

---

### D-Phase5-Frontend-3: Enum URL Mappings Extension
**Status:** ✅ IMPLEMENTED (Blair, 2026-04-09)  
**Component:** ShoppingListKeysEnum + ISettings

**New Enum Values:**
- `FamilyProfiles = 17` → `api/familyprofiles`
- `FamilyProfile = 18` → `api/familyprofile`
- `PortionRules = 19` → `api/portionrules`
- `PortionRule = 20` → `api/portionrule`

---

### D-Phase5-Testing-1: Hard vs Soft Delete Test Patterns
**Status:** ✅ VERIFIED (Josh, 2026-04-08)  
**Component:** FamilyProfileControllerTests + PortionRuleControllerTests

**Hard Delete Pattern (FamilyProfile):**
```csharp
// Test: Delete_CallsRepositoryDelete
_mockRepository.Verify(r => r.Delete(id), Times.Once);
_mockRepository.Verify(r => r.Update(It.IsAny<FamilyProfile>()), Times.Never);
```

**Soft Delete Pattern (PortionRule):**
```csharp
// Test: Delete_SoftDeletes_SetsIsActiveFalse
_mockRepository.Verify(r => r.Delete(It.IsAny<PortionRule>()), Times.Never);
_mockRepository.Verify(r => r.Update(It.IsAny<PortionRule>()), Times.Once);
// Verify IsActive=false in Update call
```

**Rationale:** Explicit verification of delete path prevents accidental soft/hard mismatch.

---

### D-Phase5-Testing-2: Moq ICollection Return Type Fix
**Status:** ✅ DOCUMENTED (Josh, 2026-04-08)  
**Component:** Test fixture patterns

**Pattern Requirement:**
```csharp
// CORRECT: Explicit type parameter required
_mockRepository
    .Setup(r => r.Get())
    .Returns(Task.FromResult<ICollection<FamilyProfile>>(profiles));

// WRONG: Silent null return without explicit type
.Returns(Task.FromResult(profiles));  // May return null
```

**Lesson:** Applied to all Phase 5 tests; documented for future sprints.

---

## Cross-Sprint Findings & Best Practices

### Finding 1: IsActive Patch Necessity
Model fields required by controller logic sometimes lag behind. Recommend:
- Pre-check all model fields against controller implementation
- Write tests first; seed fixtures validate completeness

### Finding 2: Soft vs Hard Delete Clarity
Document delete strategy **at model level** to prevent implementation mistakes:
- No IsActive → hard delete only
- IsActive present → soft delete expected

### Finding 3: Denormalisation Trade-off
DTOs carrying denormalised fields (ShopItemName, RecipeName) are acceptable when:
- Lookup field is stored at sync time (server-side responsibility)
- Client-side cache is acceptable (not real-time critical)
- Avoids N+1 lazy-load patterns in UI

### Finding 4: Client-Side Scaling Pattern
Operations like portion scaling belong **client-side** when:
- No multi-user coordination needed
- Fully deterministic (same inputs → same output)
- Reduces API surface area

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

## Phase 6: Staging Bug Fix (P0 — Critical Production Issue)

### D-Phase6-SWA-Auth-1: ShoppingListController AuthorizationLevel
**Status:** ✅ IMPLEMENTED (Peter, 2026-04-04)  
**Component:** Api/Controllers/ShoppingListController.cs

**Bug:** ShoppingListController used `AuthorizationLevel.Function` instead of `AuthorizationLevel.Anonymous`. All other controllers use `Anonymous`. Azure Static Web Apps proxies to `/api/*` without injecting function keys → 401 → SWA redirects to `/welcome` HTML → client JSON parser crashes on startup.

**Fix:**
```csharp
// Changed both functions from AuthorizationLevel.Function to AuthorizationLevel.Anonymous
public async Task<HttpResponseData> RunAll([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put")] HttpRequestData req)
public async Task<HttpResponseData> RunOne([HttpTrigger(AuthorizationLevel.Anonymous, "get", "delete", Route = "shoppinglist/{id}")] HttpRequestData req, object id)
```

**Architecture Rule:** All Azure Functions in this project MUST use `AuthorizationLevel.Anonymous`. SWA's route rules in `staticwebapp.config.json` handle authorization; Function-level key auth is incompatible with the API proxy pattern.

**Commit:** b314fde

---

### D-Phase6-SWA-DI-2: useMemoryDb Guard Logic (GOOGLE_APPLICATION_CREDENTIALS)
**Status:** ✅ IMPLEMENTED (Glenn, 2026-04-04)  
**Component:** Api/Program.cs

**Bug:** `useMemoryDb` only checked `GOOGLE_CLOUD_PROJECT`, not `GOOGLE_APPLICATION_CREDENTIALS`. In staging, project may be set but credentials file absent → Firestore SDK throws at DI resolution → Functions host crashes → all `/api/*` return HTML 500.

**Fix:**
```csharp
// Added third OR-condition to check GOOGLE_APPLICATION_CREDENTIALS
var useMemoryDb = environment == "Development" || 
                string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT")) ||
                string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS"));
```

**Behavior Matrix:**
| Environment | GOOGLE_CLOUD_PROJECT | GOOGLE_APPLICATION_CREDENTIALS | Result |
|---|---|---|---|
| Development (local) | any | any | Memory repos ✅ |
| Staging (SWA preview) | set | NOT set | Memory repos ✅ (was crashing) |
| Staging (SWA preview) | NOT set | any | Memory repos ✅ |
| Production | set | set | Firestore ✅ |

**Commit:** b73c76e

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here before implementation
- Block development if a P0/P1 decision is unresolved
- Archive resolved decisions to history.md when next session completes
