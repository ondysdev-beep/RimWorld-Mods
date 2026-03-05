# RimVerse – Kompletní průvodce krok za krokem

---

## ČÁST 1: Nastavení cloud databáze (Neon – PostgreSQL zdarma)

### Krok 1: Vytvoř si účet na Neon
1. Otevři prohlížeč a jdi na **https://neon.tech**
2. Klikni na **"Sign Up"** (vpravo nahoře)
3. Přihlaš se přes **GitHub** (nejjednodušší) nebo přes email
4. Po přihlášení se zobrazí dashboard

### Krok 2: Vytvoř nový projekt (databázi)
1. Na dashboardu klikni **"New Project"**
2. Vyplň:
   - **Name:** `rimverse`
   - **Region:** `EU (Frankfurt)` ← nejbližší k ČR
   - **Postgres Version:** nech výchozí (16)
3. Klikni **"Create Project"**
4. **DŮLEŽITÉ:** Zobrazí se ti **Connection String** – vypadá takto:
   ```
   postgresql://neondb_owner:abc123xyz@ep-cool-name-123456.eu-central-1.aws.neon.tech/neondb?sslmode=require
   ```
5. **Zkopíruj si tento connection string a ulož ho** (budeš ho potřebovat za chvíli)

---

## ČÁST 2: Nastavení serveru na Fly.io (zdarma)

### Krok 3: Vytvoř si účet na Fly.io
1. Jdi na **https://fly.io**
2. Klikni **"Sign Up"**
3. Přihlaš se přes **GitHub**
4. **Poznámka:** Fly.io vyžaduje platební kartu i pro free tier (strhne $0, jen ověření). Pokud nechceš dávat kartu, přeskoč na ČÁST 2B (Railway alternativa)

### Krok 4: Nainstaluj Fly CLI
1. Otevři **PowerShell** (jako administrátor)
2. Spusť:
   ```powershell
   irm https://fly.io/install.ps1 | iex
   ```
3. Zavři a znovu otevři PowerShell
4. Ověř instalaci:
   ```powershell
   fly version
   ```
   Mělo by se zobrazit číslo verze

### Krok 5: Přihlaš se přes CLI
1. V PowerShellu spusť:
   ```powershell
   fly auth login
   ```
2. Otevře se prohlížeč → přihlaš se
3. V terminálu se zobrazí "successfully logged in"

### Krok 6: Deployni server
1. V PowerShellu přejdi do složky projektu:
   ```powershell
   cd C:\Users\neopr\Desktop\RimVerse
   ```
2. Spusť launch (první deploy):
   ```powershell
   fly launch --config infra/fly.toml --dockerfile infra/Dockerfile
   ```
3. Fly se tě zeptá:
   - **App name:** napiš `rimverse-server` (nebo jiný unikátní název)
   - **Region:** vyber `fra` (Frankfurt)
   - **Database:** vyber **No** (používáme Neon)
   - **Redis:** vyber **No**
4. Počkej než proběhne build a deploy (2-5 minut)

### Krok 7: Nastav secrets (hesla a connection string)
1. Nastav databázový connection string (ten z kroku 2):
   ```powershell
   fly secrets set ConnectionStrings__Database="TVŮJ_NEON_CONNECTION_STRING" --app rimverse-server
   ```
   Například:
   ```powershell
   fly secrets set ConnectionStrings__Database="Host=ep-cool-name-123456.eu-central-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=abc123xyz;SSL Mode=Require" --app rimverse-server
   ```

2. Nastav JWT secret (vymysli si silné heslo, min. 32 znaků):
   ```powershell
   fly secrets set Jwt__Secret="TvojeSuperTajneHesloKtereMaNejmene32Znaku!" --app rimverse-server
   ```

### Krok 8: Ověř že server běží
1. Otevři prohlížeč a jdi na:
   ```
   https://rimverse-server.fly.dev/health
   ```
   (nahraď `rimverse-server` svým názvem aplikace)
2. Měl bys vidět JSON:
   ```json
   {"status":"healthy","service":"RimVerse Server","version":"0.1.0",...}
   ```
3. **HOTOVO!** Server běží v cloudu! 🎉

---

## ČÁST 2B: Alternativa – Railway (pokud nechceš dávat kartu na Fly.io)

### Krok 3B: Railway
1. Jdi na **https://railway.app**
2. Klikni **"Login"** → přihlaš se přes **GitHub**
3. Klikni **"New Project"** → **"Deploy from GitHub repo"**
4. Vyber repo `ondysdev-beep/RimWorld-Mods`
5. Railway automaticky detekuje Dockerfile
6. V nastavení projektu:
   - Klikni na service → **Variables**
   - Přidej:
     - `ConnectionStrings__Database` = tvůj Neon connection string
     - `Jwt__Secret` = tvoje tajné heslo
     - `ASPNETCORE_ENVIRONMENT` = `Production`
   - V **Settings** → **Networking** → **Generate Domain**
7. Počkej na deploy

---

## ČÁST 3: Nastavení RimWorld klientského modu

### Krok 9: Zjisti cestu k RimWorld
1. Otevři **Steam**
2. V knihovně klikni pravým na **RimWorld** → **Vlastnosti** → **Nainstalované soubory** → **Procházet**
3. Zkopíruj cestu, například:
   ```
   C:\Program Files (x86)\Steam\steamapps\common\RimWorld
   ```

### Krok 10: Nastav cestu v projektu
1. Otevři soubor `C:\Users\neopr\Desktop\RimVerse\src\RimVerse.Client\RimVerse.Client.csproj`
2. Najdi řádek:
   ```xml
   <RimWorldPath Condition="'$(RimWorldPath)' == ''">C:\Program Files (x86)\Steam\steamapps\common\RimWorld</RimWorldPath>
   ```
