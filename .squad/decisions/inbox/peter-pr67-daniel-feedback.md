# Decision: PR #67 Round 2 — Daniel's Follow-up Feedback

**Date:** 2026-04-23  
**Author:** Peter (Lead / Architect)  
**Source:** Daniel's 3 follow-up comments on PR #67 after sprint review

---

## Context
After the sprint review was posted (all 3 bugs fixed, 6 features implemented, 220 tests passing), Daniel tested the staging deployment and left additional feedback.

## Daniel's Feedback Summary

### Positive
- Bugs confirmed working ✅

### New Requests (Issues Created)
| Issue | Description | Priority |
|---|---|---|
| #81 | Undo "spist" on week menu — stock deduction should be reversible | P2 |
| #82 | ShopItem admin: unit field should be dropdown (SfDropDownList), not free text | P2 |
| #83 | Mobile responsiveness — all new pages are poor on mobile. Priority: content > buttons | P2 |

### Clarification on IsDone Hook
Daniel confirmed: stock update should trigger at **list level** (hele listen markert "ferdig"), not per individual item. Current implementation is correct. The N+1 concern is about Firestore writes within that operation, not the trigger level.

### Sprint-Review Follow-up Issues (Daniel Requested)
| Issue | Description | Priority |
|---|---|---|
| #77 | `Varen.IsBasic` not populated in generated shopping lists | **P1 — fix before merge** |
| #78 | No transaction guarantee in consume-meal endpoint | P3 |
| #79 | N+1 Firestore write pattern in IsDone stock hook | P3 |
| #80 | Unused WeekMenuConsume/WeekMenuSwap enum values | P4 |

## Decisions

1. **#77 blocks merge** — must be fixed in the `mealplanningv2` branch before PR #67 can be merged to `development`.
2. **#83 (mobile) is high-impact** — this is a family app used on phones while shopping. Should be prioritized in next sprint.
3. **#81 (undo-consume)** — good UX safety net. Schedule for next sprint alongside #83.
4. **#78, #79, #80** — tech debt, can be addressed opportunistically. No urgency.

## Action Items
- [ ] Glenn/Blair: Fix #77 (IsBasic population) in current branch
- [ ] Blair: #83 (mobile CSS) in next sprint
- [ ] Blair: #81 (undo-consume UI) + Glenn: backend endpoint
- [ ] Blair: #82 (unit dropdown) — quick win
