# Meal Planning v1 Scope Document

**Author:** Peter (Lead/Architect)  
**Date:** 2026-03-29  
**Status:** ✅ SCOPE FINALIZED  
**Issue:** #29 (Scoping: Meal Planning v1)  
**Blocks:** All meal planning implementation tickets (v1 + v2)

---

## Executive Summary

Meal Planning v1 is a **minimal text-based weekly meal history viewer + suggestion engine**. It is NOT recipe CRUD. 

**v1 Scope:**
- Simple weekly text entries (Mon–Sun) per week
- Suggestions based on historical meal text entries
- Standalone feature (no shopping list integration in v1)

**v2 Scope (future):**
- Recipe CRUD with ingredients
- Shopping list generation from selected recipes
- (Defer entirely — not blocking v1)

This document resolves all 6 questions from issue #29 and provides implementation-ready tickets.

---

## 1. Data Model for v1

### 1.1 Firestore Entity: `WeekMenuText` (v1-specific)

Instead of the existing complex `WeekMenu` + `DailyMeal` + `MealRecipe` hierarchy, v1 uses a flat, text-based model:

```csharp
namespace Shared.FireStoreDataModels
{
    [FirestoreData]
    public class WeekMenuText : EntityBase
    {
        [FirestoreProperty]
        public int WeekNumber { get; set; }
        
        [FirestoreProperty]
        public int Year { get; set; }
        
        [FirestoreProperty]
        public Dictionary<string, string> DailyMeals { get; set; }
        // Key: "Monday", "Tuesday", ... "Sunday"
        // Value: free text entry (e.g., "Taco", "Fisk med potet", "Pizza fra fryseren")
        
        [FirestoreProperty]
        public DateTime CreatedDate { get; set; }
        
        [FirestoreProperty]
        public DateTime? LastModified { get; set; }
        
        [FirestoreProperty]
        public bool IsArchived { get; set; }
        
        public WeekMenuText()
        {
            DailyMeals = new Dictionary<string, string>
            {
                { "Monday", "" },
                { "Tuesday", "" },
                { "Wednesday", "" },
                { "Thursday", "" },
                { "Friday", "" },
                { "Saturday", "" },
                { "Sunday", "" }
            };
            CreatedDate = DateTime.UtcNow;
            LastModified = DateTime.UtcNow;
            IsArchived = false;
        }
    }
}
```

**Why this model?**
- **Flat:** No nested collections or complex references
- **Flexible:** Free text — users can type whatever they want
- **Isolated from v2:** Completely separate from future recipe CRUD (`MealRecipe`, `MealIngredient`)
- **Simple API:** No need to resolve references or join data
- **Firestore-efficient:** Document stays well under 1 MB limit

### 1.2 DTO Model: `WeekMenuTextModel`

```csharp
namespace Shared.HandlelisteModels
{
    public class WeekMenuTextModel : EntityBase
    {
        public int WeekNumber { get; set; }
        public int Year { get; set; }
        public Dictionary<string, string> DailyMeals { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModified { get; set; }
        public bool IsArchived { get; set; }

        public WeekMenuTextModel()
        {
            DailyMeals = new Dictionary<string, string>
            {
                { "Monday", "" },
                { "Tuesday", "" },
                { "Wednesday", "" },
                { "Thursday", "" },
                { "Friday", "" },
                { "Saturday", "" },
                { "Sunday", "" }
            };
            CreatedDate = DateTime.UtcNow;
            LastModified = DateTime.UtcNow;
            IsArchived = false;
        }

        public override bool IsValid()
        {
            return WeekNumber > 0 && Year > 0 && 
                   WeekNumber <= 53 && Year >= 2020 && Year <= 2100;
        }
    }
}
```

### 1.3 Firestore Collection Key

- **Collection name:** `weekmenutexts` (follows D4 convention: `typeof(T).Name.ToLower() + "s"`)
- **No backward-compat override needed** — new collection, fresh start

### 1.4 Storage Mechanism

- Stored in Firestore at path: `/firestore-project/weekmenutexts/{documentId}`
- Each user can have multiple week menus (one per week + year combination)
- **Key constraint:** Prevent duplicate (WeekNumber, Year) entries for same user (future: when auth isolation added)

---

## 2. Suggestion Engine for v1

### 2.1 Algorithm: Simple Frequency-Based Suggestion

