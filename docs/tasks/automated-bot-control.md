# Automated Agent Control - End-to-End Test

## Ziel
Vollautomatisiertes Starten eines OpenRA Games und Steuerung eines Players über die HTTP API.

**Phase 1 (✅ Abgeschlossen)**: Bot-basierte Agent Control
**Phase 2 (✅ Abgeschlossen)**: Player-basierte Agent Control

## Status
**Phase 1 Abgeschlossen** ✅ - Bot Control funktioniert komplett
**Phase 2 Abgeschlossen** ✅ - Lokaler Player Control funktioniert!

## Implementierte Features

### 1. HTTP API Endpoints ✅
Alle Endpoints sind implementiert und funktionieren:

- `GET /api/ping` - Server Status prüfen
- `POST /api/game/start` - Game automatisch starten
- `GET /api/game/status` - Game Status abrufen
- `POST /api/game/stop` - Game stoppen
- `POST /api/game/kill` - Game beenden (kill)
- `GET /api/state` - Game State mit Units/Buildings/Enemies
- `POST /api/orders` - Orders an Units senden (move, attack, stop, build)

### 2. Implementierte Dateien

#### OpenRA.Mods.CA/Traits/GlobalAgentServer.cs
- Singleton HTTP Server, startet mit dem Engine
- Läuft auch im Shell Map State (vor Game Start)
- Port 8080 (konfigurierbar)
- Registriert/Deregistriert AgentInterface wenn Agent Bot aktiv wird

#### OpenRA.Mods.CA/Traits/AgentHttpServer.cs
- HTTP Request Handling
- CORS Support
- Endpoint Routing
- JSON Serialization
- Order Parsing (Move, Attack, Stop, Build)

#### OpenRA.Mods.CA/Traits/AgentInterface.cs
- Bot Interface für "agent" Bot Type
- Sammelt Game State (Units, Buildings, Enemies)
- Queue für externe Orders
- Registriert sich bei GlobalAgentServer

#### OpenRA.Mods.CA/Traits/GameLauncher.cs
- IGameLauncher Interface
- OpenRAGameLauncher Implementierung
- Validierung von Map, Bots, Game Speed
- Erstellt Setup Orders (slot_bot, slot_faction, slot_team, startgame)
- Game Start via Game.CreateLocalServer / Game.JoinServer

#### OpenRA.Mods.CA/Traits/GameStartConfig.cs
- Data Models für Game Start Request
- GameStartConfig (MapUid, GameSpeed, Bots Dictionary)
- BotConfig (Slot, BotType, Faction, Team)
- GameStartResult, GameStatusInfo
- Validation Logic

## Aktueller Workflow Test

### Schritt 1: Game mit Agent Bot starten ✅
```bash
curl -X POST http://localhost:8080/api/game/start \
  -H "Content-Type: application/json" \
  -d '{
    "mapUid": "all-connected",
    "gameSpeed": "fastest",
    "bots": {
      "bot1": {
        "slot": "Multi0",
        "botType": "agent"
      }
    }
  }'
```

**Status**: Erfolgreich getestet, Game startet

### Schritt 2: Game State abrufen (LÄUFT)
```bash
curl http://localhost:8080/api/state | python3 -m json.tool
```

**Erwartete Response**:
```json
{
  "tick": 1234,
  "cash": 5000,
  "resources": 0,
  "powerProvided": 100,
  "powerDrained": 50,
  "units": [
    {
      "id": 123,
      "type": "e1",
      "position": { "x": 1024, "y": 2048 },
      "cell": { "x": 10, "y": 20 },
      "health": { "current": 100, "max": 100 },
      "isIdle": true,
      "canMove": true,
      "canAttack": true
    }
  ],
  "buildings": [...],
  "enemies": [...]
}
```

### Schritt 3: Unit bewegen (TODO)
```bash
curl -X POST http://localhost:8080/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "command": "move",
    "units": [123],
    "targetCell": { "x": 15, "y": 25 },
    "queued": false
  }'
```

### Schritt 4: State prüfen (TODO)
Erneut `/api/state` aufrufen und prüfen ob Unit sich bewegt hat (neue Position/Cell).

## Probleme & Lösungen

### Problem 1: Game.Exit() funktionierte nicht
**Lösung**: `Environment.Exit(0)` verwenden statt `Game.Exit()`

### Problem 2: OrderManager Referenz fehlte
**Lösung**: Variable vor Closure deklarieren, in lobbyReady Callback verwenden

### Problem 3: Bot Type "agent" nicht erkannt
**Lösung**: AgentInterfaceInfo implementiert IBotInfo Interface

