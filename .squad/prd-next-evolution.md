# Product Requirements Document: Shopping List Application — Next Evolution

**Version:** 1.0  
**Date:** 2025-01-29  
**Author:** Peter (Lead), synthesised from team audit findings  
**Contributors:** Blair (Frontend), Glenn (Backend), Ray (Firebase), Josh (Testing)

---

## Executive Summary

The shopping list application is a well-architected Blazor WebAssembly + Azure Functions app with a standout shop-specific sorting feature, robust caching, and natural alphanumeric sorting. However, four independent audits have revealed a **critical production bug** (5 entity types writing to the wrong Firestore collection), **zero authentication or user isolation**, a **half-built meal planning system** with no frontend, **143 tests that provide false confidence** (API tests validate mocks not controllers, E2E tests always pass, no tests run in CI), and significant **mobile usability gaps** (drag-and-drop broken on iOS, admin nav unreachable on touch). This PRD defines a phased roadmap to stabilise the foundation, complete the meal planning feature, and prepare the application for multi-user production deployment.

---

## Current State Assessment

### Strengths (keep and build on)
- **Shop-specific shelf-order sorting** — flagship feature, well-implemented with O(1) dictionary lookups
- **Cache-first architecture** — `DataCacheService` + `BackgroundPreloadService` deliver fast page loads
- **Natural alphanumeric sorting** — 11 comprehensive unit tests, handles "Uke 1" through "Uke 43"
- **Smart list ordering** — multi-level sort (active → newest → natural alpha) is excellent UX
- **Clean repository abstraction** — `IGenericRepository<T>` enabled trivial Memory ↔ Firestore swap
- **AutoMapper profile** — bidirectional mappings for all entities including meal planning models
- **Parallel data loading** — `Task.WhenAll()` in `OneShoppingListPage` minimises wait time

### Critical Issues (from all 4 audits)

| # | Issue | Source | Impact |
|---|-------|--------|--------|
| 1 | **`GetCollectionKey()` maps only 4 of 9 entity types** — FrequentShoppingList, MealRecipe, MealIngredient, WeekMenu, DailyMeal all write to `"misc"` collection, causing silent data corruption in production | Glenn, Ray | Data loss / corruption |
| 2 | **Zero authentication** — API is publicly accessible; 4 of 6 controllers use `AuthorizationLevel.Anonymous` | Glenn, Blair | Security breach |
| 3 | **Zero user data isolation** — no `OwnerId` field on any entity; all users see all data | Glenn, Ray | Privacy violation |
| 4 | **No tests run in CI** — GitHub Actions workflow builds and deploys but never executes `dotnet test` | Josh | Silent regressions ship |
| 5 | **API tests validate mock infrastructure, not controllers** — 65 tests call `_mockRepository.Object.Get()` directly, zero controller code paths are exercised | Josh | False confidence |
| 6 | **Meal planning has no frontend** — MealRecipeController exists but no Blazor pages, no navigation entry, no WeekMenuController | Blair, Glenn | Feature incomplete |
| 7 | **WeekMenu/DailyMeal not registered in DI** — no repository registration, no controller, no collection key | Glenn, Ray | Feature non-functional |
| 8 | **Mobile drag-and-drop broken on iOS** — HTML5 drag API unsupported on mobile Safari; ShopConfigurationPage and CategoryManagementPage unusable on phones | Blair | Major UX gap |
| 9 | **Zero user feedback on async operations** — no toasts, no spinners, no error messages; all API failures logged to console only | Blair | Poor UX, user confusion |
| 10 | **Deep embedding anti-pattern** — ShoppingList embeds full ShopItem objects 4 levels deep; WeekMenu goes 5 levels; stale data and 1MB document limit risk | Ray | Scalability, data staleness |

---

## Issues & Opportunities — Top 10 Prioritised by Impact × Effort

