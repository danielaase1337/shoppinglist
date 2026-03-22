# Ray — Firestore Data Architecture Audit Report

**Date:** 2025-07-17  
**Author:** Ray (Firebase Expert)  
**Codebase:** `supergnisten-shoppinglist` — Blazor Static Web App + Azure Functions + Firestore  
**Purpose:** Comprehensive data model audit for Peter (Lead) PRD synthesis

---

## Executive Summary

| Severity | Count | Top Issues |
|---|---|---|
| 🔴 Critical | 5 | Collection mapping broken for 5 types, WeekMenu unregistered, MealIngredient contradiction, MealRecipeController DELETE bug, no user isolation |
| 🟡 Medium | 7 | N+1 migration writes on GET, domain leakage in EntityBase, ItemCategoryModel/Firestore mismatch, enum duplication, redundant fields |
| 🟢 Low | 5 | Dead code, naming inconsistencies, missing pagination, no batch support, hardcoded project ID |

**The four highest-risk issues in production today:**
1. `FrequentShoppingList`, `MealRecipe`, `MealIngredient`, `WeekMenu`, and `DailyMeal` all map to the `"misc"` collection and will silently collide.
2. `WeekMenu` is not registered in DI — the entire meal-planning week structure is wired to nothing.
3. `MealRecipeController.DELETE` has a type-mismatch bug: `IRepository.Delete()` returns `bool`; the handler treats it as nullable and then maps `bool → MealRecipeModel`.
4. No `UserId` / `OwnerId` field exists anywhere — every API endpoint exposes the entire global dataset to every caller.

---

## 1. Data Model Architecture

### 1.1 Entity Hierarchy

```
IEntityBase (interface — internal)
└── EntityBase (public)
    │  [FirestoreProperty] Id: string
    │  [FirestoreProperty] Name: string
    │  [FirestoreProperty] LastModified: DateTime?
    │  CssComleteEditClassName: string  ← ⚠️ UI concern in data model
    │  EditClicked: bool                ← ⚠️ UI concern in data model
    │  IsValid(): bool (virtual)
    │
    ├── [FirestoreData] FireStoreCommonBase  ← empty wrapper, currently unused
    │
    ├── [FirestoreData] ShoppingList         [Firestore: shoppinglists]
    │   ├── ListId: string                   ← 🔴 redundant, duplicates Id
    │   ├── IsDone: bool
    │   └── ShoppingItems: ICollection<ShoppingListItem>  ← embedded array
    │
    ├── [FirestoreData] ShoppingListItem     ← ⚠️ does NOT inherit EntityBase
    │   ├── Varen: ShopItem                  ← full embedded copy
    │   ├── Mengde: int
    │   └── IsDone: bool
    │       (no Id, no Name, no LastModified)
    │
    ├── [FirestoreData] ShopItem             [Firestore: shopitems]
    │   ├── Unit: string
    │   └── ItemCategory: ItemCategory       ← full embedded copy
    │
    ├── [FirestoreData] ItemCategory         [Firestore: itemcategories]
    │   └── (EntityBase only — Id, Name, LastModified)
    │
    ├── [FirestoreData] Shop                 [Firestore: shopcollection]
    │   └── ShelfsInShop: ICollection<Shelf> ← embedded array
    │
    ├── [FirestoreData] Shelf                ← embedded in Shop, NOT a root collection
    │   ├── SortIndex: int
    │   └── ItemCateogries: ICollection<ItemCategory>  ← typo preserved; full copies
    │
    ├── [FirestoreData] ShelfCategory        ← ⚠️ ORPHANED — never stored, no DI, no mapper
    │   ├── Id: int (not string!)            ← ⚠️ int PK, incompatible with EntityBase pattern
    │   ├── Name: string
    │   ├── Description: string
    │   └── static GetDefaults() → 30 hardcoded categories
    │
    ├── [FirestoreData] FrequentShoppingList [Firestore: ⚠️ "misc" — BROKEN]
    │   ├── Description: string
    │   └── Items: ICollection<FrequentShoppingItem>  ← embedded array
    │
    ├── [FirestoreData] FrequentShoppingItem ← embedded in FrequentShoppingList
    │   ├── Varen: ShopItem                  ← full embedded copy
    │   └── StandardMengde: int
    │
    ├── [FirestoreData] MealRecipe           [Firestore: ⚠️ "misc" — BROKEN]
    │   ├── Category: MealCategory (enum)
    │   ├── PopularityScore: int
    │   ├── LastUsed: DateTime?
    │   ├── IsActive: bool
    │   └── Ingredients: ICollection<MealIngredient>  ← embedded array
    │
    ├── [FirestoreData] MealIngredient       [Firestore: ⚠️ "misc" — BROKEN]
    │   │                                    ← ⚠️ also embedded in MealRecipe — contradictory
    │   ├── MealRecipeId: string             ← redundant (parent Id)
    │   ├── ShopItem: ShopItem               ← full embedded copy
    │   ├── StandardQuantity: int
    │   └── IsOptional: bool
    │
    ├── [FirestoreData] WeekMenu             [Firestore: ⚠️ "misc" — BROKEN]
    │   │                                    ← ⚠️ NOT registered in DI
    │   │                                    ← ⚠️ NO controller exists
    │   ├── WeekNumber: int
    │   ├── Year: int
    │   ├── CreatedDate: DateTime
    │   ├── IsActive: bool
    │   └── DailyMeals: ICollection<DailyMeal>  ← embedded array
    │
    ├── [FirestoreData] DailyMeal            ← embedded in WeekMenu
    │   ├── WeekMenuId: string               ← redundant (parent Id)
    │   ├── DayOfWeek: System.DayOfWeek      
    │   ├── MealRecipe: MealRecipe           ← FULL deep copy (5 levels: DailyMeal→Recipe→Ingredients→ShopItem→Category)
    │   └── CustomIngredients: ICollection<MealIngredient>
    │
    └── MealCategory (enum — duplicated)
        Shared.FireStoreDataModels.MealCategory { KidsLike, Fish, Meat, Vegetarian }
        Shared.HandlelisteModels.MealCategory   { KidsLike, Fish, Meat, Vegetarian }  ← identical duplicate
```

