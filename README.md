# RimVerse – Persistent Multiplayer pro RimWorld

**RimVerse** je mod pro RimWorld (1.5+/1.6+), který přidává persistent multiplayer svět s obchodováním, návštěvami a real-time joint sessions mezi hráči.

## Architektura

```
Cloud Server (Fly.io)          RimWorld Client (Harmony Mod)
┌─────────────────────┐        ┌─────────────────────────┐
│ ASP.NET (.NET 10)   │◄──────►│ C# (.NET 4.7.2)         │
│ SignalR WebSocket   │  WSS   │ WebSocket + REST         │
│ PostgreSQL (Neon)   │        │ Harmony Patches          │
│ EF Core 10          │        │ IMGUI (RimWorld native)  │
└─────────────────────┘        └─────────────────────────┘
```

## Struktura projektu

```
src/
├── RimVerse.Shared/       # Sdílené modely a protokol (netstandard2.0)
├── RimVerse.Server/       # Dedicated server (ASP.NET, .NET 10)
└── RimVerse.Client/       # RimWorld mod (Harmony, .NET 4.7.2)

infra/
├── Dockerfile             # Multi-stage build pro server
├── fly.toml               # Fly.io deployment
└── docker-compose.yml     # Lokální dev environment
```

## Funkce

### MVP (Fáze 1-2)
- [x] Persistent server s PostgreSQL
- [x] JWT autentizace (registrace + login)
- [x] Sdílený svět – zakládání osad s claim systémem
- [x] Real-time chat (SignalR)
- [x] Escrow trade + kontrakty
- [x] Parcel delivery systém (asynchronní zásilky)
- [x] Modpack hash validace
- [x] Bug report systém (ZIP export + clipboard)
- [x] Audit log

### Fáze 3 (Joint Sessions)
- [x] Session orchestrátor (request → accept → start → end)
- [x] Lockstep engine s desync detekcí
- [x] Determinism Harmony patches (RNG, DateTime)
- [x] JointSessionManager na klientovi

### Plánováno
- [ ] Global Market / Auction House
- [ ] Guilds + sdílené sklady
- [ ] Builder permissions (blueprinty)
- [ ] Steam autentizace
- [ ] Replay systém

## Jak spustit server lokálně

### Prerekvizity
- .NET 10 SDK
- PostgreSQL (nebo Docker)

### Pomocí Docker Compose
```bash
cd infra
docker-compose up
```
Server poběží na `http://localhost:8080`.

### Manuálně
```bash
# 1. Spusť PostgreSQL a vytvoř DB "rimverse"

# 2. Nastav connection string
export ConnectionStrings__Database="Host=localhost;Database=rimverse;Username=postgres;Password=postgres"

# 3. Spusť server
cd src/RimVerse.Server
dotnet run
```

## Jak deployovat na cloud (Fly.io)

```bash
# 1. Nainstaluj Fly CLI
# https://fly.io/docs/getting-started/installing-flyctl/

# 2. Login
fly auth login

# 3. Launch (první deploy)
cd infra
fly launch

# 4. Nastav secrets
fly secrets set ConnectionStrings__Database="postgresql://..."
fly secrets set Jwt__Secret="tvuj-super-tajny-klic-min-32-znaku!"

# 5. Deploy
fly deploy
```

### Doporučené cloud služby (zdarma pro MVP)

| Služba | Provider | Free tier |
|--------|----------|-----------|
| Server | [Fly.io](https://fly.io) | 3 shared VMs, 256MB RAM |
| Database | [Neon](https://neon.tech) | 0.5GB PostgreSQL |
| CI/CD | GitHub Actions | Zdarma pro public repo |

## Jak nastavit klientský mod

### Prerekvizity
- RimWorld (Steam nebo GOG)
- Harmony mod

### Build
```bash
# Nastav cestu k RimWorld instalaci
set RimWorldPath=C:\Program Files (x86)\Steam\steamapps\common\RimWorld

# Build
cd src/RimVerse.Client
dotnet build
```

### Instalace
1. Zkopíruj složku `RimVerse.Client/About/` a `Assemblies/` do `RimWorld/Mods/RimVerse/`
2. Aktivuj mod v RimWorld
3. V nastavení modu zadej URL serveru, username a heslo
4. Klikni "Connect"

## API Endpointy

| Endpoint | Metoda | Popis |
|----------|--------|-------|
| `/health` | GET | Health check |
| `/api/auth/register` | POST | Registrace |
| `/api/auth/login` | POST | Login |
| `/api/world` | GET | Info o světě |
| `/api/world/settlements` | GET/POST | Osady |
| `/api/world/players` | GET | Seznam hráčů |
| `/api/trade/contracts` | GET/POST | Kontrakty |
| `/api/trade/contracts/{id}/accept` | POST | Přijmout kontrakt |
| `/api/trade/contracts/{id}/reject` | POST | Odmítnout kontrakt |
| `/api/session/request` | POST | Vyžádat joint session |
| `/api/session/{id}/accept` | POST | Přijmout session |
| `/api/modpack/manifest` | GET | Modpack manifest |
| `/api/modpack/validate` | POST | Validace modpacku |
| `/api/report` | POST | Bug report |

## WebSocket Hub

Endpoint: `wss://server/hubs/game?access_token=JWT`

### Server → Client events
- `PlayerJoined` – hráč se připojil
- `PlayerLeft` – hráč odešel
- `ReceiveChatMessage` – chatová zpráva
- `WorldClockSync` – synchronizace world clock
- `ParcelDelivered` – zásilka doručena
- `SessionInvite` – pozvánka do session
- `SessionStart` – session začíná

### Client → Server methods
- `SendChatMessage(channel, content)` – odeslat chat
- `RequestWorldSync()` – vyžádat sync
- `Heartbeat()` – keepalive

## Licence

MIT
