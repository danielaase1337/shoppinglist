# Client.Tests.Playwright

Dette prosjektet inneholder End-to-End (E2E) tester for Blazor WebAssembly-klienten ved hjelp av Microsoft Playwright.

## Oversikt

Playwright-testene fokuserer på:
- **Sorteringslogikk**: Testing av den butikk-spesifikke sorteringen av handleliste-elementer
- **Syncfusion-komponenter**: Interaksjon med dropdown og autocomplete komponenter
- **Navigasjon**: Verifisering av at alle sider lastes korrekt
- **End-to-End brukerflyt**: Fullstendige brukerscenarioer

## ⚠️ VIKTIG: Start Både Client OG API Før Testing!

**Testene vil feile hvis ikke begge applikasjonene kjører:**
1. **Azure Functions API** må kjøre på `http://localhost:7071`
2. **Blazor WebAssembly Client** må kjøre på `https://localhost:7072`

### Slik starter du begge:

#### Alternativ A: Visual Studio (Anbefalt)
1. Høyreklikk på solution → "Configure Startup Projects"
2. Velg "Multiple startup projects"
3. Sett både `Client` og `Api` til "Start"
4. Trykk OK og kjør med F5

#### Alternativ B: Kommandolinje

**Terminal 1 - Start API:**
```powershell
cd Api
func start --port 7071
```

**Terminal 2 - Start Client:**
```powershell
cd Client
dotnet run
```

Vent til begge er klare (se "Now listening on..." i terminalene).

## Teststruktur

```
Client.Tests.Playwright/
├── Client.Tests.Playwright.csproj
├── GlobalUsings.cs
├── PlaywrightFixture.cs
├── Tests/
│   ├── NavigationTests.cs             # Navigasjon og routing
│   ├── ShoppingListSortingTests.cs    # Sorteringslogikk
│   ├── DebugTests.cs                  # Console error diagnostikk
│   └── PageInspectionTests.cs         # API-kall og komponent-inspeksjon
└── README.md
```

## Kjøring av Tester

### Første gang oppsett
```powershell
# Naviger til test-mappen
cd Client.Tests.Playwright

# Installer Playwright browsers
pwsh bin/Debug/net9.0/playwright.ps1 install

# Eller via dotnet
dotnet build
playwright install
```

### Kjøre testene
```powershell
# VIKTIG: Start Client og API først! (se over)

# Kjør alle Playwright-tester
dotnet test

# Kjør med detaljer
dotnet test --logger "console;verbosity=detailed"

# Kjør spesifikk test-klasse
dotnet test --filter "NavigationTests"
dotnet test --filter "ShoppingListSortingTests"
dotnet test --filter "DebugTests"

# Kjør enkelttest
dotnet test --filter "HomePage_ShouldLoadSuccessfully"
```

## Test-kategorier

### 1. NavigationTests 🧭
Tester routing og sideinnlasting:
- ✅ `HomePage_ShouldLoadSuccessfully` - Forsiden laster
- ✅ `NavigationPages_ShouldLoadCorrectly` - Hovedsider lastes uten feil
- ✅ `ShoppingListMainPage_ShouldShowShoppingLists` - Viser handlelister
- ✅ `OneShoppingListPage_WithValidId_ShouldLoadCorrectly` - Enkeltliste laster med data
- ✅ `AdminPage_ShouldLoadDatabaseManagement` - Admin-siden laster
- ✅ `ManageMyShopsPage_WithValidId_ShouldLoad` - Butikkhåndtering laster

### 2. ShoppingListSortingTests 🔀
Tester butikk-spesifikk sortering:
- 🔀 `OneShoppingListPage_WhenShopSelected_ShouldSortItemsByShelfOrder` - Sortering ved butikk-valg
- 📋 `OneShoppingListPage_WhenNoShopSelected_ShouldShowUnsortedList` - Usortert liste
- 🎯 `OneShoppingListPage_SyncfusionComponents_ShouldBeInteractive` - Syncfusion-komponenter

### 3. DebugTests 🐛
Diagnostikk-tester som fanger console-feil:
- 🔍 `DebugAdminPage_CaptureConsoleErrors` - Admin-side console
- 🔍 `DebugShoppingListPage_CaptureConsoleErrors` - Shopping list console

### 4. PageInspectionTests 🔬
Inspeksjon for debugging:
- 📊 `InspectHomePage` - Inspiser forside-struktur
- 📊 `InspectOneShoppingListPage` - Syncfusion-komponent rendering
- 📊 `InspectPageRoutes` - Verifiser alle ruter
- 🌐 `InspectAPIConnection` - Overvåk API-kall og feil

## Testdata (DEBUG-modus)

Testene bruker data fra `MemoryGenericRepository`:

**Shopping Lists:**
- ID: `test-list-1`, Navn: "Ukeshandel" (Melk, Brød, Epler)
- ID: `test-list-2`, Navn: "Middag i kveld" (Kyllingfilet)

**Shops:**
- ID: `rema-1000`, Navn: "Rema 1000" (med 4 hyller)
- ID: `ica-maxi`, Navn: "ICA Maxi" (med 3 hyller)
- ID: `2`, Navn: "Kiwi lyngås"

