# Shopping List Application - AI Coding Instructions

## Architecture Overview
This is a **3-tier Blazor WebAssembly + Azure Functions** shopping list app with shop-specific item sorting:

- **Client**: Blazor WebAssembly (.NET 9) using Syncfusion UI components
- **Api**: Azure Functions v4 (.NET 9) with AutoMapper and repository pattern  
- **Shared**: Common models split into `FireStoreDataModels` (database) and `HandlelisteModels` (DTOs)

## Critical Data Model Pattern
**Two parallel model hierarchies** - always maintain both:

```csharp
// Database models (Firestore attributes)
namespace Shared.FireStoreDataModels {
    [FirestoreData]
    public class ShopItem : EntityBase {
        [FirestoreProperty]
        public ItemCategory ItemCategory { get; set; }
    }
}

// DTO models (for API/UI)
namespace Shared.HandlelisteModels {
    public class ShopItemModel : EntityBase {
        public ItemCategoryModel ItemCategory { get; set; }
    }
}
```

**AutoMapper profiles** in `Api/ShoppingListProfile.cs` handle conversions with `.ReverseMap()`.

## Core Business Logic: Shop-Specific Sorting
The key feature sorts shopping items by **shelf traversal order** in a specific shop:

1. **Shop** → contains **Shelfs** (with `SortIndex` for store layout)
2. **Shelf** → contains **ItemCategories** 
3. **ShopItem** → belongs to **ItemCategory**
4. Sorting assigns sequential indices based on shelf order + categories within shelfs

**Implementation**: `Client/Pages/Shopping/OneShoppingListPage.razor` - `SortShoppingList()` method.

## Development Workflow

### Local Development Setup
```bash
# 1. Copy API settings
cp Api/local.settings.example.json Api/local.settings.json

# 2. For dual repository mode (DEBUG), uses MemoryGenericRepository
# 3. For production, uses GoogleFireBaseGenericRepository + Firestore
```

**Key configuration**: `Api/Program.cs` has `#if DEBUG` blocks switching between memory and Firestore repos.

### API URL Management
Client uses **Settings service** pattern via `ISettings` interface:
```csharp
// Client/Common/ISettings.cs - maps ShoppingListKeysEnum to API paths
Settings.GetApiUrl(ShoppingListKeysEnum.shopItems) // returns "api/shopitems"
Settings.GetApiUrlId(ShoppingListKeysEnum.Shop, id) // returns "api/shop/{id}"
```

## Azure Functions Controller Pattern
All controllers inherit `ControllerBase` and follow **function-per-endpoint** pattern:

```csharp
[Function("shopitems")]  // Collection operations (GET/POST/PUT)
public async Task<HttpResponseData> RunAll([HttpTrigger...])

[Function("shopitem")]   // Single item (GET/DELETE with {id} route)
public async Task<HttpResponseData> RunOne([HttpTrigger(..., Route = "shopitem/{id}")]) 
```

**Standard CRUD**: GET (all + single), POST (create), PUT (update), DELETE per entity.

## Blazor Component Patterns

### Syncfusion Integration
Heavy use of **Syncfusion components** with specific binding patterns:
```csharp
<SfDropDownList TValue="string" TItem="ShopModel" @bind-Index="@shopIndex">
    <DropDownListFieldSettings Value="Id" Text="Name" />
</SfDropDownList>

<SfAutoComplete TValue="string" TItem="ShopItemModel" @bind-Value="@autoValue">
    <AutoCompleteEvents ValueChange="@SelectedShopItemChanged" />
</SfAutoComplete>
```

### CSS Class Binding
**EntityBase** provides `CssComleteEditClassName` for UI state:
```csharp
public string CssComleteEditClassName {
    get => EditClicked ? "edit" : (IsDone ? "completed" : "");
}
```

## Repository & DI Configuration
**Generic repository pattern** with interface:
```csharp
IGenericRepository<T> : Get(), Get(id), Insert(T), Update(T), Delete(id)
```