### 1.2 Firestore Attribute Coverage

All root entities that have DI registrations correctly use `[FirestoreData]` on the class and `[FirestoreProperty]` on all stored fields. However:

- `EntityBase.CssComleteEditClassName` and `EntityBase.EditClicked` have **no** `[FirestoreProperty]` attribute (correct — but they exist on the persisted model, creating confusion between domain objects and UI view models).
- `ShelfCategory.Id` is `int`, not `string` — it would not work with the string-ID repository pattern.
- `ItemCategoryModel.SortIndex` exists on the **DTO** but is **absent** from `ItemCategory` (Firestore model) — this field is silently dropped when saving to Firestore.

### 1.3 Primary Key Management

- IDs are `string` throughout, assigned by Firestore's `Collection.Document().Id` (auto-generated) in `GoogleFireBaseGenericRepository.Insert()`.
- `MemoryGenericRepository.Insert()` uses `Guid.NewGuid().ToString()` when no ID is provided.
- `ShoppingList.ListId` is a second ID field — **redundant**, never used in any query or controller. Should be removed.
- `MealIngredient.MealRecipeId` and `DailyMeal.WeekMenuId` are parent-reference fields **inside embedded objects** — they are redundant since the parent-child relationship is already encoded in the document structure.

---

## 2. DTO Mapping Strategy

### 2.1 DTO Model Definitions

All DTOs live in `Shared/HandlelisteModels/`. They mirror Firestore models exactly:

| Firestore Model | DTO Model | Inheritance |
|---|---|---|
| `ShoppingList : EntityBase` | `ShoppingListModel : ShoppingListBaseModel : EntityBase` | ShoppingListBaseModel adds UI-specific `CssComleteEditClassName` override |
| `ShoppingListItem` (no base) | `ShoppingListItemModel : ShoppingListBaseModel` | DTO has base class; Firestore model does not |
| `ShopItem : EntityBase` | `ShopItemModel : EntityBase` | Parallel |
| `ItemCategory : EntityBase` | `ItemCategoryModel : EntityBase` | ⚠️ DTO adds `SortIndex` — not stored in Firestore |
| `Shelf : EntityBase` | `ShelfModel : EntityBase` | Parallel |
| `Shop : EntityBase` | `ShopModel : EntityBase` | Parallel |
| `FrequentShoppingList : EntityBase` | `FrequentShoppingListModel : EntityBase` | Parallel |
| `FrequentShoppingItem : EntityBase` | `FrequentShoppingItemModel : EntityBase` | Parallel |
| `MealRecipe : EntityBase` | `MealRecipeModel : EntityBase` | Parallel |
| `MealIngredient : EntityBase` | `MealIngredientModel : EntityBase` | Parallel |
| `WeekMenu : EntityBase` | `WeekMenuModel : EntityBase` | Parallel |
| `DailyMeal : EntityBase` | `DailyMealModel : EntityBase` | Parallel |
| `ShelfCategory` (orphaned) | ❌ No DTO | Not mapped |

