# Feature: Hyppige Handlelister (Frequent Lists)

## 📋 Funksjonsbeskrivelse

Funksjon for å håndtere varer som handles ofte - et system for å raskt legge til standard varer i handlelister.

## 👤 Use Case

**Som en "handler"** vil jeg kunne legge til et utvalg av varer, med et gitt antall, til en gitt handleliste ved å velge fra en forhåndsdefinert liste.

## 📖 Detaljert Beskrivelse

### Problemstilling
Når vi bruker handlelisten i ukehandlingen, er det en del varer som alltid må være med. Dette er ting som vi stort sett trenger det samme antallet av hver uke.

### Eksempler på hyppige varer:
- **Dagligvarer**: Yoghurt i pose, melk, brød
- **Kjøtt og fisk**: Kyllingfilet, fiskepinner  
- **Grønnsaker**: Bananer, gulrøtter
- **Husholdning**: Toalettpapir, oppvaskmiddel

### Konsept
Disse varetypene burde bli lagt til i en liste som jeg kan velge å legge til en aktiv handleliste. Dette skal **ikke** være "maler" (templates), men et sett med varer som kan legges til en gitt liste.

### Bruksscenarioer

#### 1. **Standard Ukehandel**
- Velg "Ukesvarer" listen
- Legg til alle varer (melk, brød, yoghurt, etc.) i én operasjon
- Tilpass mengder etter behov for denne uken

#### 2. **Spesielle Kampanjer - "Trippeltrumf" Uker**
- Velg "Trippeltrumf" listen  
- Legg til ekstra varer som fisk, toalettpapir, frysevarer
- Større mengder for å utnytte kampanjer

#### 3. **Helger og Høytider**
- Velg "Helgevarer" listen
- Legg til spesielle varer for helger (kaker, drikke, snacks)

## ⚙️ Funksjonelle Krav

### Administrasjon
- **Egen side**: "Hyppige lister" side for håndtering av slike lister
- **CRUD operasjoner**: Opprett, rediger, slett hyppige lister
- **Vareadministrasjon**: Legg til/fjern varer fra hyppige lister som vanlige handlelister

### Bruk
- **Listevalg**: Dropdown/meny for å velge hvilken "hyppig liste" som skal legges til
- **Mengdekontroll**: Mulighet til å justere mengder før import
- **Bulkimport**: Legg til alle varer fra hyppig liste i én operasjon

## 🔧 Tekniske Krav

### API Endepunkter
Lage egne endepunkter for å hente og lagre "hyppige lister":
- `GET /api/frequentlists` - Hent alle hyppige lister
- `GET /api/frequentlist/{id}` - Hent spesifikk hyppig liste
- `POST /api/frequentlists` - Opprett ny hyppig liste
- `PUT /api/frequentlists` - Oppdater hyppig liste
- `DELETE /api/frequentlist/{id}` - Slett hyppig liste

### Datamodell
Eget datamodell for disse listene - `FrequentShoppingList`:
```csharp
public class FrequentShoppingList : EntityBase 
{
    public string Description { get; set; }        // "Standard ukehandel", "Trippeltrumf", etc.
    public ICollection<FrequentShoppingItem> Items { get; set; }
}

public class FrequentShoppingItem 
{
    public ShopItem Varen { get; set; }
    public int StandardMengde { get; set; }        // Standard mengde som foreslås
}
```

### Import Funksjonalitet
- **Velg liste**: UI for å velge hvilken hyppig liste som skal importeres
- **Forhåndsvisning**: Vis varer og mengder før import
- **Import til aktiv liste**: Legg til alle varer fra hyppig liste til gjeldende handleliste

## ✅ Akseptansekritérier

### Must Have
- [ ] **Egen administrasjonsside** for "Hyppige lister"
- [ ] **Egne API endepunkter** for FrequentShoppingList CRUD
- [ ] **Eget datamodell** (FrequentShoppingList) 
- [ ] **Import funksjonalitet** - velg hyppig liste og legg alle varer til aktiv handleliste
- [ ] **Redigerbar** - kan administrere varer i hyppige lister som vanlige handlelister

### Should Have  
- [ ] **Mengdejustering** før import
- [ ] **Forhåndsvisning** av varer som skal legges til
- [ ] **Flere forhåndsdefinerte lister** (Ukehandel, Trippeltrumf, Helger)

### Could Have
- [ ] **Kategorisering** av hyppige lister
- [ ] **Bruksstatistikk** - hvilke lister brukes mest
- [ ] **Smart forslag** basert på tidligere handlevaner

## 🔄 Forskjell fra Templates

| Funksjon | Templates (Maler) | Hyppige Lister |
|----------|-------------------|----------------|
| **Formål** | Kopiere hele handlelister | Legge til standardvarer |
| **Bruk** | Lag ny liste fra mal | Utvid eksisterende liste |
| **Innhold** | Komplette handlelister | Spesifikke varekategorier |
| **Frekvens** | Ukentlig/månedlig | Daglig/hver handel |

## 📅 Implementeringsrekkefølge

1. **Datamodell** - Opprett FrequentShoppingList og FrequentShoppingItem
2. **API** - Implementer CRUD endepunkter  
3. **Repository** - Memory og Firestore implementasjoner
4. **UI - Admin** - Side for å administrere hyppige lister
5. **UI - Import** - Funksjon for å importere fra hyppige lister
6. **Testing** - Verifiser at alle akseptansekritérier er oppfylt

---

*Feature Request - Oktober 2025*  
*Status: Spesifikasjon ferdig - Klar for implementering*