| Rank | Issue / Opportunity | Impact | Effort | Priority | Owner |
|------|---------------------|--------|--------|----------|-------|
| 1 | **Fix `GetCollectionKey()` for all entity types** | Critical — data corruption | Low (add 5 lines to switch statement) | 🔴 P0 | Ray |
| 2 | **Add `dotnet test` step to CI workflow** | Critical — regressions ship silently | Low (1 YAML line) | 🔴 P0 | Josh |
| 3 | **Rewrite API tests to call controller methods** | High — 65 tests provide false assurance | Medium (refactor test classes) | 🔴 P1 | Josh |
| 4 | **Complete WeekMenu backend** (controller, DI, collection keys) | High — blocks meal planning feature | Medium (follow MealRecipeController pattern) | 🔴 P1 | Glenn |
| 5 | **Add toast/notification system for async feedback** | High — core UX quality | Medium (create NotificationService + ToastComponent) | 🔴 P1 | Blair |
| 6 | **Implement Azure SWA authentication** | High — security prerequisite for multi-user | High (auth provider setup, client integration, API middleware) | 🔴 P1 | Glenn + Blair |
| 7 | **Build meal planning UI pages** (MealManagement, WeekMenu) | High — flagship new feature | High (4 pages, shared components, nav changes) | 🟡 P2 | Blair |
| 8 | **Fix mobile touch interactions** (admin dropdown, drag-drop alternative) | Medium — blocks mobile users | Medium (Blazor toggle, up/down buttons) | 🟡 P2 | Blair |
| 9 | **Add per-user data isolation** (`OwnerId` + filtered queries) | High — coupled to auth | Medium (model + repo changes, migration) | 🟡 P2 | Ray + Glenn |
| 10 | **Resolve MealIngredient storage contradiction** (embedded vs. separate repo) | Medium — contradictory design causes confusion | Low (remove unused repo registration or use subcollection) | 🟡 P2 | Ray |

---

## Proposed Solution

### Phase 0: Stabilisation (MVP — Do Now)
**Goal:** Fix critical bugs and establish a safety net before any feature work.

| Item | Description | Acceptance Criteria |
|------|-------------|---------------------|
| Fix collection mapping | Add all entity types to `GoogleDbContext.GetCollectionKey()` | All 9 entity types resolve to correct, unique collections |
| CI test execution | Add `dotnet test --filter "FullyQualifiedName!~Playwright"` to GitHub Actions workflow | Unit tests run on every push/PR; pipeline fails on test failure |
| Fix API test architecture | Refactor `Api.Tests` to call controller `RunAll`/`RunOne` methods with real `HttpRequestData` | Tests exercise actual controller code paths including AutoMapper, validation, error handling |
| Add FrequentShoppingList tests | Create `FrequentShoppingListControllerTests.cs` | CRUD operations covered |
| Fix ShopItemCategoryController | Add try/catch to `RunOne`; return DTOs not Firestore models | Consistent error handling; correct response types |
| Fix `.Result` blocking call | Change `ShopsController.Run` to use `await` instead of `.Result` | No thread pool blocking |
| Scrub error messages | Replace `e.Message` in `GetErroRespons()` with generic message | Internal error details never reach client |
| Fix GET not-found status | Return `404` instead of `500` for missing resources | Correct HTTP semantics |

**Estimated effort:** 3–5 days  
**Dependencies:** None  
**Risk:** Low — all changes are isolated fixes

---

### Phase 1: Core UX Quality + Meal Planning Backend
**Goal:** Deliver user-visible quality improvements and complete the meal planning API.

#### 1A: UX Quality (Blair)
| Item | Description |
|------|-------------|
| Toast notification system | Create `NotificationService` + `ToastComponent`; integrate into all async operations (add/delete/save/error) |
| Delete confirmation | Replace all `JSRuntime confirm()` calls with `ConfirmDelete` modal component (already exists) |
| Loading states | Add disabled button + spinner state to all async action buttons |
| Empty state messages | Show helpful message + CTA when lists have no items |
| `@key` directive | Add `@key="item.Id"` to all `@foreach` loops for efficient Blazor diffing |
| Active nav indicator | Highlight current page in `NewNavComponent` |
| Keyboard-accessible admin dropdown | Replace CSS `:hover` with Blazor toggle + `aria-expanded` + keyboard handling |

#### 1B: Meal Planning Backend (Glenn + Ray)
| Item | Description |
|------|-------------|
| WeekMenu controller | Full CRUD + `GET /api/weekmenu/week/{weekNumber}/year/{year}` |
| DI registration | Register `IGenericRepository<WeekMenu>` in both DEBUG and RELEASE blocks |
| Generate shopping list endpoint | `POST /api/weekmenu/{id}/generate-shoppinglist` — aggregate ingredients, sum quantities, return `ShoppingListModel` |
| Resolve MealIngredient contradiction | Decision: remove separate `MealIngredient` repo (embedded in MealRecipe) OR use Firestore subcollection |
| Input validation enforcement | Call `model.IsValid()` in all POST/PUT handlers; return 400 if invalid |
| Extract LastModified migration | Move inline migration from GET handler to dedicated admin endpoint |

