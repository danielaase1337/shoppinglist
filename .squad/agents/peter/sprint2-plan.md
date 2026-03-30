# Sprint 2 Plan — Focused UI/UX + Foundation Work

**Sprint Duration:** TBD (Daniel to confirm)  
**Base Branch:** `development`  
**Status:** 🟡 ACTIVE — Planning Complete

---

## Sprint Scope Summary

**Sprint 2 focuses on UI/UX polish and data layer foundation work.** Prioritized P1 issues with realistic scope. Post-auth chain verification, we unblock notification system and mobile usability before deeper meal planning work.

**Team Capacity Allocation:**
- **Blair (Frontend):** 4 issues (Toast, Mobile Drag, i18n architecture, ManageMyShopsPage detail page)
- **Glenn (Backend):** 1 issue (Shop deletion cascade API)
- **Ray (Data):** 1 issue (LastModified migration endpoint)
- **Josh (Testing):** 1 issue (API controller test rewrite — P1 BLOCKING)
- **Peter (Architect):** 1 issue (Meal planning v1 scoping — decision capture only)

**Total Issues:** 8 (P1: 2, P2: 6)  
**Estimated Load:** Medium-high (realistic for 2-week sprint)

---

## Issues IN SPRINT 2

### 🔴 **P1 — BLOCKING ISSUES**

#### **#33 — Testing: Rewrite API controller tests to test actual controller methods**
- **Owner:** Josh (Tester)
- **Priority:** P1 (BLOCKING)
- **Status:** Not started
- **Dependencies:** None (but unblocks sprint 7)
- **Scope:**
  - Currently: All 65 API tests call mocks directly, zero controller code tested
  - Action: Refactor tests to instantiate actual ControllerBase-derived classes with mock dependencies
  - Verify: All 65 tests pass with controller methods invoked
  - Validate: CI runs tests and they pass (per D20 branching, sprint 0 fixed missing CI step)
- **Rationale:** POST-SPRINT-0 regression risk. If controller tests aren't real, we ship broken APIs. Must be done early to catch regressions from Auth work (#22-#24) before they propagate.
- **Deliverables:**
  - Refactored test structure in `Api.Tests/Controllers/`
  - All 65 tests green
  - Controller methods actually invoked (not mocks)
  - PR ready for merge to `development`

---

#### **#25 — UI: Implement INotificationService + ToastContainer for user feedback**
- **Owner:** Blair (Frontend)
- **Priority:** P1
- **Status:** Not started
- **Dependencies:** None
- **Blocks:** #28 (shop deletion UX), future meal planning UI
- **Scope:**
  - Create `Services/INotificationService` interface (Show/Dismiss methods, notification types)
  - Implement `NotificationService` with in-memory queue
  - Create `ToastContainer.razor` component
  - Add DI registration in `Program.cs`
  - Integrate into `MainLayout` or shared layout
  - Write test component to demo toast functionality
- **Design Notes:**
  - Use decision D5 (Option A: custom implementation, not third-party)
  - Support toast types: info, success, warning, error
  - Auto-dismiss after 5 seconds (configurable)
  - CSS animation on show/hide (fade in/out)
  - Accessible: announce toasts to screen readers (aria-live="polite")
- **Deliverables:**
  - `Services/NotificationService.cs` + interface
  - `Shared/ToastContainer.razor`
  - `wwwroot/css/toast.css` (animations)
  - Test page or component
  - PR ready for merge

---

### 🟡 **P2 — FEATURE WORK (High Value, Realistic Scope)**

#### **#27 — UI: Replace drag-and-drop with up/down buttons for mobile compatibility**
- **Owner:** Blair (Frontend)
- **Priority:** P2
- **Status:** Not started
- **Dependencies:** None
- **Scope:**
  - Impacts: `ShopConfigurationPage.razor` (shelf order), `CategoryManagementPage.razor` (category order)
  - Add up/down button controls next to each item
  - On click: Swap positions in list, persist to database (PUT /api/shops or equivalent)
  - Keep existing HTML5 drag-and-drop for desktop (enhancement, not primary)
  - Test on iOS Safari (simulator if available)
