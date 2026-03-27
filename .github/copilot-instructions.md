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

---

## 🍽️ Ukemeny & Middagsplanlegging - Implementasjonsplan

### Overordnet Konsept
Et system for å planlegge middager for uken, med automatisk generering av handlelister basert på valgte retter. Systemet lærer av brukerens valg og kan foreslå varierte menyer basert på kategori-balanse (barn-favoritter, fisk, kjøtt).

---

### Datamodell (Nye Entiteter)

#### 1. MealRecipe (Middagsoppskrift)
**Firestore**: `Shared/FireStoreDataModels/MealRecipe.cs`
```csharp
[FirestoreData]
public class MealRecipe : EntityBase
{
    [FirestoreProperty]
    public MealCategory Category { get; set; }
    
    [FirestoreProperty]
    public int PopularityScore { get; set; }
    
    [FirestoreProperty]
    public DateTime? LastUsed { get; set; }
    
    [FirestoreProperty]
    public ICollection<MealIngredient> Ingredients { get; set; }
    
    [FirestoreProperty]
    public bool IsActive { get; set; }
}

public enum MealCategory 
{
    KidsLike,    // Barn-favoritter
    Fish,        // Fisk
    Meat,        // Kjøtt
    Vegetarian,   // Vegetar
    Kylling, 
    Kjøttdeig, 
    Fest
}
```

**DTO**: `Shared/HandlelisteModels/MealRecipeModel.cs`
```csharp
public class MealRecipeModel : EntityBase
{
    public MealCategory Category { get; set; }
    public int PopularityScore { get; set; }
    public DateTime? LastUsed { get; set; }
    public ICollection<MealIngredientModel> Ingredients { get; set; }
    public bool IsActive { get; set; }
}
```

#### 2. MealIngredient (Ingrediens i middagsrett)
**Firestore**: `Shared/FireStoreDataModels/MealIngredient.cs`
```csharp
[FirestoreData]
public class MealIngredient : EntityBase
{
    [FirestoreProperty]
    public string MealRecipeId { get; set; }
    
    [FirestoreProperty]
    public ShopItem ShopItem { get; set; }
    
    [FirestoreProperty]
    public int StandardQuantity { get; set; }
    
    [FirestoreProperty]
    public bool IsOptional { get; set; }
}
```

**DTO**: `Shared/HandlelisteModels/MealIngredientModel.cs`
```csharp
public class MealIngredientModel : EntityBase
{
    public string MealRecipeId { get; set; }
    public ShopItemModel ShopItem { get; set; }
    public int StandardQuantity { get; set; }
    public bool IsOptional { get; set; }
}
```

#### 3. WeekMenu (Ukemeny)
**Firestore**: `Shared/FireStoreDataModels/WeekMenu.cs`
```csharp
[FirestoreData]
public class WeekMenu : EntityBase
{
    [FirestoreProperty]
    public int WeekNumber { get; set; }
    
    [FirestoreProperty]
    public int Year { get; set; }
    
    [FirestoreProperty]
    public ICollection<DailyMeal> DailyMeals { get; set; }
    
    [FirestoreProperty]
    public DateTime CreatedDate { get; set; }
    
    [FirestoreProperty]
    public bool IsActive { get; set; }
}
```

**DTO**: `Shared/HandlelisteModels/WeekMenuModel.cs`
```csharp
public class WeekMenuModel : EntityBase
{
    public int WeekNumber { get; set; }
    public int Year { get; set; }
    public ICollection<DailyMealModel> DailyMeals { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsActive { get; set; }
}
```

#### 4. DailyMeal (Daglig middag)
**Firestore**: `Shared/FireStoreDataModels/DailyMeal.cs`
```csharp
[FirestoreData]
public class DailyMeal : EntityBase
{
    [FirestoreProperty]
    public string WeekMenuId { get; set; }
    
    [FirestoreProperty]
    public DayOfWeek DayOfWeek { get; set; }
    
    [FirestoreProperty]
    public MealRecipe MealRecipe { get; set; }
    
    [FirestoreProperty]
    public ICollection<MealIngredient> CustomIngredients { get; set; }
}
```

**DTO**: `Shared/HandlelisteModels/DailyMealModel.cs`
```csharp
public class DailyMealModel : EntityBase
{
    public string WeekMenuId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public MealRecipeModel MealRecipe { get; set; }
    public ICollection<MealIngredientModel> CustomIngredients { get; set; }
}
```

