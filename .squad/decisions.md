# Squad Decisions

**Last Updated:** 2026-03-22  
**Source:** Team audits + PRD synthesis (issue #15)

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
**Status:** ✅ DECIDED (Ray + Glenn)  
**Choice:** Convert `GetCollectionKey()` from manual switch statement to **convention-based naming**: `typeof(T).Name.ToLower() + "s"`

**Current Implementation (buggy):**
- Manual switch statement
- FrequentShoppingList, MealRecipe, MealIngredient, WeekMenu, DailyMeal all default to `"misc"` collection
- Silent data corruption in production

**New Implementation:**
- Apply convention: `ShopItem` → `shopitems`, `ShoppingList` → `shoppinglists`, etc.
- New types automatically work without switch statement update
- Existing 4 collections already follow convention (low-risk change)

**Action:** Ray will implement this as first bug fix (P0 priority)

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
**Status:** ⏸️ PENDING IMPLEMENTATION  
**Issue:** Current `NewNavComponent` admin dropdown uses CSS `:hover` only. Unreachable by keyboard and mobile touch.

**Constraint:** Must convert to `@onclick` toggle + `aria-expanded` before adding meal planning navigation entries

**Scope:** This is a prerequisite for meal planning nav work (not separate sprint)

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
**Status:** ✅ DECIDED (Ray)  
**Action:** Remove `IGenericRepository<MealIngredient>` from Program.cs DI registration

**Rationale:** MealIngredient is embedded in MealRecipe; no separate repository needed. Simplifies DI config and prevents confusion.

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
| D4: Collection Key Convention | ✅ Decided | Ray | Sprint 0 (bug fix) |
| D5: Toast System | ⏸️ Pending | Blair/Peter | Sprint 1 (UI) |
| D6: Mobile Drag Fallback | ⏸️ Pending | Blair/Peter | Sprint 2 (Mobile) |
| D7: Admin Nav Accessibility | ⏸️ Implementation | Blair | Sprint 4 (Meal UI) |
| D8: Auth Provider Details | ✅ Decided | Peter/Glenn | — |
| D9: MealIngredient DI | ✅ Decided | Ray | Sprint 3 (Meal API) |
| D10: LastModified Migration | ⏸️ Pending | Ray/Glenn | Pre-launch |
| D11: Shop Management | ⏸️ Pending | Peter | Sprint planning |
| D12: Design Tokens | ⏸️ Deferred | Blair | Post-MVP |
| D13: Dark Mode | ⏸️ Deferred | Blair | Post-MVP |

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here before implementation
- Block development if a P0/P1 decision is unresolved
- Archive resolved decisions to history.md when next session completes