**Notable issue:** `ShoppingListItemModel` inherits `ShoppingListBaseModel` (which provides `IsDone` and UI CSS logic), but `ShoppingListItem` (the Firestore model) does not inherit `EntityBase` at all. The two models have a different inheritance structure — AutoMapper handles this via flat mapping, but it means `ShoppingListItem` has no `Id`, `Name`, or `LastModified` in Firestore storage.

### 2.2 AutoMapper Configuration (`Api/ShoppingListProfile.cs`)

```csharp
// All 12 mappings registered with .ReverseMap():
ShoppingList       ↔ ShoppingListModel
ShoppingListItem   ↔ ShoppingListItemModel
ShopItem           ↔ ShopItemModel
ItemCategory       ↔ ItemCategoryModel       // ⚠️ SortIndex dropped on save
Shelf              ↔ ShelfModel
Shop               ↔ ShopModel
FrequentShoppingList  ↔ FrequentShoppingListModel
FrequentShoppingItem  ↔ FrequentShoppingItemModel
MealRecipe         ↔ MealRecipeModel
MealIngredient     ↔ MealIngredientModel
WeekMenu           ↔ WeekMenuModel
DailyMeal          ↔ DailyMealModel
```

**Missing mapping:** `ShelfCategory` → no DTO, no AutoMapper entry.

**MealCategory enum cross-namespace:** AutoMapper maps `Shared.FireStoreDataModels.MealCategory` ↔ `Shared.HandlelisteModels.MealCategory`. Since both enums are identical, the mapping works, but the duplication is a maintenance hazard — if one enum gains a new value the other must be updated simultaneously.

### 2.3 Mapping Issues

| Issue | File | Impact |
|---|---|---|
| `ItemCategoryModel.SortIndex` not on Firestore model | `ItemCategoryModel.cs` vs `ItemCategory.cs` | SortIndex lost on every save |
| `ShoppingListItem` has no EntityBase, `ShoppingListItemModel` does | `ShoppingListItem.cs` | No Id stored for individual items |
| `MealCategory` duplicated in both namespaces | `FireStoreDataModels/MealCategory.cs`, `HandlelisteModels/MealCategory.cs` | Maintenance risk |
| `DailyMealModel.MealRecipe` initialised to `new MealRecipeModel()` in constructor | `DailyMealModel.cs:18` | AutoMapper may not overwrite a non-null default correctly |
| `ShopItemCategoryController` response writes raw `ItemCategory` (Firestore model) not `ItemCategoryModel` | `ShopItemCategoryController.cs:38,57,68,86` | Exposes Firestore model directly to client |

---

## 3. Repository Pattern

### 3.1 IGenericRepository Interface

```csharp
// Shared/Repository/IGenericRepository.cs
public interface IGenericRepository<T> where T : class
{
    Task<bool>          Delete(T entityToDelete);
    Task<bool>          Delete(object id);
    Task<ICollection<T>> Get();              // full collection scan — no filter
    Task<T>             Get(object id);      // by string ID only
    Task<T>             Insert(T entity);
    Task<T>             Update(T entityToUpdate);
}
```

**Gap analysis:**
- No `Query(Expression<Func<T,bool>> predicate)` — all filtering is in-memory post-fetch.
- No `GetPage(int skip, int take)` — no pagination.
- No `GetByField(string field, object value)` — no server-side Firestore WHERE queries.
- No transaction/batch support.
- `Delete(object id)` accepts `object` but only works if it is a `string` (explicitly checked inside).

### 3.2 GoogleFireBaseGenericRepository

**File:** `Shared/Repository/GoogleFireBaseGenericRepository.cs`

| Operation | Implementation | Issue |
|---|---|---|
| `Get()` | `collection.GetSnapshotAsync()` — reads ALL documents | ⚠️ Full scan, no filter |
| `Get(id)` | `collection.Document(id).GetSnapshotAsync()` | ✅ Correct single-doc read |
| `Insert(entity)` | `collection.Document()` (auto-ID) then `SetAsync(entity)` | ✅ Correct; sets `entity.Id` |
| `Update(entity)` | `collection.Document(id).SetAsync(entity)` | ✅ Full document overwrite |
| `Delete(entity)` | `collection.Document(entity.Id).DeleteAsync()` | ✅ Correct |
| `Delete(object id)` | String cast then `DeleteAsync()` | ✅ Works; non-string IDs silently return false |

**Critical design flaw — `IGoogleDbContext` shared state:**