**Goal:** Given a week number (or user request), suggest meals for Mon–Sun based on historical entries.

**Algorithm:**

```
Input: Target week number (optional; default = current week + 1)
Output: Dictionary<string, string> = { "Monday" → "Suggested meal", ... }

Steps:
1. Fetch all non-archived WeekMenuText entries from Firestore
2. Build frequency map: 
   - For each day (Monday–Sunday):
     - Count occurrences of each meal text across all weeks
     - Track frequency score per meal
3. Filter out meals used in the past 2 weeks (to encourage variety)
4. Pick top 2–3 suggestions per day by frequency, select randomly from top
5. Return 7 suggestions (one per day)
```

**Example:**
```
Historical data:
- Week 46: Monday=Taco, Tuesday=Pizza, ...
- Week 45: Monday=Taco, Tuesday=Fisk, ...
- Week 44: Monday=Biff, Tuesday=Pizza, ...

Frequency map (Monday):
- Taco: 2x
- Biff: 1x

Suggestion for next Monday (if past 2 weeks don't have Taco):
- Randomly pick from [Taco, Taco, Biff] → likely "Taco"
```

### 2.2 Implementation Location

- **Backend:** `Api/Controllers/WeekMenuTextController.cs` → `POST /api/weekmenutexts/suggest`
- **Frontend:** Triggered by "Foreslå" button in UI
- **Language:** Pure C# — no external AI/ML services

### 2.3 Edge Cases Handled

- **No historical data:** Return empty suggestions (user fills in manually)
- **Duplicate suggestions in same week:** Allow (user can override)
- **Very long meal names:** Display first 50 chars, full text on hover (tooltip)

---

## 3. UI Scope for v1

### 3.1 New Page: `WeekMenuTextPage.razor`

**Route:** `/week-menu/{weekNumber:int}/{year:int}`  
Example: `/week-menu/47/2026` for week 47 of 2026

**Components:**
- **Header:** "Ukemeny — Uke 47, 2026" with current week indicator
- **Calendar grid:** 7 text input fields (Monday–Sunday)
  - Each input is a `<textarea>` with 60 chars width, 2-3 rows
  - Placeholder: "Hva skal vi spise [Day]?"
  - Visual: Light card per day with day name label
- **Action buttons:**
  - **"Foreslå"** (Suggest): Calls API → populates fields with suggestions (if no data exists for this week)
  - **"Lagre"** (Save): Saves week menu to database via PUT endpoint
  - **"Gå til neste uke"** (Next week): Navigation link
  - **"Gå til forrige uke"** (Previous week): Navigation link
- **Sidebar/Footer:**
  - Quick navigation: dropdown to jump to specific week
  - "Arkiver ukemeny" (Archive): Hide old weeks from main list
- **Empty state:** If no weeks exist, show "Lag din første ukemeny" with pre-filled current week

### 3.2 New Page: `WeekMenuTextListPage.razor`

**Route:** `/week-menus`  
**Purpose:** List all non-archived week menus by year + week (newest first)

**Components:**
- **List display:**
  - Card per week: "Uke 47, 2026" + CreatedDate + [Edit] [Delete] buttons
  - Sorted: Newest first (by LastModified), then by WeekNumber descending
- **Create new:**
  - Quick form: "Ny ukemeny for uke [dropdown] 2026" + Create button
  - Default to current week (unless already exists)
- **Archive section** (expandable):
  - Show archived weeks if user clicks "Vis arkiverte"
  - Can unarchive from here

### 3.3 Navigation Integration

**Location:** `Client/Shared/NewNavComponent.razor` (Admin dropdown)

Add to Admin dropdown (after existing items):
```html
<li><a href="/week-menus">Ukemenyer</a></li>
```

**OR** add as main menu item (decision per Daniel):
```html
<li><a href="/week-menus">Ukemenyer</a></li>
```

### 3.4 Styling & Layout

- Reuse existing Blazor component patterns:
  - Use `.card` container (existing CSS class)
  - Use `.todo-list` structure for consistency
  - Follow existing input/button styling (btn-sm, btn-outline-link, etc.)
- No new CSS classes needed (lean approach)
- Responsive: Works on mobile (textareas stack vertically)

### 3.5 Toast/Notification Requirements

- Depends on #25 (toast system) being complete
- When saving: Show success/error toast
- When suggesting: Show "Forslag lastet inn" toast

