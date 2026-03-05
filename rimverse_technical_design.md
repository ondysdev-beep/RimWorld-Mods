# RimVerse – Technický návrh (Funkční architektura s cloud hostingem)

> Tento dokument rozšiřuje původní koncept o **konkrétní implementační detaily**, cloud infrastrukturu, DB schéma, síťový protokol a strukturu projektu tak, aby byl mod **reálně implementovatelný**.

---

## 1) Architektura – Přehled

```
┌─────────────────────────────────────────────────────────────────┐
│                        CLOUD (Fly.io / Railway)                 │
│                                                                 │
│  ┌──────────────────────┐    ┌─────────────────────────────┐    │
│  │  RimVerse Server     │    │  PostgreSQL (Supabase/Neon)  │    │
│  │  (.NET 8 / ASP.NET)  │◄──►│  nebo SQLite (embedded)     │    │
│  │                      │    └─────────────────────────────┘    │
│  │  - REST API           │                                      │
│  │  - WebSocket Gateway  │    ┌─────────────────────────────┐   │
│  │  - Session Orchestr.  │    │  Redis (Upstash – free tier)│   │
│  │  - World Clock        │◄──►│  - pub/sub, session state   │   │
│  │  - Contract Engine    │    │  - cache, rate limiting     │   │
│  └──────────┬───────────┘    └─────────────────────────────┘    │
│             │                                                    │
│             │ WebSocket (wss://) + REST (https://)               │
└─────────────┼────────────────────────────────────────────────────┘
              │
    ┌─────────┴─────────┐
    │     INTERNET       │
    └─────────┬─────────┘
              │
   ┌──────────┼──────────────────────┐
   │          │                      │
┌──▼──┐   ┌──▼──┐               ┌──▼──┐
│ P1  │   │ P2  │    ...        │ Pn  │
│Client│   │Client│              │Client│
│ Mod │   │ Mod │               │ Mod │
└─────┘   └─────┘               └─────┘
RimWorld + Harmony               RimWorld + Harmony
```

### Proč tato architektura?

- **Nemůžeš hostovat server lokálně** → vše běží v cloudu
- **WebSocket** místo raw UDP → funguje přes jakýkoliv NAT/firewall, cloud-friendly
- **REST API** pro nereal-time operace (trade, contracts, world state)
- **WebSocket** pro real-time (chat, joint sessions, notifikace)

---

## 2) Cloud infrastruktura – Konkrétní doporučení

### Varianta A: Nejlevnější (hobby / MVP)

