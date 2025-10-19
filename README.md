# 📱 Handleliste-applikasjon 

Smart handleliste-håndtering med butikk-spesifikk sortering og template-funksjonalitet.

## 🌐 Live Application

- **Production URL**: [Your Azure Static Web App URL here]
- **Development**: `https://localhost:7073` (Client) + `http://localhost:7072` (API)

## � Applikasjonsoversikt

Dette er en **3-lags Blazor WebAssembly applikasjon** for smart handleliste-håndtering med butikk-spesifikk sortering og template-funksjonalitet.

### 🏗️ Teknisk Arkitektur
- **Frontend**: Blazor WebAssembly (.NET 8.0) med Syncfusion UI-komponenter
- **Backend**: Azure Functions v4 (.NET 8.0) 
- **Database**: Google Cloud Firestore (produksjon) / In-Memory (utvikling)
- **Deployment**: Azure Static Web Apps med GitHub Actions

## 🛒 Hovedfunksjonalitet

### 1. **Smart Handlelister**
- ✅ Opprett og administrer multiple handlelister
- ✅ Legg til varer med mengde og enhet
- ✅ Marker varer som fullført under shopping
- ✅ Rediger og slett lister og varer
- ✅ Persistent lagring av alle endringer

### 2. **Template/Mal System** 🆕
- ✅ **Lag maler**: Opprett standard handlelister som kan gjenbrukes
- ✅ **Separat visning**: Maler vises i egen fane, ikke blandet med aktive lister
- ✅ **Kopier funksjon**: Lag nye handlelister basert på maler med ett klikk
- ✅ **Automatisk naming**: Kopierte lister får format "Malnavn - DD/MM"
- ✅ **Reset status**: Alle varer i kopiert liste starter som ikke-fullført

### 3. **Butikk-spesifikk Sortering** 🏪
Applikasjonens **unike feature** - sortering basert på fysisk butikklayout:

#### Hvordan det fungerer:
1. **Butikker** inneholder **hyller** med sorteringsindeks
2. **Hyller** inneholder **varekategorier** 
3. **Varer** tilhører **kategorier**
4. **Handlelister** sorteres automatisk etter butikkens hyllerekkefølge

#### Praktisk nytte:
- 🚶‍♂️ **Effektiv shopping**: Følg naturlig gang gjennom butikken
- 🛒 **Mindre gåing**: Unngå å gå frem og tilbake
- ⏱️ **Tidsbesparelse**: Raskere handleturer
- 📱 **Butikk-tilpasset**: Samme app, forskjellige butikker

### 4. **Butikk- og Varehantering**
- ✅ Administrer multiple butikker
- ✅ Definer hyllestruktur per butikk
- ✅ Kategoriser varer (Meieri, Bakeri, Kjøtt, Frukt, etc.)
- ✅ Vareregister med enheter (Stk, Kg, Liter)

## 🎯 Bruksscenarioer

### **Ukentlig Rutine**
1. Lag en **"Standard ukeshandel"** mal med faste varer
2. Hver uke: **kopier malen** til ny aktiv liste
3. **Tilpass listen** med spesielle behov for uken
4. **Velg butikk** for optimal sortering
5. **Shop systematisk** etter app-rekkefølgen

### **Spesielle Anledninger**
- Lag maler for "Helgefest", "Grillmiddag", "Bakedag"
- Kopier og tilpass etter behov
- Alle maler lagres permanent for gjenbruk

### **Familie/Husholdning**
- Flere kan legge til varer i samme liste
- Status synkroniseres automatisk
- Historikk over fullførte lister

## 🚀 Unike Konkurransefortrinn

1. **Butikk-optimalisert sortering** - ingen andre handleliste-apper gjør dette
2. **Template-system** - gjenbruk av standard lister
3. **Teknisk robusthet** - enterprise-grade arkitektur
4. **Norsk-tilpasset** - laget for norske butikkjeder og vaner
5. **Utviderbar** - enkel å legge til nye butikker og features

---

## 🔧 Development & Technical Information

### **Performance & UX**
- ⚡ **Cache-system**: Instant loading av lister
- 📶 **Offline-ready**: Fungerer uten internett (med cache)
- 🔄 **Real-time sync**: Endringer synkroniseres umiddelbart
- 📱 **Responsive**: Fungerer på mobil og desktop

### **Data Management**
- 🗄️ **Dual repository**: Memory (dev) / Firestore (prod)
- 🔄 **AutoMapper**: Automatisk mapping mellom modeller
- 🛡️ **Type safety**: Sterk typing i hele stacken
- 📊 **Structured data**: Normalisert database-design

### **Development & Deployment**
- 🚀 **CI/CD**: Automatisk deployment via GitHub Actions
- 🔧 **Hot reload**: Rask utvikling med live reload
- 🧪 **Testable**: Abstrahert repository pattern
- 📦 **Modular**: Klar separasjon av bekymringer

## 🎨 Brukergrensesnitt

### **Moderne Design**
- 🎨 **Syncfusion UI**: Profesjonelle komponenter
- ✨ **Smooth UX**: Intuitive interaksjoner
- 📋 **Todo-style**: Kjent checkbox-pattern
- 🏷️ **Color coding**: Visuell status-indikering

### **Navigation**
- 📑 **Tab-basert**: Lett bytte mellom lister og maler
- 🔍 **Search & filter**: Finn raskt det du leter etter
- ➕ **Quick add**: Rask registrering av nye varer
- ✏️ **Inline editing**: Rediger direkte i listen

## 📈 Mulige Utvidelser

### **Kort sikt**
- 🏪 **Flere butikker**: Rema, Coop, Meny, etc.
- 📊 **Statistikk**: Mest kjøpte varer, utgifter
- 🎯 **Smart forslag**: AI-baserte vareforslag

### **Lang sikt**
- 🛒 **Deling**: Del lister mellom familie/venner
- 💰 **Prissammenligning**: Beste priser på tvers av butikker
- 📱 **Native app**: iOS/Android versjon
- 🤖 **Voice input**: "Legg til melk i handlelisten"

---

## 💻 Development Setup

### Template Structure

- **Client**: The Blazor WebAssembly sample application
- **Api**: A C# Azure Functions API, which the Blazor application will call
- **Shared**: A C# class library with a shared data model between the Blazor and Functions application

### Visual Studio Code with Azure Static Web Apps CLI (Optional)

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

## 🔐 Google Firestore Configuration

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

## 🚀 Deploy to Azure Static Web Apps

This application can be deployed to [Azure Static Web Apps](https://docs.microsoft.com/azure/static-web-apps), to learn how, check out [our quickstart guide](https://aka.ms/blazor-swa/quickstart).

---

*Opprettet: Oktober 2025*  
*Teknologi: Blazor WebAssembly + Azure Functions + Firestore*