3. Změň cestu na **tvoji skutečnou cestu k RimWorld**

### Krok 11: Buildni klientský mod
1. Otevři PowerShell
2. Spusť:
   ```powershell
   cd C:\Users\neopr\Desktop\RimVerse\src\RimVerse.Client
   dotnet build
   ```
3. Pokud se build nezdaří kvůli chybějícím RimWorld DLL:
   - Ověř, že cesta v kroku 10 je správná
   - Ověř, že ve složce `RimWorldWin64_Data\Managed\` existují soubory jako `Assembly-CSharp.dll`

### Krok 12: Nainstaluj mod do RimWorld
1. Ve složce RimWorld najdi `Mods/` (např. `C:\...\RimWorld\Mods\`)
2. Vytvoř novou složku: `Mods\RimVerse\`
3. Do ní zkopíruj:
   ```
   Z: C:\Users\neopr\Desktop\RimVerse\src\RimVerse.Client\About\
   DO: C:\...\RimWorld\Mods\RimVerse\About\

   Z: C:\Users\neopr\Desktop\RimVerse\Assemblies\
   DO: C:\...\RimWorld\Mods\RimVerse\Assemblies\
   ```
4. Výsledná struktura ve složce `Mods\RimVerse\`:
   ```
   RimVerse/
   ├── About/
   │   └── About.xml
   └── Assemblies/
       ├── RimVerse.dll
       ├── RimVerse.Shared.dll
       ├── Newtonsoft.Json.dll
       └── WebSocketSharp.dll
   ```

### Krok 13: Aktivuj mod v RimWorld
1. Spusť **RimWorld**
2. Na hlavní obrazovce klikni **"Mods"**
3. V seznamu najdi **"RimVerse"** a přetáhni ho do aktivních modů
4. **DŮLEŽITÉ:** RimVerse musí být pod **Harmony** (to je závislost)
5. Klikni **"Apply"** a restartuj hru

### Krok 14: Připoj se k serveru
1. V RimWorld jdi do **Options** → **Mod Settings** → **RimVerse**
2. Vyplň:
   - **Server URL:** `https://rimverse-server.fly.dev` (tvoje URL z kroku 8)
   - **Username:** vymysli si přezdívku
   - **Password:** vymysli si heslo (min. 6 znaků)
3. Klikni **"Connect to Server"**
4. Při prvním připojení se automaticky zaregistruješ
5. **Hotovo!** Jsi připojený k serveru! 🎉

---

## ČÁST 4: Jak to používat ve hře

### Hlavní okno RimVerse
- Po načtení hry (savegame) uvidíš vpravo nahoře tlačítko **"RimVerse"**
- Kliknutím otevřeš hlavní okno s taby:
  - **Overview** – přehled, stav připojení
  - **Players** – online hráči
  - **Chat** – globální a trade chat
  - **Trade** – obchody a kontrakty
  - **Settings** – nastavení serveru

### Chat
- Otevři tab **Chat**
- Napiš zprávu a klikni **Send** (nebo Enter)
- Přepínej mezi kanály **Global** a **Trade**

### Založení osady na sdíleném světě
- (Bude doplněno v další verzi – zatím závisí na serverovém API)

### Obchodování
1. Otevři tab **Trade**
2. Klikni **"Create New Trade Offer"**
3. Vyber hráče, nabídni itemy, poptávej itemy
4. Druhý hráč dostane notifikaci a může přijmout/odmítnout
5. Po přijetí se itemy zamknou v escrow a doručí se jako zásilky

### Bug Report
1. V hlavním okně RimVerse klikni **Settings**
2. Klikni **"Report Issue"** (nebo otevři z mod settings)
3. Vyber kategorii, napiš popis
4. Klikni **"Export ZIP"** – uloží se na plochu
5. Nebo **"Copy to Clipboard"** – a vlož do GitHub Issues

---

## ČÁST 5: Správa serveru

### Monitoring
```powershell
# Zobraz logy serveru
fly logs --app rimverse-server

# Status serveru
fly status --app rimverse-server

# Škálování (pokud potřebuješ víc výkonu)
fly scale memory 512 --app rimverse-server
```

### Aktualizace serveru
```powershell
cd C:\Users\neopr\Desktop\RimVerse
fly deploy --config infra/fly.toml --dockerfile infra/Dockerfile
```

### Záloha databáze
- Jdi na **https://console.neon.tech**
- Vyber projekt `rimverse`
- Klikni **"Branches"** → **"Create Branch"** (to je záloha)

---

## ČÁST 6: Pozvání dalších hráčů

1. Pošli jim odkaz na server: `https://rimverse-server.fly.dev`
2. Oni musí:
   - Nainstalovat mod (Krok 12-13)
   - V nastavení zadat URL serveru, zvolit si username a heslo
   - Kliknout Connect
3. Hotovo – jsou na stejném serveru!

---

## Rychlý přehled důležitých URL a cest

| Co | Kde |
|----|-----|
| **Server URL** | `https://rimverse-server.fly.dev` (tvoje URL) |
| **Health check** | `https://rimverse-server.fly.dev/health` |
| **Neon dashboard** | `https://console.neon.tech` |
| **Fly.io dashboard** | `https://fly.io/dashboard` |
| **Projekt** | `C:\Users\neopr\Desktop\RimVerse\` |
| **Client mod source** | `C:\...\RimVerse\src\RimVerse.Client\` |
| **Server source** | `C:\...\RimVerse\src\RimVerse.Server\` |
| **RimWorld Mods** | `C:\...\RimWorld\Mods\RimVerse\` |
| **GitHub repo** | `https://github.com/ondysdev-beep/RimWorld-Mods` |