### Problem 4: Actor IDs wurden nicht gefunden (world.Actors leer)
**Problem**: `GetActorsByIds()` fand keine Actors obwohl `/api/state` sie sah
**Ursache**: AgentHttpServer behielt alte World-Referenz vom Shell Map State
**Lösung**: `world` Feld auf nicht-readonly gesetzt, `SetAgentInterface()` aktualisiert jetzt die World-Referenz vom Agent: `world = agent.Player.World`

## Nächste Schritte

1. ✅ Game Start via API testen
2. ✅ Game State abrufen und Units finden
3. ✅ Move Order an Unit senden
4. ✅ Prüfen ob Unit sich bewegt
5. ✅ Attack Order testen
6. ✅ Build Order testen

**STATUS: ERFOLGREICH ABGESCHLOSSEN** 🎉

## Test-Ergebnisse (2025-10-13)

### ✅ Erfolgreiche Tests

1. **Game Start via API**: Game startet automatisch mit Agent Bot
2. **Game State API**: Liefert Units, Buildings, Enemies korrekt zurück
3. **Build Order**: `{"command":"build","buildingType":"chnmcv"}` → MCV erfolgreich gebaut
4. **Move Order**: MCV bewegt von (86, 14) nach (90, 14) - **FUNKTIONIERT!**
5. **Attack Order**: API akzeptiert Attack-Move Orders

### Wichtiger Fix

**World-Referenz Problem gelöst**: `AgentHttpServer` bekommt jetzt die korrekte World-Referenz vom AgentInterface wenn ein Bot aktiviert wird. Dadurch findet `GetActorsByIds()` die Actors korrekt.

### Workflow

```bash
# 1. Game starten
./start-and-wait-for-game.sh

# 2. Match mit Agent Bot erstellen
curl -X POST http://localhost:8080/api/game/start \
  -H "Content-Type: application/json" \
  -d '{"mapUid":"all-connected","gameSpeed":"fastest","bots":{"bot1":{"slot":"Multi0","botType":"agent"}}}'

# 3. Warten bis Units spawnen (ca. 15 Sekunden)
sleep 15

# 4. Unit bauen
curl -X POST http://localhost:8080/api/orders \
  -H "Content-Type: application/json" \
  -d '{"command":"build","buildingType":"chnmcv"}'

# 5. Warten auf Build (ca. 25 Sekunden)
sleep 25

# 6. Unit bewegen
curl -X POST http://localhost:8080/api/orders \
  -H "Content-Type: application/json" \
  -d '{"command":"move","units":[76],"targetCell":{"x":90,"y":14},"queued":false}'

# 7. Movement verifizieren
sleep 5
curl -s http://localhost:8080/api/state | python3 -m json.tool
```

## Code Beispiele

### Game starten und Unit steuern (Python)
```python
import requests
import time

# 1. Game starten
response = requests.post('http://localhost:8080/api/game/start', json={
    'mapUid': 'all-connected',
    'gameSpeed': 'fastest',
    'bots': {
        'bot1': {
            'slot': 'Multi0',
            'botType': 'agent'
        }
    }
})
print(f"Game Start: {response.json()}")

# 2. Warten bis Game läuft
time.sleep(10)

# 3. State abrufen
state = requests.get('http://localhost:8080/api/state').json()
print(f"Tick: {state['tick']}, Units: {len(state['units'])}")

# 4. Erste Unit bewegen
if state['units']:
    unit = state['units'][0]
    response = requests.post('http://localhost:8080/api/orders', json={
        'command': 'move',
        'units': [unit['id']],
        'targetCell': {'x': unit['cell']['x'] + 5, 'y': unit['cell']['y']},
        'queued': False
    })
    print(f"Move Order: {response.json()}")

    # 5. Prüfen ob Unit sich bewegt
    time.sleep(2)
    new_state = requests.get('http://localhost:8080/api/state').json()
    new_unit = next(u for u in new_state['units'] if u['id'] == unit['id'])
    print(f"Old Cell: {unit['cell']}, New Cell: {new_unit['cell']}")
```

## Wichtige Hinweise

- Der GlobalAgentServer startet automatisch mit dem Engine
- Ein Agent Bot muss im Game aktiv sein damit `/api/state` und `/api/orders` funktionieren
- Die API läuft auf Port 8080 (localhost only)
- Game Speed "fastest" empfohlen für schnelle Tests
- Multiple Units können gleichzeitig Orders bekommen (gleiche ID Liste)

## Dateien zum Kompilieren

```bash
make
```

Alle Trait-Dateien sind in OpenRA.Mods.CA.dll enthalten.

## Testing Commands

```bash
# Server prüfen
curl http://localhost:8080/api/ping

# Game starten
curl -X POST http://localhost:8080/api/game/start -H "Content-Type: application/json" -d '{"mapUid":"all-connected","gameSpeed":"fastest","bots":{"bot1":{"slot":"Multi0","botType":"agent"}}}'

# Status prüfen
curl http://localhost:8080/api/game/status

# State abrufen
curl http://localhost:8080/api/state | python3 -m json.tool

# Game beenden
curl -X POST http://localhost:8080/api/game/kill
```