#### 1C: Test Infrastructure (Josh)
| Item | Description |
|------|-------------|
| Page Object Model | Create `ShoppingListPage`, `MealManagementPage`, `WeekMenuPage` page objects with `data-testid` selectors |
| Real E2E flow tests | Implement: create list → add item → check off → sort by shop (4 test cases minimum) |
| Remove always-pass tests | Delete or promote all `Assert.True(true)` tests to real assertions |
| Centralise BaseUrl | Move to `PlaywrightFixture` via environment variable |
| Meal planning API tests | Tests for MealRecipe CRUD (calling actual controller methods) |
| WeekMenu API tests | Tests for WeekMenu CRUD + generate-shoppinglist |

**Estimated effort:** 8–12 days  
**Dependencies:** Phase 0 complete  
**Risk:** Medium — meal planning backend is new code

---

### Phase 2: Meal Planning UI + Mobile + Auth Foundation
**Goal:** Complete the meal planning user experience and address mobile gaps.

#### 2A: Meal Planning Frontend (Blair)
| Page | Description |
|------|-------------|
| `MealManagementPage` (`/meals`) | List all recipes with category icons (👶🐟🥩🥬), popularity scores, search, add/delete |
| `OneMealRecipePage` (`/meals/{id}`) | Recipe detail with ingredient management using `SfAutoComplete` linked to ShopItems |
| `WeekMenuListPage` (`/weekmenus`) | Overview of weekly menus, create new for selected week |
| `OneWeekMenuPage` (`/weekmenus/{id}`) | 7-day calendar view with meal dropdown per day; "Generer handleliste" button with preview modal |
| Navigation changes | Add "Middager" and "Ukemeny" links under Admin dropdown |
| Settings/Enum changes | Add `MealRecipes`, `MealRecipe`, `WeekMenus`, `WeekMenu` to `ShoppingListKeysEnum` and URL mappings |

#### 2B: Mobile Fixes (Blair)
| Item | Description |
|------|-------------|
| Touch-friendly reorder | Add up/down arrow buttons as alternative to drag-and-drop on touch devices |
| Admin dropdown touch | Convert to Blazor-toggled menu with `@onclick` instead of CSS `:hover` |
| iOS drag polyfill | Add `mobile-drag-drop` polyfill or equivalent for Safari |
| ListSummaryFooter layout | Replace hardcoded `padding-left: 50px` with flex layout |
| Base font-size | Increase body font-size from 11px to 16px; remove compensating overrides |

#### 2C: Auth Foundation (Glenn + Blair)
| Item | Description |
|------|-------------|
| Auth provider decision | Choose: Azure SWA built-in auth (recommended — zero extra infrastructure) vs Firebase Auth vs Azure AD B2C |
| `OwnerId` field | Add to `EntityBase` or root entities; deploy with empty default |
| SWA auth integration | Client-side `AuthenticationStateProvider` + `AuthorizeRouteView` in `App.razor` |
| API auth middleware | Parse `x-ms-client-principal` header; reject unauthenticated requests |
| Login page | `LoginPage.razor` with provider selection |
| User indicator | Add avatar/name and "Log out" button to `NewNavComponent` |

**Estimated effort:** 15–20 days  
**Dependencies:** Phase 1 complete; auth provider decision  
**Risk:** High — auth touches every layer; meal planning UI is substantial new surface area

---

### Phase 3: Enhancements & Scale
**Goal:** Polish, performance, and AI-assisted features.

