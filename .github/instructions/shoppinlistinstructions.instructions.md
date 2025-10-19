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
- **EntityBase**: Base-klasse for alle entiteter med `Id`, `Name`, `LastModified` (DateTime?), og UI-relaterte properties
- **FireStoreCommonBase**: Base for Firestore-spesifikke entiteter
- **ShoppingListBaseModel**: Base for shopping list-relaterte modeller

### Tidsstempel-system
Alle entiteter har `LastModified` property:
```csharp
[FirestoreProperty]
public DateTime? LastModified { get; set; }
```

- **Automatisk oppdatering**: POST/PUT operasjoner setter `LastModified = DateTime.UtcNow`
- **Lazy migration**: GET operasjoner setter timestamp for legacy data uten `LastModified`
- **Sortering**: Brukes for å sortere lister (nyeste først)

### Mapping-strategi
Applikasjonen bruker **AutoMapper** med separate modeller for:
- **FireStoreDataModels**: Database-entiteter med Firestore-attributter
- **HandlelisteModels**: View/DTO-modeller for API og UI

## Sorteringslogikk (Hovedfunksjonalitet)

### 1. Butikk-spesifikk Vare-sortering
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

#### Butikk-spesifikk Sortering Forklart
1. **Butikk-valg**: Bruker velger en spesifikk butikk fra dropdown
2. **Hylle-rekkefølge**: Hver hylle har en `SortIndex` som definerer rekkefølgen i butikken
3. **Kategori-indeksering**: Varekategorier får tildelt sorteringsindeks basert på hvilken hylle de tilhører
4. **Vare-sortering**: Varer i handlelisten sorteres etter deres kategori-indeks

### 2. Smart Handleliste-sortering (Flere nivåer)
I `ShoppingListMainPage.razor` - `SortShoppingLists()` metoden:

```csharp
AvailableShoppingLists = AvailableShoppingLists
    .OrderBy(f => f.IsDone)                           // Nivå 1: Aktive først (false < true)
    .ThenByDescending(f => f.LastModified)            // Nivå 2: Nyeste først
    .ThenBy(f => f.Name, new NaturalSortComparer())   // Nivå 3: Natural alfabetisk sortering
    .ToList();
```

#### Sorteringsadferd
- **Aktive lister** (IsDone=false) vises øverst
- **Ferdige lister** (IsDone=true) vises nederst
- Innen hver gruppe: nyeste først, deretter alfabetisk med natural sortering

### 3. Natural Alfanumerisk Sortering
**Implementasjon**: `Client/Common/NaturalSortComparer.cs`

Implementerer `IComparer<string>` for korrekt sortering av blandet tekst og tall:
```csharp
// Korrekt sortering: "Uke 1", "Uke 2", "Uke 10", "Uke 41", "Uke 42"
// Uten natural sort: "Uke 1", "Uke 10", "Uke 2", "Uke 41", "Uke 42"
```

**Algoritme**:
1. Regex deler strenger i nummer/tekst-segmenter
2. Tall sammenlignes numerisk (int.Parse)
3. Tekst sammenlignes alfabetisk (case-insensitive)
4. Håndterer null-strenger

**Test-dekning**: 11 enhetstester dekker edge cases (nulls, case-sensitivity, mixed formats)

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
    ├── ManageMyShopsPage.razor         - Butikk-administrasjon
    ├── OneShopManagmentPage.razor      - Enkelt butikk-setup
    ├── OneShoppingListPage.razor       - Enkelt handleliste (butikk-sortering + nye varer øverst)
    ├── OneFrequentListPage.razor       - Hyppige lister (nye varer øverst)
    ├── PersonalShopPathManagement.razor
    └── ShoppingListMainPage.razor      - Oversikt (multi-level sortering)

Common/
├── NaturalSortComparer.cs              - Natural alfanumerisk sortering
├── ISettings.cs                        - API URL management
└── ShoppingListKeysEnum.cs

Shared/
├── ConfirmDelete.razor
├── LoadingComponent.razor
├── MainLayout.razor
├── NewNavComponent.razor               - Navigasjon med Admin dropdown
└── ShoppingListComponents/
    ├── ListSummaryFooter.razor
    └── OneShoppingListItemComponent.razor
```

### Navigasjonsstruktur
```
Hovedmeny:
├── Handlelister
└── Admin (Dropdown)
    ├── Hyppige Lister
    ├── Håndter butikker
    ├── Administrer varer
    └── Administrer kategorier
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

