# Shopping List Application (Handleliste)

A comprehensive **Blazor WebAssembly** shopping list application with **Azure Functions** backend and **Google Firestore** database. Features intelligent shop-specific item sorting, natural alphanumeric sorting, and timestamp-based list management.

## Technology Stack

- **Frontend**: Blazor WebAssembly (.NET 9) with Syncfusion UI components
- **Backend**: Azure Functions v4 (.NET 9) with AutoMapper and repository pattern
- **Database**: Google Cloud Firestore (NoSQL)
- **Testing**: xUnit with Playwright for E2E tests (143 total tests)

## Key Features

### 🛒 Shopping List Management
- **Multiple Shopping Lists**: Create and manage multiple shopping lists with timestamps
- **Frequent Lists**: Pre-defined templates for recurring shopping needs
- **Smart Sorting**: 
  - Active lists appear first, completed lists at the bottom
  - Natural alphanumeric sorting (e.g., "Uke 1", "Uke 10", "Uke 42" sort correctly)
  - Newest lists first within each category
- **Item Status Tracking**: Mark items and entire lists as completed

### 🏪 Shop-Specific Sorting
- **Custom Shop Layouts**: Define shops with shelves and their traversal order
- **Automatic Item Sorting**: Items automatically sort based on shelf order when shopping at a specific store
- **Category Management**: Organize items into categories linked to shop shelves

### 📊 Data Management
- **Timestamp Tracking**: `LastModified` property on all entities with automatic migration for legacy data
- **Lazy Migration**: Existing data automatically gets timestamps on first access
- **Dual Model Architecture**: Separate Firestore data models and DTO models for clean architecture

### 🧪 Testing Infrastructure
- **Unit Tests**: 126 tests covering API and Client logic
  - 65 API controller tests
  - 61 Client component tests (including natural sorting and list ordering)
- **E2E Tests**: 20 Playwright tests for critical user workflows
- **Test Coverage**: Comprehensive coverage of sorting algorithms, CRUD operations, and UI interactions

## Getting Started

### Prerequisites
- .NET 9 SDK
- Azure Functions Core Tools
- Google Cloud Firestore account (for production)
- Syncfusion license key (for UI components)

### Installation

1. Clone the repository to your local machine

1. In the **Api** folder, copy `local.settings.example.json` to `local.settings.json`

1. Continue using either Visual Studio or Visual Studio Code

### Visual Studio 2022

Once you clone the project, open the solution in the latest release of [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) with the Azure workload installed, and follow these steps:

1. Right-click on the solution and select **Set Startup Projects...**.

1. Select **Multiple startup projects** and set the following actions for each project:
    - *Api* - **Start**
    - *Client* - **Start**
    - *Shared* - None

1. Press **F5** to launch both the client application and the Functions API app.

### Visual Studio Code with Azure Static Web Apps CLI for a better development experience (Optional)