| Item | Description | Priority |
|------|-------------|----------|
| Menu suggestion algorithm | `POST /api/weekmenu/suggest` — exclude recent meals, balance categories, respect preferences | Medium |
| Suggestion UI | "🤖 Foreslå meny" button in `OneWeekMenuPage` with suggestion modal | Medium |
| Repository query support | Add `Query(Expression<Func<T, bool>> predicate)` to `IGenericRepository<T>` | Medium |
| API caching | `IMemoryCache` for Shop/ItemCategory/ShopItem with 5-10 min TTL | Medium |
| Client virtualisation | `Virtualize` component for shopping item and recipe lists (50+ items threshold) | Medium |
| CSS design tokens | Introduce CSS custom properties for colour palette and spacing scale | Medium |
| Normalise ShopItem references | Replace embedded objects with ID references in ShoppingListItem | Low |
| Firestore composite indexes | Add for `(IsActive, PopularityScore)`, `(Year, WeekNumber)` | Low |
| Dark mode | `@media (prefers-color-scheme: dark)` support | Low |
| Accessibility audit | ARIA labels, focus traps, colour contrast, skip-to-content link | Low |
| Pagination | Cursor-based pagination in `IGenericRepository<T>` | Low |

---

## Technical Debt Priorities

| # | Debt Item | Impact | Effort | When |
|---|-----------|--------|--------|------|
| 1 | `GetCollectionKey()` incomplete mapping | Data corruption | Low | Phase 0 |
| 2 | API tests test mocks not controllers | False safety net | Medium | Phase 0 |
| 3 | `MealIngredient` repo registered but data is embedded | Contradictory design | Low | Phase 1 |
| 4 | `.Result` blocking call in `ShopsController` | Potential deadlock | Low | Phase 0 |
| 5 | `ShopItemCategoryController.RunOne` returns Firestore model | Data leakage risk | Low | Phase 0 |
| 6 | `Console.WriteLine` in `GoogleDbContext` | Should use `ILogger` | Low | Phase 1 |
| 7 | Hardcoded Firestore project ID | Prevents multi-env | Low | Phase 1 |
| 8 | `Oldapp.css` legacy stylesheet | Dead code confusion | Low | Phase 2 |
| 9 | `CssComleteEditClassName` typo | Dev confusion | Low | Phase 2 |
| 10 | `ListId` redundant field on ShoppingList | Duplicates `Id` | Low | Phase 3 |
| 11 | `ShelfCategory` orphaned class | Dead code | Low | Phase 3 |
| 12 | `MealCategory` duplicated across namespaces | Maintenance burden | Low | Phase 1 |
| 13 | `!important` CSS overuse (50+ instances) | Restyling pain | Medium | Phase 3 |
| 14 | Deep embedding anti-pattern (4-5 levels) | Scale/staleness | High | Phase 3 |
| 15 | N+1 LastModified migration in GET handler | Performance on legacy data | Low | Phase 1 |

---

## Testing Roadmap

### Phase 0 (Stabilisation)
- [ ] Add `dotnet test` to CI pipeline (1 YAML line)
- [ ] Refactor API tests to call controller methods, not mocks (65 tests)
- [ ] Add `FrequentShoppingListController` tests
- [ ] Remove all `Assert.True(true)` tests

### Phase 1 (Core Quality)
- [ ] Create Page Object Model classes for Playwright
- [ ] Add `data-testid` attributes to key Blazor elements
- [ ] Implement 4+ real E2E flow tests (create list, add item, check off, sort)
- [ ] Add MealRecipe controller tests (calling actual handlers)
- [ ] Add WeekMenu controller tests + generate-shoppinglist test

### Phase 2 (Feature Complete)
- [ ] Meal planning E2E tests (create recipe, plan week, generate list)
- [ ] Auth flow tests (login, logout, protected routes, 401 handling)
- [ ] Mobile viewport tests (375px, 768px breakpoints)
- [ ] Centralise test BaseUrl via environment variable

### Phase 3 (Quality Bar)
- [ ] Accessibility tests (axe-core integration)
- [ ] Performance baseline assertions (page load < 5s, API < 1s)
- [ ] Suggestion algorithm unit tests (exclusion, category balance, edge cases)
- [ ] Contract tests between API and Client models

### Test Count Targets
| Phase | Unit | API Integration | E2E | Total |
|-------|------|-----------------|-----|-------|
| Current | 76 (real) | 0 (65 are mock-only) | ~5 (real) | ~81 |
| After Phase 0 | 76 | 65 (refactored) | 5 | 146 |
| After Phase 1 | 76 | 85 | 15 | 176 |
| After Phase 2 | 90 | 100 | 30 | 220 |
| After Phase 3 | 100 | 110 | 45 | 255 |

---

## Success Metrics