---

## 4. API Endpoints for v1

### 4.1 WeekMenuTextController

**Function name:** `weekmenutexts` (collection) + `weekmenutext` (single item)

#### GET `/api/weekmenutexts` — List all week menus

```http
GET /api/weekmenutexts
Response: 200 OK
[
  {
    "id": "uuid-1",
    "weekNumber": 47,
    "year": 2026,
    "dailyMeals": { "Monday": "Taco", "Tuesday": "Pizza", ... },
    "createdDate": "2026-03-20T10:30:00Z",
    "lastModified": "2026-03-28T15:45:00Z",
    "isArchived": false,
    "name": "Uke 47 2026"
  },
  ...
]
```

#### GET `/api/weekmenutext/{id}` — Get single week menu

```http
GET /api/weekmenutext/uuid-1
Response: 200 OK
{
  "id": "uuid-1",
  "weekNumber": 47,
  "year": 2026,
  "dailyMeals": { ... },
  "createdDate": "2026-03-20T10:30:00Z",
  "lastModified": "2026-03-28T15:45:00Z",
  "isArchived": false,
  "name": "Uke 47 2026"
}
```

#### POST `/api/weekmenutexts` — Create new week menu

```http
POST /api/weekmenutexts
Content-Type: application/json

{
  "weekNumber": 47,
  "year": 2026,
  "dailyMeals": { "Monday": "", "Tuesday": "", ... }
}

Response: 201 Created
{
  "id": "uuid-new",
  "weekNumber": 47,
  "year": 2026,
  "dailyMeals": { ... },
  "createdDate": "2026-03-29T09:00:00Z",
  "lastModified": "2026-03-29T09:00:00Z",
  "isArchived": false,
  "name": "Uke 47 2026"
}
```

#### PUT `/api/weekmenutexts` — Update week menu

```http
PUT /api/weekmenutexts
Content-Type: application/json

{
  "id": "uuid-1",
  "weekNumber": 47,
  "year": 2026,
  "dailyMeals": { "Monday": "Taco", "Tuesday": "Fisk", ... },
  "lastModified": "2026-03-29T09:00:00Z"
}

Response: 200 OK
{ ... same as POST response ... }
```

#### DELETE `/api/weekmenutext/{id}` — Archive/soft-delete

```http
DELETE /api/weekmenutext/uuid-1
Response: 200 OK
{
  "id": "uuid-1",
  "weekNumber": 47,
  "year": 2026,
  "dailyMeals": { ... },
  "isArchived": true,
  "lastModified": "2026-03-29T10:00:00Z"
}
```

**Note:** DELETE marks `isArchived = true` (soft delete) rather than hard delete. Preserves data.

#### POST `/api/weekmenutexts/suggest` — Generate suggestions

```http
POST /api/weekmenutexts/suggest
Content-Type: application/json

{
  "weekNumber": 48,
  "year": 2026
}

Response: 200 OK
{
  "weekNumber": 48,
  "year": 2026,
  "dailyMeals": {
    "Monday": "Taco",
    "Tuesday": "Fisk",
    "Wednesday": "Pizza",
    "Thursday": "Biff",
    "Friday": "Kjøttkaker",
    "Saturday": "Rustikk",
    "Sunday": "Rester"
  }
}
```

**Note:** Returns suggested text entries only — user must save explicitly.

### 4.2 DI Registration in `Api/Program.cs`

```csharp
// Add repository for WeekMenuText (alongside existing MealRecipe, etc.)
services.AddSingleton<IGenericRepository<WeekMenuText>>(
    s => new GoogleFireBaseGenericRepository<WeekMenuText>(
        s.GetRequiredService<IGoogleDbContext>(),
        s.GetRequiredService<ILogger<GoogleFireBaseGenericRepository<WeekMenuText>>>()
    )
);
```

---

## 5. v1 vs v2 Boundary (CLEAR SEPARATION)

### 5.1 What is v1 (This Sprint)

| Feature | v1 | v2 |
|---------|----|----|
| Text-based meal history | ✅ | — |
| Weekly suggestion engine | ✅ | — |
| UI: View/edit week entries | ✅ | — |
| Recipe CRUD | ❌ | ✅ |
| Ingredients per recipe | ❌ | ✅ |
| Shopping list generation | ❌ | ✅ |
| Meal categories (Fish, Meat, Kids) | ❌ | ✅ |
| Multi-user family sharing | ❌ | v2+ (future) |