1. Install the [Azure Static Web Apps CLI](https://www.npmjs.com/package/@azure/static-web-apps-cli) and [Azure Functions Core Tools CLI](https://www.npmjs.com/package/azure-functions-core-tools).

1. Open the folder in Visual Studio Code.

1. Delete file `Client/wwwroot/appsettings.Development.json`

1. In the VS Code terminal, run the following command to start the Static Web Apps CLI, along with the Blazor WebAssembly client application and the Functions API app:

    ```bash
    swa start http://localhost:5000 --api-location http://localhost:7071
    ```

    The Static Web Apps CLI (`swa`) starts a proxy on port 4280 that will forward static site requests to the Blazor server on port 5000 and requests to the `/api` endpoint to the Functions server. 

1. Open a browser and navigate to the Static Web Apps CLI's address at `http://localhost:4280`. You'll be able to access both the client application and the Functions API app in this single address. When you navigate to the "Fetch Data" page, you'll see the data returned by the Functions API app.

1. Enter Ctrl-C to stop the Static Web Apps CLI.

## Project Structure

```
shoppinglist/
├── Client/                          # Blazor WebAssembly Frontend
│   ├── Pages/
│   │   ├── Shopping/
│   │   │   ├── ShoppingListMainPage.razor      # Main list overview with smart sorting
│   │   │   ├── OneShoppingListPage.razor       # Individual list with shop-specific sorting
│   │   │   ├── OneFrequentListPage.razor       # Frequent list templates
│   │   │   ├── ManageMyShopsPage.razor         # Shop management
│   │   │   └── OneShopManagmentPage.razor      # Shop shelf configuration
│   │   └── Admin/
│   │       └── AdminDataBase.razor              # Database administration
│   ├── Common/
│   │   ├── NaturalSortComparer.cs              # Natural alphanumeric sorting algorithm
│   │   └── ISettings.cs                         # API URL management
│   └── Shared/
│       ├── NewNavComponent.razor                # Navigation with Admin dropdown
│       └── ShoppingListComponents/
│           ├── OneShoppingListItemComponent.razor
│           └── ListSummaryFooter.razor
├── Api/                             # Azure Functions Backend
│   ├── Controllers/
│   │   ├── ShoppingListController.cs            # CRUD + timestamp migration
│   │   ├── ShopsController.cs                   # Shop and shelf management
│   │   ├── ShopsItemsController.cs              # Item management
│   │   └── ShopItemCategoryController.cs        # Category management
│   ├── Program.cs                               # DI configuration (Debug/Production repos)
│   └── ShoppingListProfile.cs                   # AutoMapper profiles
├── Shared/
│   ├── FireStoreDataModels/                     # Database entities with [FirestoreData]
│   │   ├── ShoppingList.cs
│   │   ├── Shop.cs
│   │   ├── ShopItem.cs
│   │   └── ItemCategory.cs
│   ├── HandlelisteModels/                       # DTO models for API/UI
│   ├── BaseModels/
│   │   └── EntityBase.cs                        # Base with Id, Name, LastModified
│   └── Repository/
│       ├── IGenericRepository.cs
│       ├── GoogleFireBaseGenericRepository.cs   # Production Firestore implementation
│       └── MemoryGenericRepository.cs           # Debug in-memory implementation
├── Client.Tests/                    # Unit Tests (xUnit)
│   ├── NaturalSortComparerTests.cs              # 11 tests for natural sorting
│   ├── ShoppingListSortingTests.cs              # 3 tests for list ordering
│   └── ...                                       # 47 additional client tests
├── Api.Tests/                       # API Tests (xUnit)
│   └── Controllers/
│       ├── ShoppingListControllerTests.cs       # Including timestamp migration tests
│       └── ...                                   # 65 total API tests
└── Client.Tests.Playwright/         # E2E Tests
    └── Tests/
        ├── ShoppingListSortingTests.cs          # 7 E2E sorting tests
        ├── NavigationTests.cs
        └── ...                                   # 20 total E2E tests
```

## Architecture & Design Patterns

### Dual Model Pattern
The application uses **two parallel model hierarchies** for clean separation:

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

**AutoMapper** handles all conversions with `.ReverseMap()` in `Api/ShoppingListProfile.cs`.

### Repository Pattern
```csharp
public interface IGenericRepository<T> {
    Task<IEnumerable<T>> Get();
    Task<T> Get(string id);
    Task<T> Insert(T entity);
    Task Update(T entity);
    Task Delete(string id);
}
```

**Two implementations**:
- `MemoryGenericRepository<T>`: In-memory for DEBUG mode and testing
- `GoogleFireBaseGenericRepository<T>`: Firestore for production

### Sorting Algorithms

#### Natural Alphanumeric Sorting
`NaturalSortComparer` class handles proper sorting of mixed text and numbers:
```csharp
// Sorts correctly: "Uke 1", "Uke 2", "Uke 10", "Uke 41", "Uke 42"
// Not incorrect: "Uke 1", "Uke 10", "Uke 2", "Uke 41", "Uke 42"
```

Uses regex to split strings into numeric and text parts, comparing numbers numerically.

#### Shopping List Sorting (Three-Level)
In `ShoppingListMainPage.razor`:
```csharp
AvailableShoppingLists = AvailableShoppingLists
    .OrderBy(f => f.IsDone)                           // Active lists first (false < true)
    .ThenByDescending(f => f.LastModified)            // Newest first within each group
    .ThenBy(f => f.Name, new NaturalSortComparer())   // Natural alphanumeric by name
    .ToList();
```

#### Shop-Specific Item Sorting
In `OneShoppingListPage.razor` - `SortShoppingList()`:
1. Select shop from dropdown
2. Build category index based on shelf `SortIndex`
3. Assign sort indices to items based on their category
4. Sort items by ascending index (follows shelf traversal order)

### Timestamp & Migration Strategy
- **EntityBase**: All entities inherit `LastModified` (DateTime?)
- **Lazy Migration**: API checks for null `LastModified` on GET, sets to `DateTime.UtcNow` and updates
- **Automatic Tracking**: POST/PUT operations automatically set `LastModified`

### Navigation Structure
```
Main Menu:
├── Handlelister (Shopping Lists)
└── Admin (Dropdown)
    ├── Hyppige Lister (Frequent Lists)
    ├── Håndter butikker (Manage Shops)
    ├── Administrer varer (Manage Items)
    └── Administrer kategorier (Manage Categories)
```

## Google Firestore Configuration

This application uses Google Cloud Firestore as the production database.

### Local Development Setup

1. **Obtain Google Service Account Credentials**
   - Download the service account JSON file from Google Cloud Console
   - Save it to a secure location (e.g., `D:\Privat\GIT\Google keys\supergnisten-shoppinglist-eb82277057ad.json`)

2. **Set Environment Variable**
   ```powershell
   # Option 1: Set file path (for local development)
   $env:GOOGLE_CREDENTIALS = "D:\Privat\GIT\Google keys\supergnisten-shoppinglist-eb82277057ad.json"
   
   # Option 2: Set JSON content directly (for production/cloud deployment)
   $env:GOOGLE_CREDENTIALS = Get-Content "D:\Privat\GIT\Google keys\supergnisten-shoppinglist-eb82277057ad.json" -Raw
   ```

3. **Smart Credential Handling**
   The `GoogleDbContext` automatically detects whether the environment variable contains:
   - **File path**: Uses `Path.IsPathFullyQualified()` to detect and reads the JSON content
   - **JSON content**: Uses the value directly
   
   This allows seamless switching between local development (file path) and cloud deployment (JSON content).

### Debug vs Production Data
- **Debug mode** (`#if DEBUG`): Uses `MemoryGenericRepository` with in-memory test data
- **Production mode**: Uses `GoogleFireBaseGenericRepository` with live Firestore data

Switch between modes by changing the build configuration in `Api/Program.cs`.

## Testing

### Running Tests

```bash
# Run all unit tests (API + Client)
dotnet test

# Run only API tests
dotnet test Api.Tests/Api.Tests.csproj

# Run only Client tests
dotnet test Client.Tests/Client.Tests.csproj

# Run specific test class
dotnet test --filter "FullyQualifiedName~ShoppingListSortingTests"

# Run E2E tests (requires running application)
dotnet test Client.Tests.Playwright/Client.Tests.Playwright.csproj
```

### Test Coverage

**Unit Tests: 126 tests**
- API Controllers (65 tests)
  - ShoppingListController: CRUD + timestamp migration (7 new tests)
  - ShopsController: Shop and shelf management
  - ShopsItemsController: Item management
  - ShopItemCategoryController: Category management
  
- Client Components (61 tests)
  - NaturalSortComparer: 11 tests for natural sorting edge cases
  - ShoppingListSorting: 3 tests for multi-level list sorting
  - Component logic tests

**E2E Tests: 20 Playwright tests**
- Navigation and page loading
- Shopping list sorting behavior
- Item management workflows
- Shop-specific sorting verification

## Recent Enhancements (2024-2025)

### ✅ Smart List Sorting
- **Three-level sorting**: Active/Completed status → Timestamp → Natural name
- **Natural alphanumeric sorting**: Handles "Uke 1" through "Uke 43" correctly
- **Completed lists at bottom**: Active lists prioritized in main view

### ✅ Timestamp System
- **LastModified tracking**: All entities now track last modification time
- **Lazy migration**: Existing data automatically gets timestamps
- **Automatic updates**: POST/PUT operations set timestamps

### ✅ UI/UX Improvements
- **Newest items at top**: New shopping list items insert at position 0
- **Improved navigation**: Frequent Lists moved to Admin dropdown
- **Status persistence**: Fixed IsDone checkbox binding and API endpoint

### ✅ Testing Infrastructure
- **Comprehensive test suite**: 143 total tests (126 unit + 17 E2E)
- **Natural sort tests**: 11 tests covering edge cases
- **Migration tests**: Verify timestamp migration logic
- **E2E coverage**: Critical user workflows

## Deploy to Azure Static Web Apps

This application can be deployed to [Azure Static Web Apps](https://docs.microsoft.com/azure/static-web-apps), to learn how, check out [our quickstart guide](https://aka.ms/blazor-swa/quickstart).

### Deployment Notes
- Set `GOOGLE_CREDENTIALS` environment variable with JSON content (not file path)
- Configure Syncfusion license key in application settings
- Ensure Azure Functions runtime is set to .NET 9 isolated process

## Contributing

When extending functionality:
- Maintain the **dual model pattern** (Firestore + DTO)
- Follow the **repository interface** for data access
- Use **AutoMapper for all conversions** between model types
- Add tests for new features
- Follow existing architectural patterns
- Make **minimum necessary changes**

## License

This project is licensed under the MIT License.
