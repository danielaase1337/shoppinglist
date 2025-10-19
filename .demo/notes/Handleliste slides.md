---

Theme: default 
---

# Handleliste-applikasjon - Funksjonalitet og Features
---
layout: default
---

---
layout: intro
---
# Slide 1 Title

📱 Applikasjonsoversikt

Dette er en **3-lags Blazor WebAssembly applikasjon** for smart handleliste-håndtering med butikk-spesifikk sortering og template-funksjonalitet.

### 🏗️ Teknisk Arkitektur
- **Frontend**: Blazor WebAssembly (.NET 8.0) med Syncfusion UI-komponenter
- **Backend**: Azure Functions v4 (.NET 8.0) 
- **Database**: Google Cloud Firestore (produksjon) / In-Memory (utvikling)
- **Deployment**: Azure Static Web Apps med GitHub Actions

---

---
# Slide 2 Title
 🛒 Hovedfunksjonalitet

 1. **Smart Handlelister**
- ✅ Opprett og administrer multiple handlelister
- ✅ Legg til varer med mengde og enhet
- ✅ Marker varer som fullført under shopping
- ✅ Rediger og slett lister og varer
- ✅ Persistent lagring av alle endringer

 2. **Template/Mal System** 🆕
- ✅ **Lag maler**: Opprett standard handlelister som kan gjenbrukes
- ✅ **Separat visning**: Maler vises i egen fane, ikke blandet med aktive lister
- ✅ **Kopier funksjon**: Lag nye handlelister basert på maler med ett klikk
- ✅ **Automatisk naming**: Kopierte lister får format "Malnavn - DD/MM"
- ✅ **Reset status**: Alle varer i kopiert liste starter som ikke-fullført

---

# Slide 3 Title 

**Butikk-spesifikk Sortering** 🏪

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

# Slide 4. **Butikk- og Varehantering**
- ✅ Administrer multiple butikker
- ✅ Definer hyllestruktur per butikk
- ✅ Kategoriser varer (Meieri, Bakeri, Kjøtt, Frukt, etc.)
- ✅ Vareregister med enheter (Stk, Kg, Liter)

---

# slide 5 🎯 Bruksscenarioer

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

---


# slide 8 🔧 Tekniske Features

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

## 🚀 Unike Konkurransefortrinn

1. **Butikk-optimalisert sortering** - ingen andre handleliste-apper gjør dette
2. **Template-system** - gjenbruk av standard lister
3. **Teknisk robusthet** - enterprise-grade arkitektur
4. **Norsk-tilpasset** - laget for norske butikkjeder og vaner
5. **Utviderbar** - enkel å legge til nye butikker og features

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

*Opprettet: Oktober 2025*  
*Teknologi: Blazor WebAssembly + Azure Functions + Firestore*  
*Utvikler: [Din info her]*