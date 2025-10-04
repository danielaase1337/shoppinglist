# Client.Tests.Playwright

Dette prosjektet inneholder End-to-End (E2E) tester for Blazor WebAssembly-klienten ved hjelp av Microsoft Playwright.

## Oversikt

Playwright-testene fokuserer på:
- **Sorteringslogikk**: Testing av den butikk-spesifikke sorteringen av handleliste-elementer
- **Syncfusion-komponenter**: Interaksjon med dropdown og autocomplete komponenter
- **Navigasjon**: Verifisering av at alle sider lastes korrekt
- **End-to-End brukerflyt**: Fullstendige brukerscenarioer

## Teststruktur

```
Client.Tests.Playwright/
├── Client.Tests.Playwright.csproj
├── GlobalUsings.cs
├── PlaywrightFixture.cs
├── Tests/
│   ├── ShoppingListSortingTests.cs    # Hovedfokus: sorteringslogikk
│   └── NavigationTests.cs             # Navigasjon og sideinnlasting
└── README.md
```

## Kjøring av Tester

### Første gang oppsett
```powershell
# Naviger til test-mappen
cd Client.Tests.Playwright

# Installer Playwright browsers
dotnet run --project . -- install

# Eller manuelt
npx playwright install
```

### Kjøre testene
```powershell
# Kjør alle Playwright-tester
dotnet test

# Kjør med detaljer
dotnet test --logger "console;verbosity=detailed"

# Kjør spesifikk test-klasse
dotnet test --filter "ShoppingListSortingTests"
```

## Viktige Testscenarioer

### 1. Sorteringslogikk (`ShoppingListSortingTests`)
- **Butikk-valg og sortering**: Verifiserer at handleliste-elementer sorteres korrekt basert på valgt butikks hylle-rekkefølge
- **Syncfusion-interaksjon**: Tester dropdown og autocomplete komponenter
- **Standard tilstand**: Verifiserer oppførsel uten butikk-valg

### 2. Navigasjon (`NavigationTests`)
- **Sideinnlasting**: Alle hovedsider lastes uten feil
- **Innhold-verifisering**: Sider viser forventet innhold
- **Error-håndtering**: Ingen 404 eller kritiske feil

## Konfigurasjon

### Base URL
Testene forventer at Blazor WebAssembly appen kjører på:
```
https://localhost:7077
```

Endre `BaseUrl` konstanten i test-filene hvis appen kjører på annen port.

### Browser-innstillinger
I `PlaywrightFixture.cs`:
```csharp
Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = true,     // Sett til false for debugging
    SlowMo = 100        // Slow down for bedre synlighet
});
```

## Debugging

### Vis browser under testing
Endre i `PlaywrightFixture.cs`:
```csharp
Headless = false  // Viser browser-vindu
SlowMo = 500     // Langsommere for å se hva som skjer
```

### Test-spesifikke selektorer
Testene bruker en kombinasjon av:
- `data-testid` attributter (anbefalt)
- CSS-selektorer for Syncfusion komponenter
- Fallback til generiske selektorer

## Syncfusion-spesifikke Testing

### Dropdown komponenter
```csharp
var shopDropdown = page.Locator("[data-testid='shop-dropdown']").Or(
    page.Locator("div.e-dropdownlist")).First;
```

### AutoComplete komponenter
```csharp
var autoComplete = page.Locator("input.e-input").First;
await autoComplete.FillAsync("searchterm");
```

## Avhengigheter

- **Microsoft.Playwright.MSTest**: Playwright test runner
- **xunit**: Test framework
- **Client-prosjekt**: Referanse til Blazor WebAssembly app
- **Shared-prosjekt**: Felles modeller og typer

## Kjente Begrensninger

1. **Krever kjørende app**: Blazor WebAssembly appen må kjøre lokalt på port 7077
2. **Syncfusion lisens**: Kan vise lisens-advarsler i browser console
3. **API-avhengighet**: Noen tester kan kreve at API-et også kjører

## Fremtidige Forbedringer

- Legg til data-testid attributter i Blazor komponenter for bedre test-stabilitet
- Implementer Page Object Model for gjenbrukbare test-elementer
- Legg til performance-testing med Playwright
- Automatiser app-oppstart som del av test-suite