`IGoogleDbContext` stores `CollectionReference Collection` as a mutable instance property. Every `GoogleFireBaseGenericRepository<T>` constructor sets `dbContext.CollectionKey` and `dbContext.Collection`. Because `IGoogleDbContext` is registered as `AddTransient`, each injection creates a fresh instance — this **currently prevents** the collision where two repositories overwrite each other's collection reference. However, the design is fragile:
- If `IGoogleDbContext` is ever changed to `AddScoped` or `AddSingleton`, repositories will silently use the wrong collection.
- The collection should be resolved per-operation (passed into the method), not stored as shared state.

### 3.3 MemoryGenericRepository

**File:** `Shared/Repository/MemoryGenericRepository.cs` (~640 lines)

- Uses `Dictionary<string, TEntity>` — correct for O(1) ID lookup.
- Seed data is loaded asynchronously in the constructor via `Task.Run(() => AddDummyValues(...))` — this is a fire-and-forget call; if tests run before the seed completes, they may see empty data.
- Covers: `ShoppingList`, `Shop`, `ShopItem`, `ItemCategory`, `FrequentShoppingList`, `MealRecipe`.
- **Missing seeds:** `MealIngredient`, `WeekMenu`, `DailyMeal` (consistent with DI gaps).
- Seed IDs are hardcoded strings (e.g., `"milk-1"`, `"meal-1"`) — ensures test determinism but creates coupling between test data and test expectations.

### 3.4 DI Registration (`Api/Program.cs`)

| Entity | Dev (Memory) | Prod (Firestore) | Collection Key |
|---|---|---|---|
| `ShoppingList` | ✅ Registered | ✅ Registered | `shoppinglists` |
| `ShopItem` | ✅ Registered | ✅ Registered | `shopitems` |
| `ItemCategory` | ✅ Registered | ✅ Registered | `itemcategories` |
| `Shop` | ✅ Registered | ✅ Registered | `shopcollection` |
| `FrequentShoppingList` | ✅ Registered | ✅ Registered | ⚠️ `"misc"` |
| `MealRecipe` | ✅ Registered | ✅ Registered | ⚠️ `"misc"` |
| `MealIngredient` | ✅ Registered | ✅ Registered | ⚠️ `"misc"` (also embedded in MealRecipe) |
| `WeekMenu` | ❌ NOT registered | ❌ NOT registered | ⚠️ `"misc"` (moot) |

---

## 4. Collection Structure and Naming

### 4.1 Current Firestore Collections

```
Firestore: supergnisten-shoppinglist
├── shoppinglists/          ← ShoppingList documents
│   └── {docId}             Auto-generated ID
│       ├── Id: string      (duplicated from doc ID)
│       ├── Name: string
│       ├── ListId: string  (⚠️ duplicate of Id)
│       ├── IsDone: bool
│       ├── LastModified: timestamp
│       └── ShoppingItems: []  ← embedded ShoppingListItem array
│           └── { Varen: {full ShopItem}, Mengde: int, IsDone: bool }
│
├── shopitems/              ← ShopItem documents
│   └── {docId}
│       ├── Id, Name, LastModified
│       ├── Unit: string
│       └── ItemCategory: {full ItemCategory}  ← embedded
│
├── itemcategories/         ← ItemCategory documents
│   └── {docId}
│       ├── Id, Name, LastModified
│       └── (SortIndex only exists on DTO — NOT stored here)
│
├── shopcollection/         ← Shop documents  (inconsistent naming — others use plural entity names)
│   └── {docId}
│       ├── Id, Name, LastModified
│       └── ShelfsInShop: []  ← embedded Shelf array
│           └── { Id, Name, SortIndex, ItemCateogries: [ItemCategory...] }
│
└── misc/                   ← ⚠️ COLLISION ZONE in production
    ├── FrequentShoppingList documents
    ├── MealRecipe documents
    ├── MealIngredient documents  (also stored in MealRecipe.Ingredients)
    └── (WeekMenu would also land here if registered)
```

### 4.2 Collection Naming Inconsistency

| Type | Collection Name | Pattern |
|---|---|---|
| `ShoppingList` | `shoppinglists` | plural entity name |
| `ShopItem` | `shopitems` | plural entity name |
| `ItemCategory` | `itemcategories` | plural entity name |
| `Shop` | `shopcollection` | ⚠️ non-standard — should be `shops` |
| All others | `misc` | ⚠️ broken |

### 4.3 Root Collections vs Subcollections

**Current:** All entities are root collections. Embedded objects (Shelf, ShoppingListItem, MealIngredient) live as arrays inside documents, not as Firestore subcollections.

