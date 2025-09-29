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

## Development Priorities & Known Issues

### Immediate TODO Items:
1. **Add Shelf API endpoint** - Currently missing dedicated CRUD operations for shelfs
2. **Fix Firestore performance** - Optimize loading and storing operations
3. **Add testing infrastructure** - No automated testing currently exists
4. **Improve data loading performance** - Current implementation needs optimization

### Testing Strategy:
- **No current test structure** - This is first priority for reliability
- **MemoryGenericRepository** provides good foundation for unit testing
- Consider testing both repository implementations and sorting algorithms

### Performance Considerations:
- Firestore operations currently unoptimized
- Client-side sorting preferred over backend processing
- Consider caching strategies for frequently accessed shop/shelf data

When extending functionality, maintain the **dual model pattern**, follow the **repository interfaces**, and use **AutoMapper for all conversions** between FireStore and Handleliste models.
Do minimum changes at all times, and ensure new features adhere to existing architectural patterns.
