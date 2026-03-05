# RimVerse (working title) – „Persistent Sharded Multiplayer“ pro RimWorld (1.6+)

> Cíl: být **o několik “update cyklů” napřed** tím, že spojíš to nejlepší z existujících MP směrů (lockstep co‑op vs. persistent server svět) do **hybridní architektury**, která řeší hlavní bolest: **čas, mod kompatibilitu, desyncy a griefing**.

---

## 0) Proč nový MP mod, když už existují jiné?
Aktuální MP ekosystém se typicky dělí na dvě filozofie:

- **Lockstep / co‑op jedna simulace** (super na společnou kolonii)  
  Bolest: sync času, desyncy, mod kompatibilita, často „host‑dependent“.

- **Persistent server / sdílený svět** (super na více kolonií na jedné planetě)  
  Bolest: “real‑time” návštěvy, férová interakce mezi hráči s různým tempem, anti‑grief, ekonomika.

**RimVerse** jde po třetí cestě:  
➡️ **Persistent svět + per‑colony čas** … a když dojde na přímou interakci (návštěva/raid/obchod tváří v tvář), přepne se to do **instancované “joint session” (lockstep)** jen pro zúčastněné hráče.

Tohle je core diferenciátor: **svět je trvale online**, ale **mapy se synchronizují jen tehdy, když je to potřeba**.

---

## 1) USP (Unique Selling Points) – proč budeš “napřed”
### USP #1: Sharded Time Model (časové “shardy”)
Každá kolonie běží ve vlastním lokálním čase (tick line), ale svět má i **globální “World Clock”**.
Interakce mezi hráči se nedějí “na náhodných tickách”, ale přes **kontrakty** na světové časové ose:

- obchod = escrow kontrakt (platba → doručení → potvrzení)
- návštěva = handshake “synchronizační okno” + instancovaná session
- raid = plánované “raid window” + lockstep session
- pošta/parcel = serverové queue + doručení v definovaném world čase

➡️ Výsledek: hráči nemusí mít permanentně stejnou rychlost hry, a přesto to zůstane férové.

### USP #2: Instanced Joint Sessions (on-demand lockstep)
Kdykoliv je potřeba reálná interakce na mapě:
- hráč A navštíví hráče B
- raid, obrana, společná výprava
- “stavba u druhého” (pozdější fáze)

… server vytvoří **Joint Session** s pravidly:
- pevná modpack/verze (pinning)
- seed, RNG a sync policy
- lockstep tick + determinism guard
- auto-replay pro debug (volitelné)

Po skončení session se uloží **autoritatívní delta** (změny mapy, inventářů, pawnů) a server ji aplikuje na persistent svět.

➡️ Výsledek: real‑time “společné chvíle” bez toho, aby celý svět musel být pořád lockstep.

### USP #3: Determinism & Mod Compatibility Suite (největší “killer feature”)
Tady se vyhraje boj. Ne jen “aktualizovat mod na 1.6”, ale **dělat ho deterministický**.

Součást modpacku bude **RimVerse Compat Layer**:
- **Determinism Guard**: centrální RNG wrapper, seed policy, zákaz “unsynced” Random v kritických sekcích
- **Auto‑instrumentace** (heuristiky): detekce rizikových patternů (statické cache, DateTime.Now, unordered iterace)
- **Compatibility Database**: komunitní “known good / known bad / needs patch” pro mod verze
- **Headless Test Harness**: automatický replay test (2x simulace se stejným seedem) → diff detekce

Cíl: snížit “pain” z modů na minimum a udělat z toho **produkt**, ne hobby patchování.

### USP #4: Anti‑grief & Fair Play bez hardcore omezení
Server bude umět:
- “claim” zóny (omezení zakládání kolonií přímo vedle)
- raid pouze v definovaných oknech / opt‑in
- escrow trade (žádné scamování)
- role/permission systém (ally, guild, visitor, builder)
- rollback bod (jen pro adminy serveru, s audit logem)

### USP #5: In‑game Report & Patch Request (tvůj “form” nápad, ale chytře)
Místo jen formuláře:
- tlačítko **Report MP Issue**
- automaticky sbírá: game version, DLC list, modlist (packageId+verze), config hash, log, desync trace, session replay ID
- vytvoří **ZIP balíček** + dá do schránky “issue template” text
- otevře předvyplněný GitHub/Discord report (uživatel jen vloží)

➡️ Minimum friction, maximum dat pro tebe.

---

## 2) MVP (Phase 1) – to, co vydáš rychle a už je “wow”
### 2.1 Persistent Server + Sdílený svět (bez real-time map sync)
- dedicated server (Windows/Linux)
- account identity: SteamID (pokud je) + fallback token
- hráč si založí kolonii na world mapě (server kontroluje rozestupy)
- chat, mail, bulletin board
- **parcel / trade escrow**
- návštěva jako “asynchronní” (caravan přijede → otevře se trade UI přes server kontrakt)

**Proč to je MVP:**  
Už to dává komunitě „RimWorld online svět“, ale bez nejdražšího kroku (real‑time map sync).