## Bekannte Issues

- Keine aktuellen Issues
- Kill Endpoint funktioniert einwandfrei mit Environment.Exit(0)

---

## Phase 2: Player Control (In Arbeit)

### Ziel
Agent soll nicht als Bot laufen, sondern als **regulärer menschlicher Player** der über die API gesteuert wird.

### Vorteile
- Volle Player-Features (normale Sichtweite, keine Bot-Beschränkungen)
- Kann mit/gegen andere menschliche Player oder Bots spielen
- Realistisches Testing für KI-Entwicklung
- Kein "Bot" Stigma in der Lobby

### Geplante Änderungen

#### 1. AgentInterface von IBot trennen
```csharp
// Alt: AgentInterfaceInfo implementiert IBotInfo
// Neu: Nur noch normales TraitInfo
[TraitLocation(SystemActors.Player)]
public class AgentInterfaceInfo : TraitInfo  // OHNE IBotInfo
{
    [Desc("Enable agent control for this player")]
    public readonly bool Enabled = false;
    // ...
}

public class AgentInterface : ITick, INotifyCreated  // OHNE IBot
{
    // Code bleibt größtenteils gleich
}
```

#### 2. Aktivierung via Player Rules
```yaml
# In rules/world.yaml oder Map-spezifisch
Player:
    AgentInterface:
        Enabled: true
```

#### 3. GameLauncher Anpassungen
- Kein `slot_bot` Order mehr nötig
- Lokaler Player Slot wird genutzt
- Agent aktiviert sich automatisch für lokalen Player

#### 4. Alternative: Launch Parameter
```bash
./launch-game.sh Launch.Map=all-connected Launch.AgentControl=true
```

### Implementation Tasks

- [x] AgentInterface: IBotInfo Interface entfernen
- [x] AgentInterface: IBot Interface entfernen
- [x] AgentInterface: Automatische Aktivierung für lokalen Player
- [x] Rules: AgentInterface Trait zu Player Rules hinzufügen
- [x] Testing: Verify lokaler Player wird korrekt gesteuert

**STATUS: ✅ ERFOLGREICH ABGESCHLOSSEN**

### Test-Ergebnisse Phase 2 (2025-10-13)

✅ **ERFOLG! Lokaler Player Control funktioniert!**

**Test Setup:**
```bash
./launch-game.sh Launch.Map=50ff8481b93ec70c20ebb08cc298ffeb49d1cb2a Graphics.Mode=Windowed
```

**Ergebnis:**
- Lokaler Player "Commander" wurde automatisch erstellt
- AgentInterface aktivierte sich automatisch: `IsBot=False`
- API funktioniert komplett:
  - `/api/state` liefert Player Units zurück
  - `/api/orders` mit `move` command funktioniert
  - MCV bewegte sich von (42, 84) → (49, 84)

**Log Evidence:**
```
[Player Constructor] Creating player for slot Multi0: client.Index=0, client.Name=Commander, client.Bot=null
[Player] Player Commander: IsBot=False, IsHost=True, BotType=
[AgentInterface] Agent Interface activated for player Commander
[ParseMoveOrder] Called with Units: 1, TargetCell: 50,84
[GetActorsByIds] actor=found, owner=Commander, matches=YES
```

**Verwendetes Test-Script:** `test-player-control.sh`

### Aktueller Workflow (Lokaler Player)

```bash
# Einfachster Weg: Direkt Map starten
./launch-game.sh Launch.Map=50ff8481b93ec70c20ebb08cc298ffeb49d1cb2a Graphics.Mode=Windowed &

# Warten auf API
sleep 10

# API verwenden wie gewohnt
curl -s http://localhost:8080/api/state
curl -X POST http://localhost:8080/api/orders \
  -d '{"command":"move","units":[76],"targetCell":{"x":50,"y":84}}'
```

### Erwartetes Ergebnis (Alt - Für Referenz)

```bash
# Game mit lokalem Player starten
./start-and-wait-for-game.sh
curl -X POST http://localhost:8080/api/game/start \
  -H "Content-Type: application/json" \
  -d '{
    "mapUid": "all-connected",
    "gameSpeed": "fastest",
    "localPlayer": true,  # NEUER Parameter
    "bots": {
      "enemy1": {
        "slot": "Multi1",
        "botType": "normal"  # Normaler Gegner-Bot
      }
    }
  }'

# API funktioniert wie vorher
curl -s http://localhost:8080/api/state
curl -X POST http://localhost:8080/api/orders -d '{"command":"move",...}'
```
