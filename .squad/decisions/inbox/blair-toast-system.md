# Decision: Toast/Notification System — D5 Resolved

**Author:** Blair (Frontend Dev)  
**Date:** 2026-04-01  
**Issue:** #25  
**Status:** ✅ IMPLEMENTED — resolves D5 (Option A chosen)

---

## Decision

Implemented **custom `INotificationService` + `ToastContainer`** (Option A from D5).  
No third-party dependencies added.

---

## What Was Built

| File | Purpose |
|------|---------|
| `Client/Services/INotificationService.cs` | Interface + `ToastMessage` model + `ToastType` enum |
| `Client/Services/NotificationService.cs` | Scoped service, fires `OnToast` event |
| `Client/Shared/ToastContainer.razor` | Subscribes to service, renders/dismisses toasts |
| `Client/wwwroot/css/app.css` | Toast CSS — fixed bottom-right, slide-in/out animation, mobile-responsive |

---

## API

```csharp
// Inject anywhere in Blazor
@inject INotificationService Notifications

Notifications.Success("Item saved");        // auto-dismiss 3s
Notifications.Error("Failed to save");      // auto-dismiss 5s
Notifications.Warning("No shop selected");  // auto-dismiss 4s
Notifications.Info("Loading data...");      // auto-dismiss 3s
```

---

## Architecture Notes

- **Event-driven**: `NotificationService` fires `Action<ToastMessage> OnToast`; `ToastContainer` subscribes and renders.
- **No polling**: Auto-dismiss uses `Task.Delay` + leave animation (300ms CSS transition before DOM removal).
- **Scoped DI**: Registered as `AddScoped` — consistent with Blazor WASM lifecycle.
- **IDisposable**: `ToastContainer` unsubscribes from event on dispose to prevent memory leaks.
- **Stacking**: Multiple toasts render as a flex-column, newest at bottom.

---

## Proof-of-Concept Wiring

Two operations in `OneShoppingListPage.razor`:
1. **`AddVare()`** — `Notifications.Success($"{shopItem.Name} lagt til")`
2. **`DeleteVare()`** — `Notifications.Success($"{name} fjernet fra listen")` + `Notifications.Error(...)` on failure

---

## Mobile

Container uses `calc(100vw - 3rem)` width with `bottom: 1rem; right/left: 0.75rem` on screens ≤ 480px.

---

## Downstream Unblocked

- **#28** (shop deletion safeguards) — can now use toast feedback for multi-step confirm + cascade check