## Testing Infrastruktur

### Omfattende Test-suite (143 tester totalt)

#### Enhetstester - API (65 tester)
**Lokasjon**: `Api.Tests/Controllers/`

- `ShoppingListControllerTests.cs`:
  - Standard CRUD operasjoner
  - **LastModified_IsSetOnCreation**: Verifiserer timestamp på POST
  - **LastModified_IsUpdatedOnUpdate**: Verifiserer timestamp på PUT
  - **Migration_SetsLastModifiedForLegacyLists**: Tester lazy migration
  - 7 totale tester for timestamp-funksjonalitet

- `ShopsControllerTests.cs`: Butikk og hylle-administrasjon
- `ShopsItemsControllerTests.cs`: Vare CRUD operasjoner
- `ShopItemCategoryControllerTests.cs`: Kategori-administrasjon

#### Enhetstester - Client (61 tester)
**Lokasjon**: `Client.Tests/`

- `NaturalSortComparerTests.cs` (11 tester):
  - `BasicNumberSorting_SortsNumerically`: "Item 1" < "Item 10" < "Item 100"
  - `WeekNumbers_SortCorrectly`: "Uke 1" til "Uke 43"
  - `MixedCaseInsensitive_SortsCorrectly`
  - `NullStrings_HandledGracefully`
  - Edge cases og spesielle tegn

- `ShoppingListSortingTests.cs` (3 tester):
  - `ActiveLists_AppearBeforeCompletedLists`: Verifiserer IsDone=false kommer først
  - `WithinSameDate_UsesNaturalSorting`: Verifiserer natural sort når timestamps er like
  - `MixedActiveAndCompleted_SortsCorrectly`: Komplekst scenario

- Ytterligere komponent og service tester (47 tester)

#### E2E Tester - Playwright (20 tester)
**Lokasjon**: `Client.Tests.Playwright/Tests/`

- `ShoppingListSortingTests.cs` (7 tester):
  - Butikk-spesifikk sortering verifikasjon
  - Natural sortering i UI
  - Syncfusion komponent interaksjoner
  
- `NavigationTests.cs`: Sidelasting og routing
- `DebugTests.cs`: Konsoll-feil deteksjon
- `PageInspectionTests.cs`: UI element verifikasjon

### Kjøre Tester
```bash
# Alle tester
dotnet test

# Spesifikk test-klasse
dotnet test --filter "FullyQualifiedName~NaturalSortComparerTests"

# Kun Client tester
dotnet test Client.Tests/Client.Tests.csproj

# E2E tester (krever kjørende app)
dotnet test Client.Tests.Playwright/Client.Tests.Playwright.csproj
```

## Implementerte Funksjoner og Status

### ✅ Ferdigstilte Funksjoner:
1. ✅ **Testing infrastruktur** - 143 omfattende tester (enhet + E2E)
2. ✅ **Natural alfanumerisk sortering** - Håndterer "Uke 1" til "Uke 43"
3. ✅ **Tidsstempel sporing** - LastModified med lazy migration
4. ✅ **Smart liste-sortering** - Multi-level: IsDone → Dato → Navn
5. ✅ **Vare innsettings-rekkefølge** - Nye varer på toppen
6. ✅ **IsDone persistering** - Fikset checkbox binding og API endepunkt
7. ✅ **Navigasjonsforbedringer** - Hyppige Lister i Admin dropdown

### 🔄 Kjente Begrensninger:
1. **Ingen Shelf API endepunkt** - Hylle-administrasjon skjer gjennom Shop entiteter
2. **Firestore ytelse** - Kunne dratt nytte av caching-strategier
3. **Client-side sortering** - Kjerne sorteringslogikk kjører i Blazor, ikke API (by design)

### 📋 Fremtidige Vurderinger:
- Legge til visuell separator mellom aktive og ferdige lister
- Implementere data caching for ofte brukte butikker/hyller
- Vurdere backend sortering API for store datasett
- Legge til bulk-operasjoner for liste-administrasjon

## Deployment og Konfiguration

### Azure Static Web Apps
- **Client**: Deployeres som statisk web app
- **Api**: Deployeres som Azure Functions
- **Database**: Google Cloud Firestore (ekstern tjeneste)

### Konfigurasjon
- **API_Prefix**: Konfigureres i appsettings for client
- **Firestore**: Konfigureres via Google Cloud credentials
- **CORS**: Håndteres automatisk av Azure Static Web Apps