| Metric | Current | Phase 0 Target | Phase 2 Target |
|--------|---------|----------------|----------------|
| Tests running in CI | 0 | 141+ | 200+ |
| Real API tests (testing controllers) | 0 | 65+ | 100+ |
| E2E flow tests (not smoke) | 0 | 4+ | 30+ |
| Entity types with correct collection mapping | 4/9 | 9/9 | 9/9 |
| Controllers with input validation | 0/6 | 6/6 | 6/6 |
| Pages with async feedback (toasts/spinners) | 0/10 | 5/10 | 10/10 |
| Mobile-usable admin pages | 2/5 | 3/5 | 5/5 |
| Meal planning pages implemented | 0/4 | 0/4 | 4/4 |
| Authentication coverage | None | None | Full SWA auth |
| WCAG AA compliance | Poor | — | Partial |

---

## Timeline & Effort Estimates

| Phase | Duration | Team Effort | Parallel? |
|-------|----------|-------------|-----------|
| **Phase 0: Stabilisation** | 1 week | ~5 person-days | All streams parallel |
| **Phase 1: Core Quality + Meal Backend** | 2 weeks | ~12 person-days | UX, Backend, Tests parallel |
| **Phase 2: Meal UI + Mobile + Auth** | 3–4 weeks | ~20 person-days | UI and Auth can overlap |
| **Phase 3: Enhancements** | Ongoing | Backlog items | As capacity allows |

**Total to feature-complete (through Phase 2):** 6–7 weeks, ~37 person-days

---

## Risk Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Auth provider choice delays Phase 2 | Medium | High | Make decision in Phase 0; Azure SWA built-in auth is the lowest-friction option for this architecture |
| Deep embedding causes data issues at scale | Medium | High | Phase 3 normalisation planned; monitor document sizes in production |
| Syncfusion version bump breaks E2E selectors | High | Medium | Page Object Model + `data-testid` attributes decouple tests from library internals |
| Meal planning scope creep | Medium | Medium | Phase 1 is backend-only; ship API before building UI |
| `misc` collection already has corrupted data | Low | High | Audit production Firestore for `misc` collection documents; migrate if needed before fixing `GetCollectionKey()` |
| iOS drag-drop polyfill insufficient | Low | Medium | Fallback to up/down arrow buttons (works on all platforms) |

---

## Dependencies

| Dependency | Blocks | Decision Needed By |
|------------|--------|-------------------|
| Auth provider choice (Azure SWA vs Firebase vs custom) | Phase 2C (Auth) | End of Phase 0 |
| MealIngredient storage decision (embedded vs subcollection) | Phase 1B (Meal Backend) | Start of Phase 1 |
| `data-testid` attributes added to components | Phase 1C (E2E tests) | Start of Phase 1 |
| Production Firestore `misc` collection audit | Phase 0 collection fix | Before deploying fix |
| Syncfusion license for new components (SfGrid, etc.) | Phase 2A (Meal UI) | Start of Phase 2 |

---

## Open Questions

1. **Auth provider**: Azure SWA built-in auth (recommended — zero extra infrastructure) vs Firebase Auth (already have Firestore) vs Azure AD B2C (enterprise-grade but complex)?

2. **ShopItem/ItemCategory ownership**: Should the product catalogue be global/shared across all users, or per-user? Global is simpler but limits personalisation.

3. **`misc` collection in production**: Has any data already been written to the `misc` collection? If so, what entities? Need to audit before deploying the collection key fix.

4. **MealIngredient storage**: Keep embedded in MealRecipe (simpler, current design) or move to Firestore subcollection (enables independent queries)? Recommendation: keep embedded — ingredients are always accessed through their recipe.

5. **Meal planning MVP scope**: Should Phase 2 ship with the AI suggestion algorithm (Phase 3 in original plan), or is manual meal selection sufficient for launch?

6. **Norwegian vs English naming**: New entities should use which language for property names? Current code mixes (`Varen`, `Mengde` vs `ItemCategory`, `ShopItem`). Recommendation: English for new code, preserve Norwegian for backward compatibility.

7. **Performance budget**: What is the acceptable page load time? Current caching makes most loads fast, but no baseline is measured or enforced.

8. **Multi-device sync**: Once auth exists, should list changes sync in real-time (Firestore listeners) or on manual refresh? Real-time is significantly more complex.

---

*This PRD synthesises findings from Blair (UI/UX), Glenn (API/Security), Ray (Data Architecture), and Josh (Testing). All four audit reports are available in `.squad/agents/` for detailed reference.*