**DI registration** in `Api/Program.cs`:
```csharp
// Debug: Memory repositories with dummy data
s.AddSingleton<IGenericRepository<ShopItem>, MemoryGenericRepository<ShopItem>>();

// Production: Firestore repositories  
s.AddSingleton<IGenericRepository<ShopItem>, GoogleFireBaseGenericRepository<ShopItem>>();
```

## Critical Implementation Notes

- **EntityBase validation**: Override `IsValid()` for custom validation logic
- **Norwegian naming**: Properties like `ItemCateogries` (typo preserved), `Varen`, `Mengde`
- **No Shelf controller**: Shelf management happens through Shop entities only
- **Client-side sorting**: Core sorting logic runs in Blazor, not API
- **Syncfusion licensing**: Required for UI components (see `Program.cs`)

## Advanced Features

### Natural Alphanumeric Sorting
**Implementation**: `Client/Common/NaturalSortComparer.cs`

Implements `IComparer<string>` for proper sorting of mixed text and numbers:
```csharp
// Correct sorting: "Uke 1", "Uke 2", "Uke 10", "Uke 41", "Uke 42"
// Without natural sort: "Uke 1", "Uke 10", "Uke 2", "Uke 41", "Uke 42"
```

**Algorithm**:
1. Regex splits strings into number/text segments
2. Numbers compared numerically (int.Parse)
3. Text compared alphabetically (case-insensitive)
4. Handles null strings gracefully

**Test Coverage**: 11 unit tests covering edge cases (nulls, case-sensitivity, mixed formats)

### Multi-Level Shopping List Sorting
**Location**: `Client/Pages/Shopping/ShoppingListMainPage.razor` - `SortShoppingLists()` method

```csharp
AvailableShoppingLists = AvailableShoppingLists
    .OrderBy(f => f.IsDone)                           // Level 1: Active first (false < true)
    .ThenByDescending(f => f.LastModified)            // Level 2: Newest first
    .ThenBy(f => f.Name, new NaturalSortComparer())   // Level 3: Natural name sort
    .ToList();
```

**Behavior**:
- **Active lists** (IsDone=false) appear at top
- **Completed lists** (IsDone=true) appear at bottom
- Within each group: newest first, then alphabetical with natural sorting

### Timestamp System & Migration
**EntityBase** includes `LastModified` property (DateTime?):
```csharp
[FirestoreProperty]
public DateTime? LastModified { get; set; }
```

**Lazy Migration** in `Api/Controllers/ShoppingListController.cs`:
```csharp
// GET endpoints check for null LastModified
if (list.LastModified == null)
{
    list.LastModified = DateTime.UtcNow;
    await _repository.Update(list);
    _logger.LogInformation($"Migrated LastModified for list: {list.Name}");
}
```

**Automatic Updates**: POST and PUT operations set `LastModified = DateTime.UtcNow`

### Item Insertion Order
**Implementation**: `OneShoppingListPage.razor` and `OneFrequentListPage.razor`

New items insert at **position 0** (top of list):
```csharp
// Convert ICollection to List for Insert method
var itemsList = ThisShoppingList.ShoppingItems.ToList();
itemsList.Insert(0, newItem);  // Insert at top
ThisShoppingList.ShoppingItems = itemsList;
```

### Navigation Structure
**Location**: `Client/Shared/NewNavComponent.razor`

```
Main Menu:
├── Handlelister
└── Admin (CSS dropdown)
    ├── Hyppige Lister  (moved from main menu)
    ├── Håndter butikker
    ├── Administrer varer
    └── Administrer kategorier
```

Uses CSS-only dropdown (no JavaScript) for admin functions.

## Testing Infrastructure

### Comprehensive Test Suite (143 tests total)

#### Unit Tests - API (65 tests)
**Location**: `Api.Tests/Controllers/`

- `ShoppingListControllerTests.cs`:
  - Standard CRUD operations
  - **LastModified_IsSetOnCreation**: Verifies timestamp on POST
  - **LastModified_IsUpdatedOnUpdate**: Verifies timestamp on PUT
  - **Migration_SetsLastModifiedForLegacyLists**: Tests lazy migration
  - 7 total tests for timestamp functionality