### 5.2 Data Model Independence

- **v1 uses:** `WeekMenuText` (flat text entries)
- **v2 will use:** `WeekMenu` + `DailyMeal` + `MealRecipe` (hierarchical with ingredients)
- **No conversion path:** v1 and v2 entities are completely separate (legacy data can be migrated manually if needed)
- **Database isolation:** Different Firestore collections (`weekmenutexts` vs `weekmenus`)

### 5.3 Implementation Isolation

- **v1 code:** `WeekMenuTextController`, `WeekMenuTextPage.razor`, `WeekMenuTextListPage.razor`
- **v2 code:** New controllers/pages (TBD in future sprint)
- **No shared code:** To keep v1 simple and avoid premature architecture

---

## 6. Implementation Tickets (Ready to Create)

### Ticket 1: Backend Data Model + API Controller

**Title:** `[Meal v1] Implement WeekMenuText API controller + Firestore model`

**Owner:** Ray (Data Layer) + Glenn (API Controller)

**Description:**
- Create `Shared/FireStoreDataModels/WeekMenuText.cs` (Firestore entity)
- Create `Shared/HandlelisteModels/WeekMenuTextModel.cs` (DTO)
- Add AutoMapper profile in `Api/ShoppingListProfile.cs`:
  ```csharp
  CreateMap<WeekMenuText, WeekMenuTextModel>().ReverseMap();
  ```
- Create `Api/Controllers/WeekMenuTextController.cs`:
  - GET `/api/weekmenutexts` (list all)
  - GET `/api/weekmenutext/{id}` (single)
  - POST `/api/weekmenutexts` (create)
  - PUT `/api/weekmenutexts` (update)
  - DELETE `/api/weekmenutext/{id}` (soft-delete)
  - POST `/api/weekmenutexts/suggest` (suggestions)
- Register repository in `Api/Program.cs`
- Add tests: `Api.Tests/Controllers/WeekMenuTextControllerTests.cs` (6 tests: CRUD + suggest)

**Acceptance Criteria:**
- ✅ All 6 endpoints respond correctly with sample data
- ✅ AutoMapper converts both directions without errors
- ✅ Timestamps (CreatedDate, LastModified) set automatically
- ✅ All 6 controller tests passing
- ✅ DELETE marks `isArchived = true`, does not hard-delete

**Estimate:** 3–4 team-days

---

### Ticket 2: Suggestion Algorithm

**Title:** `[Meal v1] Implement meal suggestion algorithm`

**Owner:** Glenn (API Logic)

**Description:**
- Implement frequency-based suggestion logic in `WeekMenuTextController.cs`
- Algorithm (from section 2.2):
  1. Fetch all non-archived week menus
  2. Build frequency map per day
  3. Filter out meals from past 2 weeks
  4. Pick top suggestions randomly
- Handle edge cases (no data, duplicates, long text)
- Add unit tests: `Api.Tests/SuggestionAlgorithmTests.cs` (5 tests)

**Acceptance Criteria:**
- ✅ Suggestion endpoint returns 7 meals (one per day)
- ✅ Suggestions vary week-to-week (randomization works)
- ✅ No suggestions from past 2 weeks
- ✅ Handles empty history gracefully
- ✅ All 5 algorithm tests passing

**Estimate:** 2–3 team-days

---

### Ticket 3: Frontend List Page

**Title:** `[Meal v1] Create WeekMenuTextListPage — week menu overview`

**Owner:** Blair (UI)

**Description:**
- Create `Client/Pages/Shopping/WeekMenuTextListPage.razor`
- Route: `/week-menus`
- Display:
  - List of non-archived week menus (newest first)
  - Each item: "Uke 47, 2026" + created date + [Edit] [Delete]
- Actions:
  - Create new: Quick form to create menu for specific week
  - Delete: Soft-delete (archive) modal confirmation
  - Expandable: Show archived weeks on click
- Styling: Reuse `.card` + `.todo-list` CSS classes
- Service calls:
  - GET `/api/weekmenutexts` (on page load)
  - POST `/api/weekmenutexts` (create new)
  - DELETE `/api/weekmenutext/{id}` (archive)

