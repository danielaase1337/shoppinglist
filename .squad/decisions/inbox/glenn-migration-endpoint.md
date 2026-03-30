# Decision: Extract LastModified Lazy Migration to Admin Endpoint

**Decision ID:** D10 (resolves)
**Date:** 2026-04-XX
**Author:** Glenn (Backend Dev)
**Branch:** sprint/2
**Refs:** Issue #31

## Status
✅ IMPLEMENTED

## Context
`ShoppingListController` GET endpoints (`RunAll` and `RunOne`) contained inline lazy-migration logic:
if a `ShoppingList` document had a null `LastModified`, the GET would write back `DateTime.UtcNow` before returning.

This caused an N+1 write pattern on reads — every GET for a legacy list triggered a Firestore write — slowing
GET responses and coupling reads to write operations.

## Decision
Extract the migration to a dedicated one-time admin endpoint:

```
GET /api/admin/migrate-lastmodified
```

- Iterates all `ShoppingList` documents with null `LastModified`
- Sets `LastModified = DateTime.UtcNow` and persists each one
- Returns `{ "migratedCount": N }`
- Gated by `"admin"` role in SWA-injected `x-ms-client-principal` (returns 403 if absent)
- Additional `AuthorizationLevel.Function` (Azure Functions key) layer

## Files Changed
- **Created**: `Api/Controllers/AdminController.cs`
- **Modified**: `Api/Controllers/ShoppingListController.cs` — removed inline migration from `RunAll` GET and `RunOne` GET

## Auth Strategy
Used existing `GetCurrentUser(req).UserRoles` check for `"admin"` role. Consistent with auth infrastructure
established in sprint/auth-workflow (D23). No new libraries or patterns introduced.

## Trade-offs
- **Pro**: GET endpoints are now fully read-only — no write side-effects on reads
- **Pro**: Migration is idempotent — safe to re-run; documents already having `LastModified` are skipped
- **Pro**: Follows `MigrateFrequentListsController` precedent for one-time migration endpoints
- **Con**: Requires a user with "admin" role to be set up in SWA roles management before running
- **Note**: Legacy documents will no longer self-heal via GETs. Run endpoint once after deployment.

## Follow-up
- Assign "admin" role to Daniel Aase in Azure SWA roles management before running against production
- After migration is confirmed complete, this endpoint can remain as-is (idempotent, no harm) or be removed