- `ShopsControllerTests.cs`: Shop and shelf management
- `ShopsItemsControllerTests.cs`: Item CRUD operations
- `ShopItemCategoryControllerTests.cs`: Category management

#### Unit Tests - Client (61 tests)
**Location**: `Client.Tests/`

- `NaturalSortComparerTests.cs` (11 tests):
  - `BasicNumberSorting_SortsNumerically`: "Item 1" < "Item 10" < "Item 100"
  - `WeekNumbers_SortCorrectly`: "Uke 1" through "Uke 43"
  - `MixedCaseInsensitive_SortsCorrectly`
  - `NullStrings_HandledGracefully`
  - Edge cases and special characters

- `ShoppingListSortingTests.cs` (3 tests):
  - `ActiveLists_AppearBeforeCompletedLists`: Verifies IsDone=false comes first
  - `WithinSameDate_UsesNaturalSorting`: Verifies natural sort when timestamps equal
  - `MixedActiveAndCompleted_SortsCorrectly`: Complex scenario test

- Additional component and service tests (47 tests)

#### E2E Tests - Playwright (20 tests)
**Location**: `Client.Tests.Playwright/Tests/`

- `ShoppingListSortingTests.cs` (7 tests):
  - Shop-specific sorting verification
  - Natural sorting in UI
  - Syncfusion component interactions
  
- `NavigationTests.cs`: Page loading and routing
- `DebugTests.cs`: Console error detection
- `PageInspectionTests.cs`: UI element verification

### Running Tests
```bash
# All tests
dotnet test

# Specific test class
dotnet test --filter "FullyQualifiedName~NaturalSortComparerTests"

# Client tests only
dotnet test Client.Tests/Client.Tests.csproj

# E2E tests (requires running app)
dotnet test Client.Tests.Playwright/Client.Tests.Playwright.csproj
```

## Development Priorities & Implementation Status

### ✅ Completed Features:
1. ✅ **Testing infrastructure** - 143 comprehensive tests (unit + E2E)
2. ✅ **Natural alphanumeric sorting** - Handles "Uke 1" through "Uke 43"
3. ✅ **Timestamp tracking** - LastModified with lazy migration
4. ✅ **Smart list sorting** - Multi-level: IsDone → Date → Name
5. ✅ **Item insertion order** - New items at top
6. ✅ **IsDone persistence** - Fixed checkbox binding and API endpoint
7. ✅ **Navigation improvements** - Frequent Lists in Admin dropdown

### 🔄 Known Limitations:
1. **No Shelf API endpoint** - Shelf management happens through Shop entities only
2. **Firestore performance** - Could benefit from caching strategies
3. **Client-side sorting** - Core sorting logic runs in Blazor, not API (by design)

### 📋 Future Considerations:
- Add visual separator between active and completed lists
- Implement data caching for frequently accessed shops/shelves
- Consider backend sorting API for large datasets
- Add bulk operations for list management

## Code Style & Conventions

### Pattern Adherence
- ✅ Always maintain **dual model pattern** (Firestore + DTO)
- ✅ Use **AutoMapper** for all model conversions
- ✅ Follow **repository interface** for data access
- ✅ Add **tests** for new features
- ✅ Make **minimum necessary changes**
- ✅ Preserve existing architectural patterns

### Norwegian Naming
Properties use Norwegian names where appropriate:
- `Varen` (the item)
- `Mengde` (quantity)
- `ItemCateogries` (typo preserved for backward compatibility)

### CSS Binding Pattern
```csharp
public string CssComleteEditClassName {
    get => EditClicked ? "edit" : (IsDone ? "completed" : "");
}
```

### Timestamp Pattern
All new entities and updates:
```csharp
entity.LastModified = DateTime.UtcNow;
```

When extending functionality, maintain these patterns and ensure new features adhere to existing architectural conventions.