**Acceptance Criteria:**
- ✅ Page loads and displays existing week menus
- ✅ Create new week menu works
- ✅ Delete/archive button works (no hard delete)
- ✅ Archived weeks hidden by default, expandable
- ✅ Navigation links to single week editor

**Estimate:** 2–3 team-days

**Dependencies:** None (can start immediately)

---

### Ticket 4: Frontend Week Editor Page

**Title:** `[Meal v1] Create WeekMenuTextPage — week meal editor`

**Owner:** Blair (UI)

**Description:**
- Create `Client/Pages/Shopping/WeekMenuTextPage.razor`
- Route: `/week-menu/{weekNumber:int}/{year:int}`
- Display:
  - Header: "Ukemeny — Uke 47, 2026"
  - Calendar grid: 7 text inputs (Monday–Sunday)
  - Buttons: Foreslå, Lagre, Previous week, Next week
- Functionality:
  - Load week data on page load (GET `/api/weekmenutext/{id}`)
  - "Foreslå" button: Call POST `/api/weekmenutexts/suggest` → populate fields
  - "Lagre" button: Call PUT `/api/weekmenutexts` → show toast
  - Handle week not found (create new or redirect)
- Styling: Card per day, responsive grid

**Acceptance Criteria:**
- ✅ Page loads existing week or shows empty form
- ✅ Suggest button populates fields from API
- ✅ Save button persists changes
- ✅ Navigation buttons work (previous/next week)
- ✅ Toast notifications on save success/error (requires #25)
- ✅ Mobile responsive

**Estimate:** 3–4 team-days

**Dependencies:** #25 (toast system must be complete)

---

### Ticket 5: Navigation Integration

**Title:** `[Meal v1] Add week menu link to Admin navigation`

**Owner:** Blair (UI)

**Description:**
- Update `Client/Shared/NewNavComponent.razor`
- Add link to `/week-menus` in Admin dropdown
  - Option A: New item "Ukemenyer" in admin dropdown
  - Option B: Add to main menu (if Daniel prefers)
- Styling: Consistency with existing admin items
- Navigation check: Ensure page is reachable from main menu

**Acceptance Criteria:**
- ✅ Navigation link appears in expected location
- ✅ Link works and navigates to `/week-menus`
- ✅ No console errors
- ✅ Styling consistent with admin nav

**Estimate:** 0.5 team-day

**Dependencies:** Tickets 3 & 4 (pages must exist)

---

### Ticket 6: E2E Tests

**Title:** `[Meal v1] Add E2E tests for week menu workflow`

**Owner:** Josh (QA/Tests)

**Description:**
- Create `Client.Tests.Playwright/Tests/WeekMenuTextTests.cs`
- Test scenarios:
  1. Navigate to week menus, see empty list
  2. Create new week menu
  3. Enter meal data
  4. Save and verify persistence
  5. Click "Foreslå" and verify suggestions populate
  6. Archive a week menu
- Use Playwright assertions + existing E2E patterns

**Acceptance Criteria:**
- ✅ All 6 E2E tests passing
- ✅ No console errors
- ✅ Cross-browser tested (Chrome, Edge)
- ✅ Mobile viewport tested

**Estimate:** 1–2 team-days

**Dependencies:** All tickets 1–5 complete

---

## 7. Implementation Timeline & Dependencies

### Recommended Execution Order

```
Ticket 1 (Backend) ─┐
                    ├─→ Ticket 2 (Suggestion) ─┐
                                              ├─→ Ticket 6 (E2E)
Ticket 3 (List UI) ─┤
                    ├─→ Ticket 4 (Editor UI, depends #25) ─┤
                                                           │
Ticket 5 (Nav) ──────────────────────────────────────────┘
```

**Blocking Dependencies:**
- Ticket 1 must complete before Ticket 2 (algorithm depends on API)
- Ticket 2 should complete before Ticket 4 (suggest button works)
- #25 (Toast system) must complete before Ticket 4 starts (user feedback)
- Tickets 3 & 4 should complete before Ticket 5 (nav links to working pages)
- All tickets must complete before Ticket 6 (E2E tests)

### Estimated Team Effort
- Total: **13–16 team-days** across 2 weeks
- Can run Tickets 1–3 in parallel (different owners)
- Ticket 4 should wait for #25 to unblock
- Ticket 5 can run in parallel with others
- Ticket 6 final week after all features complete

### Sprint Fit
- **Sprint 4:** Tickets 1–5 (assuming #25 completes first)
- **Sprint 5:** Ticket 6 + any rework from testing

---

## 8. Questions Resolved

### Q1: Data format for v1?
✅ **Answer:** Flat Dictionary<string, string> per week (one key per day, free text values). No recipe objects.

### Q2: Storage?
✅ **Answer:** Single `WeekMenuText` entity with `Dictionary<string, string> DailyMeals` property. Firestore collection: `weekmenutexts`.

### Q3: Suggestion engine for v1?
✅ **Answer:** Simple frequency-based picker from historical text entries. No AI — just count occurrences and randomize top options.

### Q4: UI scope for v1?
✅ **Answer:** Two pages: `WeekMenuTextListPage` (overview) + `WeekMenuTextPage` (editor with 7 text inputs + Foreslå button).

### Q5: Import mechanism?
✅ **Answer:** Manual text entry only for v1. File upload deferred to v2.

### Q6: Integration with shopping lists?
✅ **Answer:** None for v1. Standalone feature. Shopping list generation deferred to v2 with recipe CRUD.

---

## 9. Blocking Issues & Constraints

### Must Complete First
- **#25 (Toast system)** — Required for UI feedback in week editor page (Ticket 4)

### May Run in Parallel
- Tickets 1–3 can start immediately (no external dependencies)
- Ticket 5 can start after Tickets 3–4 exist

### Constraints from decisions.md
- ✅ **D3** (MealIngredient embedding) — N/A for v1 (text-based, no recipes)
- ✅ **D4** (Collection key convention) — Implemented; `weekmenutexts` follows convention automatically
- ✅ **D18** (Meal v1 scope) — THIS DOCUMENT confirms v1 = text history + suggestions (DONE)
- ✅ **D19** (i18n) — All UI strings in Norwegian for v1 ✅, code in English ✅

### No Architectural Conflicts
- v1 uses completely separate entities (`WeekMenuText`) — zero coupling with future v2 recipe CRUD
- All controllers follow existing patterns (inherited from ControllerBase, standard CRUD + suggest endpoint)
- AutoMapper integration standard (add to existing profile)
- Repository pattern consistent with existing code

---

## 10. Out of Scope (v2 or Later)

❌ Recipe CRUD  
❌ Ingredients per recipe  
❌ Shopping list generation  
❌ Meal categories/icons (Fish, Meat, Kids)  
❌ Multi-user family sharing (future auth work)  
❌ AI-powered suggestions (frequency-based only)  
❌ Nutritional information  
❌ File import/export  
❌ Meal ratings / favorites system  

---

## 11. Success Criteria for v1 Complete

- [ ] All 6 API endpoints working (GET, POST, PUT, DELETE, suggest)
- [ ] All 6 backend tests passing (65 → 71 total API tests)
- [ ] Both UI pages (list + editor) rendering correctly
- [ ] Suggest button populates fields from API
- [ ] Save persists data to Firestore
- [ ] Navigation link added to admin menu
- [ ] E2E tests passing (all 6 workflow tests)
- [ ] No console errors
- [ ] Documentation updated (this scope + README if needed)
- [ ] Ready for Daniel review + user acceptance testing

---

## 12. Known Limitations & Future Enhancements

### v1 Limitations (Accepted Trade-offs)
- No recipe database → Text entries are free-form (typos not caught)
- No ingredient management → Shopping list must be created manually
- No multi-user isolation → Family app assumption (auth deferred)
- Suggestions may repeat (not "smart")

### Future Enhancements (v2+)
- Autocomplete meal suggestions from historical entries (fuzzy match)
- Meal rating system (users rate meals, influence suggestions)
- Category-based suggestion balance (e.g., "2 fish, 2 meat, 1 veg per week")
- Integration with shopping lists (select meals → auto-generate list)
- Mobile app sync (Web + native app)
- Recipe sharing between users/families
- Nutritional information display

---

## Approvals & Sign-Off

**Scoped by:** Peter (Lead/Architect)  
**Reviewed by:** (Pending Daniel feedback)  
**Approved by:** (Pending Daniel sign-off)  

**Date Finalized:** 2026-03-29  
**Ready to Create Tickets:** ✅ YES

---

## Document History

| Date | Author | Change |
|------|--------|--------|
| 2026-03-29 | Peter | Initial scope document, all 6 questions resolved, 6 implementation tickets defined |