---

### Implementasjon i Faser

#### FASE 1: Grunnleggende Middags-CRUD
**Mål**: Kunne administrere middagsretter og deres ingredienser

##### Backend (Api)
1. **Opprett Datamodeller**
   - `Shared/FireStoreDataModels/MealRecipe.cs`
   - `Shared/FireStoreDataModels/MealIngredient.cs`
   - `Shared/HandlelisteModels/MealRecipeModel.cs`
   - `Shared/HandlelisteModels/MealIngredientModel.cs`
   - `Shared/FireStoreDataModels/MealCategory.cs` (enum)

2. **AutoMapper Profile**
   - Utvid `Api/ShoppingListProfile.cs`:
   ```csharp
   CreateMap<MealRecipe, MealRecipeModel>().ReverseMap();
   CreateMap<MealIngredient, MealIngredientModel>().ReverseMap();
   ```

3. **API Controller**
   - `Api/Controllers/MealRecipeController.cs`
   - Standard CRUD endpoints:
     - `GET /api/mealrecipes` - alle retter sortert etter popularitet
     - `GET /api/mealrecipe/{id}` - enkelt rett
     - `POST /api/mealrecipes` - opprett ny
     - `PUT /api/mealrecipes` - oppdater
     - `DELETE /api/mealrecipe/{id}` - slett

4. **Repository Registrering**
   - I `Api/Program.cs`:
   ```csharp
   services.AddSingleton<IGenericRepository<MealRecipe>, GoogleFireBaseGenericRepository<MealRecipe>>();
   services.AddSingleton<IGenericRepository<MealIngredient>, GoogleFireBaseGenericRepository<MealIngredient>>();
   ```

##### Frontend (Client)
1. **Administrasjonsside for Middager**
   - `Client/Pages/Meals/MealManagementPage.razor`
   - Funksjoner:
     - Liste over alle middagsretter (sortert etter PopularityScore)
     - Søkefelt for å filtrere middager
     - Legg til ny middag (navn + kategori)
     - Slett middag
     - Navigasjon til detaljer
     - Vise kategori-ikoner: 👶 barn, 🐟 fisk, 🥩 kjøtt, 🥬 vegetar

2. **Detaljer/Rediger Middagsrett**
   - `Client/Pages/Meals/OneMealRecipePage.razor`
   - Funksjoner:
     - Rediger navn og kategori
     - Liste over ingredienser med mengde
     - Legg til ingrediens (autocomplete fra ShopItems)
     - Sett standardmengde per ingrediens
     - Marker ingredienser som valgfrie
     - Slett ingrediens

3. **Navigasjon**
   - Legg til i `Client/Shared/NewNavComponent.razor`:
   ```razor
   <a href="/meals">Middager</a>
   ```

##### Testing
- Enhetstester for MealRecipeController (CRUD operasjoner)
- E2E test: Opprette middag med ingredienser

---

#### FASE 2: Ukemeny & Automatisk Handleliste
**Mål**: Planlegge uken og generere handleliste fra valgte middager

##### Backend (Api)
1. **Opprett Datamodeller**
   - `Shared/FireStoreDataModels/WeekMenu.cs`
   - `Shared/FireStoreDataModels/DailyMeal.cs`
   - `Shared/HandlelisteModels/WeekMenuModel.cs`
   - `Shared/HandlelisteModels/DailyMealModel.cs`

2. **AutoMapper Profile**
   ```csharp
   CreateMap<WeekMenu, WeekMenuModel>().ReverseMap();
   CreateMap<DailyMeal, DailyMealModel>().ReverseMap();
   ```

3. **API Controller**
   - `Api/Controllers/WeekMenuController.cs`
   - Standard CRUD + spesielle endpoints:
     - `GET /api/weekmenus` - alle ukemenyer
     - `GET /api/weekmenu/{id}` - enkelt ukemeny
     - `GET /api/weekmenu/week/{weekNumber}/year/{year}` - hent ukemeny for spesifikk uke
     - `POST /api/weekmenus` - opprett ny ukemeny
     - `PUT /api/weekmenus` - oppdater ukemeny
     - `DELETE /api/weekmenu/{id}` - slett ukemeny
     - `POST /api/weekmenu/{id}/generate-shoppinglist` - generer handleliste