| Služba | Provider | Cena | Poznámka |
|--------|----------|------|----------|
| **Server** | [Fly.io](https://fly.io) | Free tier: 3 shared VMs, 256MB RAM | .NET 8 v Dockeru |
| **DB** | [Neon](https://neon.tech) | Free tier: 0.5GB PostgreSQL | Serverless, auto-sleep |
| **Cache/PubSub** | [Upstash Redis](https://upstash.com) | Free tier: 10k req/day | Pro session state |
| **CI/CD** | GitHub Actions | Free pro public repo | Build + deploy |

**Odhadovaná cena: $0/měsíc** pro MVP s ~10-20 hráči.

### Varianta B: Production (větší komunita)

| Služba | Provider | Cena | Poznámka |
|--------|----------|------|----------|
| **Server** | [Railway](https://railway.app) nebo Fly.io | ~$5-15/měsíc | 1GB+ RAM, persistent |
| **DB** | [Supabase](https://supabase.com) | Free → $25/měsíc | PostgreSQL + auth + realtime |
| **Cache** | Upstash Redis | $0-10/měsíc | Pay-per-request |
| **Storage** | Cloudflare R2 | Free tier 10GB | Pro snapshoty, replaye |
| **Domain** | Cloudflare | ~$10/rok | rimverse.gg nebo podobné |

### Varianta C: Oracle Cloud Free Tier (alternativa)

- **4 ARM Ampere A1** instance (24GB RAM, 4 OCPU) – **navždy zdarma**
- Ideální pro dedicated server s PostgreSQL přímo na VM
- Nevýhoda: složitější setup, méně automatizovaný

### Doporučení: **Začni s Fly.io + Neon (Varianta A)**, přejdi na Railway + Supabase když naroste komunita.

---

## 3) Struktura projektu

```
RimVerse/
├── src/
│   ├── RimVerse.Client/              # C# Mod pro RimWorld (Harmony)
│   │   ├── RimVerse.Client.csproj
│   │   ├── About/
│   │   │   ├── About.xml
│   │   │   ├── Manifest.xml
│   │   │   └── Preview.png
│   │   ├── Defs/
│   │   │   └── ...                   # RimWorld XML definice
│   │   ├── Core/
│   │   │   ├── RimVerseMod.cs        # Hlavní mod entry point
│   │   │   ├── ModSettings.cs        # Nastavení (server URL, token...)
│   │   │   └── TickScheduler.cs      # World clock sync
│   │   ├── Network/
│   │   │   ├── ApiClient.cs          # REST volání na server
│   │   │   ├── WebSocketClient.cs    # Real-time spojení
│   │   │   ├── MessageSerializer.cs  # MessagePack serializer
│   │   │   └── ConnectionManager.cs  # Reconnect, heartbeat
│   │   ├── Patches/
│   │   │   ├── DeterminismPatches.cs # Harmony patche pro RNG
│   │   │   ├── WorldPatches.cs       # World map integrace
│   │   │   ├── TradePatches.cs       # Trade UI hooky
│   │   │   └── UIPatches.cs          # Custom UI elementy
│   │   ├── Session/
│   │   │   ├── JointSessionManager.cs
│   │   │   ├── LockstepEngine.cs     # Lockstep tick synchronizace
│   │   │   ├── InputRecorder.cs      # Pro replay
│   │   │   └── SyncPolicy.cs         # Které metody se synchronizují
│   │   ├── Trade/
│   │   │   ├── EscrowManager.cs
│   │   │   ├── ParcelSystem.cs
│   │   │   └── ContractUI.cs
│   │   ├── UI/
│   │   │   ├── MainWindow.cs         # Hlavní RimVerse okno
│   │   │   ├── ChatWindow.cs
│   │   │   ├── PlayerList.cs
│   │   │   ├── TradeWindow.cs
│   │   │   ├── ReportWindow.cs       # Bug report + ZIP export
│   │   │   └── ModpackManager.cs     # One-click modpack sync
│   │   └── Compat/
│   │       ├── CompatDatabase.cs     # Known good/bad mody
│   │       ├── ModHasher.cs          # Hash modpacku
│   │       └── AutoInstrument.cs     # Detekce rizikových patternů
│   │
│   ├── RimVerse.Server/              # .NET 8 Dedicated Server
│   │   ├── RimVerse.Server.csproj
│   │   ├── Program.cs               # Entry point + DI
│   │   ├── appsettings.json
│   │   ├── Dockerfile
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs     # Login, token refresh
│   │   │   ├── WorldController.cs    # World state, settlements
│   │   │   ├── TradeController.cs    # Contracts, escrow
│   │   │   ├── SessionController.cs  # Joint session management
│   │   │   └── ReportController.cs   # Bug report příjem
│   │   ├── Hubs/
│   │   │   ├── GameHub.cs            # SignalR/WebSocket hub
│   │   │   └── ChatHub.cs            # Chat real-time
│   │   ├── Services/
│   │   │   ├── WorldClockService.cs  # Globální hodiny
│   │   │   ├── ContractEngine.cs     # Zpracování kontraktů
│   │   │   ├── SessionOrchestrator.cs# Vytváření joint sessions
│   │   │   ├── ClaimService.cs       # Territory claims
│   │   │   ├── ModpackValidator.cs   # Hash validace
│   │   │   └── AuditLogger.cs        # Audit log
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs       # EF Core DbContext
│   │   │   ├── Migrations/
│   │   │   └── Entities/
│   │   │       ├── Player.cs
│   │   │       ├── Settlement.cs
│   │   │       ├── Contract.cs
│   │   │       ├── Parcel.cs
│   │   │       ├── JointSession.cs
│   │   │       └── AuditEntry.cs
│   │   └── Models/
│   │       ├── Messages.cs           # Síťové zprávy (MessagePack)
│   │       └── DTOs.cs               # API data transfer objects
│   │
│   └── RimVerse.Shared/              # Sdílené typy a protokol
│       ├── RimVerse.Shared.csproj
│       ├── Protocol/
│       │   ├── MessageTypes.cs       # Enum všech typů zpráv
│       │   ├── PacketDefinitions.cs  # Struktury paketů
│       │   └── Constants.cs          # Verze protokolu, limity
│       ├── Models/
│       │   ├── WorldState.cs
│       │   ├── ContractData.cs
│       │   ├── SettlementData.cs
│       │   └── PlayerInfo.cs
│       └── Crypto/
│           ├── TokenHelper.cs        # JWT helper
│           └── HashHelper.cs         # SHA-256 pro modpack hash
│
├── tests/
│   ├── RimVerse.Server.Tests/
│   └── RimVerse.Shared.Tests/
│
├── infra/
│   ├── fly.toml                      # Fly.io deployment config
│   ├── railway.json                  # Railway deployment config
│   ├── Dockerfile                    # Multi-stage build
│   └── docker-compose.yml            # Lokální dev environment
│
├── docs/
│   ├── PROTOCOL.md                   # Specifikace protokolu
│   ├── SETUP.md                      # Jak nastavit dev prostředí
│   └── CLOUD_DEPLOY.md              # Jak deployovat na cloud
│
├── RimVerse.sln
├── .github/
│   └── workflows/
│       ├── build.yml                 # CI build + testy
│       └── deploy.yml                # CD na Fly.io/Railway
└── README.md
```

---

## 4) Databázové schéma (PostgreSQL / EF Core)

```sql
-- ============================================================
-- PLAYERS
-- ============================================================
CREATE TABLE players (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    display_name    VARCHAR(64) NOT NULL,
    steam_id        VARCHAR(32) UNIQUE,          -- nullable (non-Steam)
    auth_token_hash VARCHAR(128) NOT NULL,        -- bcrypt hash tokenu
    role            VARCHAR(16) DEFAULT 'player', -- player | admin | moderator
    created_at      TIMESTAMPTZ DEFAULT NOW(),
    last_seen_at    TIMESTAMPTZ DEFAULT NOW(),
    is_banned       BOOLEAN DEFAULT FALSE,
    ban_reason      TEXT
);

-- ============================================================
-- WORLD CONFIG
-- ============================================================
CREATE TABLE worlds (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(128) NOT NULL,
    seed            VARCHAR(64) NOT NULL,
    world_clock     BIGINT DEFAULT 0,            -- globální world tick
    storyteller     VARCHAR(64) DEFAULT 'Cassandra',
    difficulty      VARCHAR(32) DEFAULT 'Rough',
    modpack_hash    VARCHAR(64) NOT NULL,         -- SHA-256 modpacku
    config_json     JSONB,                        -- extra nastavení
    created_at      TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================================
-- SETTLEMENTS (kolonie hráčů)
-- ============================================================
CREATE TABLE settlements (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    world_id        UUID REFERENCES worlds(id) ON DELETE CASCADE,
    owner_id        UUID REFERENCES players(id) ON DELETE CASCADE,
    tile_id         INT NOT NULL,                 -- RimWorld tile index
    name            VARCHAR(128),
    local_tick      BIGINT DEFAULT 0,             -- lokální čas kolonie
    wealth          FLOAT DEFAULT 0,              -- pro matchmaking/balancing
    snapshot_data   BYTEA,                        -- komprimovaný stav kolonie
    snapshot_at     TIMESTAMPTZ,
    claim_radius    INT DEFAULT 3,                -- min. odstup v tiles
    created_at      TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(world_id, tile_id)
);

-- ============================================================
-- CONTRACTS (obchody, návštěvy, raidy...)
-- ============================================================
CREATE TABLE contracts (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    world_id        UUID REFERENCES worlds(id) ON DELETE CASCADE,
    type            VARCHAR(32) NOT NULL,         -- trade | visit | raid | parcel | job
    status          VARCHAR(32) DEFAULT 'pending',-- pending | accepted | in_progress
                                                  -- | completed | cancelled | disputed
    initiator_id    UUID REFERENCES players(id),
    target_id       UUID REFERENCES players(id),
    
    -- Trade specifika
    offer_items     JSONB,          -- [{defName, quantity, quality}]
    request_items   JSONB,          -- [{defName, quantity, quality}]
    
    -- Časování
    scheduled_world_tick BIGINT,    -- kdy se má kontrakt aktivovat
    expires_world_tick   BIGINT,    -- kdy vyprší
    
    -- Escrow
    escrow_locked   BOOLEAN DEFAULT FALSE,
    
    created_at      TIMESTAMPTZ DEFAULT NOW(),
    resolved_at     TIMESTAMPTZ
);

-- ============================================================
-- PARCELS (zásilky mezi hráči)
-- ============================================================
CREATE TABLE parcels (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id     UUID REFERENCES contracts(id),
    world_id        UUID REFERENCES worlds(id) ON DELETE CASCADE,
    sender_id       UUID REFERENCES players(id),
    receiver_id     UUID REFERENCES players(id),
    items_json      JSONB NOT NULL,               -- [{defName, quantity, quality, stuff}]
    pawns_json      JSONB,                        -- volitelně přepravovaní pawni
    status          VARCHAR(32) DEFAULT 'in_transit', -- in_transit | delivered | lost
    send_world_tick BIGINT NOT NULL,
    eta_world_tick  BIGINT NOT NULL,              -- odhadovaný čas doručení
    delivered_at    TIMESTAMPTZ
);

-- ============================================================
-- JOINT SESSIONS (real-time lockstep setkání)
-- ============================================================
CREATE TABLE joint_sessions (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    world_id        UUID REFERENCES worlds(id) ON DELETE CASCADE,
    type            VARCHAR(32) NOT NULL,         -- visit | raid | defense | coop
    status          VARCHAR(32) DEFAULT 'pending',-- pending | active | completed | aborted
    host_id         UUID REFERENCES players(id),
    modpack_hash    VARCHAR(64) NOT NULL,
    rng_seed        BIGINT NOT NULL,
    max_tick        BIGINT DEFAULT 60000,         -- max délka session (ticky)
    current_tick    BIGINT DEFAULT 0,
    
    -- Participants jako JSONB pole
    participants    JSONB NOT NULL,               -- [{playerId, role, ready}]
    
    -- Replay
    replay_data_url VARCHAR(512),                 -- URL na uložený replay (R2/S3)
    
    -- Delta (výsledek session)
    delta_json      JSONB,                        -- změny po session
    
    started_at      TIMESTAMPTZ,
    ended_at        TIMESTAMPTZ,
    created_at      TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================================
-- CHAT MESSAGES
-- ============================================================
CREATE TABLE chat_messages (
    id              BIGSERIAL PRIMARY KEY,
    world_id        UUID REFERENCES worlds(id) ON DELETE CASCADE,
    sender_id       UUID REFERENCES players(id),
    channel         VARCHAR(32) DEFAULT 'global', -- global | trade | guild:{id}
    content         VARCHAR(500) NOT NULL,
    created_at      TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================================
-- AUDIT LOG
-- ============================================================
CREATE TABLE audit_log (
    id              BIGSERIAL PRIMARY KEY,
    world_id        UUID REFERENCES worlds(id),
    actor_id        UUID REFERENCES players(id),
    action          VARCHAR(64) NOT NULL,         -- trade.complete, session.start, player.ban...
    details_json    JSONB,
    created_at      TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================================
-- MODPACK REGISTRY
-- ============================================================
CREATE TABLE modpack_entries (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    world_id        UUID REFERENCES worlds(id) ON DELETE CASCADE,
    package_id      VARCHAR(256) NOT NULL,        -- mod packageId
    mod_name        VARCHAR(256),
    version         VARCHAR(32),
    compat_status   VARCHAR(16) DEFAULT 'unknown',-- green | yellow | red | unknown
    notes           TEXT,
    UNIQUE(world_id, package_id)
);

-- Indexy
CREATE INDEX idx_settlements_world ON settlements(world_id);
CREATE INDEX idx_settlements_owner ON settlements(owner_id);
CREATE INDEX idx_contracts_world_status ON contracts(world_id, status);
CREATE INDEX idx_parcels_receiver ON parcels(receiver_id, status);
CREATE INDEX idx_chat_world_created ON chat_messages(world_id, created_at);
CREATE INDEX idx_audit_world_created ON audit_log(world_id, created_at);
```

---

## 5) Síťový protokol

### 5.1 Transportní vrstva

| Operace | Transport | Formát | Důvod |
|---------|-----------|--------|-------|
| Auth, World state, Trade create | **HTTPS REST** | JSON | Standardní, cacheovatelné |
| Chat, Notifikace, World clock sync | **WebSocket** (SignalR) | MessagePack | Low-latency, push |
| Joint Session (lockstep) | **WebSocket** (dedicated) | MessagePack | Musí být real-time |

**Proč SignalR?**
- Vestavěný v ASP.NET, automatický fallback (WebSocket → SSE → Long Polling)
- Podporuje MessagePack (binární, menší než JSON)
- Skupiny (rooms) pro joint sessions
- Reconnect out-of-the-box

### 5.2 Message Types (MessagePack)

```csharp
// RimVerse.Shared/Protocol/MessageTypes.cs

public enum MessageType : ushort
{
    // === SYSTEM ===
    Heartbeat           = 0x0001,
    HandshakeRequest    = 0x0002,
    HandshakeResponse   = 0x0003,
    Disconnect          = 0x0004,
    Error               = 0x0005,
    
    // === WORLD ===
    WorldClockSync      = 0x0100,  // server → client: aktuální world tick
    SettlementUpdate    = 0x0101,  // server → client: změna osady na mapě
    PlayerJoined        = 0x0102,
    PlayerLeft          = 0x0103,
    
    // === CHAT ===
    ChatMessage         = 0x0200,
    ChatHistory         = 0x0201,
    
    // === TRADE / CONTRACTS ===
    ContractOffer       = 0x0300,
    ContractAccept      = 0x0301,
    ContractReject      = 0x0302,
    ContractComplete    = 0x0303,
    ContractCancel      = 0x0304,
    ParcelSent          = 0x0310,
    ParcelDelivered     = 0x0311,
    
    // === JOINT SESSION ===
    SessionRequest      = 0x0400,  // A → server: chci navštívit B
    SessionInvite       = 0x0401,  // server → B: A tě chce navštívit
    SessionAccept       = 0x0402,  // B → server: OK
    SessionReject       = 0x0403,
    SessionStart        = 0x0404,  // server → oba: session začíná
    SessionTick         = 0x0405,  // lockstep tick (inputy)
    SessionTickAck      = 0x0406,  // potvrzení ticku
    SessionHashCheck    = 0x0407,  // hash stavu pro desync detekci
    SessionDesync       = 0x0408,  // server detekoval desync
    SessionEnd          = 0x0409,  // konec session
    SessionDelta        = 0x040A,  // delta ke commitu do world state
    
    // === PERMISSIONS ===
    PermissionGrant     = 0x0500,
    PermissionRevoke    = 0x0501,
    BuilderBlueprintSubmit = 0x0510,
    BuilderBlueprintReview = 0x0511,
    
    // === MODPACK ===
    ModpackManifest     = 0x0600,
    ModpackHashMismatch = 0x0601,
    CompatStatusUpdate  = 0x0602,
    
    // === REPORT ===
    BugReportSubmit     = 0x0700
}
```

### 5.3 Klíčové zprávy – struktura

```csharp
// Handshake (client → server)
[MessagePackObject]
public class HandshakeRequest
{
    [Key(0)] public string ProtocolVersion { get; set; }  // "1.0.0"
    [Key(1)] public string GameVersion { get; set; }      // "1.6.xxxx"
    [Key(2)] public string AuthToken { get; set; }        // JWT
    [Key(3)] public string ModpackHash { get; set; }      // SHA-256
    [Key(4)] public string[] ModList { get; set; }        // packageId[]
}

// Lockstep Tick (v joint session)
[MessagePackObject]
public class SessionTickMessage
{
    [Key(0)] public Guid SessionId { get; set; }
    [Key(1)] public long TickNumber { get; set; }
    [Key(2)] public byte[] PlayerInputs { get; set; }     // serializované příkazy
    [Key(3)] public uint StateHash { get; set; }          // CRC32 stavu pro desync check
}

// Trade Escrow
[MessagePackObject]
public class ContractOfferMessage
{
    [Key(0)] public Guid ContractId { get; set; }
    [Key(1)] public Guid TargetPlayerId { get; set; }
    [Key(2)] public TradeItem[] OfferedItems { get; set; }
    [Key(3)] public TradeItem[] RequestedItems { get; set; }
    [Key(4)] public long ExpiresAtWorldTick { get; set; }
}

[MessagePackObject]
public class TradeItem
{
    [Key(0)] public string DefName { get; set; }
    [Key(1)] public int Quantity { get; set; }
    [Key(2)] public int Quality { get; set; }           // 0-6 (Awful-Legendary)
    [Key(3)] public string StuffDefName { get; set; }   // nullable
}
```

---

## 6) Autentizace a bezpečnost

### Flow:

```
1. Hráč spustí RimWorld s RimVerse modem
2. Mod zkontroluje, jestli existuje uložený JWT token
3. Pokud ne → zobrazí "Connect to Server" UI
4. Auth flow:
   a) Steam: mod zavolá Steamworks API → získá Auth Ticket
      → pošle na server → server ověří přes Steam Web API
      → server vydá JWT
   b) Non-Steam: hráč zadá username + password
      → server ověří → vydá JWT
5. JWT se uloží lokálně (šifrovaný DPAPI/Keychain)
6. Každý REST/WS request obsahuje JWT v headeru
```

### JWT Payload:
```json
{
  "sub": "player-uuid",
  "name": "PlayerName",
  "steam_id": "76561198xxxxx",
  "role": "player",
  "world_id": "world-uuid",
  "iat": 1709683200,
  "exp": 1709769600
}
```

---

## 7) Klíčové systémy – detailní návrh

### 7.1 World Clock (Sharded Time)

```
Server udržuje globální "World Clock" (world_tick).
Každá kolonie má vlastní local_tick.

World Clock se inkrementuje:
- Buď real-time (1 world tick = 1 sekunda reálného času)
- Nebo event-driven (world tick se posune při kontraktu/parcelu)

Hráč může hrát libovolnou rychlostí lokálně.
Kontrakty a parcely se řídí world_tick, ne local_tick.

Periodicky (každých 60s) server broadcastuje WorldClockSync:
  → klienti si aktualizují svůj world_tick ukazatel
  → nezasahuje do local_tick (hráč hraje dál svým tempem)
```

### 7.2 Escrow Trade (bez podvádění)

```
1. A vytvoří ConctractOffer (nabídka: 200 steel za 50 medicine)
2. Server uloží kontrakt (status: pending)
3. B dostane notifikaci → přijme/odmítne
4. Při přijetí:
   a) Server ověří, že A má nabízené itemy (snapshot check)
   b) Itemy se "zamknou" v escrow (odstraní se z inventáře obou)
   c) Status → in_progress
5. Server vytvoří 2 parcely:
   - steel A→B (ETA: world_tick + travel_time)
   - medicine B→A (ETA: world_tick + travel_time)
6. Když parcel "dorazí" (world_tick >= ETA):
   - Server notifikuje příjemce
   - Itemy se přidají do inventáře
   - Status → completed
   - Audit log záznam
```

### 7.3 Joint Session (Lockstep)

```
SETUP:
1. A klikne "Visit B" → SessionRequest na server
2. Server ověří:
   - oba online
   - modpack hash match
   - A nemá jiný aktivní session
   - B má povolené návštěvy (anti-grief setting)
3. Server → SessionInvite na B
4. B přijme → server vytvoří JointSession v DB
5. Server → SessionStart na oba
   - obsahuje: sessionId, rngSeed, sync policy, map data B

LOCKSTEP LOOP:
  Tick N:
  1. Oba klienti pošlou SessionTick(N, inputs, stateHash)
  2. Server počká na oba (timeout 5s)
  3. Server porovná stateHash:
     - match → pošle merged inputs oběma → oba simulují tick N
     - mismatch → SessionDesync → session se zastaví
  4. Každých 60 ticků: full hash check

UKONČENÍ:
  1. Kterýkoliv hráč nebo server ukončí session
  2. Host (B) pošle finální delta (změny mapy, inventářů)
  3. Server uloží delta a aplikuje na world state
  4. Oba se vrátí do normálního režimu
```

### 7.4 Modpack Manager

```
Server uchovává "modpack manifest":
{
  "modpack_hash": "sha256:abc123...",
  "required_mods": [
    {"packageId": "brrainz.harmony", "version": "2.3.3", "compat": "green"},
    {"packageId": "ludeon.rimworld", "version": "1.6.xxxx", "compat": "green"},
    ...
  ],
  "banned_mods": ["some.cheat.mod"],
  "optional_mods": [...],  // povolené, ale ne vyžadované
}

Client flow:
1. Při připojení klient pošle svůj modpack hash
2. Server porovná:
   - match → OK, pokračuj
   - mismatch → pošle manifest → klient UI zobrazí diff
3. Klient může jedním tlačítkem:
   - stáhnout chybějící mody (přes Steam Workshop API)
   - deaktivovat nepovolené mody
   - přeřadit load order
4. Po úpravě → reconnect s novým hashem
```

---

## 8) Harmony Patch Map (Determinismus)

### Kritické patche pro lockstep synchronizaci:

```csharp
// 1. Centrální RNG – nahrazení System.Random a Verse.Rand
[HarmonyPatch(typeof(Rand), "Range", typeof(int), typeof(int))]
public static class Patch_Rand_Range
{
    static bool Prefix(int min, int max, ref int __result)
    {
        if (JointSessionManager.IsInSession)
        {
            __result = JointSessionManager.SessionRNG.Next(min, max);
            return false; // přeskoč originál
        }
        return true; // mimo session normálně
    }
}

// 2. Zákaz DateTime.Now v session
[HarmonyPatch(typeof(DateTime), "get_Now")]
public static class Patch_DateTime_Now
{
    static bool Prefix(ref DateTime __result)
    {
        if (JointSessionManager.IsInSession)
        {
            // Vrať deterministický čas založený na ticku
            __result = JointSessionManager.DeterministicTime;
            return false;
        }
        return true;
    }
}

// 3. Patch HashSet iterace (nedeterministické pořadí)
[HarmonyPatch(typeof(HashSet<Thing>), "GetEnumerator")]
public static class Patch_HashSet_Enumerator
{
    // V session: konvertuj na sorted list před iterací
    // (implementace závisí na kontextu)
}

// 4. State hash pro desync detekci
[HarmonyPatch(typeof(TickManager), "DoSingleTick")]
public static class Patch_TickManager
{
    static void Postfix()
    {
        if (JointSessionManager.IsInSession)
        {
            // Spočítej hash aktuálního stavu
            uint hash = StateHasher.ComputeHash(
                Find.CurrentMap.mapPawns,
                Find.CurrentMap.thingGrid,
                Find.CurrentMap.resourceCounter
            );
            JointSessionManager.ReportTickHash(hash);
        }
    }
}

// 5. Synchronizace hráčských příkazů (designátory, drafty...)
[HarmonyPatch(typeof(Designator), "DesignateSingleCell")]
public static class Patch_Designator
{
    static bool Prefix(Designator __instance, IntVec3 c)
    {
        if (JointSessionManager.IsInSession)
        {
            // Místo přímé akce → pošli jako input command
            JointSessionManager.EnqueueInput(new DesignateCommand
            {
                DesignatorType = __instance.GetType().FullName,
                Cell = c,
                PlayerId = RimVerseMod.LocalPlayerId
            });
            return false;
        }
        return true;
    }
}
```

---

## 9) Client Mod – UI Návrh

### Hlavní RimVerse tlačítko (v horním menu baru RimWorld):

```
┌─────────────────────────────────────────────┐
│  🌐 RimVerse                                │
├─────────────────────────────────────────────┤
│  Status: Connected (rimverse.fly.dev)       │
│  World: "Alpha Sector" | Players: 12/50     │
│  World Clock: Day 45, 14:00                 │
│                                             │
│  [📧 Messages (3)]  [📦 Parcels (1)]       │
│  [🤝 Trade]  [👥 Players]  [💬 Chat]       │
│                                             │
│  ─── Active Contracts ───                   │
│  • Trade with @Player2 (pending accept)     │
│  • Parcel from @Player5 (ETA: 2 days)       │
│                                             │
│  [⚙️ Settings]  [🐛 Report Issue]           │
└─────────────────────────────────────────────┘
```

### Report Issue dialog:
```
┌─────────────────────────────────────────────┐
│  🐛 Report MP Issue                         │
├─────────────────────────────────────────────┤
│  Category: [Desync ▼]                       │
│  Description: [________________]            │
│                                             │
│  Auto-collected data:                       │
│  ✓ Game version: 1.6.4104                   │
│  ✓ Mod list: 45 mods (hash: a3b2c1...)     │
│  ✓ Player log (last 500 lines)             │
│  ✓ Session replay ID: sess_abc123           │
│  ✓ Desync trace (if applicable)            │
│                                             │
│  [📋 Copy to Clipboard]  [📤 Submit]       │
│  [📁 Export ZIP]                            │
└─────────────────────────────────────────────┘
```

---

## 10) Deployment – krok za krokem

### 10.1 Fly.io deployment (doporučeno pro MVP)

```dockerfile
# Dockerfile (multi-stage build)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/RimVerse.Shared/ RimVerse.Shared/
COPY src/RimVerse.Server/ RimVerse.Server/
RUN dotnet publish RimVerse.Server/RimVerse.Server.csproj \
    -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "RimVerse.Server.dll"]
```

```toml
# fly.toml
app = "rimverse-server"
primary_region = "fra"  # Frankfurt (blízko CZ)

[build]
  dockerfile = "Dockerfile"

[http_service]
  internal_port = 8080
  force_https = true

  [[http_service.checks]]
    path = "/health"
    interval = "30s"
    timeout = "5s"

[env]
  ASPNETCORE_ENVIRONMENT = "Production"
  DATABASE_URL = "postgresql://..." # z Neon
  REDIS_URL = "redis://..."        # z Upstash
```

```bash
# Deploy příkazy:
fly launch                    # první setup
fly deploy                    # deploy nové verze
fly secrets set DATABASE_URL="postgresql://..."
fly secrets set JWT_SECRET="super-secret-key"
fly logs                      # monitoring
```

### 10.2 GitHub Actions CI/CD

```yaml
# .github/workflows/deploy.yml
name: Deploy RimVerse Server
on:
  push:
    branches: [main]
    paths: ['src/RimVerse.Server/**', 'src/RimVerse.Shared/**']

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0'
      - run: dotnet test tests/

  deploy:
    needs: test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: superfly/flyctl-actions/setup-flyctl@master
      - run: flyctl deploy --remote-only
        env:
          FLY_API_TOKEN: ${{ secrets.FLY_API_TOKEN }}
```

---

## 11) Konfigurace serveru (appsettings.json)

```json
{
  "RimVerse": {
    "World": {
      "Name": "Alpha Sector",
      "MaxPlayers": 50,
      "MinSettlementDistance": 3,
      "WorldClockMode": "RealTime",
      "WorldClockTicksPerSecond": 1
    },
    "Session": {
      "MaxDurationTicks": 120000,
      "DesyncCheckInterval": 60,
      "TimeoutSeconds": 10,
      "MaxConcurrentSessions": 10
    },
    "Trade": {
      "EscrowEnabled": true,
      "ParcelBaseDeliveryTicks": 3600,
      "ParcelSpeedPerTile": 100
    },
    "Security": {
      "MaxLoginAttempts": 5,
      "TokenExpirationHours": 24,
      "RateLimitPerMinute": 60
    },
    "Modpack": {
      "EnforceHash": true,
      "AllowUnknownMods": false,
      "BannedMods": []
    }
  },
  "ConnectionStrings": {
    "Database": "Host=...;Database=rimverse;Username=...;Password=...",
    "Redis": "redis://..."
  },
  "Jwt": {
    "Secret": "OVERRIDE_VIA_ENV_VARIABLE",
    "Issuer": "rimverse-server",
    "Audience": "rimverse-client"
  }
}
```

---

## 12) Odhadovaný technický stack

| Vrstva | Technologie | Verze |
|--------|-------------|-------|
| **Client Mod** | C# (.NET Framework 4.7.2) | RimWorld target |
| **Harmony Patches** | Harmony | 2.3+ |
| **Client Networking** | WebSocketSharp nebo NativeWebSocket | - |
| **Client Serialization** | MessagePack-CSharp | 2.5+ |
| **Server** | ASP.NET Core | .NET 8 |
| **Server Real-time** | SignalR | (součást ASP.NET) |
| **Server ORM** | Entity Framework Core | 8.0 |
| **Database** | PostgreSQL (Neon) | 16 |
| **Cache** | Redis (Upstash) | 7 |
| **Auth** | JWT (System.IdentityModel.Tokens.Jwt) | - |
| **CI/CD** | GitHub Actions | - |
| **Hosting** | Fly.io | - |
| **Object Storage** | Cloudflare R2 | Pro replaye |

---

## 13) Prioritizovaný implementační plán

### Milestone A: "Hello Multiplayer World" (4-6 týdnů)

1. **Server skeleton** – ASP.NET + EF Core + Neon DB + Fly.io deploy
2. **Auth flow** – JWT + jednoduchý login (token-based, Steam později)
3. **Client mod skeleton** – Harmony, WebSocket connect, settings UI
4. **World state** – sdílená world mapa, zakládání osad, claims
5. **Chat** – SignalR hub, globální chat
6. **Modpack hash check** – manifest + validace při joinu

### Milestone B: "Living Economy" (6-10 týdnů)

7. **Escrow trade** – contract engine, UI, notifikace
8. **Parcel system** – zásilky s ETA, doručení
9. **World clock** – synchronizace, kontrakt timing
10. **Bug report** – automatický sběr dat, ZIP export
11. **Player list** – online hráči, profily, role

### Milestone C: "Face to Face" (10-16 týdnů)

12. **Joint Session v1** – lockstep engine, session orchestrator
13. **Determinism Guard** – RNG patche, hash check, desync detekce
14. **Visit flow** – návštěva mapy druhého hráče
15. **Co-op defense** – společný boj proti AI raidům
16. **Replay system** – ukládání inputů, replay pro debug

### Milestone D: "Community" (16+ týdnů)

17. **Builder permissions** – blueprinty, schvalování
18. **Global market** – auction house
19. **Guilds** – sdílené sklady, práva
20. **Compat database** – komunitní hodnocení modů
21. **Steam auth** – plná Steam integrace

---

## 14) Rizika a mitigace

| Riziko | Dopad | Mitigace |
|--------|-------|----------|
| **Desyncy v joint session** | Vysoký | Hash check každých 60 ticků, replay pro debug, omezený scope session |
| **Mod nekompatibilita** | Vysoký | Compat database, hash enforcement, whitelist approach |
| **Cloud latence** | Střední | Frankfurt region (Fly.io), MessagePack (menší pakety), optimistický klient |
| **Free tier limity** | Střední | Monitoring, auto-scaling, přechod na placený tier při růstu |
| **Cheating/exploity** | Střední | Server je autorita pro ekonomiku, escrow, audit log |
| **RimWorld update rozbije mod** | Vysoký | Pinning verze, rychlý CI/CD, komunitní testing |

---

> **Další krok:** Chceš, abych začal implementovat server skeleton (ASP.NET + DB + Fly.io config) nebo client mod skeleton (Harmony + WebSocket)?