**Items:**
- Melk, Brød, Epler, Kyllingfilet, Bananer, Yoghurt, Laks, Gulrøtter

## Konfigurasjon

### URLs
```csharp
Client (Blazor): https://localhost:7072
API (Functions):  http://localhost:7071  // VIKTIG: API må også kjøre!
```

**Endre i `PlaywrightFixture.cs`** hvis du bruker andre porter.

### Browser-innstillinger
I `PlaywrightFixture.cs`:
```csharp
Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = true,     // Sett til false for debugging
    SlowMo = 0          // Øk for langsommere kjøring (ms)
});
```

## Debugging og Feilsøking

### Problem: "Failed to fetch" feil
**Løsning:** API-et kjører ikke! Start Azure Functions API på port 7071.

### Problem: Tester timeout
**Løsning:** 
1. Sjekk at BÅDE Client og API kjører
2. Verifiser URL-er: Client på 7072, API på 7071
3. Se på console output i test-resultatene

### Problem: Syncfusion-komponenter ikke funnet
**Løsning:**
1. Øk wait-tider i testene (standard er 3-4 sekunder)
2. Sjekk Syncfusion-lisens i `Client/Program.cs`
3. Sett `Headless = false` og se hva som skjer i nettleseren

### Vis browser under testing
```csharp
// I PlaywrightFixture.cs
Headless = false,  // Viser browser-vindu
SlowMo = 500      // Langsommere for å se hva som skjer
```

### Ta screenshots
```csharp
await page.ScreenshotAsync(new PageScreenshotOptions 
{ 
    Path = "debug-screenshot.png" 
});
```

### Console logging
Testene fanger allerede console-output. Sjekk test-resultatene for:
- `[log]`, `[error]`, `[warning]` fra browser console
- API-kall detaljer (PageInspectionTests)

## Syncfusion-spesifikk Testing

### Dropdown komponenter
```csharp
var shopDropdown = page.Locator(".e-dropdownlist").First;
await shopDropdown.ClickAsync();

var firstOption = page.Locator(".e-list-item").First;
await firstOption.ClickAsync();
```

### AutoComplete komponenter
```csharp
var autoComplete = page.Locator(".e-autocomplete .e-input").First;
await autoComplete.ClickAsync();
await page.Keyboard.TypeAsync("Melk");
```

## CI/CD Integration

### GitHub Actions Eksempel
```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v3
  with:
    dotnet-version: '9.0.x'

- name: Install Azure Functions Core Tools
  run: npm install -g azure-functions-core-tools@4 --unsafe-perm true

- name: Install Playwright
  run: pwsh Client.Tests.Playwright/bin/Debug/net9.0/playwright.ps1 install --with-deps

- name: Start API
  run: |
    cd Api
    func start --port 7071 &
    sleep 10

- name: Start Client  
  run: |
    cd Client
    dotnet run &
    sleep 15

- name: Run Playwright Tests
  run: |
    cd Client.Tests.Playwright
    dotnet test --logger "trx"
```

## Avhengigheter

- **Microsoft.Playwright.MSTest**: Playwright for .NET
- **xunit**: Test framework
- **Client-prosjekt**: Blazor WebAssembly app
- **Shared-prosjekt**: Felles modeller
- **Azure Functions Core Tools**: For API-oppstart

## Kjente Begrensninger

1. ⚠️ **Krever kjørende app OG API**: Både Blazor og Functions må kjøre
2. 📝 **Syncfusion lisens**: Kan vise advarsler i console (OK for testing)
3. 🔐 **Ingen autentisering**: Testene kjører uten login
4. 💾 **In-memory data**: I DEBUG-modus brukes `MemoryGenericRepository`

## Fremtidige Forbedringer

- [ ] Legg til `data-testid` attributter for bedre selektorer
- [ ] Implementer Page Object Model pattern
- [ ] Automatiser app-oppstart i test-suite
- [ ] Legg til performance-metrics testing
- [ ] Test Firestore-integrasjon (produksjons-modus)
- [ ] Parallellisere test-kjøring
- [ ] Legg til visual regression testing

## Skriv Nye Tester

### Basic Test Template
```csharp
[Fact]
public async Task MyNewTest()
{
    var page = await _fixture.CreatePageAsync();
    
    try
    {
        await page.GotoAsync($"{BaseUrl}/your-route");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(2000); // Blazor init
        
        var content = await page.TextContentAsync("body");
        Assert.Contains("Expected Text", content);
    }
    finally
    {
        await page.CloseAsync(); // VIKTIG: Alltid close!
    }
}
```

### Tips
- ✅ Alltid `await page.CloseAsync()` i `finally`
- ⏱️ Bruk `WaitForLoadStateAsync(LoadState.NetworkIdle)`
- 🔄 Legg til ekstra `WaitForTimeoutAsync` for Syncfusion
- 🎯 Bruk spesifikke selektorer (`.e-dropdownlist`, `.e-autocomplete`)
- 📸 Ta screenshots ved feil for debugging