4. **Generate Shopping List Logic**
   ```csharp
   // I WeekMenuController
   public async Task<ShoppingListModel> GenerateShoppingList(string weekMenuId)
   {
       var weekMenu = await _repository.Get(weekMenuId);
       var ingredients = new Dictionary<string, ShoppingListItemModel>();
       
       foreach (var dailyMeal in weekMenu.DailyMeals)
       {
           var mealIngredients = dailyMeal.CustomIngredients.Any() 
               ? dailyMeal.CustomIngredients 
               : dailyMeal.MealRecipe.Ingredients;
           
           foreach (var ingredient in mealIngredients)
           {
               var key = ingredient.ShopItem.Id;
               if (ingredients.ContainsKey(key))
                   ingredients[key].Mengde += ingredient.StandardQuantity;
               else
                   ingredients[key] = new ShoppingListItemModel
                   {
                       Varen = ingredient.ShopItem,
                       Mengde = ingredient.StandardQuantity,
                       IsDone = false
                   };
           }
       }
       
       return new ShoppingListModel
       {
           Name = $"Uke {weekMenu.WeekNumber} {weekMenu.Year}",
           ShoppingItems = ingredients.Values.ToList()
       };
   }
   ```

##### Frontend (Client)
1. **Oversikt Ukemenyer**
   - `Client/Pages/Meals/WeekMenuListPage.razor`
   - Liste over alle ukemenyer
   - Opprett ny ukemeny for valgt uke
   - Navigere til detaljer

2. **Ukemeny Detaljer**
   - `Client/Pages/Meals/OneWeekMenuPage.razor`
   - 7-dagers kalendervisning (Mandag-Søndag)
   - Dropdown per dag for å velge middag
   - Middager sortert etter popularitet
   - Vis kategori-ikoner per dag
   - Knapp: "Generer handleliste"
   - Flow for å generere liste:
     1. Klikk knapp
     2. Vis forhåndsvisning av ingredienser (modal)
     3. Tillat justering av mengder
     4. Velg: "Lagre som ShoppingList" eller "Lagre som FrequentList"

3. **Integrasjon med Eksisterende Sider**
   - I `ShoppingListMainPage.razor`: Legg til knapp "Importer fra ukemeny"
   - I `FrequentListsPage.razor`: Vis ukemeny-genererte lister med spesielt ikon (📅)

##### Testing
- Test generering av handleliste fra ukemeny
- Test summering av ingredienser (samme vare i flere middager)
- Test custom ingredients override

---

#### FASE 3: AI-forslag & Variasjon
**Mål**: Systemet foreslår varierte menyer basert på historikk og preferanser

##### Backend (Api)
1. **Suggestion Endpoint**
   - `POST /api/weekmenu/suggest`
   - Request body:
   ```csharp
   public class MenuSuggestionRequest
   {
       public int WeekNumber { get; set; }
       public int Year { get; set; }
       public Dictionary<MealCategory, int> CategoryPercentages { get; set; }
       // Default: { KidsLike: 30, Fish: 30, Meat: 40 }
   }
   ```
   
   - Response: `MenuSuggestionModel` med 7 foreslåtte middager

2. **Suggestion Algorithm**
   ```csharp
   public async Task<MenuSuggestionModel> SuggestMenu(MenuSuggestionRequest request)
   {
       // 1. Hent siste 4 ukers menyer
       var recentMenus = await GetRecentMenus(4);
       var recentMealIds = recentMenus.SelectMany(m => m.DailyMeals)
           .Select(dm => dm.MealRecipe.Id).ToList();
       
       // 2. Hent alle aktive middagsretter
       var allMeals = await _mealRepository.Get();
       
       // 3. Filtrer bort middager brukt siste 2 uker
       var availableMeals = allMeals
           .Where(m => !recentMealIds.Contains(m.Id))
           .OrderByDescending(m => m.PopularityScore)
           .ToList();
       
       // 4. Balanser kategorier basert på preferanser
       var suggestions = new List<MealRecipeModel>();
       foreach (var category in request.CategoryPercentages)
       {
           var count = (int)Math.Round(7 * category.Value / 100.0);
           var categoryMeals = availableMeals
               .Where(m => m.Category == category.Key)
               .Take(count);
           suggestions.AddRange(categoryMeals);
       }
       
       // 5. Fyll opp til 7 hvis nødvendig
       while (suggestions.Count < 7)
       {
           var filler = availableMeals
               .Where(m => !suggestions.Contains(m))
               .FirstOrDefault();
           if (filler != null) suggestions.Add(filler);
           else break;
       }
       
       return new MenuSuggestionModel { SuggestedMeals = suggestions };
   }
   ```