- **Decision Reference:** D6 (Option B: up/down buttons primary, drag as enhancement)
- **Deliverables:**
  - Updated `ShopConfigurationPage.razor` with buttons
  - Updated `CategoryManagementPage.razor` with buttons
  - CSS for button styling
  - Manual test on mobile device
  - PR ready for merge

---

#### **#28 — Feature: Shop deletion safeguards — multi-step confirm + dependency check**
- **Owner:** Blair (UI) + Glenn (API cascade check)
- **Priority:** P2
- **Status:** Not started (BLOCKED ON #25)
- **Dependencies:** ✅ #25 (Toast system)
- **Scope:**
  - **UI (Blair):**
    - Add delete button to `ManageMyShopsPage.razor`
    - Step 1: Confirm delete modal (ask "Are you sure?")
    - Step 2: Show dependency warning if any ShoppingLists use this shop's sort config
    - Step 3: Final confirmation before delete
    - On success: show toast, redirect to shop list
    - On error: show error toast with reason
  - **API (Glenn):**
    - `DELETE /api/shop/{id}` — Add cascade check
    - Query: find all ShoppingLists, check if any reference this shop's `SelectedShop` field
    - If yes: return 409 Conflict with message "Shop in use by N lists. Cannot delete."
    - If no: delete shop and return 200
    - Log deletions for audit
- **Decision Reference:** D16 (Shop deletion safeguards)
- **Deliverables:**
  - Updated `ManageMyShopsPage.razor` with delete UI
  - Updated `ShopsController.cs` with cascade logic
  - Integration test: attempt delete with/without dependent lists
  - PR ready for merge

---

#### **#30 — Feature: Add i18n resource file architecture for future English localization**
- **Owner:** Blair (Frontend)
- **Priority:** P2
- **Status:** Not started
- **Dependencies:** None
- **Scope:**
  - Create `Client/Resources/` directory structure:
    - `Resources/UI.en.resx` (English UI strings)
    - `Resources/UI.no.resx` (Norwegian UI strings — future)
  - Populate with strings from current UI (sample 20-30 key strings from main pages)
  - Create `Services/LocalizationService.cs` to load/switch languages
  - Update 2-3 pages to use resource strings (e.g., button labels in `MainLayout.razor`)
  - Document how to add new strings for future developers
  - **No UI language switch yet** — architecture only, v1 stays Norwegian
- **Decision Reference:** D19 (i18n architecture, code in English, UI in Norwegian for v1)
- **Deliverables:**
  - `.resx` files with sample strings
  - `LocalizationService.cs`
  - 2-3 pages refactored to use resources
  - Documentation: `docs/i18n.md`
  - PR ready for merge

---

#### **#31 — Feature: Extract LastModified lazy migration to one-time admin endpoint**
- **Owner:** Ray (Firebase) + Glenn (API)
- **Priority:** P2
- **Status:** Not started
- **Dependencies:** None
- **Scope:**
  - **Current State (Problem):**
    - Every GET /api/shoppinglists checks each list for null LastModified
    - If null, sets timestamp and writes to DB (N+1 write problem)
    - Slows down GET response time
  - **Solution:**
    - Create `GET /api/admin/migrate-lastmodified` endpoint (Ray + Glenn)
    - Scans all collections for entities with null LastModified
    - Batch-updates them to DateTime.UtcNow
    - Returns count of migrated entities
    - Endpoint disabled after first run (or requires admin flag)
  - **Code Location:**
    - New controller: `Api/Controllers/MigrateLastModifiedController.cs`
    - Or add to existing `Api/Controllers/AdminController.cs`
  - **Testing:**
    - Unit test: Create mock entity with null LastModified, verify update
    - Manual test: Run endpoint, check migration count
- **Decision Reference:** D10 (Option A: Extract to one-time endpoint)
- **Deliverables:**
  - `MigrateLastModifiedController.cs` (or AdminController update)
  - Migration logic in repository layer
  - Unit tests
  - Documentation: how to run migration
  - PR ready for merge

---

#### **#32 — Feature: Complete ManageMyShopsPage — shop detail management is a stub**
- **Owner:** Blair (Frontend)
- **Priority:** P2
- **Status:** Not started
- **Scope:**
  - Current: `/managemyshops/{Id}` renders only a title placeholder (dead end)
  - Complete: Implement full shop management page
    - Display shop name, edit name
    - List all shelves with sort order
    - Add shelf button
    - Delete shelf button (with safeguard)
    - Reorder shelves (use #27 up/down buttons)
    - List categories in each shelf
    - Add category button
    - Delete category button
    - Reorder categories (use #27 up/down buttons)
  - Call APIs: `PUT /api/shops`, `GET /api/shops/{id}`, etc.
  - Save operations show success toast (#25)
  - Error operations show error toast (#25)
- **Decision Reference:** D17 (Complete ManageMyShopsPage, not remove)
- **Deliverables:**
  - Completed `ManageMyShopsPage.razor`
  - Nested shelf/category UI components
  - Integration with #27 (up/down buttons)
  - PR ready for merge
- **Estimated Effort:** Moderate (new component, reuse existing patterns)

---

#### **#29 — Scoping: Meal Planning v1 — text-based meal history + weekly suggestions**
- **Owner:** Peter (Architect)
- **Priority:** P2
- **Status:** Not started (SCOPING ONLY, NO IMPLEMENTATION)
- **Dependencies:** None
- **Scope:**
  - **This is a SCOPING issue, not implementation**
  - Goal: Capture requirements and architecture decisions for meal planning v1
  - Deliverable: GitHub issue + detailed architecture doc
  - Do NOT build any code in Sprint 2
  - Must align with Daniel before finalizing scope
  - **v1 Scope Boundaries (from D18):**
    - Input: User manually enters meal history (text form or list)
    - Parsing: Extract meal names + dates from text
    - Algorithm: Suggest weekly meals that avoid repeats from last 4 weeks
    - Output: Weekly meal plan (7 days) with suggested recipes
    - **OUT of v1:** Recipe CRUD, ingredient linking, shopping list auto-gen
  - **Tasks:**
    - Write `.squad/decisions/inbox/peter-mealplan-v1-scope.md` capturing architecture
    - Update #29 with final scope document (link to decision)
    - Create separate GitHub issue for v1 implementation sprint
- **Deliverables:**
  - Scope document: `.squad/decisions/inbox/peter-mealplan-v1-scope.md`
  - Updated GitHub issue #29 with linked decision
  - Separate implementation issue created for future sprint
  - No code changes

---

### ✅ **ALREADY COMPLETE (for reference)**

#### **#26 — UI: Convert admin nav dropdown from CSS :hover to @onclick + aria-expanded**
- **Status:** ✅ IMPLEMENTED (2026-03-27, Blair)
- **Closure:** Already merged to `development`
- **Reference:** Decision D7
- **Why Listed:** Historical context; don't duplicate work

---

## Sprint Roadmap (Dependency Graph)

```
INDEPENDENT (no blockers):
  #33 (Test rewrite) → unblocks Sprint 7
  #25 (Toast system) → unblocks #28
  #27 (Mobile buttons) → independent
  #29 (Meal planning scope) → independent
  #30 (i18n architecture) → independent
  #31 (LastModified migration) → independent

DEPENDENT:
  #28 (Shop deletion) depends on ✅ #25 (Toast)
  #32 (ManageMyShopsPage) uses ✅ #27 (Up/down buttons) — can parallelize

RECOMMENDED WORK ORDER:
  1. Start #33 (test rewrite) immediately — P1 blocking
  2. Start #25 (toast) in parallel — high blocking value
  3. Start #27 (mobile buttons) once #25 running — used by #32
  4. Start #31, #30, #29 in parallel (independent)
  5. Start #32 once #27 code-complete (uses buttons)
  6. Start #28 once #25 code-complete (uses toast)
```

---

## Architecture & Technical Concerns

### **#33 Test Rewrite — Concerns**
1. **Scope Risk:** 65 tests need refactoring. Estimate carefully.
   - Mitigation: Start with one controller class, establish pattern, then scale.
2. **Controller Dependency Injection:** Need to properly mock IGenericRepository, ILogger, etc.
   - Pattern to establish in first test file, then replicate.
3. **Auth Header Mocking:** Controllers now parse x-ms-client-principal header (from #22-#24 auth work).
   - Ensure mock headers are set up in test fixtures.

**Recommendation:** Josh should pair with Glenn to establish test pattern for one controller first, then scale.

---

### **#25 Toast System — Concerns**
1. **Accessibility:** Must announce toasts to screen readers (aria-live).
   - Pattern: Use Syncfusion accessibility patterns if available, else custom.
2. **CSS Animations:** Keep animations performant (use CSS transforms, not layout reflows).
3. **State Management:** NotificationService must handle concurrent toasts (queue, not singleton).

**Recommendation:** Design NotificationService with queue first (not just last-in-wins).

---

### **#28 Shop Deletion — Concerns**
1. **Data Integrity:** The cascade check must be accurate.
   - Query: scan ShoppingList collection for `SelectedShop` field matching shop ID
   - Edge case: Handle null/missing SelectedShop field gracefully
2. **UX Flow:** Three-step modal might feel heavy. Keep copy concise.

**Recommendation:** Glenn should write unit test for cascade check logic before UI work.

---

### **#29 Meal Planning Scope — Concerns**
1. **This is scoping-only, not implementation.** Do NOT write code.
2. **Critical Decision Needed:** How does meal history input work?
   - Option A: Free-form text parsing ("Taco Monday, Pizza Friday, ...")
   - Option B: Date picker + dropdown to select meals retroactively
   - **Must align with Daniel** before implementation sprint
3. **Not in Sprint 2 Implementation:** v1 recipe CRUD is v2 scope (per D18)

**Recommendation:** Schedule design review with Daniel after scope document is ready.

---

## Non-Issues / Out of Scope

**Issues NOT in Sprint 2:**
- **#22, #23, #24** (Auth chain) — ✅ Complete, already merged
- **#16-#21** (Sprint 0 bugs) — ✅ Complete
- **Any Sprint 3+ issues** (blocked by meal planning scope)

**Deferral Justification:**
- Meal planning implementation deferred until v1 scope finalized with Daniel (#29 scoping only)
- Sprint 3+ issues require meal planning decisions first
- Auth is complete; no auth rework needed this sprint

---

## Success Criteria for Sprint 2

**Definition of Done:**
1. ✅ All 8 issues moved to "Closed" on GitHub
2. ✅ All associated PRs merged to `development`
3. ✅ `development` builds and passes all tests (90 API + 61 Client + E2E suite)
4. ✅ Staging deployment successful (per D20 branching strategy)
5. ✅ No regressions in existing shopping list functionality
6. ✅ Code review completed by Peter (architecture) + team

**Testing Gate:**
- Josh must verify all tests pass on `development` before closing sprint
- Manual mobile testing required for #27 (drag-drop replacement)
- Toast system integration tested on at least 2 pages

---

## Sprint Metrics

| Metric | Value | Notes |
|--------|-------|-------|
| **Issues Planned** | 8 | 2 P1, 6 P2 |
| **Team Members** | 5 | Blair (4), Glenn (1), Ray (1), Josh (1), Peter (1) |
| **Estimated Days** | 10-12 | Realistic for 2-week sprint, including code review |
| **Blockers** | 1 | Meal planning v1 scope (Daniel alignment needed) |
| **Tech Debt Addressed** | 2 | #33 (testing), #31 (migration endpoint) |
| **UI/UX Improvements** | 4 | #25, #26, #27, #28, #32 (5 frontend items) |

---

## Next Steps (After Sprint Plan Approval)

1. **Daniel reviews sprint plan** (this document) — confirm scope + priorities
2. **Peter creates sprint board** in GitHub Projects (or equivalent)
3. **Team picks up issues** in recommended work order
4. **Weekly syncs** (TBD schedule)
5. **Sprint closure** when all issues closed + tests green

---

## Appendix: Issue Checklist

- [ ] #25 — Toast system (Blair, P1)
- [ ] #27 — Mobile drag buttons (Blair, P2)
- [ ] #28 — Shop deletion safeguards (Blair+Glenn, P2, depends #25)
- [ ] #29 — Meal planning scope (Peter, P2, scoping only)
- [ ] #30 — i18n architecture (Blair, P2)
- [ ] #31 — LastModified migration (Ray+Glenn, P2)
- [ ] #32 — ManageMyShopsPage completion (Blair, P2)
- [ ] #33 — API test rewrite (Josh, P1)

---

**Plan Approved By:** [Pending — awaiting Daniel review]  
**Plan Created By:** Peter (Lead/Architect)  
**Date:** 2026-XX-XX  
**Status:** 🟡 Ready for Presentation