### 2.2 Modpack Manager (host → klienti)
- host/server publikuje modpack manifest (IDs + verze + config hash)
- klient jedním klikem: stáhne, ověří, porovná hash, zablokuje join při mismatch
- “safe mode”: server může vyžadovat kompatibilitu status (green/yellow/red)

---

## 3) Phase 2 – to, co tě posune “o několik update napřed”
### 3.1 Real-time návštěvy přes Joint Session (lockstep jen pro zúčastněné)
Flow:
1) A pošle request “Visit B” (server ověří pravidla, časové okno)
2) B přijme → server vytvoří Joint Session
3) oba klienti přepnou do session (lockstep)
4) po ukončení se uloží delta a server ji aplikuje

Minimal scope:
- návštěvy + obchod “face to face”
- společná obrana proti AI raidům
- žádná editace B mapy Ačkem (jen interakce, trade, combat)

### 3.2 “Builder Permissions” (příprava na stavbu na mapě druhého)
- B může A povolit “Builder” roli
- A může klást jen blueprinty / plány (ne hotové konstrukce)  
  (B je pak “schvaluje” a kolonisté je realizují)
- konflikty se řeší jako “PR request” (přehled změn + accept/reject)

Tohle je extrémně atraktivní: co‑op pomoc bez toho, aby někdo griefoval.

---

## 4) Phase 3 – server ekonomika a endgame “online” obsah
- **Global Market / Auction House** (s escrow a taxem)
- **Kontrakty**: “vyrob 500 steel”, “doruč 20 medicine”, “ochraň karavanu”
- **Společné world projekty**: silnice, orbital relay, “public trade hub”
- “Guilds” a sdílené sklady (se správou práv)

---

## 5) Technická architektura (konkrétně)
### 5.1 Komponenty
1) **Client Mod (C# / Harmony)**  
   - UI, integrace do RimWorld
   - determinism hooks
   - network layer (UDP reliable / LiteNetLib nebo vlastní messagepack protokol)

2) **Dedicated Server (.NET 8+)**  
   - autorita pro world state, kontrakty, identity, audit log
   - session orchestrator (vytváření joint sessions)

3) **Database**  
   - SQLite pro jednoduché servery  
   - volitelně PostgreSQL pro velké servery

### 5.2 Data model (minimum)
- Players (id, auth, perms)
- World (seed, storyteller config, “world clock”)
- Settlements (tile, owner, claims, snapshots)
- Contracts (type, parties, status, escrow)
- Parcels (items/pawns, origin, destination, etaWorldTime)
- JointSessions (participants, modpack hash, replay id, state)

### 5.3 Determinismus – jak na to prakticky
- central RNG API (jediný povolený zdroj RNG v session)
- patch rizikových callů (Random, unordered collections, time-based calls)
- per-session “sync policy registry” (metody označené jako synced / unsynced)
- replay: ukládání inputů + tick hash

### 5.4 Performance
- svět běží event-driven (kontrakty, fronty), ne tick‑driven
- tick jen v joint session (krátké a izolované)

---

## 6) Security & Privacy (ať ti to komunita neshodí)
- default: žádné sbírání dat mimo logy, které user explicitně přiloží
- report tlačítko ukáže přesně, co balíček obsahuje
- server admin má audit log (kdo co poslal/obdržel)

---

## 7) “Konkurenční” roadmapa (rychlé milníky)
### Milestone A (4–8 týdnů)
- persistent server + join + kolonizace + chat
- escrow trade + parcel delivery
- modpack manifest + hash check
- report tlačítko + zip export

### Milestone B (8–16 týdnů)
- joint session v1: visit + trade + combat
- replay + desync diff
- builder permissions (blueprints)

### Milestone C (16+ týdnů)
- market/auction
- guilds + shared infra
- advanced compat layer + komunitní databáze kompatibility

---

## 8) Proč to bude pro hráče “to pravé”
- nemusí sedět u stejného času a stejné rychlosti hry
- mohou mít **vlastní** kolonie, ale svět je živý a propojený
- když chtějí “spolu”, dostanou **real-time** session bez rozbití celého světa
- modpack je “one‑click” a kompatibilita má jasná pravidla
- griefing je řešený systémově, ne “dobrou vůlí”

---

## 9) Bonus: názvy a branding (rychlá volba)
- **RimVerse** (nejuniverzálnější)
- **RimLink** (síťový vibe)
- **RimShard** (časové shardy)
- **WorldNet: Rim** (server-first)

---

## 10) Co bych od tebe chtěl jako dev setup (prakticky)
- RimWorld 1.6 mod dev prostředí (C# + decompilace assemblies)
- Harmony + (volitelně) Prepatcher
- lokální dedicated server (.NET 8) + test klienti (2–3 instance)
- CI: automatické buildy + headless determinism test

---

### Poznámka k “hned vedle” osídlení
Vanilla běžně brání zakládání settlementů přímo vedle sebe – RimVerse to řeší systémem **claims** a minimálního odstupu (konfigurovatelné na serveru).

---

Pokud chceš, navážu dalším dokumentem: **detailní specifikace protokolu** (message types), **DB schema**, a konkrétní “Harmony patch map” pro determinismus guard + RNG policy.