**This is a valid Firestore pattern** for entities that are:
- Always accessed through their parent (Shelf always fetched with Shop ✅)
- Have bounded size (a shop won't have 10,000 shelves ✅)

**This is problematic** for:
- `MealIngredient` inside `MealRecipe` — `MealIngredient` also has its own DI-registered repository, implying it was intended as a top-level collection. The two intents contradict each other.
- `DailyMeal.MealRecipe` — a full `MealRecipe` with all its `Ingredients` (each with a full `ShopItem`) is copied into every `DailyMeal`. This is 5 levels of nesting and can cause documents to approach Firestore's 1 MB limit.

### 4.4 Missing Firestore Indexes

The following composite indexes are **required** for future query patterns but do not yet exist (indexes are not defined in any configuration file in the repo):

```json
// firestore.indexes.json (does not exist yet)
{
  "indexes": [
    {
      "collectionGroup": "shoppinglists",
      "queryScope": "COLLECTION",
      "fields": [
        { "fieldPath": "IsDone", "order": "ASCENDING" },
        { "fieldPath": "LastModified", "order": "DESCENDING" }
      ]
    },
    {
      "collectionGroup": "mealrecipes",
      "queryScope": "COLLECTION",
      "fields": [
        { "fieldPath": "IsActive", "order": "ASCENDING" },
        { "fieldPath": "PopularityScore", "order": "DESCENDING" }
      ]
    },
    {
      "collectionGroup": "weekmenus",
      "queryScope": "COLLECTION",
      "fields": [
        { "fieldPath": "Year", "order": "ASCENDING" },
        { "fieldPath": "WeekNumber", "order": "ASCENDING" }
      ]
    }
  ]
}
```

---

## 5. Data Normalization Analysis

### 5.1 Embedded Objects Inventory

| Parent | Embedded Type | Depth | Justification | Risk |
|---|---|---|---|---|
| `ShoppingList.ShoppingItems` | `ShoppingListItem` | 2 | Valid snapshot-at-time | Item rename not reflected |
| `ShoppingListItem.Varen` | `ShopItem` (full) | 3 | Snapshot + avoids join | 30-item list = 30 ShopItem copies |
| `ShopItem.ItemCategory` | `ItemCategory` (full) | 2 | Rarely changes | Low risk |
| `Shop.ShelfsInShop` | `Shelf[]` | 2 | Always loaded with Shop | ✅ Justified |
| `Shelf.ItemCateogries` | `ItemCategory[]` | 3 | Always loaded with Shelf | ✅ Justified |
| `FrequentShoppingList.Items` | `FrequentShoppingItem[]` | 2 | Valid template snapshot | Item rename not reflected |
| `FrequentShoppingItem.Varen` | `ShopItem` (full) | 3 | Same as ShoppingList | Stale copies |
| `MealRecipe.Ingredients` | `MealIngredient[]` | 2 | Contradicts separate repo | Blocked by design issue |
| `MealIngredient.ShopItem` | `ShopItem` (full) | 3 | Same fan-out risk | Stale copies |
| `WeekMenu.DailyMeals` | `DailyMeal[]` | 2 | Plan snapshot | 7 meals max, bounded |
| `DailyMeal.MealRecipe` | `MealRecipe` (full) | 3-5 | ⚠️ Full recipe in each day | Document size risk |
| `DailyMeal.CustomIngredients` | `MealIngredient[]` | 3 | Override only if needed | Bounded if ≤ 5 overrides |

### 5.2 Denormalization Justification

**Firestore read model (snapshot semantics) justifies embedding for:**
- `ShoppingListItem.Varen` — a shopping trip is a point-in-time snapshot; historical accuracy matters more than current item state. ✅
- `FrequentShoppingItem.Varen` — a template list should reflect what was chosen, not whatever the item is called today. ✅

**Embedding is NOT justified for:**
- `DailyMeal.MealRecipe` (full object) — a week menu should reference the live recipe so updates propagate. The current copy-on-save means recipe edits are invisible to existing menus.
- `MealIngredient` (has its own repository but is also embedded) — contradictory design.

### 5.3 Update Anomalies

| Scenario | Affected Entities | Impact |
|---|---|---|
| User renames `ShopItem` "Melk" → "Helmelk" | All `ShoppingList`, `FrequentShoppingList`, `MealRecipe` documents containing that item | Historical data permanently stale; no propagation mechanism |
| User changes `ItemCategory` name | All `ShopItem` documents embedding that category | Same staleness problem |
| User edits a `MealRecipe` ingredient list | All `WeekMenu.DailyMeal` entries that copied the recipe | Week menus do not update |
| `ShopItem.Unit` changes | Anywhere the ShopItem is embedded | Silent stale data |

---

## 6. Timestamps and Auditing

### 6.1 LastModified Implementation

`LastModified: DateTime?` is defined on `EntityBase` (Firestore model base) and is a nullable `DateTime`.

**Where it is set:**
- `ShoppingListController` (`Api/Controllers/ShoppingListController.cs:82,102`): Set to `DateTime.UtcNow` on every POST and PUT. ✅
- `MealRecipeController` (`Api/Controllers/MealRecipeController.cs:63,83`): Set to `DateTime.UtcNow` on POST and PUT. ✅
- `ShopsController`, `ShopsItemsController`, `ShopItemCategoryController`, `FrequentShoppingListController`: **NOT set** on any write operation. ❌

**Where it is missing:**
- `Shop`, `ShopItem`, `ItemCategory`, `FrequentShoppingList` — LastModified is never populated on writes to these collections.

### 6.2 Lazy Migration Pattern (inline on GET)

`ShoppingListController.RunAll()` (lines 55–70) and `RunOne()` (lines 139–144) contain an inline migration that sets `LastModified` on documents that don't have it:

```csharp
// ShoppingListController.cs:55-70
foreach (var list in result)
{
    if (!list.LastModified.HasValue)
    {
        list.LastModified = DateTime.UtcNow;
        await repo.Update(list);   // ⚠️ 1 write per document, in a loop, during GET
        hasUpdates = true;
    }
}
```

**Problems:**
1. **N+1 write pattern on GET**: If 100 lists need migration, this fires 100 sequential `SetAsync()` calls during a GET request, adding ~100 × 20ms = 2 seconds of latency to the first GET.
2. **Only covers ShoppingList**: The same stale data exists in all other collections but is not migrated.
3. **No migration is idempotent-safe**: The same documents are re-evaluated on every GET until `LastModified` is populated.

### 6.3 Audit Trail Capabilities

**Currently provided:** Single `LastModified` timestamp — who changed it and what changed is not recorded.

**What is missing for a complete audit trail:**
- `CreatedAt: DateTime` — when the entity was first created
- `CreatedBy: string` — user ID of creator  
- `ModifiedBy: string` — user ID of last modifier
- Firestore does not provide built-in history; change history requires a separate `audit_log` collection or Firebase Extensions.

---

## 7. Known Bugs

### BUG-1: MealRecipeController DELETE type mismatch (🔴 Runtime error)

**File:** `Api/Controllers/MealRecipeController.cs:127-137`

```csharp
var delRes = await _repository.Delete(id);  // Delete() returns bool, not T
if (delRes == null)                          // bool can never be null — always false
{
    // never reached
}
else
{
    await response.WriteAsJsonAsync(_mapper.Map<MealRecipeModel>(delRes)); // maps bool → MealRecipeModel
    return response;
}
```

`IGenericRepository<T>.Delete(object id)` returns `bool`. The handler assigns it to `var delRes` (inferred as `bool`). Since `bool` cannot be null, the `null` check always passes to the `else` branch, which then calls `_mapper.Map<MealRecipeModel>(true)`. AutoMapper will throw `AutoMapperMappingException` at runtime because there is no `bool → MealRecipeModel` map registered.

**Fix:**
```csharp
var delRes = await _repository.Delete(id);  // bool
if (!delRes)
    return await GetErroRespons($"Could not delete meal recipe with id {id}", req);
return req.CreateResponse(HttpStatusCode.NoContent);
```

### BUG-2: ShopItemCategoryController returns Firestore model to client (🟡 Leaked internals)

**File:** `Api/Controllers/ShopItemCategoryController.cs:38,57,68,86`

```csharp
await okRespons.WriteAsJsonAsync(itemCategories);      // line 38: returns raw ItemCategory, not ItemCategoryModel
await okRespons.WriteAsJsonAsync(mapper.Map<ItemCategory>(newItemCat)); // line 57: maps to Firestore model, not DTO
```

The GET handler returns the raw Firestore model collection. The POST/PUT handlers map back to `ItemCategory` (Firestore model) instead of `ItemCategoryModel` (DTO). This exposes Firestore-annotated objects to the client and skips the DTO boundary.

---

## 8. Scalability Concerns

### 8.1 Full Collection Scans

Every `GET /api/{collection}` calls `Get()` which reads all documents. Current read cost:

| Collection | Estimated Documents | Reads/Request | Concern |
|---|---|---|---|
| `shoppinglists` | Grows unbounded | All | ⚠️ High |
| `shopitems` | ~50–500 | All | Medium |
| `itemcategories` | ~10–30 | All | Low |
| `shopcollection` | ~2–10 | All | Low |
| `mealrecipes` | ~20–200 | All | Medium |

### 8.2 Document Size Risk

The worst-case `WeekMenu` document:
- 7 `DailyMeal` entries, each embedding a full `MealRecipe`
- Each `MealRecipe` has 8 `MealIngredient` entries, each with a full `ShopItem`
- 7 × (1 MealRecipe + 8 × (1 ShopItem + 1 ItemCategory))
- Estimated: 7 × 9 × ~400 bytes = ~25 KB per document

While 25 KB is well below Firestore's 1 MB limit for a single week, 40+ weeks of history would approach it. More critically, deeply nested objects increase serialization cost.

### 8.3 Client-Side DataCacheService

`Client/Services/DataCacheService.cs` implements an in-memory cache with TTLs:
- Items/Categories/Shops: 5-minute TTL
- Details (individual list, shop): 10-minute TTL
- Frequent lists: 5-minute TTL

This is a well-designed client-side mitigation but does not reduce server-side Firestore read costs — it only reduces API call frequency.

**Missing from cache service:** MealRecipe caching. `ShoppingListKeysEnum` does not include `MealRecipes` or `WeekMenus`.

### 8.4 Background Preloading

`BackgroundPreloadService` (referenced from `DataCacheService.PreloadActiveShoppingListsAsync()`) preloads all active (non-done) shopping lists in parallel on startup. This is good UX but doubles Firestore reads on app load: one read for the list-of-lists, then one per active list.

---

## 9. Data Validation

### 9.1 Validation Coverage

| Entity | `IsValid()` Method | What It Validates |
|---|---|---|
| `EntityBase` | ✅ `virtual` | `!IsNullOrEmpty(Name)` |
| `ShopItemModel` | ✅ `override` | `Name` + `ItemCategory.IsValid()` |
| `MealRecipeModel` | ✅ `override` | `Name` only |
| `MealIngredientModel` | ✅ `override` | `ShopItem != null`, `ShopItem.IsValid()`, `StandardQuantity > 0` |
| `WeekMenuModel` | ✅ `override` | `Name`, `WeekNumber > 0`, `Year > 0` |
| `DailyMealModel` | ✅ `override` | `MealRecipe != null`, `MealRecipe.IsValid()` |
| `ShoppingListModel` | ❌ Not overridden | Only `Name` via EntityBase |
| `ShopModel` | ❌ Not overridden | Only `Name` via EntityBase |
| `FrequentShoppingListModel` | ❌ Not overridden | Only `Name` via EntityBase |

**Critical gap:** Validation is only on DTO models, never enforced on the API side. Controllers do not call `IsValid()` before writing to Firestore. Invalid data (empty names, zero quantities) can be persisted silently.

### 9.2 Missing Validation

- No `[Required]`, `[MaxLength]`, or `[Range]` attributes anywhere (no Data Annotations).
- No minimum/maximum for `Mengde`/`StandardMengde`/`StandardQuantity` at the API layer.
- `WeekNumber` is not validated against Firestore (1–53).
- `DayOfWeek` enum is not validated for uniqueness within a `WeekMenu.DailyMeals` list (could have two Mondays).
- No null-guard on `ShoppingList.ShoppingItems` before iterating — if Firestore stores null it will NullRef.

---

## 10. Prioritised Improvement List

| Priority | Item | File(s) | Effort | Risk |
|---|---|---|---|---|
| 🔴 **Critical** | Fix `GetCollectionKey()` — add all 5 missing types | `GoogleDbContext.cs` | Low | Low |
| 🔴 **Critical** | Register `WeekMenu` in DI + create controller | `Program.cs`, new controller | Low | Low |
| 🔴 **Critical** | Fix `MealIngredient` contradiction: choose embedded-only OR root collection | `Program.cs`, `GoogleDbContext.cs` | Low | Low |
| 🔴 **Critical** | Fix `MealRecipeController.DELETE` bool→Model type error | `MealRecipeController.cs:127-137` | Low | None |
| 🔴 **Critical** | Extract inline LastModified migration from GET into dedicated migration endpoint | `ShoppingListController.cs:55-70` | Low | Low |
| 🟡 **Medium** | Fix `ShopItemCategoryController` to return `ItemCategoryModel` not `ItemCategory` | `ShopItemCategoryController.cs:38,57,68,86` | Low | None |
| 🟡 **Medium** | Add `LastModified` writes to Shop, ShopItem, ItemCategory, FrequentShoppingList controllers | 4 controller files | Low | None |
| 🟡 **Medium** | Remove `MealCategory` enum duplication — consolidate to one namespace | `MealCategory.cs` × 2 | Low | Low |
| 🟡 **Medium** | Move UI concerns (`CssComleteEditClassName`, `EditClicked`) out of `EntityBase` | `EntityBase.cs` | Medium | Medium |
| 🟡 **Medium** | Add `SortIndex` to `ItemCategory` Firestore model (or remove from DTO) | `ItemCategory.cs` | Low | None |
| 🟡 **Medium** | Add `OwnerId` field to root entities (pre-auth groundwork) | `EntityBase.cs` or 5 entity classes | Medium | High (migration later) |
| 🟡 **Medium** | Fix `shopcollection` → `shops` collection naming | `GoogleDbContext.cs` + data migration | Low | Medium (needs data copy) |
| 🟡 **Medium** | Add `firestore.indexes.json` for composite query support | New file | Low | None |
| 🟡 **Medium** | Add `MealRecipes` and `WeekMenus` to `ShoppingListKeysEnum` and `DataCacheService` | `ShoppingListKeysEnum.cs`, `DataCacheService.cs` | Low | None |
| 🟢 **Low** | Remove orphaned `ShelfCategory` class (dead code) | `ShelfCategory.cs` | Low | None |
| 🟢 **Low** | Remove redundant `ListId` from `ShoppingList` | `ShoppingList.cs` | Low | None (field unused) |
| 🟢 **Low** | Remove redundant `MealRecipeId` from `MealIngredient`, `WeekMenuId` from `DailyMeal` | `MealIngredient.cs`, `DailyMeal.cs` | Low | None |
| 🟢 **Low** | Delete `Class1.cs` (empty dead file) | `HandlelisteModels/Class1.cs` | Low | None |
| 🟢 **Low** | Add `Query(Expression<Func<T,bool>>)` or `GetWhere(string field, object value)` to `IGenericRepository` | `IGenericRepository.cs` | High | Medium |
| 🟢 **Low** | Add pagination to `IGenericRepository` | `IGenericRepository.cs` | Medium | Low |
| 🟢 **Low** | Move project ID `"supergnisten-shoppinglist"` out of hardcode into environment config | `GoogleDbContext.cs:14` | Low | None |
| 🟢 **Low** | Replace `DailyMeal.MealRecipe` full copy with `MealRecipeId` reference | `DailyMeal.cs` + migration | High | High |

---

## 11. Recommended Firestore Collection Structure (Target State)

```
Firestore: supergnisten-shoppinglist
├── shoppinglists/          ← unchanged
├── shopitems/              ← unchanged
├── itemcategories/         ← unchanged
├── shops/                  ← rename from "shopcollection"
├── frequentlists/          ← new (was "misc")
├── mealrecipes/            ← new (was "misc")
│   └── {recipeId}/
│       └── ingredients/    ← optional: move to subcollection if MealIngredient repo is kept
└── weekmenus/              ← new (was "misc", was unregistered)
    └── {weekMenuId}/
        └── dailymeals/     ← optional: subcollection for bounded 7-day access
```

---

## 12. Phase-by-Phase Migration Plan

### Phase 1 — Zero-Risk Fixes (1–2 hours, no data migration needed)
1. Extend `GoogleDbContext.GetCollectionKey()` to cover all 5 missing types.
2. Register `IGenericRepository<WeekMenu>` in both dev and prod DI blocks in `Program.cs`.
3. Fix `MealRecipeController.DELETE` to handle `bool` return correctly.
4. Delete `Class1.cs`.

### Phase 2 — Data Model Corrections (half-day, no migration needed for new fields)
1. Remove `MealIngredient` from DI or move it to its own root collection — pick one model.
2. Fix `ShopItemCategoryController` to always return `ItemCategoryModel`.
3. Add `LastModified` writes to all controllers that are missing them.
4. Extract the inline migration in `ShoppingListController` to a `/api/admin/migrate-timestamps` endpoint.
5. Consolidate `MealCategory` enum to `Shared.FireStoreDataModels` only; remove from `HandlelisteModels`.

### Phase 3 — Groundwork Before Authentication (full day, requires deployment)
1. Add `string OwnerId { get; set; }` with `[FirestoreProperty]` to `ShoppingList`, `Shop`, `FrequentShoppingList`, `MealRecipe`, `WeekMenu`. Deploy with empty string default.
2. Rename `shopcollection` → `shops` (requires copying documents in Firestore console or a one-time migration script, as Firestore collections cannot be renamed).
3. Add `firestore.indexes.json` with composite index definitions.
4. Add `SortIndex` to `ItemCategory` Firestore model.

### Phase 4 — Architecture Improvements (ongoing)
1. Add `Query(string field, object value)` to `IGenericRepository<T>` and implement in both repository classes.
2. Add pagination (`GetPage(int skip, int take)` or cursor-based) to `IGenericRepository<T>`.
3. Implement `IMemoryCache` in API for `Shop` and `ItemCategory` responses.
4. Move `CssComleteEditClassName` and `EditClicked` out of `EntityBase` into client-side view models.
