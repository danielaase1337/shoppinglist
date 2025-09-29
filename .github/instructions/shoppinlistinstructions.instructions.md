---
applyTo: '**'
---

# Handleliste-applikasjon - Arkitektur og Kodestil Dokumentasjon

## Teknologistack og Arkitektur

### Overordnet Struktur
Applikasjonen er bygget som en **3-lags arkitektur** med følgende komponenter:

1. **Client** - Blazor WebAssembly frontend (.NET 9.0)
2. **Api** - Azure Functions backend (.NET 9.0)
3. **Shared** - Delt bibliotek for datamodeller og business logic (.NET 9.0)

### Teknologier i Bruk

#### Backend (Api-prosjektet)
- **Azure Functions v4** - Serverless compute platform
- **Google Cloud Firestore** - NoSQL database
- **AutoMapper** - Object-to-object mapping
- **Dependency Injection** - Built-in .NET DI container

#### Frontend (Client-prosjektet)
- **Blazor WebAssembly** - Single Page Application framework
- **Syncfusion Blazor Components** - UI komponentbibliotek
- **HttpClient** - For API-kommunikasjon
- **Dependency Injection** - For service management

#### Database og Data Layer
- **Google Cloud Firestore** - Dokument-basert NoSQL database
- **Repository Pattern** - Abstrahert dataacess layer

## Data-modell og Entiteter

### Hovedentiteter og deres relasjoner

```
Shop (Butikk)
├── ShelfsInShop: Collection<Shelf>
    └── Shelf (Hylle)
        ├── SortIndex: int (for butikk-spesifikk rekkefølge)
        └── ItemCategories: Collection<ItemCategory>
            └── ItemCategory (Varekategori)
                ├── Name: string
                └── Id: string

ShoppingList (Handleliste)  
└── ShoppingItems: Collection<ShoppingListItem>
    └── ShoppingListItem
        ├── Varen: ShopItem
        ├── Mengde: int
        └── IsDone: bool

ShopItem (Vare)
├── Unit: string (Stk, vekt osv.)
└── ItemCategory: ItemCategory
```

### Arv og Base-klasser
- **EntityBase**: Base-klasse for alle entiteter med `Id`, `Name`, og UI-relaterte properties
- **FireStoreCommonBase**: Base for Firestore-spesifikke entiteter
- **ShoppingListBaseModel**: Base for shopping list-relaterte modeller

### Mapping-strategi
Applikasjonen bruker **AutoMapper** med separate modeller for:
- **FireStoreDataModels**: Database-entiteter med Firestore-attributter
- **HandlelisteModels**: View/DTO-modeller for API og UI

## Sorteringslogikk (Hovedfunksjonalitet)

### Eksisterende implementasjon
Sorteringslogikken finnes i `OneShoppingListPage.razor` i `SortShoppingList()` metoden:

```csharp
void SortShoppingList()
{
    if (shopIndex == null || shopIndex == -1) return;
    
    SelectedShop = AvailableShops.ToArray()[shopIndex.Value];
    var itemCategoriesWithShortIndex = new List<ItemCategoryModel>();
    
    // Bygger opp sorterings-indeks basert på hyller
    foreach (var shelf in SelectedShop.ShelfsInShop)
    {
        var startSort = shelf.SortIndex;
        foreach (var cat in shelf.ItemCateogries)
        {
            startSort += 1;
            cat.SortIndex = startSort;
            itemCategoriesWithShortIndex.Add(cat);
        }
    }
    
    // Tildeler sorterings-indeks til varer i handlelisten
    foreach (var item in ThisShoppingList.ShoppingItems)
    {
        var cat = item.Varen.ItemCategory;
        var thisCatSort = itemCategoriesWithShortIndex.FirstOrDefault(f => f.Id.Equals(cat.Id));
        if (thisCatSort != null)
            item.Varen.ItemCategory.SortIndex = thisCatSort.SortIndex;
    }
    
    // Sorterer listen
    ThisShoppingList.ShoppingItems = ThisShoppingList.ShoppingItems
        .OrderBy(f => f.Varen.ItemCategory.SortIndex).ToList();
    FilterList(activeListFiler);
}
```

### Sorteringslogikk Forklart
1. **Butikk-valg**: Bruker velger en spesifikk butikk fra dropdown
2. **Hylle-rekkefølge**: Hver hylle har en `SortIndex` som definerer rekkefølgen i butikken
3. **Kategori-indeksering**: Varekategorier får tildelt sorteringsindeks basert på hvilken hylle de tilhører
4. **Vare-sortering**: Varer i handlelisten sorteres etter deres kategori-indeks