##### Frontend (Client)
1. **Forslag i Ukemeny**
   - I `OneWeekMenuPage.razor`:
   - Knapp: "🤖 Foreslå meny"
   - Modal med foreslåtte middager:
     - Vis middag per dag med årsak ("Ikke brukt på 3 uker", "Populær", "Fisk mangler")
     - Mulighet til å bytte ut forslag
     - Knapp: "Godta forslag" → fyller ukemenyen

2. **Statistikk i Middags-administrasjon**
   - I `MealManagementPage.razor`:
   - Seksjon: "📊 Statistikk"
     - Mest brukte middager siste 30 dager
     - Kategori-balanse siste måned (pie chart eller prosent)
     - Varslinger: "⚠️ Du har ikke hatt fisk på 2 uker"

3. **Preferanser (valgfritt)**
   - `Client/Pages/Meals/MealPreferencesPage.razor`
   - Sliders for kategori-balanse:
     - Barn-favoritter: 0-100%
     - Fisk: 0-100%
     - Kjøtt: 0-100%
     - Vegetar: 0-100%
   - Lagre i localStorage
   - Brukes som default i suggestion requests

##### Testing
- Test suggestion algorithm med ulike historikker
- Test kategori-balansering
- Test edge cases (ikke nok middager i en kategori)

---

### UI/UX Design

#### Ikoner for Kategorier
- 👶 `KidsLike` - Barn-favoritter
- 🐟 `Fish` - Fisk
- 🥩 `Meat` - Kjøtt
- 🥬 `Vegetarian` - Vegetar

#### Ukemeny Kalendervisning
```
┌─────────────────────────────────────────────┐
│ Ukemeny - Uke 47 2025              [🤖][📝]│
├─────────────────────────────────────────────┤
│ Mandag    │ [Velg middag ▼           ]  🥩 │
│ Tirsdag   │ Taco                        👶🥩│
│ Onsdag    │ [Velg middag ▼           ]  🐟 │
│ Torsdag   │ Fiskegrateng                 🐟 │
│ Fredag    │ Pizza                       👶🥩│
│ Lørdag    │ Biff                         🥩 │
│ Søndag    │ [Velg middag ▼           ]  🥩 │
└─────────────────────────────────────────────┘
[🤖 Foreslå meny]  [📝 Generer handleliste]
```

#### Middags-administrasjon
```
┌─────────────────────────────────────────────┐
│ Middagsretter              [Søk: _____] [+] │
├─────────────────────────────────────────────┤
│ 🥇 Taco (47 ganger)                 👶🥩 [→]│
│    Sist brukt: 15.nov                       │
├─────────────────────────────────────────────┤
│ 🥈 Pizza (42 ganger)                👶🥩 [→]│
│    Sist brukt: 08.nov                       │
├─────────────────────────────────────────────┤
│ 🥉 Fiskegrateng (28 ganger)          🐟  [→]│
│    Sist brukt: 20.nov                       │
└─────────────────────────────────────────────┘
```

---

### Integrasjon med Eksisterende System

#### Gjenbruk av ShopItems
- `MealIngredient.ShopItem` lenker til eksisterende `ShopItemModel`
- Autocomplete fra `ShopItems` når man legger til ingredienser
- Ingen duplikasjon av varedata

#### Gjenbruk av FrequentShoppingList
- Ukemeny-genererte lister kan lagres som `FrequentShoppingListModel`
- Kan importeres til ShoppingList som vanlig
- Markeres med spesiell kilde: "Fra Uke 47 2025"

#### Gjenbruk av ShoppingList
- Direkte generering til `ShoppingListModel`
- Bruker eksisterende `ShoppingListItemModel` struktur
- Fullt kompatibel med butikk-sortering

---

### API URL Management
Legg til i `Client/Common/ShoppingListKeysEnum.cs`:
```csharp
public enum ShoppingListKeysEnum
{
    // ... existing ...
    MealRecipes,
    MealRecipe,
    WeekMenus,
    WeekMenu
}
```

