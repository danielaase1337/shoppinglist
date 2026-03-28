# Glenn Auth API — Implementation Decisions

**Date:** 2026-03-28
**Branch:** squad/auth-workflow
**Author:** Glenn (Backend Dev)

---

## Decision: HttpRequestData vs HttpRequest

**Chose `HttpRequestData`** (from `Microsoft.Azure.Functions.Worker.Http`), NOT `HttpRequest` (ASP.NET Core).

**Rationale:** This project uses Azure Functions Isolated Worker model. All existing controllers receive `HttpRequestData` as the trigger parameter. There is no ambient `Request` property on the controller class. Using `HttpRequest` would require a framework dependency not present in the project.

**Impact:** `ClientPrincipal.Parse()` and all `AuthExtensions` methods take `HttpRequestData`. `ControllerBase` helper methods also take `req` as a parameter rather than using a property accessor.

---

## Decision: ControllerBase Helper Signature

**Pattern chosen:**
```csharp
protected ClientPrincipal? GetCurrentUser(HttpRequestData req)
protected string? GetCurrentUserId(HttpRequestData req)
protected string? GetCurrentUserName(HttpRequestData req)
```

**Rejected alternative:** `protected ClientPrincipal? GetCurrentUser() => Request.GetClientPrincipal()` — not viable because Azure Functions Isolated Worker has no ambient `Request` property.

---

## Decision: v1 Auth Enforcement Level

Per **D2** (family app, no per-user isolation): auth is parsed and available everywhere, but NOT enforced as a gate on any endpoint in v1. Read operations remain open. Write enforcement (returning 401) deferred to a future sprint when per-user isolation (v2/FamilyId) is implemented.

**Logging principle:** When a principal is available, controllers can log the user for observability. No behavioral change based on auth status in v1.

---

## Decision: DebugFunction Production Guard

Used `#if !DEBUG` preprocessor directive — returns `HttpStatusCode.NotFound` in Release builds. This is a compile-time gate, matching the build configuration used for production deployments. No environment variable check needed.

---

## Decision: Startup Log Placement

Log is emitted after `host.Build()` (resolves `ILogger<Program>` from DI) and before `host.Run()`. This confirms auth infrastructure is wired up without requiring a middleware registration step.