## API-struktur og Endepunkter

### Controller-mønster
Alle API-controllere følger samme mønster og arver fra `ControllerBase`:

**Støttede HTTP-metoder per controller:**
- `GET` (alle og enkelt)
- `POST` (opprett ny)
- `PUT` (oppdater eksisterende)
- `DELETE` (slett)

### API Endepunkter
```
/api/shoppinglists     - Handlelister (GET, POST, PUT)
/api/shoppinglist/{id} - Enkelt handleliste (GET, DELETE)
/api/shops            - Butikker (GET, POST, PUT)
/api/shop/{id}        - Enkelt butikk (GET, DELETE)
/api/shopitems        - Varer (GET, POST, PUT)
/api/shopitem/{id}    - Enkelt vare (GET, DELETE)
```

## Frontend-arkitektur

### Blazor-komponent Struktur
```
Pages/
├── Index.razor
├── Admin/
│   └── AdminDataBase.razor
└── Shopping/
    ├── ManageMyShopsPage.razor      - Butikk-administrasjon
    ├── OneShopManagmentPage.razor   - Enkelt butikk-setup
    ├── OneShoppingListPage.razor    - Hovedkomponent for handleliste
    ├── PersonalShopPathManagement.razor
    └── ShoppingListMainPage.razor   - Oversikt over alle lister

Shared/
├── ConfirmDelete.razor
├── LoadingComponent.razor
├── MainLayout.razor
├── NewNavComponent.razor
└── ShoppingListComponents/
    ├── ListSummaryFooter.razor
    └── OneShoppingListItemComponent.razor
```

### State Management og Data Flow
- **HttpClient**: Direkte API-kall fra komponenter
- **Dependency Injection**: Services registreres i Program.cs
- **Settings Service**: Sentralisert API URL management
- **Syncfusion komponenter**: For avanserte UI-elementer (dropdown, autocomplete)

## Kodestil og Konvensjoner

### Naming Conventions
- **Klasser**: PascalCase (f.eks. `ShoppingListController`)
- **Properties**: PascalCase (f.eks. `ShoppingItems`)
- **Fields**: camelCase (f.eks. `shopIndex`)
- **Metoder**: PascalCase (f.eks. `SortShoppingList()`)

### Arkitektur-mønstre i bruk
- **Repository Pattern**: `IGenericRepository<T>` for data access
- **Dependency Injection**: Løs kopling av services
- **DTO Pattern**: Separate modeller for API og database
- **Azure Functions Pattern**: Serverless function-based API

### Error Handling
- Try-catch blokker i alle API-controllere
- Logging via `ILogger`
- HTTP status koder for API-respons
- Null-checking og validering

## Databse-tilkobling (Firestore)

### Firestore Attributter
```csharp
[FirestoreData]         // Markerer klasse for Firestore
[FirestoreProperty]     // Markerer property for serialisering
```

### Repository Implementation
- **GoogleFireBaseGenericRepository**: Konkret implementasjon
- **MemoryGenericRepository**: In-memory implementasjon for testing
- **IGoogleDbContext**: Database context interface

## Forbedringspunkter og Anbefalinger

### Eksisterende Utfordringer
1. **Manglende controller for Shelf-entiteter** - Ingen API for hyllehåndtering
2. **Sorteringslogikk kun på client-side** - Bør flyttes til backend
3. **Ingen validering av butikk-hylle relasjoner**
4. **Begrenset error handling** i frontend

### Foreslåtte Forbedringer
1. **Legg til ShelfController** for full CRUD på hyller
2. **Implementer server-side sortering** med cached resultater
3. **Legg til validering** av data-integritet
4. **Forbedre error handling** og bruker-feedback
5. **Legge til enhetstester** for sorteringslogikk

## Deployment og Konfiguration

### Azure Static Web Apps
- **Client**: Deployeres som statisk web app
- **Api**: Deployeres som Azure Functions
- **Database**: Google Cloud Firestore (ekstern tjeneste)

### Konfigurasjon
- **API_Prefix**: Konfigureres i appsettings for client
- **Firestore**: Konfigureres via Google Cloud credentials
- **CORS**: Håndteres automatisk av Azure Static Web Apps