Legg til i `Client/Common/ISettings.cs`:
```csharp
case ShoppingListKeysEnum.MealRecipes:
    return $"{API_Prefix}mealrecipes";
case ShoppingListKeysEnum.MealRecipe:
    return $"{API_Prefix}mealrecipe";
case ShoppingListKeysEnum.WeekMenus:
    return $"{API_Prefix}weekmenus";
case ShoppingListKeysEnum.WeekMenu:
    return $"{API_Prefix}weekmenu";
```

---

### Testing Strategy

#### Enhetstester (Api.Tests)
- `MealRecipeControllerTests.cs` - CRUD operasjoner
- `WeekMenuControllerTests.cs` - CRUD + generate shopping list
- `MenuSuggestionTests.cs` - suggestion algorithm

#### Integrasjonstester
- Test full flow: Opprett middag → Legg til i ukemeny → Generer liste
- Test ingrediens-summering (samme vare i flere middager)
- Test suggestion med ulike historikker

#### E2E Tester (Playwright)
- Test opprettelse av middag med ingredienser
- Test ukemeny-planlegging
- Test generering av handleliste fra ukemeny

---

### Utviklingsrekkefølge (Sprinter)

**Sprint 1: Datamodell & Backend Grunnlag**
1. Opprett alle Firestore modeller
2. Opprett alle DTO modeller
3. Utvid AutoMapper profil
4. Opprett MealRecipeController
5. Registrer repositories
6. Enhetstester for controller

**Sprint 2: Middags-administrasjon Frontend**
7. MealManagementPage (liste + CRUD)
8. OneMealRecipePage (detaljer + ingredienser)
9. Legg til "Middager" i navigasjon
10. E2E test: Opprett middag med ingredienser

**Sprint 3: Ukemeny Backend**
11. Opprett WeekMenu/DailyMeal modeller
12. Opprett WeekMenuController
13. Implementer GenerateShoppingList endpoint
14. Enhetstester for ukemeny + liste-generering

**Sprint 4: Ukemeny Frontend**
15. WeekMenuListPage (oversikt)
16. OneWeekMenuPage (kalender + valg av middager)
17. Implementer "Generer handleliste" flow
18. Integrasjon med ShoppingList/FrequentList
19. E2E test: Full flow fra ukemeny til handleliste

**Sprint 5: AI-forslag (valgfritt)**
20. Implementer suggestion algorithm backend
21. Suggestion endpoint
22. Frontend: Forslag-UI i OneWeekMenuPage
23. Frontend: Statistikk i MealManagementPage
24. Testing av suggestion logic

---

### Potensielle Utvidelser (Fremtidig)

1. **Sesongvarer**
   - `MealRecipe.Season` property (Spring, Summer, Fall, Winter)
   - Filtrer forslag basert på årstid

2. **Multi-bruker Preferanser**
   - Bruker-profiler med individuelle preferanser
   - Familie-modus med balansering mellom preferanser

3. **Ernæringsinformasjon**
   - Kalorier, protein, karbohydrater per middagsrett
   - Vis ernæringsbalanse for uken

4. **Restevare-håndtering**
   - Forslag basert på hva som er i kjøleskapet
   - Reduser matsvinn ved å bruke rester

5. **Deling av Oppskrifter**
   - Del middagsretter med andre brukere
   - Importér populære retter fra community

---

### Arkitektoniske Avgjørelser

#### Hvorfor ikke separate Ingredients-tabell?
- `MealIngredient` lenker direkte til `ShopItem` (eksisterende vare)
- Unngår duplikasjon av varedata
- Enklere datahåndtering

#### Hvorfor PopularityScore i stedet for Usage Count?
- Enklere å justere vekting (f.eks. nylige bruk teller mer)
- Kan implementere decay over tid
- Raskere sortering (enkelt tall vs. telle historikk)

#### Hvorfor WeekNumber i stedet for DateRange?
- Enklere å sammenligne historikk ("siste 4 uker")
- Naturlig organisering av menyer
- Standard uke-nummerering (ISO 8601)

---

### Implementasjonsnotater

- **Følg eksisterende mønstre**: Bruk samme struktur som ShoppingList/FrequentList
- **AutoMapper alltid**: Aldri send Firestore-modeller direkte til frontend
- **Repository pattern**: All data access gjennom `IGenericRepository<T>`
- **Caching**: Vurder å cache middagsretter (sjelden endret data)
- **Norwegian naming**: Vurder norske navn for nye properties hvor passende
- **LastModified**: Legg til på alle nye entiteter for sporing
